//****************************************************************
//
//	TerrainFunction
//
//****************************************************************
#ifndef TERRAIN_FUNCTION_H
#define TERRAIN_FUNCTION_H

#include "PR/Common/PRAssert.h"
#include "PR/Maths/Maths.h"
#include "PR/Maths/PerlinNoise.h"

#ifndef PR_DBG_PLANET
#define PR_DBG_PLANET PR_DBG_ON
#endif//PR_DBG_PLANET

class TerrainFunction
{
public:
	TerrainFunction(
		float seed,
		float height_variation);

	float SampleHeight(const pr::v4& point) const;

private:
	pr::FRandom					m_rand;
	float						m_height_variation;
	pr::PerlinNoiseGenerator	m_perlin;
};

#endif//TERRAIN_FUNCTION_H
