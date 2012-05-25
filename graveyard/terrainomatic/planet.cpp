//***************************************************************
//
//	Planet
//
//***************************************************************

#include "Planet.h"
#include "PR/Geometry/Manipulator/Manipulator.h"

using namespace pr;

//*****
Planet::Planet(float seed)
:m_rand              (seed)
,m_instance_to_world (m4x4Identity)
,m_radius            (2.0f)//(FRand(m_rand, 10.0f, 50.0f))
,m_sea_level         (m_radius * 1.0f)//(FRand(m_rand, m_radius * 0.9f, m_radius * 1.0f))
,m_hilliness         (0.05f)//(FRand(m_rand, 0.05f, 0.15f))
,m_terrain_function  (m_rand.next(), m_hilliness)
{
	GenerateGlobe();
	GeneratePatch();
}

//*****
// Create a globe model
void Planet::GenerateGlobe()
{
	// Create the globe model
	geometry::GenerateGeosphereSettings settings;
	settings.m_divisions = 4;
	settings.m_radius    = m_radius;
	geometry::GenerateGeosphere(m_globe, settings);

	Mesh& mesh = m_globe.m_frame.front().m_mesh;
	mesh.m_geometry_type &= ~geometry::EType_Texture;
	mesh.m_geometry_type |=  geometry::EType_Colour;

	// Apply the terrain function to the vertices of the globe
	for( TVertexCont::iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v )
	{
		float height = m_terrain_function.SampleHeight(v->m_vertex);
		
		v->m_vertex *= UnitHeightToAltitude(height); v->m_vertex.w = 1.0f;
		v->m_colour  = UnitHeightToColour(height);
	}

	geometry::GenerateNormals(mesh);
}

//*****
// Create a patch model
void Planet::GeneratePatch()
{
	// Create the patch model
	//geometry::GeneratePatchSettings settings;
	//settings.m_patch_dimension	.Set(1.0f, 1.0f);
	//settings.m_patch_divisions	.Set(50, 50);
	//settings.m_patch_origin		= -0.5f * settings.m_patch_dimension;
	//geometry::GeneratePatch(m_patch, settings;

	//Mesh& mesh = m_globe.m_frame.front().m_mesh;
	//mesh.m_geometry_type &= ~geometry::EType_Texture;
	//mesh.m_geometry_type |=  geometry::EType_Colour;
}

//*****
// Return an actual height to use based on the 
float Planet::UnitHeightToAltitude(float unit_height) const
{
	return Maximum(m_radius + unit_height * m_radius, m_sea_level);
}

//*****
// Return a colour appropriate to 'altitude'
pr::Colour32 Planet::UnitHeightToColour(float unit_height) const
{
	// Make this better:
	float altitude = UnitHeightToAltitude(unit_height);
	if( altitude == m_sea_level ) return Colour32::construct(50, 140, 190, 0xFF);
//	if( altitude >  m_radius + 0.5f * m_hilliness * m_radius ) return Colour32::construct(0xFF, 0xFF, 0xFF, 0xFF);
	return Colour32::construct(70, 130,  70, 0xFF);
}

