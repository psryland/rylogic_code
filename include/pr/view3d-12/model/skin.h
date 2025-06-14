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
		struct BoneWeight { int bone; float weight; };

		// Supports up to 4 influences per vertex
		iv4 m_bones;
		v4 m_weights;

		BoneWeight get(int i) const
		{
			return { m_bones[i], m_weights[i] };
		}
		void set(int i, BoneWeight influence)
		{
			m_bones.arr[i] = influence.bone;
			m_weights.arr[i] = influence.weight;
		}
	};

	// Data required to skin a mesh
	struct Skin
	{
		// See description in "animation.h"
		D3DPtr<ID3D12Resource> m_res; // Buffer of 'Skinfluence[]'
		Descriptor m_srv;             // SRV of the skin influence buffer
		uint64_t m_skel_id;           // The skeleton that this skin is matched with.

		Skin();
		Skin(ResourceFactory& factory, std::span<Skinfluence const> verts, uint64_t skel_id);

		// True if 'has skin'
		explicit operator bool() const
		{
			return m_res != nullptr;
		}
	};
}
