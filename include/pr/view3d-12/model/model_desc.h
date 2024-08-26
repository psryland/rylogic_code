//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	struct ModelDesc
	{
		ResDesc  m_vb;      // The vertex buffer description and initialisation data
		ResDesc  m_ib;      // The index buffer description and initialisation data
		BBox     m_bbox;    // Model space bounding box
		string32 m_name;    // Debugging name for the model

		ModelDesc()
			: m_vb()
			, m_ib()
			, m_bbox(BBox::Reset())
			, m_name()
		{}

		// Construct using a set number of verts and indices
		ModelDesc(ResDesc const& vb, ResDesc const& ib, BBox const& bbox = BBox::Reset(), char const* name = "")
			: m_vb(vb)
			, m_ib(ib)
			, m_bbox(bbox)
			, m_name(name)
		{}

		// Construct the model buffer from spans of verts and indices
		template <typename TVert, typename TIndx>
		ModelDesc(std::span<TVert const> vert, std::span<TIndx const> idxs, BBox const& bbox = BBox::Reset(), char const* name = "")
			: m_vb(ResDesc::VBuf<TVert>(vert.size(), vert))
			, m_ib(ResDesc::IBuf<TIndx>(idxs.size(), idxs))
			, m_bbox(bbox)
			, m_name(name)
		{}

		// Construct the model buffer from static arrays of verts and indices
		template <typename TVert, typename TIndx, size_t VSize, size_t ISize>
		ModelDesc(TVert const (&vert)[VSize], TIndx const (&idxs)[ISize], BBox const& bbox = BBox::Reset(), char const* name = "")
			: m_vb(ResDesc::VBuf<TVert>(VSize, { &vert[0], VSize }))
			, m_ib(ResDesc::IBuf<TIndx>(ISize, { &idxs[0], ISize }))
			, m_bbox(bbox)
			, m_name(name)
		{}

		ModelDesc& name(std::string_view name)
		{
			m_name = name;
			return *this;
		}
	};
}
