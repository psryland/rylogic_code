//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Functions that enable diagnostic features

#pragma once
#include "pr/view3d/forward.h"

namespace pr::rdr
{
	struct DiagState
	{
		float m_normal_lengths;
		Colour32 m_normal_colour;
		bool m_bboxes_visible;           // True if we should draw object bounding boxes

		DiagState();
	};

	// Enable/Disable normals on 'model'. Set length to 0 to disable
	void ShowNormals(Model* model, bool show);

	// Create a scale transform that positions a unit box at 'bbox'
	m4x4 BBoxTransform(BBox const& bbox);
}
