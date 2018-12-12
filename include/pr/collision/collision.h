//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#include "pr/container/tri_table.h"
#include "pr/collision/shapes.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"
#include "pr/collision/col_sphere_vs_sphere.h"
#include "pr/collision/col_sphere_vs_box.h"
#include "pr/collision/col_box_vs_box.h"

namespace pr::collision
{
	// Function type for collection detection
	using Detect = bool (*)(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w, Contact& c);
	inline bool CollisionNotImplemented(Shape const&, m4_cref<>, Shape const&, m4_cref<>, Contact&) { throw std::runtime_error("Collision not implemented"); }

	// Collide two shapes
	inline bool Collide(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w, Contact& contact)
	{
		using namespace pr::tri_table;

		// Check the order and index values haven't been changed
		static_assert(int(EShape::Sphere  ) == 0, "");
		static_assert(int(EShape::Box     ) == 1, "");
		static_assert(int(EShape::Line    ) == 2, "");
		static_assert(int(EShape::Triangle) == 3, "");
		static_assert(int(EShape::Polytope) == 4, "");
		static_assert(int(EShape::Array   ) == 5, "");
		static_assert(int(EShape::BVTree  ) == 6, "");
		static_assert(int(EShape::Terrain ) == 7, "");
		static_assert(int(EShape::NumberOf) == 8, "");

		// Tri-Table of collision functions
		static Detect s_collision_functions[Size(EType::Inclusive, int(EShape::NumberOf))] = 
		{
			SphereVsSphere,          // (0 v 0) - Sphere v Sphere  

			SphereVsBox,             // (1 v 0) - Box v Sphere 
			BoxVsBox,                // (1 v 1) - Box v Box 

			CollisionNotImplemented, // (2 v 0) - Line v Sphere
			CollisionNotImplemented, // (2 v 1) - Line v Box
			CollisionNotImplemented, // (2 v 2) - Line v Line

			CollisionNotImplemented, // (3 v 0) - Triangle v Sphere
			CollisionNotImplemented, // (3 v 1) - Triangle v Box
			CollisionNotImplemented, // (3 v 2) - Triangle v Line
			CollisionNotImplemented, // (3 v 3) - Triangle v Triangle

			CollisionNotImplemented, // (4 v 0) - Polytope v Sphere
			CollisionNotImplemented, // (4 v 1) - Polytope v Box
			CollisionNotImplemented, // (4 v 2) - Polytope v Line
			CollisionNotImplemented, // (4 v 3) - Polytope v Triangle
			CollisionNotImplemented, // (4 v 4) - Polytope v Polytope

			CollisionNotImplemented, // (5 v 0) - Array v Sphere
			CollisionNotImplemented, // (5 v 1) - Array v Box
			CollisionNotImplemented, // (5 v 2) - Array v Line
			CollisionNotImplemented, // (5 v 3) - Array v Triangle
			CollisionNotImplemented, // (5 v 4) - Array v Polytope
			CollisionNotImplemented, // (5 v 5) - Array v Array

			CollisionNotImplemented, // (6 v 0) - BVTree v Sphere
			CollisionNotImplemented, // (6 v 1) - BVTree v Box
			CollisionNotImplemented, // (6 v 2) - BVTree v Line
			CollisionNotImplemented, // (6 v 3) - BVTree v Triangle
			CollisionNotImplemented, // (6 v 4) - BVTree v Polytope
			CollisionNotImplemented, // (6 v 5) - BVTree v Array
			CollisionNotImplemented, // (6 v 6) - BVTree v BVTree

			CollisionNotImplemented, // (7 v 0) - Terrain v Sphere
			CollisionNotImplemented, // (7 v 1) - Terrain v Box
			CollisionNotImplemented, // (7 v 2) - Terrain v Line
			CollisionNotImplemented, // (7 v 3) - Terrain v Triangle
			CollisionNotImplemented, // (7 v 4) - Terrain v Polytope
			CollisionNotImplemented, // (7 v 5) - Terrain v Array
			CollisionNotImplemented, // (7 v 6) - Terrain v BVTree
			CollisionNotImplemented, // (7 v 7) - Terrain v Terrain
		};

		// Get the appropriate collision function
		auto func = s_collision_functions[Index(EType::Inclusive, int(lhs.m_type), int(rhs.m_type))];

		// Ensure the lowest shape type value is 'lhs'
		auto flip = lhs.m_type > rhs.m_type;

		// Test for contact
		auto result = flip
			? func(rhs, r2w, lhs, l2w, contact)
			: func(lhs, l2w, rhs, r2w, contact);
		
		// Flip back
		if (flip)
			contact.flip();

		return result;
	}
}

