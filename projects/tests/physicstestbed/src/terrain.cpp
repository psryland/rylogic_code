//******************************************
// Terrain
//******************************************

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Terrain.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "pr/filesys/filesys.h"

Terrain::Terrain(const parse::Terrain& terrain, PhysicsEngine& engine)
:m_ldr(0)
{
	// Load the default if no file is given
	if( terrain.m_ldr_str.empty() )
	{
		// Use the default terrain call back
		engine.SetDefaultTerrain();

		// Create a surface for the terrain by sampling it
		unsigned int const div_x = 20, div_z = 20;
		float terr_x, terr_z, terr_w, terr_d;
		engine.GetTerrainDimensions(terr_x, terr_z, terr_w, terr_d);

		std::string ldr_str = pr::Fmt("*SurfaceWHD ground 8000A000 { %d %d \n", div_x + 1, div_z + 1);
		for( float z = terr_z; z <= terr_z + terr_d; z += terr_d / div_z )
		{
			for( float x = terr_x; x <= terr_x + terr_w; x += terr_w / div_x )
			{
				float height;
				pr::v4 normal;
				engine.SampleTerrain(pr::v4::make(x, -30.0f, z, 1.0f), height, normal);
				ldr_str += pr::Fmt("%3.3f %3.3f %3.3f\n", x, height, z);
			}
		}
		ldr_str += "}\n";
		
		if( m_ldr ) ldrUnRegisterObject(m_ldr);
		m_ldr = ldrRegisterObject(ldr_str.c_str(), ldr_str.size());
	}
	else
	{
		if( m_ldr ) ldrUnRegisterObject(m_ldr);
		m_ldr = ldrRegisterObject(terrain.m_ldr_str.c_str(), terrain.m_ldr_str.size());

		// Load the terrain data
		engine.SetTerrain(terrain);
	}
}

Terrain::~Terrain()
{
	ldrUnRegisterObject(m_ldr);
}

// Dump terrain data to the export file
void Terrain::ExportTo(pr::Handle& file, bool physics_scene)
{file;
	if( physics_scene )
	{
	}
	else
	{
//		std::string src = ldrGetSourceString(m_ldr);
//		file.Write(src.c_str(), src.size());
	}
}
