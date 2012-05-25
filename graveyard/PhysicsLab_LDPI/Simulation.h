//**************************************************************
//
//	A class for managing the stepping of the simulation
//
//**************************************************************

#ifndef SIMULATION_H
#define SIMULATION_H

#include "PR/Physics/Physics.h"
#include "PR/DynamicObjectMap/DynamicObjectMap.h"
#include "PhysicsLab_LDPI/PhysicsObject.h"
#include "PhysicsLab_LDPI/Event.h"
#include "PhysicsLab_LDPI/GroundPlane.h"

class Simulation
{
public:
	Simulation();
	~Simulation();

	bool	Initialise();
	void	UnInitialise();
	
	void	Reset();
	void	Step(float elapsed_seconds);
	void	UpdateObjectTransforms();
	float	GetSimulationTime() const					{ return m_time; }

private:
	friend class SceneParser;							// This guy fills in our containers
	PhysicsEngine			m_physics_engine;			// The physics engine
	DynamicObjectMap		m_dom;						// Broadphase collision detection structure
	TBinaryData				m_physics_objects;			// Contains the physical storage of the physics models
	TObjectContainer		m_object;					// The physics objects
	TEventContainer			m_event;					// The event stream
	GroundPlane				m_ground;
	uint					m_current_event;			// The next event to occur in the event stream
	float					m_time;						// The current time in seconds
};

#endif//SIMULATION_H