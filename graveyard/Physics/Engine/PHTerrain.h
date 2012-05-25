//********************************************************************
//
//	The Terrain lookup structure
//
//********************************************************************
// The terrain system is assumed to be a 3D system. All lookups consist
// of a point and direction. To use a 2D height field with this system
// the client terrain system can ignore 'm_direction' and always set
// 'm_fraction' to 0.0f for a collision.
//
//	Depth vs Fraction:
//		Depth is the distance in meters that 'm_position' is from the
//		terrain in the direction of 'm_direction'.
//		Fraction is the fraction along 'm_direction' that is the collision
//		with the terrain.
//		When comparing terrain lookups, fraction is tested first. If not 0.0
//		or 1.0 then the deepest terrain lookup is the one that will collide
//		first. If 0.0 or 1.0 then m_depth is used.
//
// This structure is used for many different kinds of lookups controlled by the enum flags.
//
// Notes:
//	- 'm_direction' is not necessarily normalised.
//	- If 'm_direction' is zero, 'm_fraction' should be zero if 'm_position' is below ground.
//		'm_depth' is up to the client code but should be self consistent.
//	- If eQuickOut is set then the terrain function may return after detecting no collision
//		regardless of other flags.
//	- If a flag is set then the corresponding member should be set by the client code unless
//		eQuickOut is set.
//	- The physics engine can determine the position of the terrian using
//		m_position + m_fraction * m_direction
//	- 'm_fraction' should be in the range 0.0 to 1.0
//	- If 'm_fraction' == 0.0f then 'm_depth' should be >= 0.0
//	- 'm_collision' should be true if 'm_fraction' < 1.0f
//
#ifndef PR_PHYSICS_TERRAIN_H
#define PR_PHYSICS_TERRAIN_H

#include "PR/Maths/Maths.h"

namespace pr
{
	namespace ph
	{
		struct Terrain
		{
			// The presence of a flag is intended to guarantee a valid value for
			// the member it represents.
			enum LookupType
			{
				eCheck			= 0x01,		// Boolean test for collision
				eNormal			= 0x02,		// Want the collision normal
				eDepth			= 0x04,		// Want the depth of 'm_position' relative to the terrain.
				eFraction		= 0x08,		// Want the fraction of 'm_direction' at the intersection
				eMaterial		= 0x10,		// Want the material of the terrain
				eQuickOut		= 0x20 | eCheck,	// If present means data is only wanted if there is a collision
				eFull			= eCheck | eNormal | eDepth | eFraction | eMaterial,
				eFullQuickOut	= eFull | eQuickOut,
			};

			void	SetNoCollision()							{ m_depth = -maths::float_max; m_fraction = 1.0f; m_collision = false; }
			bool	IsDeeperThan(const Terrain& other) const	{ return (!FEql(m_fraction, other.m_fraction)) ? (m_fraction < other.m_fraction) : (m_depth > other.m_depth); }
			v4		Intersect() const							{ return m_position + m_fraction * m_direction; }

			// In
			uint32	m_lookup_type;			// The type of terrain lookup to do
			v4		m_position;				// The location that terrain is required for
			v4		m_direction;			// The line segment from 'm_position' to use for a terrain intersection
			
			// Out
			bool	m_collision;			// True if there is an intersection with the terrain
			v4		m_normal;				// The terrain normal at the intersection
			float	m_depth;				// The depth that 'm_position' is below the terrain. -ve = above, +ve = below
			float	m_fraction;				// The fraction of 'm_direction' at the point of collision. 0.0f = immediate (first) collision, 1.0 = last collision
			uint	m_material_index;		// The material that the terrain is made out of
		};
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_TERRAIN_H
