//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/skeleton.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	Skeleton::Skeleton(std::span<uint64_t const> ids, std::span<string32 const> names, std::span<m4x4 const> bones, std::span<uint8_t const> hierarchy)
		: m_ids(ids)
		, m_names(names)
		, m_bones(bones)
		, m_hierarchy(hierarchy)
	{
	}

	// The ID of the root bone
	uint64_t Skeleton::Id() const
	{
		return m_ids.empty() ? 0 : m_ids.front();
	}

	// Ref-counting clean up function
	void Skeleton::RefCountZero(RefCounted<Skeleton>* doomed)
	{
		auto skel = static_cast<Skeleton*>(doomed);
		rdr12::Delete(skel);
	}
}
