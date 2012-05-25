//****************************************************************
//
//	TerrainFunction
//
//****************************************************************
#include "TerrainFunction.h"
#include "PR/Maths/Maths.h"

using namespace pr;

//*****
TerrainFunction::TerrainFunction(
	float seed,
	float height_variation)
:m_rand(seed)
,m_perlin((int)FRand(m_rand, 0.0f, 1000.0f))
,m_height_variation(height_variation)
{}

//*****
// Return a height for a given unit direction
float TerrainFunction::SampleHeight(const v4& point) const
{
	const uint MAX_HARMONICS = 100;	// This could be NUM_HARMONICS_AT_UINT_LENGTH / point.Length3() up to some maximum

	v4    vec = point;//0.5 * (point + v4::construct(100.0f, 100.0f, 100.0f, 0.0f));
	float amp = m_height_variation;

	int freq = 1;
	v4 rad = {maths::pi, maths::pi, maths::pi, 0.0f};
	float height = 0.0f;
	for( uint i = 0; i < MAX_HARMONICS; ++i )
	{
		float perlin = m_perlin.Noise(vec); PR_ASSERT(PR_DBG_PLANET, Abs(perlin) <= 1.0f);
		height += amp * perlin;

		++freq;
		vec = point + rad / (float)freq;
		amp = m_height_variation / (float)freq;
	}
	
	return height;
}
