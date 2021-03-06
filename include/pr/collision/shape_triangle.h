﻿//*********************************************
// Collision
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/collision/shape.h"
#include "pr/geometry/closest_point.h"

namespace pr::collision
{
	struct ShapeTriangle
	{
		Shape m_base;
		m4x4  m_v; // <x,y,z> = verts of the triangle, w = normal. Cross3(w, y-x) should point toward the interior of the triangle

		ShapeTriangle() = default;
		ShapeTriangle(v4_cref<> a, v4_cref<> b, v4_cref<> c, m4_cref<> shape_to_model = m4x4Identity, MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
			:m_base(EShape::Triangle, sizeof(ShapeTriangle), shape_to_model, material_id, flags)
			,m_v(a, b, c, Normalise(Cross3(b-a,c-b)))
		{
			assert(a.w == 0.0f && b.w == 0.0f && c.w == 0.0f);
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
	static_assert(is_shape<ShapeTriangle>::value, "");

	// Return the bounding box for a sphere shape
	inline BBox CalcBBox(ShapeTriangle const& shape)
	{
		auto bbox = BBox::Reset();
		Grow(bbox, shape.m_v.x);
		Grow(bbox, shape.m_v.y);
		Grow(bbox, shape.m_v.z);
		return shape.m_base.m_s2p * bbox;
	}

	// Shift the centre of a triangle
	inline void ShiftCentre(ShapeTriangle& shape, v4 const& shift)
	{
		assert(shift.w == 0.0f);
		if (FEql(shift, v4Zero)) return;
		shape.m_v.x -= shift;
		shape.m_v.y -= shift;
		shape.m_v.z -= shift;
		shape.m_base.m_s2p.pos += shift;
	}

	// Return a support vertex for a triangle
	inline v4 SupportVertex(ShapeTriangle const& shape, v4_cref<> direction, int, int& sup_vert_id)
	{
		auto d = v4{
			Dot3(direction, shape.m_v.x),
			Dot3(direction, shape.m_v.y),
			Dot3(direction, shape.m_v.z),
			0};

		sup_vert_id = MaxElementIndex(d.xyz);
		return shape.m_v[(int)sup_vert_id];
	}

	// Find the nearest point and distance from a point to a shape. 'shape' and 'point' are in the same space
	inline void ClosestPoint(ShapeTriangle const& shape, v4_cref<> point, float& distance, v4& closest)
	{
		closest = ClosestPoint_PointToTriangle(point, shape.m_v.x, shape.m_v.y, shape.m_v.z);
		distance = Length(point - closest);
	}
}