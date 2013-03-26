//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/model_generator.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/util/lock.h"

using namespace pr::rdr;
using namespace pr::rdr::model;

// Line *****************************************************************************************

// Return the model buffer requirements of an array of lines
void pr::rdr::model::LineSize(std::size_t num_lines, Range& v_range, Range& i_range)
{
	v_range.set(0, 2 * num_lines);
	i_range.set(0, 2 * num_lines);
}

//// Return model settings for creating an array of lines
//pr::rdr::MdlSettings pr::rdr::model::LineModelSettings(std::size_t num_lines)
//{
//	Range v_range, i_range;
//	LineSize(v_range, i_range, num_lines);
//
//	MdlSettings settings();
//	settings.m_vertex_type = vf::GetTypeFromGeomType(geom::EVC);
//	settings.m_Vcount      = v_range.size();
//	settings.m_Icount      = i_range.size();
//	return settings;
//}
