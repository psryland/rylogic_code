//*********************************************
// Collision
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/collision/shape.h"

namespace pr
{
	namespace collision
	{
		// A sphere collision shape
		struct ShapeSphere
		{
			Shape m_base;
			float m_radius;
			bool  m_hollow;

			ShapeSphere() = default;
			ShapeSphere(float radius, m4_cref<> shape_to_model = m4x4Identity, bool hollow = false, MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
				:m_base(EShape::Sphere, sizeof(ShapeSphere), shape_to_model, material_id, flags)
				,m_radius(radius)
				,m_hollow(hollow)
			{
				m_base.m_bbox = CalcBBox(*this);
			}
			operator Shape const&() const
			{
				return m_base;
			}
			operator Shape&()
			{
				return m_base;
			}
			operator Shape const*() const
			{
				return &m_base;
			}
			operator Shape*()
			{
				return &m_base;
			}
		};
		static_assert(is_shape<ShapeSphere>::value, "");

		// Return the bounding box for a sphere shape (in parent space)
		inline BBox CalcBBox(ShapeSphere const& shape)
		{
			return BBox(shape.m_base.m_s2p.pos, v4(shape.m_radius, shape.m_radius, shape.m_radius, 0.0f));
		}

		// Shift the centre of a sphere
		inline void ShiftCentre(ShapeSphere&, v4& shift)
		{
			assert("impossible to shift the centre of an implicit object" && FEql(shift, v4Zero));
			(void)shift; 
		}

		// Return a support vertex for a sphere
		inline v4 SupportVertex(ShapeSphere const& shape, v4_cref<> direction, int, int& sup_vert_id)
		{
			// We need to quantise the normal otherwise the iterative algorithms perform badly
			auto dir = Normalise(direction);

			// Generate an id for the vertex in this direction
			sup_vert_id =
				static_cast<size_t>((dir.x + 1.0f) * 0.5f * (1 << 4)) << 20 |
				static_cast<size_t>((dir.y + 1.0f) * 0.5f * (1 << 4)) << 10 |
				static_cast<size_t>((dir.z + 1.0f) * 0.5f * (1 << 4)) << 0;

			return dir * shape.m_radius + v4Origin;
		}

		// Find the nearest point and distance from a point to a shape. 'shape' and 'point' are in the same space
		inline void ClosestPoint(ShapeSphere const& shape, v4_cref<> point, float& distance, v4& closest)
		{
			distance  = Length(point);
			closest   = ((shape.m_radius / distance) * point).w1();
			distance -= shape.m_radius;
		}
	}
}