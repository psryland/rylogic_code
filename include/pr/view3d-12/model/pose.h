//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/descriptor.h"
#include "pr/view3d-12/utility/cmd_list.h"

namespace pr::rdr12
{
	// A runtime version of a skeleton
	struct Pose : RefCounted<Pose>
	{
		// See description in "animation.h"
		AnimatorPtr m_animator;       // The driver of the animation
		SkeletonPtr m_skeleton;       // The skeleton (in rest-pose)
		D3DPtr<ID3D12Resource> m_res; // The runtime bone buffer (i.e. m4x4[])
		Descriptor m_srv;             // SRV of the bone buffer
		double m_time0;               // The animation time last applied
		double m_time1;               // The animation time to display next

		Pose(ResourceFactory& factory, SkeletonPtr skeleton, AnimatorPtr animator = nullptr);

		// Set the animation time
		void AnimTime(double time_s);

		// Number of bones in this pose
		int BoneCount() const;

		// Reset to the rest pose
		void ResetPose(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer);

		// Update the bone transforms
		void Update(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer);

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Pose>* doomed);
	};
}
