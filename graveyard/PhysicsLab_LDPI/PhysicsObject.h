//*******************************************************************
//
//	A physics object displayed in the PhysicsLab LineDrawer plugin
//
//*******************************************************************
#ifndef PHYSICS_OBJECT_H
#define PHYSICS_OBJECT_H

#include "PR/Common/StdVector.h"
#include "PR/Common/StdString.h"
#include "PR/Maths/Maths.h"
#include "PR/DynamicObjectMap/DynamicObject.h"
#include "PR/Physics/Physics.h"
#include "LineDrawer/PlugInInterface.h"

class PhysicsObject
{
public:
	PhysicsObject();
	~PhysicsObject();

	void UpdateTransform();
	Instance& Physics()						{ return m_physics; }
	void RegVelocity();
	void RegAngVelocity();
	void RegAngMomentum();
	void UnRegVelocity()						{ if( m_velocity     != InvalidObjectHandle ) ldrUnRegisterObject(m_velocity);     m_velocity     = InvalidObjectHandle; }
	void UnRegAngVelocity()						{ if( m_ang_velocity != InvalidObjectHandle ) ldrUnRegisterObject(m_ang_velocity); m_ang_velocity = InvalidObjectHandle; }
	void UnRegAngMomentum()						{ if( m_ang_momentum != InvalidObjectHandle ) ldrUnRegisterObject(m_ang_momentum); m_ang_momentum = InvalidObjectHandle; }
	
private:
	friend class SceneParser;
	void RegisterObject();
	void GenerateLdrString(std::string& ldr_string);
	void GenerateLdrStringPrimitive(std::string& ldr_string, const Primitive& prim);

private:
	std::string			m_name;					// A name for the object
	ObjectHandle		m_handle;				// A handle for the object in LineDrawer
	uint				m_colour;				// The object colour
	m4x4				m_instance_to_world;	// Position/Orientation in the world
	BoundingBox			m_bbox;					// Object-Orientated bounding box
	ph::Instance		m_physics;				// A physics instance for this object
	DynamicObject		m_dynamic_object;		// Used to track this object in the world

	// Generation members
	uint				m_physics_model_index;	// The index of the physics model in the model list

	//
	ObjectHandle		m_velocity;				// Handle to a line drawer object showing the velocity
	ObjectHandle		m_ang_velocity;			// Handle to a line drawer object showing the ang velocity
	ObjectHandle		m_ang_momentum;			// Handle to a line drawer object showing the ang momentum
};

typedef std::vector<PhysicsObject> TObjectContainer;

#endif//PHYSICS_OBJECT_H
