﻿//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/skeleton.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	Skeleton::Skeleton(uint32_t id, std::span<uint32_t const> bone_ids, std::span<string32 const> names, std::span<m4x4 const> o2bp, std::span<uint8_t const> hierarchy)
		: m_id(id)
		, m_bone_ids(bone_ids)
		, m_names(names)
		, m_o2bp(o2bp)
		, m_hierarchy(hierarchy)
	{
	}

	// The ID of the root bone
	uint32_t Skeleton::Id() const
	{
		return m_id;
	}

	// The number of bones in this skeleton
	int Skeleton::BoneCount() const
	{
		return isize(m_bone_ids);
	}

	// Ref-counting clean up function
	void Skeleton::RefCountZero(RefCounted<Skeleton>* doomed)
	{
		auto skel = static_cast<Skeleton*>(doomed);
		rdr12::Delete(skel);
	}
}
