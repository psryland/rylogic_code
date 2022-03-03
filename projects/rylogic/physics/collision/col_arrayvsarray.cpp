//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/shape/shapearray.h"
#include "physics/utility/debug.h"

using namespace pr;
using namespace pr::ph;

// Detect collisions between two array shape objects
void pr::ph::ArrayVsArray(Shape const& objA, m4x4 const& a2w, Shape const& objB, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)
{
	ShapeArray const& arr_shapeA = shape_cast<ShapeArray>(objA);
	ShapeArray const& arr_shapeB = shape_cast<ShapeArray>(objB);

	// Test all the primitives of objA against objB
	for( Shape const *sA = arr_shapeA.begin(), *sA_end = arr_shapeA.end(); sA != sA_end; sA = Inc(sA) )
	{
		for( Shape const *sB = arr_shapeB.begin(), *sB_end = arr_shapeB.end(); sB != sB_end; sB = Inc(sB) )
		{
			CollisionFunction func = GetCollisionDetectionFunction(*sA, *sB);
			if( sA->m_type < sB->m_type )
			{
				func(*sA, a2w * sA->m_shape_to_model, *sB, b2w * sB->m_shape_to_model, manifold, cache);
			}
			else
			{
				manifold.Flip();
				func(*sB, b2w * sB->m_shape_to_model, *sA, a2w * sA->m_shape_to_model, manifold, cache);
				manifold.Flip();
			}
		}
	}
}
