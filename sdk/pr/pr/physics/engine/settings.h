//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_ENGINE_SETTINGS_H
#define PR_PHYSICS_ENGINE_SETTINGS_H

#include "pr/physics/types/forward.h"
#include "pr/physics/material/material.h"

namespace pr
{
	namespace ph
	{
		// Settings for the physics engine
		struct Settings
		{
			// Memory management
			AllocFunction			m_Allocate;
			DeallocFunction			m_Deallocate;

			// Broadphase
			IBroadphase*			m_broadphase;

			// Terrain
			ITerrain*				m_terrain;

			// Collision
			std::size_t				m_constraint_buffer_size;	// The maximum number of constraints that can be processed 'simultaneously'
			std::size_t				m_collision_cache_size;		// Prime numbers are good for this
			IPreCollisionObserver*	m_pre_col_observer;
			IPstCollisionObserver*	m_pst_col_observer;

			Settings()
			{
				m_Allocate					= pr::DefaultAlloc;
				m_Deallocate				= pr::DefaultDealloc;
				m_broadphase				= 0;
				m_terrain					= 0;
				m_constraint_buffer_size	= 65536;
				m_collision_cache_size		= pr::meta::prime_gtreq<1000>::value;
				m_pre_col_observer			= 0;
				m_pst_col_observer			= 0;
			};
		};
	}
}

#endif
