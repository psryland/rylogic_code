﻿//*********************************************
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

		Ids m_ids;             // A unique ID for each bone
		Names m_names;         // A name for each bone (debugging mostly)
		Bones m_o2bp;          // The inverse of the bind-pose to object-space transform for each bone
		Hierarchy m_hierarchy; // Depth-first ordered list of bones. First == root == 0.

		Skeleton(std::span<uint64_t const> ids, std::span<string32 const> names, std::span<m4x4 const> o2bp, std::span<uint8_t const> hierarchy);

		// The ID of the root bone
		uint64_t Id() const;

		// The number of bones in this skeleton
		int BoneCount() const;

		// Walk the skeleton hierarchy calling 'func' for each bone.
		// The caller decides what is pushed to the stack at each level (Ret).
		// 'Func' is Ret Func(int bone_index, Ret const* parent) {...}
		template <typename Ret, std::invocable<int, Ret const*> Func> requires std::same_as<std::invoke_result_t<Func, int, Ret const*>, Ret>
		void WalkHierarchy(Func func) const
		{
			pr::vector<Ret> ancestor = {};
			for (int idx = 0, iend = isize(m_hierarchy); idx != iend; ++idx)
			{
				for (; !ancestor.empty() && m_hierarchy[idx] <= m_hierarchy[idx - 1];)
					ancestor.pop_back();

				auto const* parent = !ancestor.empty() ? &ancestor.back() : nullptr;
				ancestor.push_back(func(idx, parent));
			}
		}

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Skeleton>* doomed);
	};
}
