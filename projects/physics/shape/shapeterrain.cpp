//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/shape/shapeterrain.h"
#include "pr/physics/shape/shape.h"

using namespace pr;
using namespace pr::ph;

// Construct a shape array
ShapeTerrain& ShapeTerrain::set(ITerrain* terrain, const m4x4& shape_to_model, MaterialId material_id, uint flags)
{
	m_base.set(EShape_Terrain, sizeof(ShapeTerrain), shape_to_model, material_id, flags);
	m_terrain = terrain;
	return *this;
}
