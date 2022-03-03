//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#include "src/forward.h"
#include "src/commands/cross_section.h"

using namespace pr;
using namespace pr::geometry;

// Clip a model using the given plane
void CrossSection(p3d::Mesh& mesh, v4 const& plane)
{
	(void)mesh,plane;
	throw std::runtime_error("Not implemented");

}

// Clip a model using the given plane
void CrossSection(p3d::File& p3d, v4 const& plane)
{
	for (auto& mesh : p3d.m_scene.m_meshes)
		CrossSection(mesh, plane);
}
