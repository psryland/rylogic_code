//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/pose.h"
#include "pr/view3d-12/model/skeleton.h"
#include "pr/view3d-12/model/animator.h"
#include "pr/view3d-12/resource/resource_factory.h"
#include "pr/view3d-12/resource/resource_store.h"
#include "pr/view3d-12/utility/update_resource.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	Pose::Pose(ResourceFactory& factory, SkeletonPtr skeleton, AnimatorPtr animator)
		: m_animator(animator)
		, m_skeleton(skeleton)
		, m_res()
		, m_srv()
		, m_time0(-1.0)
		, m_time1(-1.0)
	{
		ResourceStore::Access store(factory.rdr());

		// Create the buffer for the bone matrices
		ResDesc rdesc = ResDesc::Buf<m4x4>(isize(skeleton->m_bones), {}).def_state(D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE);
		m_res = factory.CreateResource(rdesc, "pose");

		// Create the pose SRV
		D3D12_SHADER_RESOURCE_VIEW_DESC srv_desc = {
			.Format = DXGI_FORMAT::DXGI_FORMAT_UNKNOWN,
			.ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_BUFFER,
			.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
			.Buffer = {
				.FirstElement = 0,
				.NumElements = s_cast<UINT>(BoneCount()),
				.StructureByteStride = sizeof(m4x4),
				.Flags = D3D12_BUFFER_SRV_FLAGS::D3D12_BUFFER_SRV_FLAG_NONE,
			},
		};
		m_srv = store.Descriptors().Create(m_res.get(), srv_desc);
	}

	// Set the animation time
	void Pose::AnimTime(double time_s)
	{
		m_time1 = time_s;
	}

	// Number of bones in this pose
	int Pose::BoneCount() const
	{
		return isize(m_skeleton->m_bones);
	}

	// Reset to the rest pose
	void Pose::ResetPose(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer)
	{
		auto update = UpdateSubresourceScope(cmd_list, upload_buffer, m_res.get(), alignof(m4x4), 0, BoneCount() * sizeof(m4x4));
		for (m4x4 *ptr = update.ptr<m4x4>(), *end = ptr + BoneCount(); ptr != end; ++ptr) *ptr = m4x4::Identity();
		update.Commit();
	}

	// Update the bone transforms
	void Pose::Update(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer)
	{
		// No change in time, assume up to date already
		if (m_time1 == m_time0)
			return;

		// No animator, return to the rest pose
		if (m_animator == nullptr)
		{
			ResetPose(cmd_list, upload_buffer);
			return;
		}

		// Ask the animator to populate the bone transforms
		auto update = UpdateSubresourceScope(cmd_list, upload_buffer, m_res.get(), alignof(m4x4), 0, BoneCount() * sizeof(m4x4));
		{
			// Read the animated bone positions into the buffer to start with.
			// These are rest_pose-to-animated_pose transforms.
			m_animator->Animate({ update.ptr<m4x4>(), s_cast<size_t>(BoneCount()) }, m_time1);
			m_time0 = m_time1;

			// Bone transforms need to take points from object space -> rest-bone space -> animated position space -> object space
			// We need to pre and post multiply from/to object space.
			auto ptr = update.ptr<m4x4>();
			for (int i = 0; i != BoneCount(); ++i, ++ptr)
			{
				// object space to bone space
				auto o2b = InvertFast(m_skeleton->m_bones[i]);
				
				// Bone space to object space
				auto b2o = m_skeleton->m_bones[i];

				*ptr = b2o * *ptr * o2b;
			}
		}
		update.Commit();
	}

	// Ref-counting clean up function
	void Pose::RefCountZero(RefCounted<Pose>* doomed)
	{
		auto pose = static_cast<Pose*>(doomed);
		rdr12::Delete<Pose>(pose);
	}
}
