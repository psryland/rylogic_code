//*********************************************
// Collision
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

// A Shape is the basic type used for narrow phase collision.
// It may be a single shape or a collection of child shapes.
// In collision detection, collision pairs that are returned from the broad
// phase are passed to the collision dispatcher which creates a collision agent
// containing the appropriate narrow phase collision detection function.
//
// Notes:
// * Shapes MUST be memory location independent. (i.e. no pointers, byte offsets only)
//	 This is so they can be copied around/saved to file/appended to/etc
// * Shapes must have their origin within the shape. This is a requirement of collision
//	 detection which uses the relative positions of the centres as a starting point for
//	 finding the overlap between objects.
// * Shapes for rigid bodies should be in centre of mass frame

#include <type_traits>
#include <cassert>
#include "pr/maths/maths.h"
#include "pr/common/cast.h"
#include "pr/common/scope.h"

namespace pr::collision
{
	// New Shape CheckList:
	//  - add entry in 'PR_COLLISION_SHAPES'
	//  - add new shape_type.h file and implement
	//  - add include to shapes.h
	//  - add to support.h
	//  - update collision.h

	// Primitive shape types: x(Name, Composite)
	// These are trivially copyable types that have 'Shape' as the first member.
	// Order is affects the collision detection tri-table.
	#define PR_COLLISION_SHAPES(x)\
		x(Sphere  , false)\
		x(Box     , false)\
		x(Line    , false)\
		x(Triangle, false)\
		x(Polytope, false)\
		x(Array   , true )

	// Forward declare shape types
	#define PR_COLLISION_SHAPE_FORWARD(name, comp) struct Shape##name;
	PR_COLLISION_SHAPES(PR_COLLISION_SHAPE_FORWARD)
	#undef PR_COLLISION_SHAPE_FORWARD

	// Shape type enum
	enum class EShape :int
	{
		// Special value to indicate the shape is a dummy object
		NoShape = -1,

		#define PR_COLLISION_SHAPE_ENUM(name, comp) name,
		PR_COLLISION_SHAPES(PR_COLLISION_SHAPE_ENUM)
		#undef PR_COLLISION_SHAPE_ENUM

		NumberOf,
	};
	inline char const* ToString(EShape shape)
	{
		switch (shape)
		{
			case EShape::NoShape: return "NoShape";
			#define PR_COLLISION_SHAPE_TOSTRING(name, comp) case EShape::name: return #name;
			PR_COLLISION_SHAPES(PR_COLLISION_SHAPE_TOSTRING)
			#undef PR_COLLISION_SHAPE_TOSTRING
			default: assert("Unknown shape type" && false); return "Unknown";
		}
	}

	// Other forwards
	struct Ray;

	// Physics material
	using MaterialId = unsigned int;

	// Shape base. All shapes must have this as their first member
	struct Shape
	{
		enum class EFlags
		{
			None                       = 0,
			WholeShapeTerrainCollision = 1 << 0, // Pass the whole shape to the terrain collision function
			_bitwise_operators_allowed,
		};

		// Transform from shape space to parent shape space (or physics model space for root objects)
		m4x4 m_s2p;

		// A bounding box for the shape (and its children if it's a composite shape)
		BBox m_bbox;

		// The type of shape this is. One of EShape
		EShape m_type;

		// The physics material that this shape is made out of
		MaterialId m_material_id;

		// Flags for the shape. Bitwise OR of EShapeFlags
		EFlags m_flags;

		// The size in bytes of this shape and its data
		size_t m_size;

		Shape() = default;
		Shape(EShape type, size_t size, m4_cref<> shape_to_model = m4x4Identity, MaterialId material_id = 0, EFlags flags = EFlags::None)
			:m_s2p(shape_to_model)
			,m_bbox(BBox::Reset())
			,m_type(type)
			,m_material_id(material_id)
			,m_flags(flags)
			,m_size(size)
		{}
	};

	// Traits
	template <typename T> struct is_shape :std::false_type {};
	template <> struct is_shape<Shape> :std::true_type
	{
		static EShape const shape_type = EShape::NoShape;
		static bool const composite = false;
	};
	#define PR_COLLISION_SHAPE_TRAITS(name, comp)\
	template <> struct is_shape<Shape##name> :std::true_type\
	{\
		static constexpr EShape shape_type = EShape::name;\
		static bool const composite = comp;\
	};
	PR_COLLISION_SHAPES(PR_COLLISION_SHAPE_TRAITS)
	#undef PR_COLLISION_SHAPE_TRAITS
	template <typename T> constexpr bool is_shape_v = is_shape<T>::value;
	template <typename T> using enable_if_shape = std::enable_if_t<is_shape_v<T>>;
	template <typename T> using enable_if_composite_shape = std::enable_if_t<is_shape<T>::composite>;
	static_assert(is_shape_v<ShapeSphere>);

	// Shape cast helpers
	template <typename TShape, typename = enable_if_shape<TShape>> inline TShape const& shape_cast(Shape const& shape)
	{
		assert("Invalid shape cast" && shape.m_type == is_shape<TShape>::shape_type);
		return reinterpret_cast<TShape const&>(shape);
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline TShape const* shape_cast(Shape const* shape)
	{
		assert("Invalid shape cast" && (shape == nullptr || shape->m_type == is_shape<TShape>::shape_type));
		return reinterpret_cast<TShape const*>(shape);
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline TShape& shape_cast(Shape& shape)
	{
		assert("Invalid shape cast" && shape.m_type == is_shape<TShape>::shape_type);
		return reinterpret_cast<TShape&>(shape);
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline TShape* shape_cast(Shape* shape)
	{
		assert("Invalid shape cast" && (shape == nullptr || shape->m_type == is_shape<TShape>::shape_type));
		return reinterpret_cast<TShape*>(shape);
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline Shape const& shape_cast(TShape const& shape)
	{
		return shape.m_base;
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline Shape const* shape_cast(TShape const* shape)
	{
		return shape ? &shape->m_base : nullptr;
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline Shape& shape_cast(TShape& shape)
	{
		return shape.m_base;
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline Shape* shape_cast(TShape* shape)
	{
		return shape ? &shape->m_base : nullptr;
	}
	inline Shape const& shape_cast(Shape const& shape)
	{
		return shape;
	}
	inline Shape const* shape_cast(Shape const* shape)
	{
		return shape;
	}
	inline Shape& shape_cast(Shape& shape)
	{
		return shape;
	}
	inline Shape* shape_cast(Shape* shape)
	{
		return shape;
	}
		
	// A constant reference to a shape
	struct ShapeCRef
	{
		Shape const* m_shape;
		ShapeCRef(Shape const* shape) :m_shape(shape) {}
		ShapeCRef(Shape const& shape) :m_shape(&shape) {}
		template <typename TShape, typename = enable_if_shape<TShape>> ShapeCRef(TShape const& shape) :m_shape(&shape.m_base) {}
		template <typename TShape, typename = enable_if_shape<TShape>> ShapeCRef(TShape const* shape) :m_shape(shape.m_base) {}
		Shape const* operator ->() const { return m_shape; }
		Shape const& operator *() const { return *m_shape; }
		operator Shape const*() const { return m_shape; }
		operator Shape const&() const { return *m_shape; }
	};

	// Increment a shape pointer
	// for (Shape const *s = shape.begin(), *s_end = shape.end(); s != s_end; s = next(s)) {}
	template <typename TShape, typename = enable_if_shape<TShape>> inline TShape const* next(TShape const* p)
	{
		return reinterpret_cast<TShape const*>(byte_ptr(p) + p->m_base.m_size);
	}
	template <typename TShape, typename = enable_if_shape<TShape>> inline TShape* next(TShape* p)
	{
		return reinterpret_cast<TShape*>(byte_ptr(p) + p->m_base.m_size);
	}
	template <> inline Shape const* next(Shape const* p)
	{
		return reinterpret_cast<Shape const*>(byte_ptr(p) + p->m_size);
	}
	template <> inline Shape* next(Shape* p)
	{
		return reinterpret_cast<Shape*>(byte_ptr(p) + p->m_size);
	}

	// Forward declare standard shape functions
	template <typename = void> BBox CalcBBox(Shape const& shape);
	template <typename = void> void ShiftCentre(Shape&, v4 const& shift);
	template <typename = void> v4   SupportVertex(Shape const& shape, v4_cref<> direction, int hint_vert_id, int& sup_vert_id);
	template <typename = void> void ClosestPoint(Shape const& shape, v4_cref<> point, float& distance, v4& closest);
}
namespace pr
{
	// Convert a shape enum to a string
	inline char const* ToString(collision::EShape shape)
	{
		return collision::ToString(shape);
	}
}
