//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include <list>
#include "terrainexporter/forward.h"

namespace pr
{
	namespace terrain
	{
		// This structure is used to choose a small set of planes to best
		// represent the faces of the terrain. There's nothing fancy about it,
		// all it's doing is trying to find a plane in the current set that will
		// do for each new face added. If one can't be found then a new plane is
		// added. After adding all faces, the planes are averaged and the process
		// is repeated so that the set of planes converges on a minimal set.
		// This method does guarantee that the maximum error will always be less
		// than 'm_position_tolerance' however it may not find the true optimal
		// set of planes.
		struct PlaneDictionary
		{
			struct Page
			{
				Plane	m_plane;		// Must be the first member
				double	m_avr[4];		// A weighted average plane
				double	m_sum;			// The sum of weights added to 'm_avr'
				int		m_index;		// An index assigned once the smoothing process has finished
			};
			typedef std::list< pr::Proxy<Page> > TPlaneLookup;

			TPlaneLookup	m_lookup;
			float			m_position_tolerance;

			PlaneDictionary()
			:m_position_tolerance(0.0f)
			{}

			// Return a plane that describes 'face' so that none of its verts
			// deviate by more than 'm_position_tolerance' from the plane
			Plane const* GetPlane(Face const& face, float& max_error);

			// Average the entries in the plane dictionary and reset the averaging members
			void Average();

			// Remove entries that don't have any references
			void RemoveEmptyEntries()					{ m_lookup.remove_if(*this); }
			bool operator() (const Page& page) const	{ return page.m_sum == 0.0f; }
		};
	}//namespace terrain
}//namespace pr

