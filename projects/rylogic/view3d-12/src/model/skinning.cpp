//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/skinning.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/skeleton.h"
#include "pr/view3d-12/model/animator.h"
#include "pr/view3d-12/utility/utility.h"
#include "pr/view3d-12/resource/resource_factory.h"
#include "pr/view3d-12/resource/resource_store.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	Skinning::Skinning(ResourceFactory& factory, std::span<Skinfluence const> verts, SkeletonPtr skeleton, AnimatorPtr animator)
		: m_animator(animator)
		, m_skeleton(skeleton)
		, m_skel(factory.CreateResource(ResDesc::Buf<m4x4>(isize(skeleton->m_bones), skeleton->m_bones).usage(EUsage::UnorderedAccess), "skel"))
		, m_skin(factory.CreateResource(ResDesc::Buf<Skinfluence>(isize(verts), verts).usage(EUsage::UnorderedAccess), "skin"))
		, m_srv_skel()
		, m_srv_skin()
		, m_time0(-1.0)
		, m_time1(-1.0)
		, m_bone_count(isize(m_skeleton->m_bones))
		, m_vert_count(isize(verts))
	{
		ResourceStore::Access store(factory.rdr());

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
			m_srv_skel = store.Descriptors().Create(m_skel.get(), srv_desc);
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
			m_srv_skin = store.Descriptors().Create(m_skin.get(), srv_desc);
		}
	}
	
	// Set the animation time (in seconds)
	void Skinning::AnimTime(double time_s)
	{
		m_time1 = time_s;
	}

	// Reset to the rest pose
	void Skinning::ResetPose(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer)
	{
		auto update = UpdateSubresourceScope(cmd_list, upload_buffer, m_skel.get(), alignof(m4x4), 0, m_bone_count * sizeof(m4x4));
		memcpy(update.ptr<m4x4>(), m_skeleton->m_bones.data(), m_skeleton->m_bones.size() * sizeof(m4x4));
		update.Commit();
	}

	// Update the bone transforms
	void Skinning::Update(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer)
	{
		// No change in time, assume up to date already
		if (m_time0 == m_time1)
			return;

		// No animator, return to the rest pose
		if (m_animator == nullptr)
		{
			ResetPose(cmd_list, upload_buffer);
			return;
		}

		// Ask the animator to populate the bone transforms
		auto update = UpdateSubresourceScope(cmd_list, upload_buffer, m_skel.get(), alignof(m4x4), 0, m_bone_count * sizeof(m4x4));
		m_animator->Animate({ update.ptr<m4x4>(), s_cast<size_t>(m_bone_count) }, m_time1);
		m_time0 = m_time1;
		update.Commit();
	}

	// Ref-counting clean up function
	void Skinning::RefCountZero(RefCounted<Skinning>* doomed)
	{
		auto skin = static_cast<Skinning*>(doomed);
		rdr12::Delete<Skinning>(skin);
	}
}
