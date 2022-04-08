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
#include "pr/view3d-12/utility/wrappers.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12
{
	// include generated header files
	#include PR_RDR_SHADER_COMPILED_DIR(forward_vs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(forward_ps.h)
	//todo #include PR_RDR_SHADER_COMPILED_DIR(forward_radial_fade_ps.h)

	using namespace hlsl::fwd;

	RenderForward::RenderForward(Scene& scene)
		: RenderStep(scene)
	{
		Renderer::Lock lock(scene.rdr());
		auto device = lock.D3DDevice();

		// Create the root signature
		D3DPtr<ID3DBlob> signature, error;
		D3D12_DESCRIPTOR_RANGE diffuse_tex_desc[] = {
			{// t0,0
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = 1U,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t0),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_DESCRIPTOR_RANGE envmap_tex_desc[] = {
			{// t1,0
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = 1U,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t1),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_DESCRIPTOR_RANGE smap_tex_desc[] = {
			{// t2,MaxShadowMaps
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = hlsl::MaxShadowMaps,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t2),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_DESCRIPTOR_RANGE proj_tex_desc[] = {
			{// t3,MaxProjectedTextures
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = hlsl::MaxProjectedTextures,
				.BaseShaderRegister = s_cast<UINT>(ETexReg::t3),
				.RegisterSpace = 0U,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			},
		};
		D3D12_ROOT_PARAMETER params[] = {
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
		D3D12_ROOT_SIGNATURE_DESC rsgdesc =  {
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
		};
		Throw(D3D12SerializeRootSignature(&rsgdesc, D3D_ROOT_SIGNATURE_VERSION_1_0, &signature.m_ptr, &error.m_ptr));
		Throw(device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), __uuidof(ID3D12RootSignature), (void**)&m_root_sig.m_ptr));

		// Create the PSO
		D3D12_GRAPHICS_PIPELINE_STATE_DESC psodesc = {
			.pRootSignature = m_root_sig.get(),
			.VS = { .pShaderBytecode = &forward_vs[0], .BytecodeLength = sizeof(forward_vs), },
			.PS = { .pShaderBytecode = &forward_ps[0], .BytecodeLength = sizeof(forward_ps), },
			.DS = { .pShaderBytecode = nullptr, .BytecodeLength = 0U, },
			.HS = { .pShaderBytecode = nullptr, .BytecodeLength = 0U, },
			.GS = { .pShaderBytecode = nullptr, .BytecodeLength = 0U, },
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
			.DSVFormat = DXGI_FORMAT_D24_UNORM_S8_UINT,
			.SampleDesc = MultiSamp{},
			.NodeMask = 0U,
			.CachedPSO = {
				.pCachedBlob = nullptr,
				.CachedBlobSizeInBytes = 0U,
			},
			.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
		};
		Throw(device->CreateGraphicsPipelineState(&psodesc, __uuidof(ID3D12PipelineState), (void**)&m_pso.m_ptr));

		// Create a descriptor heaps for the constant buffers.
		// Create a constant buffer for each back buffer frame so we can write to one while the other is in flight.
		{//CBufFrame
			auto desc = BufferDesc::CBuf(scene.wnd().BBCount() * cbuf_size_aligned_v<CBufFrame>);
			Throw(device->CreateCommittedResource(
				&HeapProps::Upload(),
				D3D12_HEAP_FLAG_NONE,
				&desc,
				D3D12_RESOURCE_STATE_GENERIC_READ,
				nullptr,
				__uuidof(ID3D12Resource),
				(void**)&m_cbuf_frame));
			Throw(m_cbuf_frame->SetName(L"RenderForward:CBufFrame"));
		}
		{//CBufNugget
			auto desc = BufferDesc::CBuf(scene.wnd().BBCount() * cbuf_size_aligned_v<CBufNugget>);
			Throw(device->CreateCommittedResource(
				&HeapProps::Upload(),
				D3D12_HEAP_FLAG_NONE,
				&desc,
				D3D12_RESOURCE_STATE_GENERIC_READ,
				nullptr,
				__uuidof(ID3D12Resource),
				(void**)&m_cbuf_nugget));
			Throw(m_cbuf_frame->SetName(L"RenderForward:CBufNugget"));
		}
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
		cmd_list->SetPipelineState(m_pso.get());
		cmd_list->SetGraphicsRootSignature(m_root_sig.get());

		// Add a resource barrier for switching the back buffer to the render target state
		D3D12_RESOURCE_BARRIER barrier0 = {
			.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
			.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
			.Transition = {
				.pResource = bb.m_render_target.get(),
				.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
				.StateBefore = D3D12_RESOURCE_STATE_PRESENT,
				.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET,
			},
		};
		cmd_list->ResourceBarrier(1, &barrier0);

		// Get the back buffer view handle and set the back buffer as the render target.
		cmd_list->OMSetRenderTargets(1, &bb.m_rtv, false, nullptr/*&bb.m_dsv*/); //todo

		// Clear the render target to the background colour
		if (m_scene->m_bkgd_colour != ColourZero)
		{
			cmd_list->ClearRenderTargetView(bb.m_rtv, m_scene->m_bkgd_colour.arr, 0, nullptr);
			//todo cmd_list->ClearDepthStencilView(bb.m_dsv, D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0U, 0, nullptr);
		}

		// Set the viewport
		cmd_list->RSSetViewports(1, &m_scene->m_viewport);

		#if 0 // todo
		// Check if shadows are enabled
		auto smap_rstep = m_scene->FindRStep<ShadowMap>();
		StateStack::SmapFrame smap_frame(ss, smap_rstep);
		#endif

		// Set the frame constants
		CBufFrame cb0 = {};
		SetViewConstants(m_scene->m_cam, cb0.m_cam);
		//todo SetLightingConstants(m_scene->m_global_light, m_scene->m_cam, cb0.m_global_light);
		//todo SetShadowMapConstants(smap_rstep, cb0.m_shadow);
		//todo SetEnvMapConstants(m_scene->m_global_envmap.get(), cb0.m_env_map);
		WriteConstants(cb0, m_cbuf_frame.get(), bb.m_bb_index);
		cmd_list->SetGraphicsRootConstantBufferView(0, m_cbuf_frame->GetGPUVirtualAddress() + bb.m_bb_index * cbuf_size_aligned_v<CBufFrame>);

		// Draw each element in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			// Something not rendering?
			//  - Check the tint for the nugget isn't 0x00000000.
			// Tips:
			//  - To uniquely identify an instance in a shader for debugging, set the Instance Id (cb1.m_flags.w)
			//    Then in the shader, use: if (m_flags.w == 1234) ...

			//todo StateStack::DleFrame frame(ss, dle);
			auto const& nugget = *dle.m_nugget;

			// Set pipeline state
			cmd_list->IASetPrimitiveTopology(To<D3D12_PRIMITIVE_TOPOLOGY>(nugget.m_topo));
			cmd_list->IASetVertexBuffers(0U, 1U, &nugget.m_model->VBufView());
			cmd_list->IASetIndexBuffer(&nugget.m_model->IBufView());

			// Set the per-nugget constants
			CBufNugget cb1 = {};
			SetModelFlags(*dle.m_instance, nugget, *m_scene, cb1);
			SetTxfm(*dle.m_instance, m_scene->m_cam, cb1);
			SetTint(*dle.m_instance, nugget, cb1);
			SetEnvMap(*dle.m_instance, nugget, cb1);
			SetTexDiffuse(nugget, cb1);
			WriteConstants(cb1, m_cbuf_nugget.get(), bb.m_bb_index);
			cmd_list->SetGraphicsRootConstantBufferView(1, m_cbuf_nugget->GetGPUVirtualAddress() + bb.m_bb_index * cbuf_size_aligned_v<CBufNugget>);

			// Draw the nugget
			DrawNugget(cmd_list, nugget);//, ss);
		}

		// Transition the back buffer to the presenting state.
		D3D12_RESOURCE_BARRIER barrier1 = {
			.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
			.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
			.Transition = {
				.pResource = bb.m_render_target.get(),
				.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
				.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET,
				.StateAfter = D3D12_RESOURCE_STATE_PRESENT,
			},
		};
		cmd_list->ResourceBarrier(1, &barrier1);
	}

	// Call draw for a nugget
	void RenderForward::DrawNugget(ID3D12GraphicsCommandList* cmd_list, Nugget const& nugget)//todo, StateStack& ss)
	{
		// Render solid or wireframe nuggets
		if (nugget.m_fill_mode == EFillMode::Default ||
			nugget.m_fill_mode == EFillMode::Solid ||
			nugget.m_fill_mode == EFillMode::Wireframe ||
			nugget.m_fill_mode == EFillMode::SolidWire)
		{
			//todo ss.Commit();
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
			//todo m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
			//todo m_bsb.Set(EBS::BlendEnable, FALSE, 0);
			//todo 
			//todo ss.Commit();
			cmd_list->DrawIndexedInstanced(
				s_cast<UINT>(nugget.m_irange.size()), 1U,
				s_cast<UINT>(nugget.m_irange.m_beg), 0, 0U);

			//todo m_rsb.Clear(ERS::FillMode);
			//todo m_bsb.Clear(EBS::BlendEnable, 0);
		}

		// Render points for 'Points' mode
		if (nugget.m_fill_mode == EFillMode::Points)
		{
			//todo ss.m_pending.m_topo = ETopo::PointList;
			//todo ss.m_pending.m_shdrs.m_gs = m_scene->m_diag.m_gs_fillmode_points.get();
			//todo ss.Commit();
			cmd_list->DrawInstanced(
				s_cast<UINT>(nugget.m_vrange.size()), 1U,
				s_cast<UINT>(nugget.m_vrange.m_beg), 0U);
		}
	}

}
