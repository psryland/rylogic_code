//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
// A Shape is the basic type used for narrow phase collision.
// It may be a single shape or a collection of child shapes
//
// Notes:
// * Shapes MUST be memory location independent. (i.e. no pointers, byte offsets only)
//	 This is so that can be copied around/saved to file/appended to/etc
// * Shapes must have their origin within the shape. This is a requirement of collision
//	 detection which uses the relative positions of the centres as a starting point for
//	 finding the overlap between objects.
// * Shapes for rigidbodies should be in centre of mass frame
// * Shapes do not contain mass properties but can be used to calculate mass properties.
//   Their sole purpose is for collision detection

#pragma once

#include "pr/common/fmt.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace collision
	{
		// Shape types
		enum class EShape
		{
			// Special value to indicate the shape is a dummy object
			NoShape,

			// Primitive shapes
			Sphere,
			Triangle,
			Box,
			Capsule,
			Cylinder,
			Polytope,

			// Compound shapes
			Array,  // An array of child shapes
			BVTree, // A bounding volume tree of shapes

			Terrain,
		};

		// Shape base. All shapes must have this as their first member. (i.e. POD struct inheritance)
		// In effect, this is a header object for a shape.
		struct Shape
		{
			// Transform from shape space to parent object space (parent may be another shape)
			m4x4 m_s2p;

			// A bounding box for the shape (and its children if it's a composite shape)
			BBox m_bbox;

			// The type of shape this is
			EShape m_type;

			// The size in bytes of this shape and its data
			std::size_t m_size;

			// Flags for the shape. Bitwise OR of EShapeFlags
			uint32 m_flags;

			static Shape make(EShape type, std::size_t size, const m4x4& s2p, uint flags)
			{
				return Shape{s2p, BBoxReset, type, size, flags};
			}
		};

		// Shape traits classes
		template <typename TShape> struct shape_traits {};
		inline char const* ToString(EShape ty)
		{
			switch (ty) {
			default: return "";
			case EShape::NoShape : return "NoShape";
			case EShape::Sphere  : return "Sphere";
			case EShape::Triangle: return "Triangle";
			case EShape::Box     : return "Box";
			case EShape::Capsule : return "Capsule";
			case EShape::Cylinder: return "Cylinder";
			case EShape::Polytope: return "Polytope";
			case EShape::Array   : return "Array";
			case EShape::BVTree  : return "BVTree";
			case EShape::Terrain : return "Terrain";
			}
		}

		// Shape casting helpers
		template <typename T> inline T const* shape_cast(Shape const* shape) { assert(!shape || shape->m_type == shape_traits<T>::eshape && FmtS("Attempting to cast %s to %s", ToString(shape->m_type), ToString(shape_traits<T>::eshape))); return reinterpret_cast<T const*>(shape); }
		template <typename T> inline T*       shape_cast(Shape*       shape) { assert(!shape || shape->m_type == shape_traits<T>::eshape && FmtS("Attempting to cast %s to %s", ToString(shape->m_type), ToString(shape_traits<T>::eshape))); return reinterpret_cast<T*      >(shape); }
		template <typename T> inline T const& shape_cast(Shape const& shape) { return *shape_cast<T>(&shape); }
		template <typename T> inline T&       shape_cast(Shape&       shape) { return *shape_cast<T>(&shape); }


		//// General shape functions
		//Shape*			GetDummyShape		();
		//const char*		GetShapeTypeStr		(EShape shape_type);
		//BBox&	CalcBBox			(Shape const& shape, BBox& bbox);
		//MassProperties& CalcMassProperties	(Shape const& shape, float density, MassProperties& mp);
		//void			ShiftCentre			(Shape& shape, v4& shift);
		//v4				SupportVertex		(Shape const& shape, v4 const& direction, std::size_t hint_vert_id, std::size_t& sup_vert_id);
		//void			ClosestPoint		(Shape const& shape, v4 const& point, float& distance, v4& closest);

		//// Increment a shape pointer
		//template <typename ShapeType> inline ShapeType const* Inc(ShapeType const* p) { return reinterpret_cast<ShapeType const*>(byte_ptr(p) + p->m_base.m_size); }
		//template <typename ShapeType> inline ShapeType*       Inc(ShapeType*       p) { return reinterpret_cast<ShapeType*      >(byte_ptr(p) + p->m_base.m_size); }
		//template <>                   inline Shape const*     Inc(Shape const*     p) { return reinterpret_cast<Shape const*    >(byte_ptr(p) + p->m_size); }
		//template <>                   inline Shape*           Inc(Shape*           p) { return reinterpret_cast<Shape*          >(byte_ptr(p) + p->m_size); }
	}
}

