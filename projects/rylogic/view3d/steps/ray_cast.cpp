//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/steps/ray_cast.h"
#include "view3d/shaders/common.h"
#include "view3d/render/state_stack.h"

namespace pr::rdr
{
	namespace hlsl::ray_cast
	{
		#include "view3d/shaders/hlsl/utility/ray_cast_cbuf.hlsli"
	}
	using namespace hlsl::ray_cast;

	#pragma region Shaders

	// Ray cast shaders are specific to this render step, don't bother making them stock shaders
	#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_vs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_face_gs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_edge_gs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_vert_gs.h)

	// Ray cast shaders
	struct RayCastVS :ShaderT<ID3D11VertexShader, RayCastVS>
	{
		using base = ShaderT<ID3D11VertexShader, RayCastVS>;
		RayCastVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader>& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_vs.cso"));
		}
	};
	struct RayCastFaceGS :ShaderT<ID3D11GeometryShader, RayCastFaceGS>
	{
		using base = ShaderT<ID3D11GeometryShader, RayCastFaceGS>;
		RayCastFaceGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader>& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_face_gs.cso"));
		}
	};
	struct RayCastEdgeGS :ShaderT<ID3D11GeometryShader, RayCastEdgeGS>
	{
		using base = ShaderT<ID3D11GeometryShader, RayCastEdgeGS>;
		RayCastEdgeGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader>& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_edge_gs.cso"));
		}
	};
	struct RayCastVertGS :ShaderT<ID3D11GeometryShader, RayCastVertGS>
	{
		using base = ShaderT<ID3D11GeometryShader, RayCastVertGS>;
		RayCastVertGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader>& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_vert_gs.cso"));
		}
	};

	#pragma endregion

	// Stream output stage buffer format
	StreamOutDesc so_buffer_desc(
	{
		{0, "WSIntercept", 0, 0, 4, 0},
		{0, "SnapType", 0, 0, 1, 0},
		{0, "RayIndex", 0, 0, 1, 0},
		{0, "InstPtr", 0, 0, 2, 0},
	});

	// To render to a texture then read the resulting pixel data on a CPU:
	// - Create a texture that the GPU can render into (D3D11_BIND_RENDER_TARGET, EUsage::DEFAULT).
	// - Create a staging texture (EUsage::STAGING) that the GPU will copy data to (via CopyResource).
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
		,m_snap_distance(1.0f)
		,m_flags(EHitTestFlags::Verts | EHitTestFlags::Edges | EHitTestFlags::Faces)
		,m_include([](auto){ return true; })
		,m_cbuf_frame(m_shdr_mgr->GetCBuf<FrameCBuf>("RayCast::FrameCBuf"))
		,m_cbuf_nugget(m_shdr_mgr->GetCBuf<NuggetCBuf>("RayCast::NuggetCBuf"))
		,m_buf_results()
		,m_buf_stage()
		,m_stage_idx()
		,m_vs()
		,m_gs_face()
		,m_gs_edge()
		,m_gs_vert()
		,m_continuous(continuous)
	{
		// Set render states
		m_dsb.Set(EDS::DepthEnable, FALSE);

		// Get/Create shader instances
		{
			auto id = MakeId("RayCastStepVS");
			if ((m_vs = m_shdr_mgr->FindShader<RayCastVS>(id)) == nullptr)
			{
				VShaderDesc vs_desc(ray_cast_vs, Vert());
				auto dx = m_shdr_mgr->GetVS(id, &vs_desc);
				m_vs = m_shdr_mgr->CreateShader<RayCastVS>(id, dx, "ray_cast_vs");
			}
		}{
			auto id = MakeId("RayCastStepFaceGS");
			if ((m_gs_face = m_shdr_mgr->FindShader<RayCastFaceGS>(id)) == nullptr)
			{
				GShaderDesc gs_desc(ray_cast_face_gs);
				auto dx = m_shdr_mgr->GetGS(id, &gs_desc, so_buffer_desc);
				m_gs_face = m_shdr_mgr->CreateShader<RayCastFaceGS>(id, dx, "ray_cast_face_gs");
			}
		}{
			auto id = MakeId("RayCastStepEdgeGS");
			if ((m_gs_edge = m_shdr_mgr->FindShader<RayCastEdgeGS>(id)) == nullptr)
			{
				GShaderDesc gs_desc(ray_cast_edge_gs);
				auto dx = m_shdr_mgr->GetGS(id, &gs_desc, so_buffer_desc);
				m_gs_edge = m_shdr_mgr->CreateShader<RayCastEdgeGS>(id, dx, "ray_cast_edge_gs");
			}
		}{
			auto id = MakeId("RayCastStepVertGS");
			if ((m_gs_vert = m_shdr_mgr->FindShader<RayCastVertGS>(id)) == nullptr)
			{
				GShaderDesc gs_desc(ray_cast_vert_gs);
				auto dx = m_shdr_mgr->GetGS(id, &gs_desc, so_buffer_desc);
				m_gs_vert = m_shdr_mgr->CreateShader<RayCastVertGS>(id, dx, "ray_cast_vert_gs");
			}
		}

		// Set up the shader buffers
		InitBuffers();
	}

	// Create the buffers used by the shaders
	void RayCastStep::InitBuffers()
	{
		// Create buffers to be used in the shaders
		Renderer::Lock lock(m_scene->rdr());
		auto device = lock.D3DDevice();

		char zeros[MaxIntercepts * sizeof(Intercept)] = {};
		SubResourceData init_data(&zeros[0], sizeof(zeros), sizeof(zeros));

		// Create a GPU buffer to receive the intercepts
		{
			// Reset in case this method is public one day
			m_buf_results = nullptr;
			m_buf_zeros = nullptr;

			BufferDesc bdesc = {};
			bdesc.Usage = D3D11_USAGE_DEFAULT;
			bdesc.BindFlags = D3D11_BIND_STREAM_OUTPUT;
			bdesc.ByteWidth = MaxIntercepts * sizeof(Intercept);
			pr::Check(device->CreateBuffer(&bdesc, &init_data, m_buf_results.address_of()));
			pr::Check(device->CreateBuffer(&bdesc, &init_data, m_buf_zeros.address_of()));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_buf_results.get(), "RayCast Output Intercepts"));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_buf_zeros.get(), "RayCast Output Zero"));
		}

		// Create CPU staging buffers to copy the intercept data output to
		{
			// Reset in case this method is public one day
			for (auto& stage : m_buf_stage)
				stage = nullptr;

			BufferDesc bdesc = {};
			bdesc.Usage = D3D11_USAGE_STAGING;
			bdesc.BindFlags = D3D11_BIND_FLAG(0);
			bdesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
			bdesc.ByteWidth = MaxIntercepts * sizeof(Intercept);
			for (int i = 0, iend = m_continuous ? _countof(m_buf_stage) : 1; i != iend; ++i)
			{
				auto& stage = m_buf_stage[i];
				pr::Check(device->CreateBuffer(&bdesc, nullptr, stage.address_of()));
				PR_EXPAND(PR_DBG_RDR, NameResource(stage.get(), "RayCast Staging Buffer"));
			}
		}
	}

	// Set the rays to cast.
	void RayCastStep::SetRays(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, InstFilter include)
	{
		// Save the rays so we can match ray indices to the actual ray.
		m_rays.assign(rays, rays + std::min(count, MaxRays));
		m_snap_distance = snap_distance;
		m_flags = flags;
		m_include = include;
	}

	// Read the results from the ray casts
	void RayCastStep::ReadOutput(ResultsOut const& cb)
	{
		Renderer::Lock rdrlock(m_scene->rdr());
		auto dc = rdrlock.ImmediateDC();

		// Get the staging buffer to read from
		auto& stage = m_buf_stage[m_stage_idx];

		// Read the values out of the buffer.
		// There will be duplicates in the buffer because of shared verts/edges in the models.
		// Sort the results by distance and skip duplicates.
		rdr::Lock lock(dc, stage.get(), 0, sizeof(Intercept), EMap::Read, EMapFlags::None);
		std::span<Intercept> intercepts(lock.ptr<Intercept>(), MaxIntercepts);
		intercepts = intercepts.subspan(0, index_if(intercepts, [](auto& i){ return i.inst_ptr == nullptr; }));

		// Returns the squared distance from the ray
		auto DistSqFromRay = [](HitTestRay const& ray, Intercept const& intercept)
		{
			return DistanceSq_PointToInfiniteLine(intercept.ws_intercept.w1(), ray.m_ws_origin, ray.m_ws_direction);
		};

		// Sort the intercepts from nearest to furtherest.
		// This is a bit of a fuzzy ordering because of snapping.
		sort(intercepts, [&](Intercept const& l, Intercept const& r)
		{
			// Cases:
			//  - If either intercept is a face snap, then sort by distance
			//    because faces should occlude any intercepts behind them.
			//  - Otherwise, sort by distance if the difference is greater
			//    than the snap distance.
			//  - If two intercepts are within the snap distance:
			//    - Sort by closest to the ray.

			// If one of the intercepts is a face snap
			if ((ESnapType)l.snap_type == ESnapType::Face ||
				(ESnapType)r.snap_type == ESnapType::Face)
			{
				// At least one of the intercepts is a face, sort by distance
				if (Abs(l.ws_intercept.w - r.ws_intercept.w) > maths::tinyf)
					return l.ws_intercept.w < r.ws_intercept.w;

				// If the intercepts are at the same distance, prioritise by snap type
				// (Remember face snap have zero distance from the ray)
				return l.snap_type < r.snap_type;
			}

			// Neither intercept is a face snap. Sort roughly by distance first
			if (Abs(l.ws_intercept.w - r.ws_intercept.w) > m_snap_distance)
				return l.ws_intercept.w < r.ws_intercept.w;

			// If one of the intercepts is an edge snap
			if ((ESnapType)l.snap_type == ESnapType::Edge ||
				(ESnapType)r.snap_type == ESnapType::Edge)
			{
				// If one of the intercepts is a point snap, it has priority
				if (l.snap_type != r.snap_type)
					return l.snap_type < r.snap_type;

				// Sort by distance of the intercepts from the ray
				auto dist_l = DistSqFromRay(m_rays[l.ray_index], l);
				auto dist_r = DistSqFromRay(m_rays[r.ray_index], r);
				return dist_l < dist_r;
			}

			// The intercepts are point snaps. Sort by distance from the ray
			auto dist_l = DistSqFromRay(m_rays[l.ray_index], l);
			auto dist_r = DistSqFromRay(m_rays[r.ray_index], r);
			return dist_l < dist_r;
		});

		// Forward each unique intercept to the callback
		for (int i = 0, iend = int(intercepts.size()); i != iend; )
		{
			auto& intercept = intercepts[i];

			// Forward the hits to the callback
			HitTestResult result  = {};
			result.m_ws_origin    = m_rays[intercept.ray_index].m_ws_origin;
			result.m_ws_direction = m_rays[intercept.ray_index].m_ws_direction;
			result.m_ws_intercept = intercept.ws_intercept.w1();
			result.m_instance     = type_ptr<BaseInstance>(intercept.inst_ptr);
			result.m_distance     = intercept.ws_intercept.w;
			result.m_ray_index    = intercept.ray_index;
			result.m_snap_type    = static_cast<ESnapType>(intercept.snap_type);
			if (!cb(result))
				return;

			// Skip duplicates
			auto Eql = [](Intercept const& l, Intercept const& r)
			{
				return
					l.ws_intercept == r.ws_intercept &&
					l.inst_ptr     == r.inst_ptr &&
					l.ray_index    == r.ray_index;
			};
			for (++i; i != iend && Eql(intercepts[i], intercept); ++i)
			{}
		}
	}

	// Add model nuggets to the draw list for this render step
	void RayCastStep::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
	{
		// Ignore instances that are filtered out
		if (!m_include(&inst))
			return;

		Lock lock(*this);
		auto& drawlist = lock.drawlist();

		// Add a drawlist element for each nugget in the instance's model
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
			nug.AddToDrawlist(drawlist, inst, nullptr, Id);

		m_sort_needed = true;
	}

	// Update the provided shader set appropriate for this render step
	void RayCastStep::ConfigShaders(ShaderSet1& ss, ETopo topo) const
	{
		ss = ShaderSet1{};
		ss.m_vs = const_cast<Shader*>(m_vs.get());
		switch (topo)
		{
		case ETopo::PointList:
			ss.m_gs = const_cast<Shader*>(m_gs_vert.get());
			break;
		case ETopo::LineList:
		case ETopo::LineListAdj:
		case ETopo::LineStrip:
		case ETopo::LineStripAdj:
			ss.m_gs = const_cast<Shader*>(m_gs_edge.get());
			break;
		case ETopo::TriList:
		case ETopo::TriStrip:
			ss.m_gs = const_cast<Shader*>(m_gs_face.get());
			break;
		default:
			throw std::runtime_error("Unsupported primitive type");
		}
	}

	// Perform the render step
	void RayCastStep::ExecuteInternal(StateStack& ss)
	{
		auto dc = ss.m_dc;

		// Sort the drawlist if needed
		SortIfNeeded();

		// Zero the results buffer
		dc->CopyResource(m_buf_results.get(), m_buf_zeros.get());

		// Update the frame constants
		{
			FrameCBuf cb = {};
			static_assert(sizeof(Ray) == sizeof(HitTestRay));
			std::memcpy(&cb.m_rays[0], m_rays.data(), sizeof(Ray) * int(m_rays.size()));
			cb.m_ray_count = int(m_rays.size());
			cb.m_snap_mode = int(m_flags);
			cb.m_snap_dist = m_snap_distance;
			WriteConstants(dc, m_cbuf_frame.get(), cb, EShaderType::GS);
		}

		// Set the stream output stage targets
		StateStack::SOFrame so_frame(ss, m_buf_results.get(), 0U);

		// Draw each element in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			StateStack::DleFrame frame(ss, dle);
			ss.Commit();

			auto const& nugget = *dle.m_nugget;

			{// Set the per-nugget constants
				NuggetCBuf cb = {};
				cb.m_o2w = GetO2W(*dle.m_instance);
				cb.m_inst_ptr = dle.m_instance;
				WriteConstants(dc, m_cbuf_nugget.get(), cb, EShaderType::VS | EShaderType::GS);
			}

			// Draw the nugget
			dc->DrawIndexed(
				UINT(nugget.m_irange.size()),
				UINT(nugget.m_irange.m_beg),
				0);
		}

		// Initiate the copy to the staging buffer.
		// 'CopyResource()' is basically an async function that returns a future.
		// When you call 'Map()' it waits for the future to be completed.
		auto& stage = m_buf_stage[m_stage_idx];
		dc->CopyResource(stage.get(), m_buf_results.get());

		// If running in continuous mode, cycle through the multi-buffers
		// so that 'Map()' does not stall the pipeline.
		if (m_continuous)
			m_stage_idx = (m_stage_idx + 1) % _countof(m_buf_stage);
	}
}
