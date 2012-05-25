//**************************************************************
//
//	A class to manage the terrain ground plane
//
//**************************************************************

#ifndef GROUND_PLANE_H
#define GROUND_PLANE_H

class GroundPlane
{
public:
	void	RegisterObject();
	static	void GetTerrainData(Terrain& terrain);

private:
	ObjectHandle	m_handle;
};

//*****
// Create a plane to represent the terrain
inline void GroundPlane::RegisterObject()
{
	const char str[] = "*QuadLU ground FF00A000 { -10 0 -10 10 0 10 }";
	m_handle = ldrRegisterObject(str, sizeof(str));
}

//*****
// Return the intersection with the terrain
inline void GroundPlane::GetTerrainData(Terrain& terrain)
{
	float start = Dot3(v4YAxis, terrain.m_position);
	float end   = start + Dot3(v4YAxis, terrain.m_direction);

	terrain.m_collision				= end < 0.0f;
	if( terrain.m_lookup_type & Terrain::eQuickOut && !terrain.m_collision ) return;

	terrain.m_normal				= v4YAxis;
	if( start < 0.0f )				terrain.m_fraction = 0.0f;
	else if( end > 0.0f )			terrain.m_fraction = 1.0f;
	else							terrain.m_fraction = start / (start - end);
	terrain.m_depth					= -terrain.m_position[1];
	terrain.m_material_index		= 0;
}

#endif//GROUND_PLANE_H