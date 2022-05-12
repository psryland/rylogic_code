//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_SHAPE_POLYTOPE_HELPER_H
#define PR_PHYSICS_SHAPE_POLYTOPE_HELPER_H

#include "pr/container/byte_data.h"
#include "pr/physics/types/forward.h"
#include "pr/physics/shape/shapepolytope.h"

namespace pr
{
	namespace ph
	{
		// A helper class for creating a polytope and required vertex and face memory
		struct ShapePolytopeHelper
		{
			ByteCont m_data;

			ShapePolytope const& get() const	{ return *reinterpret_cast<ShapePolytope const*>(&m_data[0]); }
			ShapePolytope&       get()			{ return *reinterpret_cast<ShapePolytope*      >(&m_data[0]); }

			// Use an array of verts to create a polytope.
			ShapePolytope& set(v4 const* verts, std::size_t num_verts, m4x4 const& shape_to_model, MaterialId material_id, uint32_t flags);

			// Use an array of verts and faces to create a polytope. Verts and faces must be convex
			ShapePolytope& set(v4 const* verts, std::size_t num_verts, ShapePolyFace const* faces, std::size_t num_faces, m4x4 const& shape_to_model, MaterialId material_id, uint32_t flags);
		};
	}
}

#endif
