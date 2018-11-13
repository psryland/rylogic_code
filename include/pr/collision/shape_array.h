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
		// Shape array
		struct ShapeArray
		{
			Shape m_base;

			// The number of shapes in the array
			size_t m_num_shapes;

			// This 'header struct' is followed by an array of other shape types
			// TShape[m_num_shapes]

			ShapeArray() = default;
			ShapeArray(size_t num_shapes, size_t size_in_bytes, m4_cref<> shape_to_model = m4x4Identity, MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
				:m_base(EShape::Array, size_in_bytes, shape_to_model, material_id, flags)
				,m_num_shapes(num_shapes)
			{
				// Careful: We can't be sure of what follows this object in memory.
				// The shapes that belong to this array may not be there yet.
			}
			operator Shape const&() const
			{
				return m_base;
			}
			operator Shape&()
			{
				return m_base;
			}

			// use 'next(Shape*)' to increment the iterator
			Shape const* begin() const { return reinterpret_cast<Shape const*>(this + 1); }
			Shape*       begin()       { return reinterpret_cast<Shape*      >(this + 1); }
			Shape const* end() const   { return reinterpret_cast<Shape const*>(byte_ptr(this) + m_base.m_size); }
			Shape*       end()         { return reinterpret_cast<Shape*      >(byte_ptr(this) + m_base.m_size); }
		};
		static_assert(is_shape<ShapeArray>::value, "");

		// Calculate the bounding box for the shape.
		// Assumes child shape bounding boxes have been set already
		inline BBox CalcBBox(ShapeArray const& shape)
		{
			auto bb = BBoxReset;
			for (Shape const* i = shape.begin(), *i_end = shape.end(); i != i_end; i = next(i))
				Encompass(bb, i->m_s2p * i->m_bbox);
			return bb;
		}
	}
}