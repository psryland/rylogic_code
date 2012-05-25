//**********************************************************************
//
//	Scene Parser
//
//**********************************************************************
//	Builds a list of 'RigidBodies' from a script file
//
#ifndef SCENE_PARSER_H
#define SCENE_PARSER_H

#include "PR/Common/PRScript.h"
#include "PR/Physics/ModelBuilder/PhysicsModelBuilder.h"
#include "PhysicsLab_LDPI/PhysicsObject.h"
#include "PhysicsLab_LDPI/Event.h"

class SceneParser
{
public:
	SceneParser();
	~SceneParser();

	bool	Initialise();
	void	UnInitialise();
	bool	Parse(const char* filename);

private:
	bool	ParsePhysicsObject	(PhysicsObject& object);
	bool	ParseModel			(PhysicsObject& object);
	bool	ParsePrimitive		(Primitive& prim);
	bool	ParseTransform		(m4x4& transform);
	bool	ParseEvent			(Event& event);
	PhysicsObject* FindObject	(std::string& name);

private:
	PhysicsObjectBuilder		m_physics_object_builder;	// Wot builds the physics models
	ScriptLoader				m_loader;					
	std::string					m_keyword;

	// Pointers to the containers to fill
	PhysicsObjectList*			m_object_list;
	TObjectContainer*			m_object;
	TEventContainer*			m_event;
};

#endif//SCENE_PARSER_H
