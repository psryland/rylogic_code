//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_SHAPE_SPHERE_H
#define PR_PHYSICS_SHAPE_SPHERE_H

#include "pr/physics/shape/shape.h"

namespace pr
{
	namespace ph
	{
		// Simple sphere
		struct ShapeSphere
		{
			Shape	m_base;
			float	m_radius;

			enum { EShapeType = EShape_Sphere };
			static ShapeSphere make(float radius, const m4x4& shape_to_model, MaterialId material_id, uint flags) { ShapeSphere s; s.set(radius, shape_to_model, material_id, flags); return s; }
			ShapeSphere&       set (float radius, const m4x4& shape_to_model, MaterialId material_id, uint flags);
			operator Shape const&() const { return m_base; }
			operator Shape& ()            { return m_base; }
		};

		// Shape functions
		BBox&           CalcBBox          (ShapeSphere const& shape , BBox& bbox);
		MassProperties& CalcMassProperties(ShapeSphere const& shape , float density, MassProperties& mp);
		void            ShiftCentre       (ShapeSphere& shape       , v4& shift);
		v4              SupportVertex     (ShapeSphere const& shape , v4 const& direction, std::size_t hint_vert_id, std::size_t& sup_vert_id);
		void            ClosestPoint      (ShapeSphere const& shape , v4 const& point, float& distance, v4& closest);
	}
}

#endif
