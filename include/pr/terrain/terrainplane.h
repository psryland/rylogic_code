//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#ifndef PR_PHYSICS_TERRAIN_PLANE_H
#define PR_PHYSICS_TERRAIN_PLANE_H

#include "pr/Physics/types/Types.h"
#include "pr/Physics/Terrain/ITerrain.h"

namespace pr
{
	namespace ph
	{
		// Create a flat plane terrain
		struct TerrainPlane : ITerrain
		{
			void CollideSpheres(terrain::Sample* points, std::size_t num_points, TerrainContact terrain_contact_cb, void* context)
			{
				for( terrain::Sample *p = points, *p_end = points + num_points; p != p_end; ++p )
				{
					// If the sphere penetrates the ground
					if( p->m_point.y - p->m_radius < 0.0f )
					{
						terrain::Result result;
						result.m_sample				= p;
						result.m_sample_index		= p - points;
						result.m_terrain_point		= p->m_point;
						result.m_terrain_point.y	= 0.0f;
						result.m_normal				= v4YAxis;
						result.m_material_id		= 0;
						if( !terrain_contact_cb(result, context) ) return;
					}
				}			
			}
			void CollideShape(Shape const&, m4x4 const&, ContactManifold&)
			{
				PR_ASSERT_STR(PR_DBG_PHYSICS, false, "TerrainPlane doesn't support this");
			}
		};
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_TERRAIN_PLANE_H
