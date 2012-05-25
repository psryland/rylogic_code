//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/shape/shapearray.h"
#include "pr/physics/shape/shapeterrain.h"
#include "physics/utility/debug.h"

using namespace pr;
using namespace pr::ph;

// Detect collisions between two array shape objects
void pr::ph::ArrayVsTerrain(Shape const& arr, m4x4 const& a2w, Shape const& terrain, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)
{
	ShapeArray const& arr_shape = shape_cast<ShapeArray>(arr);

	// Test all the primitives of objA against the terrain
	for( Shape const *sA = arr_shape.begin(), *sA_end = arr_shape.end(); sA != sA_end; sA = Inc(sA) )
	{
		CollisionFunction func = GetCollisionDetectionFunction(*sA, terrain);
		func(*sA, a2w * sA->m_shape_to_model, terrain, b2w, manifold, cache);
	}
}
