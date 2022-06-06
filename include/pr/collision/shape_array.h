//*********************************************
// Collision
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/collision/shape.h"

namespace pr::collision
{
	// Shape array
	struct ShapeArray
	{
		Shape m_base;

		// The number of shapes in the array
		size_t m_num_shapes;

		// Followed by an array of other shape types (with different sizes):
		// ShapeBox s0;
		// ShapeSphere s1;
		// ...

		ShapeArray() = default;
		ShapeArray(size_t num_shapes, size_t size_in_bytes, m4_cref<> shape_to_model = m4x4::Identity(), MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
			:m_base(EShape::Array, size_in_bytes, shape_to_model, material_id, flags)
			,m_num_shapes(num_shapes)
		{
			// Careful: We can't be sure of what follows this object in memory.
			// The shapes that belong to this array may not be there yet.
			// Differ calculating the bounding box to the caller.
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

		// Access the shapes in the array. Use 'next(Shape*)' to increment the iterator
		Shape const* begin() const { return reinterpret_cast<Shape const*>(this + 1); }
		Shape*       begin()       { return reinterpret_cast<Shape*      >(this + 1); }
		Shape const* end() const   { return reinterpret_cast<Shape const*>(byte_ptr(this) + m_base.m_size); }
		Shape*       end()         { return reinterpret_cast<Shape*      >(byte_ptr(this) + m_base.m_size); }
	};
	static_assert(is_shape_v<ShapeArray>);

	// Calculate the bounding box for the shape.
	inline BBox CalcBBox(ShapeArray const& shape)
	{
		auto bb = BBox::Reset();
		for (Shape const* i = shape.begin(), *i_end = shape.end(); i != i_end; i = next(i))
			Grow(bb, CalcBBox(*i));

		return shape.m_base.m_s2p * bb;
	}
}