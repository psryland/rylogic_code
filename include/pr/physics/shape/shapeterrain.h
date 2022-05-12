//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_SHAPE_TERRAIN_H
#define PR_PHYSICS_SHAPE_TERRAIN_H

#include "pr/physics/shape/shape.h"

namespace pr
{
	namespace ph
	{
		// Shape representing a terrain system
		struct ShapeTerrain
		{
			Shape		m_base;
			ITerrain*	m_terrain;

			enum { EShapeType = EShape_Terrain };
			static ShapeTerrain	make(ITerrain* terrain, const m4x4& shape_to_model, MaterialId material_id, uint32_t flags) { ShapeTerrain t; t.set(terrain, shape_to_model, material_id, flags); return t; }
			ShapeTerrain&		set (ITerrain* terrain, const m4x4& shape_to_model, MaterialId material_id, uint32_t flags);
			operator Shape const&() const	{ return m_base; }
			operator Shape& ()				{ return m_base; }
		};
	}
}

#endif
