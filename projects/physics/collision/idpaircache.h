//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_COLLISION_ID_PAIR_CACHE_H
#define PR_PHYSICS_COLLISION_ID_PAIR_CACHE_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		namespace mesh_vs_mesh
		{
			enum { MaxIterations = 10 };
			struct IdPairCache
			{
				struct IdPair { std::size_t p, q; };
				IdPairCache()
				:m_size(0)
				{}
				void Add(std::size_t p_id, std::size_t q_id)
				{
					PR_ASSERT(PR_DBG_PHYSICS, m_size != MaxIterations, "");
					m_id[m_size].p = p_id;
					m_id[m_size].q = q_id;
					++m_size;
				}
				// Returns true if the latest id pair has re-occurred
				// If true is returned, 'dup_index' equals the index of the duplicate
				bool ReoccurringPair(int& dup_index) const
				{
					for( dup_index = m_size-2; dup_index >= 0; --dup_index )
					{
						if( m_id[m_size - 1].p == m_id[dup_index].p &&
							m_id[m_size - 1].q == m_id[dup_index].q )
							return true;
					}
					return false;
				}
				IdPair	m_id[MaxIterations];
				int		m_size;
			};
		}
	}
}

#endif
