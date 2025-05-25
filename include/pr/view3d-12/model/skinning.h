//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/descriptor.h"

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
		Model* m_model;                // The model that owns this skinning data
		D3DPtr<ID3D12Resource> m_skel; // The skeleton (i.e. buffer of bone matrices m4x4[])
		D3DPtr<ID3D12Resource> m_skin; // Buffer of SkinInfluence[]
		Descriptor m_srv_skel;
		Descriptor m_srv_skin;
		int m_bone_count;
		int m_vert_count;

		Skinning(Model* model, int bone_count, int vert_count, ID3D12Resource* skel, ID3D12Resource* skin);

		// Renderer access
		Renderer const& rdr() const;
		Renderer& rdr();

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Skinning>* doomed);
	};
}
