//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
{
	// Model buffer / Model creation settings
	struct MdlSettings
	{
		VBufferDesc m_vb;   // The vertex buffer description plus initialisation data
		IBufferDesc m_ib;   // The index buffer description plus initialisation data
		BBox        m_bbox; // Model space bounding box
		string32    m_name; // Debugging name for the model

		MdlSettings()
			:m_vb()
			,m_ib()
			,m_bbox(BBox::Reset())
			,m_name()
		{}

		// Construct using a set number of verts and indices
		MdlSettings(VBufferDesc const& vb, IBufferDesc const& ib, BBox const& bbox = BBox::Reset(), char const* name = "")
			:m_vb(vb)
			,m_ib(ib)
			,m_bbox(bbox)
			,m_name(name)
		{}

		// Construct the model buffer with typical defaults
		template <class Vert, size_t VSize, typename Indx, size_t ISize>
		MdlSettings(Vert const (&vert)[VSize], Indx const (&idxs)[ISize], BBox const& bbox = BBox::Reset(), char const* name = "")
			:m_vb(VBufferDesc(vert))
			,m_ib(IBufferDesc(idxs))
			,m_bbox(bbox)
			,m_name(name)
		{}

		//// Construct from containers of verts and indices
		//template <typename VCont, typename ICont>
		//MdlSettings(VCont const& vcont, ICont const& icont, BBox const& bbox = BBox::Reset(), char const* name = "")
		//	:m_vb(VBufferDesc(vcont.size(), vcont.data()))
		//	,m_ib(IBufferDesc(icont.size(), icont.data()))
		//	,m_bbox(bbox)
		//	,m_geom(geom)
		//	,m_has_alpha(has_alpha)
		//	,m_name(name)
		//{}
	};
}

