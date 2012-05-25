//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_SHAPE_BOX_H
#define PR_PHYSICS_SHAPE_BOX_H

#include "pr/physics/shape/shape.h"

namespace pr
{
	namespace ph
	{
		struct ShapeBox
		{
			Shape	m_base;
			v4		m_radius;

			enum { EShapeType = EShape_Box };
			static ShapeBox	make(v4 const& dim, m4x4 const& shape_to_model, MaterialId material_id, uint flags) { ShapeBox s; s.set(dim, shape_to_model, material_id, flags); return s; }
			ShapeBox&		set (v4 const& dim, m4x4 const& shape_to_model, MaterialId material_id, uint flags);
			operator Shape const&() const	{ return m_base; }
			operator Shape& ()				{ return m_base; }
		};

		// Shape functions
		BoundingBox&	CalcBBox			(ShapeBox const& shape, BoundingBox& bbox);
		MassProperties& CalcMassProperties	(ShapeBox const& shape, float density, MassProperties& mp);
		void			ShiftCentre			(ShapeBox& shape, v4& shift);
		v4				SupportVertex		(ShapeBox const& shape, v4 const& direction, std::size_t hint_vert_id, std::size_t& sup_vert_id);
		void			ClosestPoint		(ShapeBox const& shape, v4 const& point, float& distance, v4& closest);
	}
}

#endif
