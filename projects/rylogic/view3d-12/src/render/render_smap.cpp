//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/render/render_smap.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/texture/texture_base.h"
#include "pr/view3d-12/sampler/sampler.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/pipe_state.h"
#include "pr/view3d-12/utility/shadow_caster.h"
#include "view3d-12/src/utility/barrier_batch.h"
#include "pr/view3d-12/utility/diagnostics.h"

#define PR_DBG_SMAP 1
#if PR_DBG_SMAP
#pragma message(PR_LINK "WARNING: ************************************************** Shadow Map debugging enabled")
#include "pr/view3d-12/instance/instance.h"
#include "view3d-12/src/render/render_forward.h"
namespace pr::rdr12
{
	// An instance for a quad that displays a texture for debugging it's content
	#define PR_RDR_INST(x)\
		x(m4x4, m_i2w, EInstComp::I2WTransform)\
		x(m4x4, m_c2s, EInstComp::C2STransform)\
		x(ModelPtr, m_model, EInstComp::ModelPtr)\
		x(Texture2DPtr, m_tex_diffuse, EInstComp::DiffTexture)\
		x(SamplerPtr, m_sam_diffuse, EInstComp::DiffTextureSampler)\
		x(EInstFlag, m_flags, EInstComp::Flags)
	PR_RDR12_DEFINE_INSTANCE(DebugQuadInstance, PR_RDR_INST);
	#undef PR_RDR_INST

	struct DebugQuad :DebugQuadInstance
	{
		Scene* m_scene;

		// Create an instance of a quad to display in the lower left of the screen
		void Create(Scene& scene, ShadowCaster& caster)
		{
			m_scene = &scene;
			m_i2w = m4x4::Identity();
			m_c2s = m4x4::ProjectionOrthographic(1.0f, 1.0f, -0.01f, 1000.0f, true);
			m_model = scene.res().CreateModel(EStockModel::UnitQuad);
			m_tex_diffuse = caster.m_smap;
			m_sam_diffuse = scene.res().CreateSampler(EStockSampler::PointClamp);
			m_flags = SetBits(m_flags, EInstFlag::ShadowCastExclude, true);
		}

		// Clean up the debug quad
		void Destroy()
		{
			m_scene->FindRStep<RenderForward>()->RemoveInstance(*this);
			m_model = nullptr;
			m_tex_diffuse = nullptr;
			m_sam_diffuse = nullptr;
		}

		// Add the debug quad to the render forward step (only)
		void Update()
		{
			// Scale the unit quad and position in the lower left
			const float scale = 0.3f;
			m_i2w = m_scene->m_cam.CameraToWorld() * m4x4::Scale(scale, v4{-0.495f + scale/2, -0.495f + scale/2, 0, 1});
			m_scene->FindRStep<RenderForward>()->AddInstance(*this);
		}
	} g_smap_quad;
}
#endif

namespace pr::rdr12
{
	RenderSmap::RenderSmap(Scene& scene, Light const& light, int size, DXGI_FORMAT format)
		: RenderStep(Id, scene)
		, m_shader(scene.d3d())
		, m_default_tex(res().CreateTexture(EStockTexture::White))
		, m_default_sam(res().CreateSampler(EStockSampler::LinearClamp))
		, m_casters()
		, m_smap_size(size)
		, m_smap_format(format)
		, m_bbox_scene(BBox::Reset())
	{
		// Create the default PSO description
		m_default_pipe_state = D3D12_GRAPHICS_PIPELINE_STATE_DESC {
			.pRootSignature = m_shader.m_signature.get(),
			.VS = m_shader.m_code.VS,
			.PS = m_shader.m_code.PS,
			.DS = m_shader.m_code.DS,
			.HS = m_shader.m_code.HS,
			.GS = m_shader.m_code.GS,
			.StreamOutput = StreamOutputDesc{},
			.BlendState = BlendStateDesc{}.enable(0).blend(0, D3D12_BLEND_OP_MAX, D3D12_BLEND_SRC_COLOR, D3D12_BLEND_DEST_COLOR),
			.SampleMask = UINT_MAX,
			.RasterizerState = RasterStateDesc{}.Set(D3D12_CULL_MODE_BACK),
			.DepthStencilState = DepthStateDesc{}.Enabled(false),
			.InputLayout = Vert::LayoutDesc(),
			.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED,
			.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
			.NumRenderTargets = 1U,
			.RTVFormats = {
				format,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
			},
			.DSVFormat = DXGI_FORMAT_UNKNOWN,
			.SampleDesc = MultiSamp{},
			.NodeMask = 0U,
			.CachedPSO = {
				.pCachedBlob = nullptr,
				.CachedBlobSizeInBytes = 0U,
			},
			.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
		};

		AddLight(light);

		PR_EXPAND(PR_DBG_SMAP, g_smap_quad.Create(scene, m_casters[0]));
	}
	RenderSmap::~RenderSmap()
	{
		PR_EXPAND(PR_DBG_SMAP, g_smap_quad.Destroy());
	}
	
	// Add a shadow casting light source
	void RenderSmap::AddLight(Light const& light)
	{
		m_casters.push_back(ShadowCaster(*this, light, m_smap_size, m_smap_format));
	}

	// Add model nuggets to the draw list for this render step
	void RenderSmap::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist)
	{
		// Ignore instances that don't cast shadows
		if (AnySet(GetFlags(inst), EInstFlag::ShadowCastExclude))
			return;

		bool grow_bounds = true;

		// Add a drawlist element for each nugget in the instance's model
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
		{
			// Filter out nuggets that can't cast shadows
			if (AnySet(nug.m_nflags, ENuggetFlag::ShadowCastExclude | ENuggetFlag::Hidden))
				continue;

			for (;;)
			{
				// Don't add nuggets without a surface area
				if (nug.FillMode() != EFillMode::Default && nug.FillMode() != EFillMode::Solid)
					break;

				// Don't add alpha back faces when using 'Points' fill mode
				if (nug.m_id == Nugget::AlphaNuggetId)
					break;

				// Create the combined sort key for this nugget
				// Ignore the shader sort key, because they're all using the smap shader
				auto sk = nug.m_sort_key;
				auto sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);
				if (sko) sk = sko->Combine(sk);

				// Set the texture id part of the key if not set already
				if (!AnySet(sk, SortKey::TextureIdMask) && nug.m_tex_diffuse != nullptr)
					sk = SetBits(sk, SortKey::TextureIdMask, nug.m_tex_diffuse->SortId() << SortKey::TextureIdOfs);

				// Grow the scene bounds by the model bbox if nuggets were added
				for (;grow_bounds;)
				{
					grow_bounds = false;

					// Ignore models with invalid bounding boxes
					if (!nug.m_model->m_bbox.valid())
						break;

					// Ignore instances with non-affine transforms
					auto i2w = GetO2W(inst);
					if (!IsAffine(i2w))
						break;

					// Grow the scene bounds
					auto bbox = i2w * nug.m_model->m_bbox;
					assert(bbox.valid() && "Model bounding box is invalid");
					Grow(m_bbox_scene, bbox);
					break;
				}

				// Add an element to the drawlist
				DrawListElement dle = {
					.m_sort_key = sk,
					.m_nugget = &nug,
					.m_instance = &inst,
				};
				drawlist.push_back(dle);
				break;
			}

			// Recursively add dependent nuggets
			AddNuggets(inst, nug.m_nuggets, drawlist);
		}
	}

	// Perform the render step
	void RenderSmap::ExecuteInternal(BackBuffer&)
	{
		PR_EXPAND(PR_DBG_SMAP, auto x = pr::Scope<void>([&] { g_smap_quad.Update(); }));

		// Nothing to render if there are no objects
		if (m_casters.empty() || !m_bbox_scene.valid() || m_bbox_scene.is_point())
			return;

		// Sort the draw list if needed
		SortIfNeeded();

		// Bind the descriptor heaps
		auto des_heaps = {wnd().m_heap_view.get(), wnd().m_heap_samp.get()};
		m_cmd_list.SetDescriptorHeaps({ des_heaps.begin(), des_heaps.size() });

		// Render the shadow map for each shadow caster. TODO in parallel?
		for (auto& caster : m_casters)
		{
			auto& smap = *caster.m_smap.get();

			// Transition the caster resource to a render target
			BarrierBatch barriers(m_cmd_list);
			barriers.Transition(smap.m_res.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
			barriers.Commit();

			// Calculate the projection transforms
			caster.UpdateParams(scn(), m_bbox_scene);

			// Bind the smap as the render target
			m_cmd_list.OMSetRenderTargets({ &smap.m_rtv.m_cpu, 1 }, FALSE, nullptr);

			// Clear the render target to the background colour
			constexpr float reset[] = {0,0,0,0};
			m_cmd_list.ClearRenderTargetView(smap.m_rtv.m_cpu, reset);

			// Set the viewport and scissor rect.
			Viewport vp(iv2(m_smap_size, m_smap_size));
			m_cmd_list.RSSetViewports({ &vp, 1 });
			m_cmd_list.RSSetScissorRects(vp.m_clip);

			// Set the signature for the shader used for this nugget
			m_cmd_list.SetGraphicsRootSignature(m_shader.m_signature.get());

			// Set shader constants for the frame
			m_shader.Setup(m_cmd_list.get(), m_cbuf_upload, caster, nullptr);

			// Draw each element in the draw list
			Lock lock(*this);
			for (auto& dle : lock.drawlist())
			{
				auto const& nugget = *dle.m_nugget;
				auto desc = m_default_pipe_state;

				// Set pipeline state
				desc.Apply(PSO<EPipeState::TopologyType>(To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(nugget.m_topo)));
				m_cmd_list.IASetPrimitiveTopology(nugget.m_topo);
				m_cmd_list.IASetVertexBuffers(0U, { &nugget.m_model->m_vb_view, 1 });
				m_cmd_list.IASetIndexBuffer(&nugget.m_model->m_ib_view);

				// Set shader constants for the nugget
				m_shader.Setup(m_cmd_list.get(), m_cbuf_upload, caster, &dle);

				// Bind textures to the pipeline
				auto tex = FindDiffTexture(*dle.m_instance) << nugget.m_tex_diffuse << m_default_tex;
				auto srv_descriptor = wnd().m_heap_view.Add(tex->m_srv);
				m_cmd_list.SetGraphicsRootDescriptorTable(shaders::smap::ERootParam::DiffTexture, srv_descriptor);

				// Bind samplers to the pipeline (can't use static samplers because each mode may use different address modes)
				auto sam = FindDiffTextureSampler(*dle.m_instance) << nugget.m_sam_diffuse << m_default_sam;
				auto sam_descriptor = wnd().m_heap_samp.Add(sam->m_samp);
				m_cmd_list.SetGraphicsRootDescriptorTable(shaders::smap::ERootParam::DiffTextureSampler, sam_descriptor);

				// Apply PSO overrides?
 
				// Draw the nugget
				DrawNugget(nugget, desc);
			}

			// Transition the caster resource to a SRV
			barriers.Transition(smap.m_res.get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE|D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
			barriers.Commit();
		}
	}

	// Call draw for a nugget
	void RenderSmap::DrawNugget(Nugget const& nugget, PipeStateDesc& desc)
	{
		m_cmd_list.SetPipelineState(m_pipe_state_pool.Get(desc));
		if (!nugget.m_irange.empty())
		{
			m_cmd_list.DrawIndexedInstanced(
				nugget.m_irange.size(), 1U,
				nugget.m_irange.m_beg, 0U, 0U);
		}
		else
		{
			m_cmd_list.DrawInstanced(
				nugget.m_vrange.size(), 1U,
				nugget.m_vrange.m_beg, 0U);
		}
	}
}

