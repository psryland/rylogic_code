//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
//#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/shaders/shader_manager.h"
//#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/ray_cast.h"
//#include "pr/renderer11/steps/shadow_map.h"
//#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/common.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		// To render to a texture then read the resulting pixel data on a CPU:
		// - Create a texture that the GPU can render into (D3D11_BIND_RENDER_TARGET, D3D11_USAGE_DEFAULT).
		// - Create a staging texture (D3D11_USAGE_STAGING) that the GPU will copy data to (via CopyResource).
		// - Render to the render target texture.
		// - Call ID3D11DeviceContext::CopyResource() or ID3D11DeviceContext::CopySubresource()
		// - Map (ID3D11DeviceContext::Map()) the staging resource to get access to the pixels.
		// The call to Map will block until the gfx pipeline has completed the CopyResource() call. So, calling
		// CopyResource(), immediately followed by Map() effectively flushes the pipeline.
		// This can be handled in two ways; Call CopyResource(), do loads of gfx work, then call Map() some time later.
		// Or, use triple buffering like so:
		// - Frame #1 - start CopyResource() to staging texture #1
		// - Frame #2 - start CopyResource() to staging texture #2
		// - Frame #3 - start CopyResource() to staging texture #3 and Map() staging texture #1 to access data.
		// - Frame #4 - start CopyResource() to staging texture #1 and Map() staging texture #2 to access data.
		// - etc
		// This way you can keep FPS but introduce latency, which is acceptable in high frame rate applications.

		RayCastStep::RayCastStep(Scene& scene, bool continuous)
			:RenderStep(scene)
			,m_rays()
			,m_snap_distance()
			,m_flags()
			,m_continuous(continuous)
			,m_rt()
			,m_db()
			,m_stage()
			,m_rtv()
			,m_dsv()
			,m_main_rtv()
			,m_main_dsv()
			,m_cbuf_frame(m_shdr_mgr->GetCBuf<hlsl::util::CBufRayCastFrame>("Util::CBufRayCastFrame"))
			,m_cbuf_nugget(m_shdr_mgr->GetCBuf<hlsl::util::CBufRayCastNugget>("Util::CBufRayCastNugget"))
			,m_vs()
			,m_gs_face()
			,m_gs_edge()
			,m_gs_vert()
			,m_ps()
			,m_cs()
			,m_stage_idx()
		{
			// Get pointers to the shaders
			m_vs      = m_shdr_mgr->FindShader(RdrId(EStockShader::RayCastVS));
			m_gs_face = m_shdr_mgr->FindShader(RdrId(EStockShader::RayCastFaceGS));
			m_gs_edge = m_shdr_mgr->FindShader(RdrId(EStockShader::RayCastEdgeGS));
			m_gs_vert = m_shdr_mgr->FindShader(RdrId(EStockShader::RayCastVertGS));
			m_ps      = m_shdr_mgr->FindShader(RdrId(EStockShader::RayCastPS));
			m_cs      = m_shdr_mgr->FindShader(RdrId(EStockShader::RayCastCS));

			// Set up the output render target
			InitRT();
		}

		// Set the ray to cast.
		void RayCastStep::SetRays(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags)
		{
			// Save the rays to cast
			m_rays.assign(rays, rays + count);
			m_snap_distance = snap_distance;
			m_flags = flags;
		}

		// Read the results from the ray casts
		void RayCastStep::ReadOutput(HitTestResult* results, int count)
		{
			// If running in continuous mode, read from 'm_stage_idx' which will be 3 frames ago.
			// Otherwise, read from the staging texture last used in a CopyResource() call.
			auto& stage = m_continuous
				? m_stage[m_stage_idx]
				: m_stage[(m_stage_idx + _countof(m_stage) - 1) % _countof(m_stage)];

			// Read the values out of the texture
			Renderer::Lock rdrlock(m_scene->rdr());
			LockT<v4> lock(rdrlock.ImmediateDC(), stage.get(), 0, D3D11_MAP_READ, 0);
			for (int i = 0; i != count; ++i)
			{
				auto intercepts = lock.ptr();

				HitTestResult result = {};
				result.m_ws_origin = m_rays[i].m_ws_origin;
				result.m_ws_direction = m_rays[i].m_ws_direction;
				result.m_ws_intercept = intercepts[i].w1();
				result.m_instance_id = int(floor(intercepts[i].w));
				results[i] = result;

				OutputDebugStringA(FmtS("Ray:[%s, %s]  Intercept:%s  Id:%d\n"
					,To<std::string>(result.m_ws_origin).c_str()
					,To<std::string>(result.m_ws_direction).c_str()
					,To<std::string>(result.m_ws_intercept).c_str()
					,result.m_instance_id
				));
			}
			OutputDebugStringA("\n");
		}

		// Create the render target for the output of the ray cast
		void RayCastStep::InitRT()
		{
			// Create render targets if there are rays to cast
			Renderer::Lock lock(m_scene->rdr());
			auto device = lock.D3DDevice();

			// Create a texture for the ray cast results to go in
			{
				TextureDesc tdesc = {};
				tdesc.Width  = MAX_RAYS;
				tdesc.Height = 1;
				tdesc.Format = DXGI_FORMAT_R32G32B32A32_FLOAT; // x,y,z,Id = world space position of intersect, instance id
				tdesc.MipLevels = 1;
				tdesc.ArraySize = 1;
				tdesc.SampleDesc = MultiSamp(1,0);
				tdesc.Usage = D3D11_USAGE_DEFAULT;
				tdesc.BindFlags = D3D11_BIND_RENDER_TARGET;
				tdesc.CPUAccessFlags = 0;
				tdesc.MiscFlags = 0;
				pr::Throw(device->CreateTexture2D(&tdesc, 0, &m_rt.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_rt.get(), "ray cast RT"));

				// Create a render target view
				RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
				pr::Throw(device->CreateRenderTargetView(m_rt.get(), &rtvdesc, &m_rtv.m_ptr));
			}
			{// Create a depth buffer for the nearest ray cast intercept
				TextureDesc tdesc = {};
				tdesc.Width  = MAX_RAYS;
				tdesc.Height = 1;
				tdesc.Format = DXGI_FORMAT_D32_FLOAT; // ray parametric value of the intercept
				tdesc.MipLevels = 1;
				tdesc.ArraySize = 1;
				tdesc.SampleDesc = MultiSamp(1,0);
				tdesc.Usage = D3D11_USAGE_DEFAULT;
				tdesc.BindFlags = D3D11_BIND_DEPTH_STENCIL;
				tdesc.CPUAccessFlags = 0;
				tdesc.MiscFlags = 0;
				pr::Throw(device->CreateTexture2D(&tdesc, 0, &m_db.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_db.get(), "ray cast DB"));

				// Create a depth stencil view
				DepthStencilViewDesc dsvdesc(tdesc.Format, D3D11_DSV_DIMENSION_TEXTURE2D);
				pr::Throw(device->CreateDepthStencilView(m_db.get(), &dsvdesc, &m_dsv.m_ptr));
			}
			{// Create triple buffered staging textures
				TextureDesc tdesc = {};
				tdesc.Width  = MAX_RAYS;
				tdesc.Height = 1;
				tdesc.Format = DXGI_FORMAT_R32G32B32A32_FLOAT; // x,y,z,Id = world space position of intersect, instance id
				tdesc.MipLevels = 1;
				tdesc.ArraySize = 1;
				tdesc.SampleDesc = MultiSamp(1,0);
				tdesc.Usage = D3D11_USAGE_STAGING;
				tdesc.BindFlags = D3D11_BIND_FLAG(0);
				tdesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
				for (auto& stage : m_stage)
				{
					pr::Throw(device->CreateTexture2D(&tdesc, 0, &stage.m_ptr));
					PR_EXPAND(PR_DBG_RDR, NameResource(stage.get(), "ray cast stage"));
				}
			}
		}

		// Bind the render target to the output merger for this step
		void RayCastStep::BindRT(bool bind)
		{
			Renderer::Lock lock(m_scene->rdr());
			auto dc = lock.ImmediateDC();
			if (bind)
			{
				// Save a reference to the main render target/depth buffer
				dc->OMGetRenderTargets(1, &m_main_rtv.m_ptr, &m_main_dsv.m_ptr);

				// Bind our RT to the OM
				dc->OMSetRenderTargets(1, &m_rtv.m_ptr, m_dsv.m_ptr);
			}
			else
			{
				// Restore the main RT and depth buffer
				dc->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);

				// Release our references
				m_main_rtv = nullptr;
				m_main_dsv = nullptr;
			}
		}

		// Add model nuggets to the draw list for this render step
		void RayCastStep::AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets)
		{
			// Ignore instances with no instance Id
			if (UniqueId(inst) == 0)
				return;

			Lock lock(*this);
			auto& drawlist = lock.drawlist();

			// Add a drawlist element for each nugget in the instance's model
			drawlist.reserve(drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				ShaderSet ss = {m_vs, nullptr, m_ps, m_cs};
				switch (nug.m_topo) {
				case EPrim::PointList:
					ss.m_gs = m_gs_vert;
					break;
				case EPrim::LineList:
				case EPrim::LineStrip:
					ss.m_gs = m_gs_edge;
					break;
				case EPrim::TriList:
				case EPrim::TriStrip:
					ss.m_gs = m_gs_face;
					break;
				default:
					throw std::exception("Unsupported primitive type");
				}

				nug.AddToDrawlist(drawlist, inst, nullptr, Id, ss);
			}

			m_sort_needed = true;
		}

		// Perform the render step
		void RayCastStep::ExecuteInternal(StateStack& ss)
		{
			auto dc = ss.m_dc;

			// Sort the drawlist if needed
			SortIfNeeded();

			// Bind the render target to the OM
			auto bind_rt = pr::CreateScope(
				[this]{ BindRT(true); },
				[this]{ BindRT(false); });

			// Clear the render target/depth buffer.
			// The depth data is the fractional distance between the frustum plane (0) and the light (1).
			// We only care about points in front of the frustum faces => reset depths to zero.
			dc->ClearRenderTargetView(m_rtv.m_ptr, pr::ColourZero.arr);
			dc->ClearDepthStencilView(m_dsv.m_ptr, D3D11_CLEAR_DEPTH, 1.0f, 0U);

			// Set the viewport
			Viewport vp(float(MAX_RAYS), 1.0f);
			dc->RSSetViewports(1, &vp);

			{// Set the frame constants
				hlsl::util::CBufRayCastFrame cb = {};
				cb.m_ray_count = int(m_rays.size());
				cb.m_snap_mode = int(m_flags);
				cb.m_snap_dist = m_snap_distance;
				cb.m_ray_max_length = m_scene->m_view.Far();
				for (int i = 0, iend = int(m_rays.size()); i != iend; ++i)
				{
					cb.m_ws_ray_origin[i] = m_rays[i].m_ws_origin;
					cb.m_ws_ray_direction[i] = m_rays[i].m_ws_direction;
				}
				WriteConstants(dc, m_cbuf_frame.get(), cb, EShaderType::GS);
			}

			// Draw each element in the draw list
			Lock lock(*this);
			for (auto& dle : lock.drawlist())
			{
				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				auto const& nugget = *dle.m_nugget;

				{// Set the per-nugget constants
					hlsl::util::CBufRayCastNugget cb = {};
					cb.m_o2w = GetO2W(*dle.m_instance);
					cb.m_flags.w = UniqueId(*dle.m_instance);
					WriteConstants(dc, m_cbuf_nugget.get(), cb, EShaderType::VS|EShaderType::GS);
				}

				// Draw the nugget
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_beg),
					0);
			}

			// Initiate the CopyResource() to the staging texture.
			// 'CopyResource()' is basically an async function that returns a future.
			// When you call 'Map()' it awaits on that future to be completed.
			auto& stage = m_stage[m_stage_idx];
			dc->CopyResource(stage.get(), m_rt.get());
			m_stage_idx = (m_stage_idx + 1) % _countof(m_stage);
		}
	}
}
