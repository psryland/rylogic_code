//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader_registers.h"

namespace pr::rdr12
{
	template <typename EParam, typename ESamp>
	requires (std::is_enum_v<EParam> && std::is_enum_v<ESamp>)
	struct RootSig
	{
		// Notes:
		//  - This type helps build a root signature
		pr::vector<D3D12_ROOT_PARAMETER1, 16, false> m_root_params;
		pr::vector<D3D12_STATIC_SAMPLER_DESC, 8, false> m_static_samplers;
		pr::deque<D3D12_DESCRIPTOR_RANGE1, 16> m_des_range;
		D3D12_ROOT_SIGNATURE_FLAGS Flags;

		RootSig()
			:m_root_params()
			,m_des_range()
			,m_static_samplers()
			,Flags(
				D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
				//D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
				//D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
				//D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
				//D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
				//D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS	|
				D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_NONE)
		{}

		// Add a u32 constant parameter
		void U32(EParam index, ECBufReg reg, int num_values, D3D12_SHADER_VISIBILITY shader_visibility = D3D12_SHADER_VISIBILITY_ALL)
		{
			get(index) = D3D12_ROOT_PARAMETER1{
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,
				.Constants = {.ShaderRegister = s_cast<UINT>(reg), .RegisterSpace = 0U, .Num32BitValues = s_cast<UINT>(num_values)},
				.ShaderVisibility = shader_visibility,
			};
		}

		// Add a constant buffer parameter
		void CBuf(EParam index, ECBufReg reg, D3D12_SHADER_VISIBILITY shader_visibility = D3D12_SHADER_VISIBILITY_ALL)
		{
			get(index) = D3D12_ROOT_PARAMETER1 {
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
				.Descriptor = {.ShaderRegister = s_cast<UINT>(reg), .RegisterSpace = 0U},
				.ShaderVisibility = shader_visibility,
			};
		}

		// Add a texture descriptor range parameter
		void Tex(EParam index, ETexReg reg, int count = 1, D3D12_SHADER_VISIBILITY shader_visibility = D3D12_SHADER_VISIBILITY_ALL, D3D12_DESCRIPTOR_RANGE_FLAGS flags = D3D12_DESCRIPTOR_RANGE_FLAG_NONE)
		{
			m_des_range.push_back(D3D12_DESCRIPTOR_RANGE1 {
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
				.NumDescriptors = s_cast<UINT>(count),
				.BaseShaderRegister = s_cast<UINT>(reg),
				.RegisterSpace = 0U,
				.Flags = flags,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			});
			get(index) = D3D12_ROOT_PARAMETER1 {
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {.NumDescriptorRanges = 1U, .pDescriptorRanges = &m_des_range.back()},
				.ShaderVisibility = shader_visibility,
			};
		}

		// Add a Unordered access view descriptor range parameter
		void Uav(EParam index, EUAVReg reg, int count = 1, D3D12_SHADER_VISIBILITY shader_visibility = D3D12_SHADER_VISIBILITY_ALL, D3D12_DESCRIPTOR_RANGE_FLAGS flags = D3D12_DESCRIPTOR_RANGE_FLAG_NONE)
		{
			m_des_range.push_back(D3D12_DESCRIPTOR_RANGE1{
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_UAV,
				.NumDescriptors = s_cast<UINT>(count),
				.BaseShaderRegister = s_cast<UINT>(reg),
				.RegisterSpace = 0U,
				.Flags = flags,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			});
			get(index) = D3D12_ROOT_PARAMETER1{
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {.NumDescriptorRanges = 1U, .pDescriptorRanges = &m_des_range.back()},
				.ShaderVisibility = shader_visibility,
			};
		}

		// Add a sampler descriptor range parameter
		void Samp(EParam index, ESamReg reg, int count = 1, D3D12_SHADER_VISIBILITY shader_visibility = D3D12_SHADER_VISIBILITY_ALL, D3D12_DESCRIPTOR_RANGE_FLAGS flags = D3D12_DESCRIPTOR_RANGE_FLAG_NONE)
		{
			m_des_range.push_back(D3D12_DESCRIPTOR_RANGE1{
				.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SAMPLER,
				.NumDescriptors = s_cast<UINT>(count),
				.BaseShaderRegister = s_cast<UINT>(reg),
				.RegisterSpace = 0U,
				.Flags = flags,
				.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
			});
			get(index) = D3D12_ROOT_PARAMETER1{
				.ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
				.DescriptorTable = {.NumDescriptorRanges =  1U, .pDescriptorRanges = &m_des_range.back()},
				.ShaderVisibility = shader_visibility,
			};
		}

		// Add a static sampler
		void Samp(ESamp index, D3D12_STATIC_SAMPLER_DESC const& desc)
		{
			get(index) = desc;
		}

		// Compile the shader signature
		D3DPtr<ID3D12RootSignature> Create(ID3D12Device* device)
		{
			// Create the root signature
			D3D12_VERSIONED_ROOT_SIGNATURE_DESC rs_desc = {
				.Version = D3D_ROOT_SIGNATURE_VERSION_1_1,
				.Desc_1_1 = {
					.NumParameters = s_cast<UINT>(m_root_params.size()),
					.pParameters = m_root_params.data(),
					.NumStaticSamplers = s_cast<UINT>(m_static_samplers.size()),
					.pStaticSamplers = m_static_samplers.data(),
					.Flags = Flags,
				},
			};
			
			D3DPtr<ID3DBlob> signature, error;
			Throw(D3D12SerializeVersionedRootSignature(&rs_desc, &signature.m_ptr, &error.m_ptr));
			
			D3DPtr<ID3D12RootSignature> shader_sig;
			Throw(device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), __uuidof(ID3D12RootSignature), (void**)&shader_sig.m_ptr));
			return shader_sig;
		}

	private:
		
		// Index access to parameters 
		D3D12_ROOT_PARAMETER1& get(EParam index)
		{
			auto i = s_cast<int>(index);
			if (i >= s_cast<int>(m_root_params.size()))
				m_root_params.resize(i + 1);
			
			return m_root_params[i];
		}

		// Index access to static samplers
		D3D12_STATIC_SAMPLER_DESC& get(ESamp index)
		{
			auto i = s_cast<int>(index);
			if (i >= s_cast<int>(m_static_samplers.size()))
				m_static_samplers.resize(i + 1);
			
			return m_static_samplers[i];
		}
	};
}
