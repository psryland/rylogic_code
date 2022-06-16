//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once
#include "pr/collision/shape.h"

namespace pr::collision
{
	// A Line segment shape
	struct ShapeLine
	{
		Shape m_base;
		float m_radius; // Line is the Z axis, centred on the origin, with length = 2*m_radius

		explicit ShapeLine(float length, m4_cref<> shape_to_parent = m4x4::Identity(), MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
			:m_base(EShape::Line, sizeof(ShapeLine), shape_to_parent, material_id, flags)
			,m_radius(length * 0.5f)
		{
			m_base.m_bbox = CalcBBox(*this);
		}
		explicit ShapeLine(v4_cref<> a, v4_cref<> b, m4_cref<> shape_to_parent = m4x4::Identity(), MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
			:ShapeLine(Length(b-a), shape_to_parent * m4x4::Transform(b-a, v4::ZAxis(), (a+b)/2.0f), material_id, flags)// checkme
		{}
		
		// Conversion
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
	static_assert(is_shape_v<ShapeLine>);

	// Return the bounding box for a line shape
	template <typename>
	BBox pr_vectorcall CalcBBox(ShapeLine const& shape)
	{
		return BBox(v4::Origin(), v4(0, 0, shape.m_radius, 0.0f));
	}

	// Shift the centre of a line
	template <typename>
	void pr_vectorcall ShiftCentre(ShapeLine&, v4 const& shift)
	{
		assert("impossible to shift the centre of an implicit object" && FEql(shift, v4Zero));
		(void)shift; 
	}

	// Return a support vertex for a line
	template <typename>
	v4 pr_vectorcall SupportVertex(ShapeLine const& shape, v4_cref<> direction, int, int& sup_vert_id)
	{
		sup_vert_id = direction.z >= 0;
		return v4(0, 0, Sign(direction.z) * shape.m_radius, 1);
	}

	// Find the nearest point and distance from a point to a shape. 'shape' and 'point' are in the same space
	template <typename>
	void pr_vectorcall ClosestPoint(ShapeLine const& shape, v4_cref<> point, float& distance, v4& closest)
	{
		if (Abs(point.z) < shape.m_radius)
		{
			closest = v4(0, 0, point.z, 1);
			distance = Len(point.x, point.y);
		}
		else
		{
			closest = v4(0, 0, Sign(point.z) * shape.m_radius, 1);
			distance = Length(point - closest);
		}
	}
}
