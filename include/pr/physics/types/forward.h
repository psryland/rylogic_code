//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_FORWARD_H
#define PR_PHYSICS_FORWARD_H

#include <new>
#include <limits>
#include <process.h>
#include <algorithm>
#include <vector>
#include "pr/common/min_max_fix.h"
#include "pr/common/cast.h"
#include "pr/common/fmt.h"
#include "pr/common/scope.h"
#include "pr/common/allocator.h"
#include "pr/physics/utility/events.h"
#include "pr/container/chain.h"
#include "pr/container/tri_table.h"
#include "pr/container/vector.h"
#include "pr/container/stack.h"
#include "pr/maths/maths.h"
#include "pr/meta/prime_gtreq.h"
#include "pr/geometry/geometry.h"

namespace pr
{
	namespace ph
	{
		enum
		{
			NoConstraintSet		= 0xFF
		};

		enum EShape
		{
			// Primitive shapes
			EShape_Sphere,
			EShape_Capsule,
			EShape_Box,
			EShape_Cylinder,
			EShape_Polytope,
			EShape_Triangle,

			// Compound shapes
			EShape_Array,		// An array of child shapes
			EShape_BVTree,		// A bounding volume tree of shapes

			EShape_Terrain,
			EShape_NumberOf,

			EShape_NoShape		// Special value to indicate the shape is a dummy object
		};

		enum EShapeHierarchy
		{
			EShapeHierarchy_Single,
			EShapeHierarchy_Array,
			EShapeHierarchy_BvTree,
			EShapeHierarchy_NumberOf
		};

		enum EMotion
		{
			EMotion_Static,
			EMotion_Keyframed,
			EMotion_Dynamic
		};

		// Engine
		class Engine;

		// Shape type forwards
		struct Shape;
		struct ShapeSphere;
		struct ShapeBox;
		struct ShapeCylinder;
		struct ShapePolytope;
		struct ShapePolyFace;
		struct ShapePolyNbrs;
		struct ShapeTriangle;
		struct ShapeArray;
		struct ShapeTerrain;

		// Model type forwards
		struct Rigidbody;
		struct Support;

		/// Collision
		struct IPreCollisionObserver;
		struct IPstCollisionObserver;
		struct ConstraintMatrix;
		struct ConstraintVelocity;
		struct CollisionAgent;
		struct CollisionCache;
		class  ContactManifold;
		struct Manifold;	// should be in ::collision
		struct Contact;
		namespace mesh_vs_mesh
		{
			struct Couple;
			struct CacheData;
			struct Vert;
			struct TrackVert;
			struct Simplex;
			struct Triangle;
		}

		// Ray
		struct Ray;
		struct RayCastResult;
		struct RayVsWorldResult;

		// Material
		typedef uint32_t MaterialId;
		struct Material;

		// Terrain
		struct ITerrain;

		// Broad phase
		struct BPPair;
		struct BPEntity;
		class  IBroadphase;
	}
}

#endif
