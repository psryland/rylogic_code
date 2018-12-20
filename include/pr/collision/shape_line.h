//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once
#include "pr/collision/shape.h"

namespace pr
{
	namespace collision
	{
		// A Line segment shape
		struct ShapeLine
		{
			Shape m_base;
			float m_radius; // Line is the Z axis, centred on the origin, with length = 2*m_radius

			ShapeLine() = default;
			ShapeLine(float length, m4_cref<> shape_to_model = m4x4Identity, MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
				:m_base(EShape::Line, sizeof(ShapeLine), shape_to_model, material_id, flags)
				,m_radius(length * 0.5f)
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

		// Return the bounding box for a line shape
		inline BBox CalcBBox(ShapeLine const& shape)
		{
			return shape.m_base.m_s2p * BBox(v4Origin, v4(0, 0, shape.m_radius, 0.0f));
		}

		// Shift the centre of a line
		inline void ShiftCentre(ShapeLine&, v4& shift)
		{
			assert("impossible to shift the centre of an implicit object" && FEql3(shift, v4Zero));
			(void)shift; 
		}

		// Return a support vertex for a line
		inline v4 SupportVertex(ShapeLine const& shape, v4_cref<> direction, int, int& sup_vert_id)
		{
			sup_vert_id = direction.z >= 0;
			return v4(0, 0, Sign(direction.z) * shape.m_radius, 1);
		}

		// Find the nearest point and distance from a point to a shape. 'shape' and 'point' are in the same space
		inline void ClosestPoint(ShapeLine const& shape, v4_cref<> point, float& distance, v4& closest)
		{
			if (Abs(point.z) < shape.m_radius)
			{
				closest = v4(0, 0, point.z, 1);
				distance = Len2(point.x, point.y);
			}
			else
			{
				closest = v4(0, 0, Sign(point.z) * shape.m_radius, 1);
				distance = Length3(point - closest);
			}
		}
	}
}
