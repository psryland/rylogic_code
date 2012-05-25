//*******************************************************************
//
//	A physics object displayed in the PhysicsLab LineDrawer plugin
//
//*******************************************************************
#ifndef RIGID_BODY_H
#define RIGID_BODY_H

#include "Common/Fmt.h"
#include "Common/StdString.h"
#include "Common/StdVector.h"
#include "Maths/Maths.h"
#include "DynamicObjectMap/DynamicObject.h"
#include "Physics/Physics.h"
#include "LineDrawer/PlugInInterface.h"

struct RigidBody
{
	void	GenerateLdrString();

	std::string		m_ldr_string;
	ObjectHandle	m_handle;
	std::string		m_name;
	uint			m_colour;
	m4x4			m_instance_to_world;
	BoundingBox		m_bbox;
	Instance		m_physics;
	DynamicObject	m_dynamic_object;
};

typedef std::vector<RigidBody*> TRigidBodyArray;

#endif//RIGID_BODY_H