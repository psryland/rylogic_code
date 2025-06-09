//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/descriptor.h"

namespace pr::rdr12
{
	struct Skeleton: RefCounted<Skeleton>
	{
		// See description in "animation.h"
		using Ids = pr::vector<uint64_t>;
		using Bones = pr::vector<m4x4>;
		using Names = pr::vector<string32>;
		using Hierarchy = pr::vector<uint8_t>;

		Ids m_ids;                     // A unique ID for each bone
		Names m_names;                 // A name for each bone (debugging mostly)
		Bones m_bones;                 // The rest-pose bone transforms (o2p transforms, *not* o2w or o2modelspace)
		Hierarchy m_hierarchy;         // Depth-first ordered list of bones

		Skeleton(std::span<uint64_t const> ids, std::span<string32 const> names, std::span<m4x4 const> bones, std::span<uint8_t const> hierarchy);

		// The ID of the root bone
		uint64_t Id() const;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Skeleton>* doomed);
	};
}
