//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/animation.h"
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
		TimeRange m_time_range;       // The time span from the animation to use
		double m_time0;               // The animation time last applied
		double m_time1;               // The animation time to display next
		double m_stretch;             // Playback speed multiplier
		EAnimStyle m_style;           // The style of animation
		EAnimFlags m_flags;           // Behaviour flags

		Pose(ResourceFactory& factory, SkeletonPtr skeleton, AnimatorPtr animator, EAnimStyle style, EAnimFlags flags, TimeRange time_range, double stretch);

		// The root bone transform in animation space at 'time'
		m4x4 RootToAnim(double time, EAnimFlags flags) const;
		m4x4 RootToAnim() const;

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
