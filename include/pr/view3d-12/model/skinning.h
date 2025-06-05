//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/animator.h"
#include "pr/view3d-12/model/skeleton.h"
#include "pr/view3d-12/resource/descriptor.h"
#include "pr/view3d-12/utility/update_resource.h"

namespace pr::rdr12
{
	// Influence data for a single vertex in a mesh
	struct Skinfluence
	{
		// Supports up to 4 influences per vertex
		iv4 m_bones;
		v4 m_weights;
	};

	// Data required to skin a mesh
	struct Skinning : RefCounted<Skinning>
	{
		// Notes:
		//  - Each Skinning instance contains the runtime state for a skeleton
		//    and the bones/weights that influence each vertex of the model.
		//  - Typically a single model will have a Skinning instance, although it is
		//    possible to have a Skinning instance per 'Instance', allowing multiple
		//    instances of the animated model at different animation times.
		//  - An Animator is used to update the Skin's bone transforms. If a Skinning
		//    doesn't have an animator, the skin defaults to the skeleton's rest pose.

		AnimatorPtr m_animator;        // The driver of the animation
		SkeletonPtr m_skeleton;        // The skeleton (in rest-pose)
		D3DPtr<ID3D12Resource> m_skel; // The runtime bone buffer (i.e. m4x4[])
		D3DPtr<ID3D12Resource> m_skin; // Buffer of 'Skinfluence[]'
		Descriptor m_srv_skel;         // SRV of the bone buffer
		Descriptor m_srv_skin;         // SRV of the skin influence buffer
		double m_time0;                // The animation time last applied
		double m_time1;                // The animation time requested for the next render
		int m_bone_count;
		int m_vert_count;

		Skinning(ResourceFactory& factory, std::span<Skinfluence const> verts, SkeletonPtr skeleton, AnimatorPtr animator = nullptr);

		// Set the animation time (in seconds)
		void AnimTime(double time_s);

		// Reset to the rest pose
		void ResetPose(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer);

		// Update the bone transforms
		void Update(GfxCmdList& cmd_list, GpuUploadBuffer& upload_buffer);

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Skinning>* doomed);
	};
}
