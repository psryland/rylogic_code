//**************************************************************
//
//	A class for managing the stepping of the simulation
//
//**************************************************************

#include "Stdafx.h"
#include "PhysicsLab_LDPI/Simulation.h"
#include "PhysicsLab_LDPI/PhysicsMaterials.h"
#include "PhysicsLab_LDPI/PhysicsLab.h"

//*****
// Construction
Simulation::Simulation()
:m_physics_engine()
,m_dom(10, 10)
,m_object_list()
,m_object()
,m_event()
,m_current_event(0)
,m_time(0.0f)
{}

Simulation::~Simulation()
{
	UnInitialise();
}

//*****
// Initialise the simulation
bool Simulation::Initialise()
{
	// Initialise the physics engine
	PhysicsEngineSettings pesettings;
	pesettings.m_max_collision_groups		= 1;
	pesettings.m_material					= g_physics_materials;
	pesettings.m_max_physics_materials		= g_max_physics_materials;
	pesettings.m_use_terrain				= true;
	pesettings.m_GetTerrainData				= GroundPlane::GetTerrainData;
	pesettings.m_max_resting_speed			= 0.1f;

	m_physics_engine.Initialise(pesettings);
	m_physics_engine.CollisionGroup(0, 0)	= ZerothOrderCollision;
	return true;
}

//*****
// UnInitialise the simulation
void Simulation::UnInitialise()
{
	m_physics_engine.RemoveAll();
	ldrUnRegisterAllObjects();
	m_object.clear();
	m_event.clear();
	m_object_list.Release();
}

//*****
// Reset time to zero
void Simulation::Reset()
{
	m_current_event = 0;
	m_time			= 0.0f;
}

//*****
// Step the simulation
void Simulation::Step(float elapsed_seconds)
{
	m_time += elapsed_seconds;
	PhysicsLab::Get().RefreshWindowText();

	// See if we need to apply some events
	while( m_current_event < (uint)m_event.size() && m_event[m_current_event].Time() < m_time )
	{
		// Apply the event
		m_event[m_current_event].Apply();
		++m_current_event;
	}

	// Update the state of the objects
	m_physics_engine.Step(elapsed_seconds);

	// Update the transforms of the objects in LineDrawer
	UpdateObjectTransforms();
}

//*****
// Update the transforms for the objects in LineDrawer
void Simulation::UpdateObjectTransforms()
{
	for( std::size_t i = 0; i < m_object.size(); ++i )
	{
		m_object[i].UpdateTransform();
	}
}
