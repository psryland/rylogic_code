//******************************************
// Terrain
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "pr/linedrawer/plugininterface.h"

// Terrain
class Terrain
{
public:
	Terrain(const parse::Terrain& terrain, PhysicsEngine& engine);
	~Terrain();

	void ExportTo(pr::Handle& file, bool physics_scene);

	pr::ldr::ObjectHandle	m_ldr;
};
typedef std::map<pr::ldr::ObjectHandle, Terrain*> TTerrain;
