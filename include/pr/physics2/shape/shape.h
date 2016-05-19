//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

// A Shape is the basic type used for narrow phase collision.
// It may be a single shape or a collection of child shapes.
// In collision detection, collision pairs that are returned from the broad
// phase are passed to the collision dispatcher which creates a collision agent
// containing the appropriate narrow phase collision detection function.
//
//  +------------------------------+
//  | Shape                        |
//  |  BBox, mass, mass tensor,    |
//  |  inv mass tensor, shape*     |
//  +------------------------------+
//  | transform                    |
//  +------------------------------+
//  | Shape Data                   |
//  +------------------------------+
//
// Notes:
// * Shapes MUST be memory location independent. (i.e. no pointers, byte offsets only)
//	 This is so they can be copied around/saved to file/appended to/etc
// * Shapes must have their origin within the shape. This is a requirement of collision
//	 detection which uses the relative positions of the centres as a starting point for
//	 finding the overlap between objects.
// * Shapes for rigid bodies should be in centre of mass frame

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/mass.h"

namespace pr
{
	namespace physics
	{
		// Primitive shape types
		enum class EShape
		{
			// Primitive shapes
			Sphere,
			Box,
			Polytope,
			Cylinder,
			Triangle,

			// Compound shapes
			Array,  // An array of child shapes
			BVTree, // A bounding volume tree of shapes

			Terrain,
			NumberOf,
			NoShape, // Special value to indicate the shape is a dummy object
		};

		// Forward declare shape types
		struct Shape;
		struct ShapeSphere;
		struct ShapeBox;
		struct ShapePolytope;
		struct ShapeCylinder;
		struct ShapeTriangle;
		struct ShapeArray;

		// Shape base. All shapes must have this as their first member
		struct Shape
		{
			enum class EFlags
			{
				None                       = 0,
				WholeShapeTerrainCollision = 1 << 0, // Pass the whole shape to the terrain collision function
				_bitwise_operators_allowed,
			};

			// Transform from shape space to physics model space (or parent shape space)
			m4x4 m_shape_to_model;

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
			Shape(EShape type, size_t size, m4x4_cref shape_to_model = m4x4Identity, MaterialId material_id = 0, EFlags flags = EFlags::None)
				:m_shape_to_model(shape_to_model)
				,m_bbox(BBoxReset)
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
		template <> struct is_shape<ShapeSphere> :std::true_type
		{
			static EShape const shape_type = EShape::Sphere;
			static bool const composite = false;
		};
		template <> struct is_shape<ShapeBox> :std::true_type
		{
			static EShape const shape_type = EShape::Box;
			static bool const composite = false;
		};
		template <> struct is_shape<ShapePolytope> :std::true_type
		{
			static EShape const shape_type = EShape::Polytope;
			static bool const composite = false;
		};
		template <> struct is_shape<ShapeCylinder> :std::true_type
		{
			static EShape const shape_type = EShape::Cylinder;
			static bool const composite = false;
		};
		template <> struct is_shape<ShapeTriangle> :std::true_type
		{
			static EShape const shape_type = EShape::Triangle;
			static bool const composite = false;
		};
		template <> struct is_shape<ShapeArray> :std::true_type
		{
			static EShape const shape_type = EShape::Array;
			static bool const composite = true;
		};
		template <typename T> using enable_if_shape = typename std::enable_if<is_shape<T>::value>::type;
		template <typename T> using enable_if_composite_shape = typename std::enable_if<is_shape<T>::composite>::type;

		// Shape cast helpers
		template <typename T, typename = enable_if_shape<T>> inline T const& shape_cast(Shape const& shape)
		{
			assert("Invalid shape cast" && shape.m_type == is_shape<T>::shape_type);
			return reinterpret_cast<T const&>(shape);
		}
		template <typename T, typename = enable_if_shape<T>> inline T const* shape_cast(Shape const* shape)
		{
			assert("Invalid shape cast" && (shape == nullptr || shape->m_type == is_shape<T>::shape_type));
			return reinterpret_cast<T const*>(shape);
		}
		template <typename T, typename = enable_if_shape<T>> inline T& shape_cast(Shape& shape)
		{
			assert("Invalid shape cast" && shape.m_type == is_shape<T>::shape_type);
			return reinterpret_cast<T&>(shape);
		}
		template <typename T, typename = enable_if_shape<T>> inline T* shape_cast(Shape* shape)
		{
			assert("Invalid shape cast" && (shape == nullptr || shape->m_type == is_shape<T>::shape_type));
			return reinterpret_cast<T*>(shape);
		}

		// Return a shape to use in place of a real shape for objects that don't need a shape really
		inline Shape* NoShape()
		{
			static Shape s_no_shape(EShape::NoShape, sizeof(Shape));
			return &s_no_shape;
		}

		// Calculate the bounding box for a shape.
		template <typename = void> inline BBox CalcBBox(Shape const& shape)
		{
			switch (shape.m_type)
			{
			default: assert("Unknown primitive type" && false); return BBoxReset;
			case EShape::Sphere:   return CalcBBox(shape_cast<ShapeSphere>  (shape));
			case EShape::Box:      return CalcBBox(shape_cast<ShapeBox>     (shape));
			case EShape::Polytope: return CalcBBox(shape_cast<ShapePolytope>(shape));
			}
		}

		// Calculate the mass properties of a shape
		template <typename = void> inline MassProperties CalcMassProperties(Shape const& shape, float density)
		{
			switch (shape.m_type)
			{
			default: assert("Unknown primitive type" && false); return MassProperties();
			case EShape::Sphere: return CalcMassProperties(shape_cast<ShapeSphere>(shape), density);
			case EShape::Box:    return CalcMassProperties(shape_cast<ShapeBox>   (shape), density);
			}
		}

		// Shift the centre a shape. Updates 'shape.m_shape_to_model' and 'shift'
		template <typename = void> inline void ShiftCentre(Shape& shape, v4& shift)
		{
			switch (shape.m_type)
			{
			default: assert("Unknown primitive type" && false); return;
			case EShape::Sphere:   return ShiftCentre(shape_cast<ShapeSphere>  (shape), shift);
			case EShape::Box:      return ShiftCentre(shape_cast<ShapeBox>     (shape), shift);
			case EShape::Polytope: return ShiftCentre(shape_cast<ShapePolytope>(shape), shift);
			}
		}

		// Returns the support vertex for 'shape' in 'direction'. 'direction' is in shape space
		template <typename = void> inline v4 SupportVertex(Shape const& shape, v4_cref direction, size_t hint_vert_id, size_t& sup_vert_id)
		{
			switch (shape.m_type)
			{
			default: assert("Unknown primitive type" && false); return v4Zero;
			case EShape::Sphere:   return SupportVertex(shape_cast<ShapeSphere>  (shape), direction, hint_vert_id, sup_vert_id);
			case EShape::Box:      return SupportVertex(shape_cast<ShapeBox>     (shape), direction, hint_vert_id, sup_vert_id);
			case EShape::Polytope: return SupportVertex(shape_cast<ShapePolytope>(shape), direction, hint_vert_id, sup_vert_id);
			case EShape::Triangle: return SupportVertex(shape_cast<ShapeTriangle>(shape), direction, hint_vert_id, sup_vert_id);
			}
		}

		// Returns the closest point on 'shape' to 'point'. 'shape' and 'point' are in the same space
		template <typename = void> inline void ClosestPoint(Shape const& shape, v4_cref point, float& distance, v4& closest)
		{
			switch (shape.m_type)
			{
			default: assert("Unknown primitive type" && false); return;
			case EShape::Sphere: return ClosestPoint(shape_cast<ShapeSphere>  (shape), point, distance, closest);
			case EShape::Box:    return ClosestPoint(shape_cast<ShapeBox>     (shape), point, distance, closest);
			}
		}

		// Increment a shape pointer
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
	}

	// Convert a shape enum to a string
	inline char const* ToString(physics::EShape shape)
	{
		switch (shape)
		{
		default: assert("Unknown shape type" && false); return "unknown";
		case physics::EShape::Sphere:   return "sphere";
		case physics::EShape::Box:      return "box";
		case physics::EShape::Polytope: return "polytope";
		case physics::EShape::Triangle: return "triangle";
		case physics::EShape::Array:    return "array";
		case physics::EShape::BVTree:   return "BVtree";
		case physics::EShape::Terrain:  return "terrain";
		}
	}
}
