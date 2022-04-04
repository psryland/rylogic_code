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
		BufferDesc m_vb;      // The vertex buffer description and initialisation data
		BufferDesc m_ib;      // The index buffer description and initialisation data
		BBox       m_bbox;    // Model space bounding box
		wstring32  m_name;    // Debugging name for the model

		ModelDesc()
			:m_vb()
			,m_ib()
			,m_bbox(BBox::Reset())
			,m_name()
		{}

		// Construct using a set number of verts and indices
		ModelDesc(BufferDesc const& vb, BufferDesc const& ib, BBox const& bbox = BBox::Reset(), wchar_t const* name = L"")
			:m_vb(vb)
			,m_ib(ib)
			,m_bbox(bbox)
			,m_name(name)
		{}

		// Construct the model buffer with typical defaults
		template <typename Vert, size_t VSize, typename Indx, size_t ISize>
		ModelDesc(Vert const (&vert)[VSize], Indx const (&idxs)[ISize], BBox const& bbox = BBox::Reset(), wchar_t const* name = L"")
			:m_vb(BufferDesc::VBuf<Vert>(VSize, &vert[0]))
			,m_ib(BufferDesc::IBuf<Indx>(ISize, &idxs[0]))
			,m_bbox(bbox)
			,m_name(name)
		{}
	};
}
