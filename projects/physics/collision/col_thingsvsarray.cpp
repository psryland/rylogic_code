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

namespace pr
{
	namespace ph
	{
		// Detect collisions between two array shape objects
		template <typename ShapeThing>
		void ThingVsArray(Shape const& thg, m4x4 const& a2w, Shape const& arr, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)
		{
			ShapeArray const& arr_shape = shape_cast<ShapeArray>(arr);

			// Test the thing against all the primitives of arr
			for( Shape const *sB = arr_shape.begin(), *sB_end = arr_shape.end(); sB != sB_end; sB = Inc(sB) )
			{
				CollisionFunction func = GetCollisionDetectionFunction(thg, *sB);
				if( thg.m_type < sB->m_type )
				{
					func(thg, a2w, *sB, b2w * sB->m_shape_to_model, manifold, cache);
				}
				else
				{
					manifold.Flip();
					func(*sB, b2w * sB->m_shape_to_model, thg, a2w, manifold, cache);
					manifold.Flip();
				}
			}
		}
	}//namespace ph
}//namespace pr

// Instantiations
void pr::ph::SphereVsArray(Shape const& objA, m4x4 const& a2w, Shape const& arr, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)		{ ThingVsArray<ShapeSphere  >(objA, a2w, arr, b2w, manifold, cache); }
void pr::ph::BoxVsArray   (Shape const& objA, m4x4 const& a2w, Shape const& arr, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)		{ ThingVsArray<ShapeBox     >(objA, a2w, arr, b2w, manifold, cache); }
void pr::ph::MeshVsArray  (Shape const& objA, m4x4 const& a2w, Shape const& arr, m4x4 const& b2w, ContactManifold& manifold, CollisionCache* cache)		{ ThingVsArray<ShapePolytope>(objA, a2w, arr, b2w, manifold, cache); }
