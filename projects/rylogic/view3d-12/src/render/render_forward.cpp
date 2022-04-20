//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/render/render_forward.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/shaders/shader_registers.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "view3d-12/src/utility/pipe_states.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12
{
	using namespace hlsl::fwd;

	RenderForward::RenderForward(Scene& scene)
		: RenderStep(scene)
		, m_shader(scene.rdr().res_mgr(), scene.wnd().BBCount())
	{
		Renderer::Lock lock(scene.rdr());
		auto device = lock.D3DDevice();

		// Create the root signature
		D3DPtr<ID3DBlob> signature, error;
		D3D12_DESCRIPTOR_RANGE1 diffuse_tex_desc[] = {
			{// t0,0
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = 1U,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t0),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_DESCRIPTOR_RANGE1 envmap_tex_desc[] = {
			{// t1,0
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = 1U,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t1),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_DESCRIPTOR_RANGE1 smap_tex_desc[] = {
			{// t2,MaxShadowMaps
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = hlsl::MaxShadowMaps,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t2),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_DESCRIPTOR_RANGE1 proj_tex_desc[] = {
			{// t3,MaxProjectedTextures
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = hlsl::MaxProjectedTextures,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t3),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_ROOT_PARAMETER1 params[] = {
			{// Constant buffer for per-frame data
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
				.Descriptor = {.ShaderRegister = CBufFrame::shader_register, .RegisterSpace = CBufFrame::register_space },
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			},
			{// Constant buffer for per-nugget data
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
				.Descriptor = {.ShaderRegister = CBufNugget::shader_register, .RegisterSpace = CBufNugget::register_space },
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			},
			{// Constant buffer for fade data
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
				.Descriptor = {.ShaderRegister = CBufFade::shader_register, .RegisterSpace = CBufFade::register_space },
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			},
			{// Diffuse texture
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {
					.NumDescriptorRanges = _countof(diffuse_tex_desc),
					.pDescriptorRanges = &diffuse_tex_desc[0],
				},
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			},
			{// EnvMap texture
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {
					.NumDescriptorRanges = _countof(envmap_tex_desc),
					.pDescriptorRanges = &envmap_tex_desc[0],
				},
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			},
			{// SMap textures
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {
					.NumDescriptorRanges = _countof(smap_tex_desc),
					.pDescriptorRanges = &smap_tex_desc[0],
				},
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			},
			{// Projected textures
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {
					.NumDescriptorRanges = _countof(proj_tex_desc),
					.pDescriptorRanges = &proj_tex_desc[0],
				},
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
			}
		};
		D3D12_STATIC_SAMPLER_DESC samplers[] = {
			SamDescStatic(ESamReg::s0, 0), // Diffuse texture sampler
			SamDescStatic(ESamReg::s1, 0), // Env map sampler
			SamDescStatic(ESamReg::s2, 0), // Shadow map sampler
			SamDescStatic(ESamReg::s3, 0), // Projected texture sampler
		};
		D3D12_VERSIONED_ROOT_SIGNATURE_DESC rs_desc = {
			.Version = D3D_ROOT_SIGNATURE_VERSION_1_1,
			.Desc_1_1 = {
				.NumParameters = _countof(params),
				.pParameters = &params[0],
				.NumStaticSamplers = _countof(samplers),
				.pStaticSamplers = &samplers[0],
				.Flags =
					D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
					//D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
					//D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS	|
					D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_NONE,
			},
		};
		Throw(D3D12SerializeVersionedRootSignature(&rs_desc, &signature.m_ptr, &error.m_ptr));
		Throw(device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), __uuidof(ID3D12RootSignature), (void**)&m_shader_sig.m_ptr));

		// Create the default PSO
		m_default_pipe_state = D3D12_GRAPHICS_PIPELINE_STATE_DESC {
			.pRootSignature = m_shader_sig.get(),
			.VS = m_shader.Code.VS,
			.PS = m_shader.Code.PS,
			.DS = m_shader.Code.DS,
			.HS = m_shader.Code.HS,
			.GS = m_shader.Code.GS,
			.StreamOutput = StreamOutputDesc{},
			.BlendState = {
				.AlphaToCoverageEnable = FALSE,
				.IndependentBlendEnable = FALSE,
				.RenderTarget = {
					BlendStateDesc{},
					BlendStateDesc{},
					BlendStateDesc{},
					BlendStateDesc{},
					BlendStateDesc{},
					BlendStateDesc{},
					BlendStateDesc{},
					BlendStateDesc{},
				}
			},
			.SampleMask = 0U,
			.RasterizerState = {
				RasterStateDesc{},
			},
			.DepthStencilState = {
				DepthStateDesc{}
			},
			.InputLayout = {
				.pInputElementDescs = &Vert::Layout()[0],
				.NumElements = _countof(Vert::Layout()),
			},
			.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED,
			.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
			.NumRenderTargets = 1U,
			.RTVFormats = {
				DXGI_FORMAT_B8G8R8A8_UNORM,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
			},
			.DSVFormat = DXGI_FORMAT_D32_FLOAT,
			.SampleDesc = MultiSamp{},
			.NodeMask = 0U,
			.CachedPSO = {
				.pCachedBlob = nullptr,
				.CachedBlobSizeInBytes = 0U,
			},
			.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
		};
	}

	// Add model nuggets to the draw list for this render step.
	void RenderForward::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist)
	{
		// Add a drawlist element for each nugget in the instance's model
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
		{
			// Ignore if flagged as not visible
			if (AllSet(nug.m_nflags, ENuggetFlag::Hidden))
				continue;

			// Don't add alpha back faces when using 'Points' fill mode
			if (nug.m_id == Nugget::AlphaNuggetId && nug.m_fill_mode == EFillMode::Points)
				continue;

			// If not visible for other reasons, don't render but add child nuggets.
			if (nug.Visible())
			{
				// Create the combined sort key for this nugget
				auto sk = nug.m_sort_key;
				auto sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);
				if (sko) sk = sko->Combine(sk);

				// Set the texture id part of the key if not set already
				if (!AnySet(sk, SortKey::TextureIdMask) && nug.m_tex_diffuse != nullptr)
					sk = SetBits(sk, SortKey::TextureIdMask, nug.m_tex_diffuse->m_sort_id << SortKey::TextureIdOfs);

				// Set the shader id part of the key if not set already
				if (!AnySet(sk, SortKey::ShaderIdMask))
				{
					#if 0 //todo
					auto shdr_id = 0;
					for (auto& shdr : m_smap[rstep].Enumerate())
					{
						if (shdr == nullptr) continue;
						shdr_id = shdr_id*13 ^ shdr->m_sort_id; // hash the sort ids together
					}
					sk |= (shdr_id << SortKey::ShaderIdOfs) & SortKey::ShaderIdMask;
					#endif
				}

				// Add an element to the drawlist
				DrawListElement dle =
				{
					.m_sort_key = sk,
					.m_nugget = &nug,
					.m_instance = &inst,
				};
				drawlist.push_back(dle);
			}

			// Recursively add dependent nuggets
			AddNuggets(inst, nug.m_nuggets, drawlist);
		}
	}
	
	// Update the provided shader set appropriate for this render step
	void RenderForward::ConfigShaders(ShaderSet1& ss, ETopo) const
	{
		(void)ss;
		#if 0 //todo
		if (ss.m_vs == nullptr) ss.m_vs = m_vs.get();
		if (ss.m_ps == nullptr) ss.m_ps = m_ps.get();
		#endif
	}

	// Perform the render step
	void RenderForward::ExecuteInternal(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list)
	{
		// Sort the draw list if needed
		SortIfNeeded();

		// Set the pipeline for this render step
		cmd_list->SetGraphicsRootSignature(m_shader_sig.get());

		// Get the back buffer view handle and set the back buffer as the render target.
		cmd_list->OMSetRenderTargets(1, &bb.m_rtv, false, &bb.m_dsv);

		// Clear the render target to the background colour
		if (scn().m_bkgd_colour != ColourZero)
		{
			cmd_list->ClearRenderTargetView(bb.m_rtv, scn().m_bkgd_colour.arr, 0, nullptr);
			cmd_list->ClearDepthStencilView(bb.m_dsv, D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0U, 0, nullptr);
		}

		// Set the viewport
		auto const& vp = scn().m_viewport;
		cmd_list->RSSetViewports(1, &vp);
		cmd_list->RSSetScissorRects(s_cast<UINT>(vp.m_clip.size()), vp.m_clip.data());

		#if 0 // todo
		// Check if shadows are enabled
		auto smap_rstep = scn().FindRStep<ShadowMap>();
		StateStack::SmapFrame smap_frame(ss, smap_rstep);
		#endif

		// Set the frame constants
		CBufFrame cb0 = {};
		SetViewConstants(cb0.m_cam, scn().m_cam);
		SetLightingConstants(cb0.m_global_light, scn().m_global_light, scn().m_cam);
		//todo SetShadowMapConstants(cb0.m_shadow, smap_rstep);
		//todo SetEnvMapConstants(cb0.m_env_map, scn().m_global_envmap.get());
		{
			// Copy 'cb0' to 'm_shader.m_cbuf_frame[bb_index]'
			MapResource map(m_shader.m_cbuf_frame.get(), 0U, cbuf_size_aligned_v<CBufFrame>);
			map.at<CBufFrame>(bb.m_bb_index * cbuf_size_aligned_v<CBufFrame>) = cb0;
		}
		// Bind the constants to the pipeline
		auto address = m_shader.m_cbuf_frame->GetGPUVirtualAddress() + bb.m_bb_index * cbuf_size_aligned_v<CBufFrame>;
		cmd_list->SetGraphicsRootConstantBufferView(CBufFrame::shader_register, address);
		

		// Draw each element in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			// Something not rendering?
			//  - Check the tint for the nugget isn't 0x00000000.
			// Tips:
			//  - To uniquely identify an instance in a shader for debugging, set the Instance Id (cb1.m_flags.w)
			//    Then in the shader, use: if (m_flags.w == 1234) ...
			auto const& nugget = *dle.m_nugget;
			auto desc = m_default_pipe_state;

			// Set pipeline state
			desc.PrimitiveTopologyType = To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(nugget.m_topo);
			cmd_list->IASetPrimitiveTopology(To<D3D12_PRIMITIVE_TOPOLOGY>(nugget.m_topo));
			cmd_list->IASetVertexBuffers(0U, 1U, &nugget.m_model->m_vb_view);
			cmd_list->IASetIndexBuffer(&nugget.m_model->m_ib_view);
			// todo apply nugget BS,RS,DS

			// Set the per-nugget constants
			CBufNugget cb1 = {};
			SetModelFlags(cb1, *dle.m_instance, nugget, scn());
			SetTxfm(cb1, *dle.m_instance, scn().m_cam);
			SetTint(cb1, *dle.m_instance, nugget);
			SetEnvMap(cb1, *dle.m_instance, nugget);
			SetTexDiffuse(cb1, nugget);
			{
				// Copy 'cb1' to 'm_shader.m_cbuf_nugget[bb_index]'
				MapResource map(m_shader.m_cbuf_nugget.get(), 0U, cbuf_size_aligned_v<CBufNugget>);
				map.at<CBufNugget>(bb.m_bb_index * cbuf_size_aligned_v<CBufNugget>) = cb1;
			}
			// Bind the constants to the pipeline
			auto address = m_shader.m_cbuf_nugget->GetGPUVirtualAddress() + bb.m_bb_index * cbuf_size_aligned_v<CBufNugget>;
			cmd_list->SetGraphicsRootConstantBufferView(CBufNugget::shader_register, address);

			// Draw the nugget **** 

			// Render solid or wireframe nuggets
			if (nugget.m_fill_mode == EFillMode::Default ||
				nugget.m_fill_mode == EFillMode::Solid ||
				nugget.m_fill_mode == EFillMode::Wireframe ||
				nugget.m_fill_mode == EFillMode::SolidWire)
			{
				cmd_list->SetPipelineState(wnd().PipeState(desc));
				if (nugget.m_irange.empty())
				{
					cmd_list->DrawInstanced(
						s_cast<UINT>(nugget.m_vrange.size()), 1U,
						s_cast<UINT>(nugget.m_vrange.m_beg), 0U);
				}
				else
				{
					cmd_list->DrawIndexedInstanced(
						s_cast<UINT>(nugget.m_irange.size()), 1U,
						s_cast<UINT>(nugget.m_irange.m_beg), 0, 0U);
				}
			}

			// Render wire frame over solid for 'SolidWire' mode
			if (!nugget.m_irange.empty() && 
				nugget.m_fill_mode == EFillMode::SolidWire && (
				nugget.m_topo == ETopo::TriList ||
				nugget.m_topo == ETopo::TriListAdj ||
				nugget.m_topo == ETopo::TriStrip ||
				nugget.m_topo == ETopo::TriStripAdj))
			{
				auto fill_mode = desc.RasterizerState.FillMode;
				desc.RasterizerState.FillMode = D3D12_FILL_MODE_WIREFRAME;
				desc.BlendState.RenderTarget[0].BlendEnable = FALSE;
				cmd_list->SetPipelineState(wnd().PipeState(desc));

				cmd_list->DrawIndexedInstanced(
					s_cast<UINT>(nugget.m_irange.size()), 1U,
					s_cast<UINT>(nugget.m_irange.m_beg), 0, 0U);

				desc.RasterizerState.FillMode = fill_mode;
				//ps.Clear(EPipeState::BlendEnable0);
			}

			// Render points for 'Points' mode
			if (nugget.m_fill_mode == EFillMode::Points)
			{
				desc.PrimitiveTopologyType = To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(ETopo::PointList);
				desc.GS = scn().m_diag.m_gs_fillmode_points->Code.GS;
				scn().m_diag.m_gs_fillmode_points->Setup();
				cmd_list->SetPipelineState(wnd().PipeState(desc));

				cmd_list->DrawInstanced(
					s_cast<UINT>(nugget.m_vrange.size()), 1U,
					s_cast<UINT>(nugget.m_vrange.m_beg), 0U);
			}
		}
	}
}
