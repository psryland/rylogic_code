//***************************************************************
//
//	Planet
//
//***************************************************************
#ifndef PLANET_H
#define PLANET_H

#include "PR/Common/PRAssert.h"
#include "PR/Maths/Maths.h"
#include "PR/Geometry/PRColour.h"
#include "PR/Geometry/PRGeometry.h"
#include "TerrainFunction.h"

#ifndef PR_DBG_PLANET
#define PR_DBG_PLANET PR_DBG_ON
#endif//PR_DBG_PLANET

class Planet
{
public:
	Planet(float seed);

private:
	void GenerateGlobe();
	void GeneratePatch();
	float UnitHeightToAltitude(float unit_height) const;
	pr::Colour32 UnitHeightToColour(float altitude) const;

private:
	#pragma message(PR_LINK "These should be private")
public:
	pr::FRandom		m_rand;
	pr::m4x4		m_instance_to_world;
	float			m_radius;
	float			m_sea_level;
	float			m_hilliness;
	TerrainFunction	m_terrain_function;
	pr::Geometry	m_globe;
	pr::Geometry	m_patch;
};

#endif//PLANET_H
