//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/skinning.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	Skinning::Skinning(Model* model, int bone_count, int vert_count, ID3D12Resource* skel, ID3D12Resource* skin)
		: m_model(model)
		, m_skel(skel, true)
		, m_skin(skin, true)
		, m_srv_skel()
		, m_srv_skin()
		, m_bone_count(bone_count)
		, m_vert_count(vert_count)
	{
		ResourceStore::Access store(rdr());

		// Create the skeleton SRV
		{
			D3D12_SHADER_RESOURCE_VIEW_DESC srv_desc = {
				.Format = DXGI_FORMAT::DXGI_FORMAT_UNKNOWN,
				.ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_BUFFER,
				.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
				.Buffer = {
					.FirstElement = 0,
					.NumElements = s_cast<UINT>(m_bone_count),
					.StructureByteStride = sizeof(m4x4),
					.Flags = D3D12_BUFFER_SRV_FLAGS::D3D12_BUFFER_SRV_FLAG_NONE,
				},
			};
			m_srv_skel = store.Descriptors().Create(skel, srv_desc);
		}

		// Create the skin SRV
		{
			D3D12_SHADER_RESOURCE_VIEW_DESC srv_desc = {
				.Format = DXGI_FORMAT::DXGI_FORMAT_UNKNOWN,
				.ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_BUFFER,
				.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
				.Buffer = {
					.FirstElement = 0,
					.NumElements = s_cast<UINT>(m_vert_count),
					.StructureByteStride = sizeof(Skinfluence),
					.Flags = D3D12_BUFFER_SRV_FLAGS::D3D12_BUFFER_SRV_FLAG_NONE,
				},
			};
			m_srv_skin = store.Descriptors().Create(skin, srv_desc);
		}
	}

	// Renderer access
	Renderer const& Skinning::rdr() const
	{
		return m_model->rdr();
	}
	Renderer& Skinning::rdr()
	{
		return m_model->rdr();
	}

	// Ref-counting clean up function
	void Skinning::RefCountZero(RefCounted<Skinning>* doomed)
	{
		auto skin = static_cast<Skinning*>(doomed);
		rdr12::Delete<Skinning>(skin);
	}
}
