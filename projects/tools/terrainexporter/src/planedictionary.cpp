//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/planedictionary.h"
#include "terrainexporter/utility.h"
#include "terrainexporter/face.h"

using namespace pr;
using namespace pr::terrain;

// Return a plane that describes 'face' so that none of its verts
// deviate by more than 'm_position_tolerance' from the plane
Plane const* PlaneDictionary::GetPlane(Face const& face, float& max_error)
{
	// Look for a plane that can represent this face
	for (TPlaneLookup::iterator p = m_lookup.begin(), p_end = m_lookup.end(); p != p_end; ++p)
	{
		Page& page = *p;
		
		// See if the verts of 'face' are within 'm_position_tolerance' of this plane
		bool is_suitable = true;
		float err = 0.0f;
		for (int i = 0; i != 3; ++i)
		{
			float dist = Abs(Dot4(page.m_plane, face.m_original_vertex[i]));
			if (dist >= m_position_tolerance) { is_suitable = false; break; }
			err = pr::Max(err, dist);
		}

		// If this plane is suitable, add the plane for this face to
		// the averaging part of 'page' weighted by the area of the face
		// and return the plane
		if (is_suitable)
		{
			max_error = err;
			pr::v4 plane = CalculatePlane(face);
			float area = CalculateArea(face);
			page.m_avr[0] += plane[0] * area;
			page.m_avr[1] += plane[1] * area;
			page.m_avr[2] += plane[2] * area;
			page.m_avr[3] += plane[3] * area;
			page.m_sum += area;
			return &page.m_plane;
		}
	}
	
	pr::v4 plane = CalculatePlane(face);
	float area = CalculateArea(face);
	max_error = 0.0f;

	// If we get to here then no suitable plane was found. Add a new one
	Page page;
	page.m_plane  = plane;
	page.m_avr[0] = plane[0] * area;
	page.m_avr[1] = plane[1] * area;
	page.m_avr[2] = plane[2] * area;
	page.m_avr[3] = plane[3] * area;
	page.m_sum    = area;
	m_lookup.push_back(page);
	return &m_lookup.back().get().m_plane;
}

// Average the entries in the plane dictionary and reset the averaging members
void PlaneDictionary::Average()
{
	for (TPlaneLookup::iterator p = m_lookup.begin(), p_end = m_lookup.end(); p != p_end; ++p)
	{
		Page& page = *p;

		double len = sqrt(page.m_avr[0]*page.m_avr[0] + page.m_avr[1]*page.m_avr[1] + page.m_avr[2]*page.m_avr[2]);
		page.m_avr[0] /= len;
		page.m_avr[1] /= len;
		page.m_avr[2] /= len;
		page.m_avr[3] /= page.m_sum;
		page.m_plane.set(float(page.m_avr[0]), float(page.m_avr[1]), float(page.m_avr[2]), float(page.m_avr[3]));

		page.m_avr[0] = page.m_avr[1] = page.m_avr[2] = page.m_avr[3] = 0.0;
		page.m_sum = 0.0f;
	}
}

