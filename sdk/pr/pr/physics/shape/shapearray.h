//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_SHAPE_ARRAY_H
#define PR_PHYSICS_SHAPE_ARRAY_H

#include "pr/physics/types/forward.h"
#include "pr/physics/shape/shape.h"

namespace pr
{
	namespace ph
	{
		// Shape array
		struct ShapeArray
		{
			Shape m_base;

			// The number of shapes in the array
			std::size_t m_num_shapes;

			// The header struct is followed by an array of other shape types
			//Shape[m_num_shapes]

			enum { EShapeType = EShape_Array };
			static ShapeArray make(std::size_t num_shapes, std::size_t size_in_bytes, const m4x4& shape_to_model, MaterialId material_id, uint flags) { ShapeArray a; a.set(num_shapes, size_in_bytes, shape_to_model, material_id, flags); return a; }
			ShapeArray&       set (std::size_t num_shapes, std::size_t size_in_bytes, const m4x4& shape_to_model, MaterialId material_id, uint flags);
			operator Shape const&() const { return m_base; }
			operator Shape&()             { return m_base; }

			// use pr::ph::Inc(Shape*) to increment the iterator
			Shape const* begin() const { return reinterpret_cast<Shape const*>(this + 1); }
			Shape*       begin()       { return reinterpret_cast<Shape*      >(this + 1); }
			Shape const* end() const   { return reinterpret_cast<Shape const*>(byte_ptr(this) + m_base.m_size); }
			Shape*       end()         { return reinterpret_cast<Shape*      >(byte_ptr(this) + m_base.m_size); }
		};

		// Shape functions
		BBox& CalcBBox(ShapeArray const& shape, BBox& bbox);
	}
}

#endif
