//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/shape/shapearray.h"
#include "pr/physics/shape/shape.h"

using namespace pr;
using namespace pr::ph;

// Construct a shape array
ShapeArray& ShapeArray::set(std::size_t num_shapes, std::size_t size_in_bytes, const m4x4& shape_to_model, MaterialId material_id, uint flags)
{
	m_base.set(EShape_Array, size_in_bytes, shape_to_model, material_id, flags);
	m_num_shapes = static_cast<uint>(num_shapes);
	CalcBBox(*this, m_base.m_bbox);
	return *this;
}

// Calculate the bounding box for the shape.
// Assumes child shape bounding boxes have been set already
BoundingBox& pr::ph::CalcBBox(ShapeArray const& shape, BoundingBox& bbox)
{
	bbox.reset();
	for( Shape const* i = shape.begin(), *i_end = shape.end(); i != i_end; i = pr::ph::Inc(i) )
	{
		Encompass(bbox, i->m_shape_to_model * i->m_bbox);
	}
	return bbox;
}
