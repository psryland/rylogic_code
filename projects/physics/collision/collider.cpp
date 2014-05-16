//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"

using namespace pr;
using namespace pr::ph;

namespace pr
{
	namespace ph
	{
		namespace collision
		{
			CollisionFunction g_detection_functions[pr::TriTable<EShape_NumberOf, pr::tri_table::EType::Inclusive>::size] =
			{
				SphereVsSphere,			// EShape_Sphere   - EShape_Sphere

				UnknownVsUnknown,		// EShape_Sphere   - EShape_Capsule
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Capsule

				SphereVsBox,			// EShape_Sphere   - EShape_Box
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Box
				BoxVsBox,				// EShape_Box      - EShape_Box

				SphereVsCylinder,		// EShape_Sphere   - EShape_Cylinder
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Cylinder
				MeshVsMesh,//BoxVsCylinder,			// EShape_Box      - EShape_Cylinder
				MeshVsMesh,				// EShape_Cylinder - EShape_Cylinder

				MeshVsMesh,				// EShape_Sphere   - EShape_Polytope
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Polytope
				MeshVsMesh,				// EShape_Box      - EShape_Polytope
				MeshVsMesh,				// EShape_Cylinder - EShape_Polytope
				MeshVsMesh,				// EShape_Polytope - EShape_Polytope

				SphereVsTriangle,		// EShape_Sphere   - EShape_Triangle
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Triangle
				BoxVsTriangle,			// EShape_Box      - EShape_Triangle
				MeshVsMesh,				// EShape_Cylinder - EShape_Triangle
				MeshVsMesh,				// EShape_Polytope - EShape_Triangle
				MeshVsMesh,				// EShape_Triangle - EShape_Triangle

				SphereVsArray,			// EShape_Sphere   - EShape_Array
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Array
				BoxVsArray,				// EShape_Box      - EShape_Array
				UnknownVsUnknown,		// EShape_Cylinder - EShape_Array
				MeshVsArray,			// EShape_Polytope - EShape_Array
				UnknownVsUnknown,		// EShape_Triangle - EShape_Array
				ArrayVsArray,			// EShape_Array    - EShape_Array

				UnknownVsUnknown,		// EShape_Sphere   - EShape_BVTree
				UnknownVsUnknown,		// EShape_Capsule  - EShape_BVTree
				UnknownVsUnknown,		// EShape_Box      - EShape_BVTree
				UnknownVsUnknown,		// EShape_Cylinder - EShape_BVTree
				UnknownVsUnknown,		// EShape_Polytope - EShape_BVTree
				UnknownVsUnknown,		// EShape_Triangle - EShape_BVTree
				UnknownVsUnknown,		// EShape_Array    - EShape_BVTree
				UnknownVsUnknown,		// EShape_BVTree   - EShape_BVTree

				SphereVsTerrain,		// EShape_Sphere   - EShape_Terrain
				UnknownVsUnknown,		// EShape_Capsule  - EShape_Terrain
				BoxVsTerrain,			// EShape_Box      - EShape_Terrain
				UnknownVsUnknown,		// EShape_Cylinder - EShape_Terrain
				MeshVsTerrain,			// EShape_Polytope - EShape_Terrain
				TriangleVsTerrain,		// EShape_Triangle - EShape_Terrain
				ArrayVsTerrain,			// EShape_Array    - EShape_Terrain
				UnknownVsUnknown,		// EShape_BVTree   - EShape_Terrain
				UnknownVsUnknown,		// EShape_Terrain  - EShape_Terrain
			};
		}
	}
}

// Collision detection function for unknown types
void pr::ph::UnknownVsUnknown(Shape const& shapeA, m4x4 const&, Shape const& shapeB, m4x4 const&, ContactManifold&, CollisionCache*)
{
	shapeA; shapeB;
	PR_INFO(PR_DBG_PHYSICS, Fmt("No %s vs. %s collision detection function registered", GetShapeTypeStr(shapeA.m_type), GetShapeTypeStr(shapeB.m_type)).c_str());
}

// Return a function appropriate for detecting collisions between 'objA' and 'objB'
CollisionFunction pr::ph::GetCollisionDetectionFunction(Shape const& objA, Shape const& objB)
{
	return collision::g_detection_functions[pr::tri_table::Index<pr::tri_table::EType::Inclusive>(objA.m_type, objB.m_type)];
}

// Collide two shapes
bool pr::ph::Collide(Shape const& shapeA, m4x4 const& a2w, Shape const& shapeB, m4x4 const& b2w)
{
	PR_EXPAND(PR_DBG_COLLISION, std::string str; ldr::PhCollisionScene("scene", "FFFFFFFF", shapeA, a2w, shapeB, b2w, str));
	PR_EXPAND(PR_DBG_COLLISION, StringToFile(str, "C:/deleteme/collision_scene.pr_script"));

	ContactManifold manifold;
	GetCollisionDetectionFunction(shapeA, shapeB)(shapeA, a2w, shapeB, b2w, manifold, 0);
	return manifold.IsOverlap();
}
void pr::ph::Collide(Shape const& shapeA, m4x4 const& a2w, Shape const& shapeB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)
{
	PR_EXPAND(PR_DBG_COLLISION, std::string str; ldr::PhCollisionScene("scene", "FFFFFFFF", shapeA, a2w, shapeB, b2w, str));
	PR_EXPAND(PR_DBG_COLLISION, StringToFile(str, "C:/DeleteMe/collision_scene.pr_script"); str.clear());

	GetCollisionDetectionFunction(shapeA, shapeB)(shapeA, a2w, shapeB, b2w, manifold, cache);

	PR_EXPAND(PR_DBG_COLLISION, ldr::phContactManifold("results", "FFFFFFFF", manifold, str));
	PR_EXPAND(PR_DBG_COLLISION, StringToFile(str, "C:/DeleteMe/collision_results.pr_script"); str.clear());
}
