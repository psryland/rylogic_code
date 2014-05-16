//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_COLLIDER_H
#define PR_PHYSICS_COLLIDER_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		typedef void (*CollisionFunction)(Shape const& objA, m4x4 const& a2w, Shape const& objB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache);
		CollisionFunction GetCollisionDetectionFunction(const Shape& objA, const Shape& objB);
		
		// Collide overloads - Specific shape type overloads are faster
		bool Collide(Shape const& shapeA, m4x4 const& a2w, Shape const& shapeB, m4x4 const& b2w);
		void Collide(Shape const& shapeA, m4x4 const& a2w, Shape const& shapeB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache);
		bool Collide(ShapeBox const& shapeA, m4x4 const& a2w, ShapeBox const& shapeB, m4x4 const& b2w);
		bool Collide(ShapeBox const& shapeA, m4x4 const& a2w, ShapeBox const& shapeB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache);
		bool Collide(ShapeBox const& shapeA, m4x4 const& a2w, ShapeCylinder const& shapeB, m4x4 const& b2w);
		bool Collide(ShapeBox const& shapeA, m4x4 const& a2w, ShapeCylinder const& shapeB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache);
		bool CollideBruteForce(const Shape& shapeA, m4x4 const& a2w, const Shape& shapeB, m4x4 const& b2w, v4& normal, bool test_collision_result);

		// Return the nearest points between two shapes
		bool GetNearestPoints(Shape const& shapeA, m4x4 const& a2w, Shape const& shapeB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache);

		// Collision functions - The lower EShape value must be objA
		void UnknownVsUnknown	(Shape const& objA     ,m4x4 const& a2w ,Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void SphereVsSphere		(Shape const& objA     ,m4x4 const& a2w ,Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void SphereVsBox		(Shape const& sphere   ,m4x4 const& a2w ,Shape const& box      ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void SphereVsCylinder	(Shape const& sphere   ,m4x4 const& a2w ,Shape const& cylinder ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void SphereVsTriangle	(Shape const& sphere   ,m4x4 const& a2w ,Shape const& triangle ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void BoxVsBox			(Shape const& objA     ,m4x4 const& a2w ,Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void BoxVsCylinder		(Shape const& objA     ,m4x4 const& a2w, Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void CylinderVsCylinder	(Shape const& objA     ,m4x4 const& a2w, Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void MeshVsMesh			(Shape const& objA     ,m4x4 const& a2w ,Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void BoxVsTriangle		(Shape const& objA     ,m4x4 const& a2w ,Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void SphereVsArray		(Shape const& objA     ,m4x4 const& a2w ,Shape const& arr      ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void BoxVsArray			(Shape const& objA     ,m4x4 const& a2w ,Shape const& arr      ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void MeshVsArray		(Shape const& objA     ,m4x4 const& a2w ,Shape const& arr      ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void ArrayVsArray		(Shape const& objA     ,m4x4 const& a2w ,Shape const& objB     ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void SphereVsTerrain	(Shape const& sphere   ,m4x4 const& a2w ,Shape const& terrain  ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void BoxVsTerrain		(Shape const& box      ,m4x4 const& a2w ,Shape const& terrain  ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void MeshVsTerrain		(Shape const& mesh     ,m4x4 const& a2w ,Shape const& terrain  ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void TriangleVsTerrain	(Shape const& triangle ,m4x4 const& a2w ,Shape const& terrain  ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
		void ArrayVsTerrain		(Shape const& arr      ,m4x4 const& a2w ,Shape const& terrain  ,m4x4 const& b2w ,ContactManifold& manifold ,CollisionCache* cache);
	}
}

#endif

