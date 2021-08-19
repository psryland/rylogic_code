//*******************************************************************************
// Terrain Exporter
//  Copyright (C) Rylogic Ltd 2009
//*******************************************************************************
#ifndef PR_TERRAIN_SEARCH_H
#define PR_TERRAIN_SEARCH_H
#pragma once

#include "pr/maths/maths.h"
#include "pr/terrain/terrain.h"

namespace pr
{
	namespace terrain
	{
		// Function object for finding a single height immediately below a query point
		struct SingleHeightLookup
		{
			pr::v4		m_plane;
			float		m_query_height;
			float		m_height;
			pr::uint	m_material_id;
			pr::uint	m_surface_flags;

			// Select a single height to use from the terrain. (Ignores water but records its height)
			void operator ()(float height, pr::v4 const& plane, pr::uint material_id, pr::uint surface_flags)
			{
				// Reject any default heights
				if (height == pr::terrain::DefaultHeight()) return;

				// Adjust the 'query_height' by the height tolerance
				float query_height = m_query_height + pr::terrain::HeightTolerance();

				// Use 'height' if it is above 'm_height' and below the query height
				// or if 'm_height' is above the query height and 'height' is below 'm_height' (possibly below 'query_height' as well)
				if ((m_height < height && height < query_height) ||	(m_height > query_height && height < m_height))
				{
					m_height = height;
					m_plane = plane;
					m_material_id = material_id;
					m_surface_flags = surface_flags;
					return;
				}

				// If 'm_height' is the default height then we want to use 'height' regardless
				if (m_height == pr::terrain::DefaultHeight())
				{
					m_height = height;
					m_plane = plane;
					m_material_id = material_id;
					m_surface_flags = surface_flags;
					return;
				}
			}

			explicit SingleHeightLookup(float query_height)
			:m_query_height(query_height)
			,m_height(pr::terrain::DefaultHeight())
			,m_plane(pr::v4YAxis)
			{}
		};
	}//namespace terrain
}//namespace pr

#endif//PR_TERRAIN_SEARCH_H
