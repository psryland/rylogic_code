//**********************************************************************
//
//	Scene Parser
//
//**********************************************************************
//	Builds a list of PhysicsObjects and events from a script file
//
#include "Stdafx.h"
#include "PR/Common/StdAlgorithm.h"
#include "PhysicsLab_LDPI/SceneParser.h"
#include "PhysicsLab_LDPI/PhysicsLab.h"
#include "PhysicsLab_LDPI/PhysicsMaterials.h"

//*****
// Construction
SceneParser::SceneParser()
:m_physics_object_builder()
,m_loader()
,m_keyword()
,m_object_list(0)
,m_object(0)
,m_event(0)
{}

SceneParser::~SceneParser()
{
	UnInitialise();
}

//*****
// Initialise the scene parser
bool SceneParser::Initialise()
{
	// Initialise the physics object builder
	PhysicsObjectBuilderSettings obsettings;
	obsettings.m_material				= g_physics_materials;
	obsettings.m_num_materials			= g_max_physics_materials;
	Verify(m_physics_object_builder.Initialise(obsettings));
	return true;
}

//*****
// UnInitialise the scene parser
void SceneParser::UnInitialise()
{}

//*****
// Parse a physics scene and create all of the physics objects
bool SceneParser::Parse(const char* filename)
{
	// Clear the data from the simulation
	PhysicsLab::Get().m_simulation.UnInitialise();
	m_object_list					= &PhysicsLab::Get().m_simulation.m_object_list;
	m_object						= &PhysicsLab::Get().m_simulation.m_object;
	m_event							= &PhysicsLab::Get().m_simulation.m_event;
	PhysicsEngine* physics_engine	= &PhysicsLab::Get().m_simulation.m_physics_engine;
	v4 gravity						= {0.0f, -1.0f, 0.0f, 0.0f};

	Verify(m_physics_object_builder.Reset());

	// Parse a Physics Scene
	if( Failed(m_loader.LoadFromFile(filename)) ) return false;
	while( m_loader.GetKeyword(m_keyword) )
	{
		if( str::EqualsNoCase(m_keyword, "PhysicsObject") )
		{
			m_object->push_back(PhysicsObject());
			if( !ParsePhysicsObject(m_object->back()) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Event") )
		{
			m_event->push_back(Event());
			if( !ParseEvent(m_event->back()) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Gravity") )
		{
			if( !m_loader.ExtractVector3(gravity, 0.0f) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Density") )
		{
			if( !m_loader.ExtractFloat(g_physics_materials[0].m_density) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "StaticFriction") )
		{
			if( !m_loader.ExtractFloat(g_physics_materials[0].m_static_friction) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "DynamicFriction") )
		{
			if( !m_loader.ExtractFloat(g_physics_materials[0].m_dynamic_friction) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Elasticity") )
		{
			if( !m_loader.ExtractFloat(g_physics_materials[0].m_elasticity) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "TangentialElasticity") )
		{
			if( !m_loader.ExtractFloat(g_physics_materials[0].m_tangential_elasticity) ) return false;
		}
	}

	Verify(m_physics_object_builder.ExportPhysicsObjectList(*m_object_list));
	
	// Complete the physics objects
	for( uint i = 0; i < m_object->size(); ++i )
	{
		PhysicsObject& object = (*m_object)[i];
		
		object.m_dynamic_object.m_owner			= this;
		object.m_dynamic_object.m_bounding_box	= &object.m_bbox;
		
		object.m_physics.m_physics_object		= m_object_list->GetObject(object.m_physics_model_index);
		object.m_physics.m_object_to_world		= &object.m_instance_to_world;
		object.m_physics.m_collision_group		= 0;
		object.m_physics.Reset();
		object.m_physics.SetGravity				(gravity);
		object.m_physics.SetAngVelocity			(object.m_physics.m_ang_velocity);
			
		object.m_name;
		object.RegisterObject();
		object.m_colour;
		object.m_instance_to_world;
		object.m_bbox							= object.m_physics.WorldBBox();

		// Register the object with the physics engine
		physics_engine->Add(&object.m_physics);
	}

	// Complete the events
	for( uint i = 0; i < m_event->size(); ++i )
	{
		Event& event = (*m_event)[i];
		event.m_target = FindObject(event.m_target_name);
	}

	// Sort the events
	if( !m_event->empty() )
		std::sort(m_event->begin(), m_event->end());

	// Create the ground plane
	PhysicsLab::Get().m_simulation.m_ground.RegisterObject();

	return true;
}

//*****
// Parse a physics object
bool SceneParser::ParsePhysicsObject(PhysicsObject& object)
{
	if( !m_loader.FindSectionStart() ) return false;
	while( m_loader.GetKeyword(m_keyword) )
	{
		if( str::EqualsNoCase(m_keyword, "Name") )
		{
			if( !m_loader.ExtractString(object.m_name) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Colour") )
		{
			if( !m_loader.ExtractUInt(object.m_colour, 16) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Transform") )
		{
			if( !ParseTransform(object.m_instance_to_world) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Position") )
		{
			if( !m_loader.ExtractVector3(object.m_instance_to_world[3], 1.0f) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Velocity") )
		{
			if( !m_loader.ExtractVector3(object.m_physics.m_velocity, 0.0f) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "AngVel") )
		{
			if( !m_loader.ExtractVector3(object.m_physics.m_ang_velocity, 0.0f) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Model") )
		{
			if( !ParseModel(object) ) return false;
		}
	}
	if( !m_loader.FindSectionEnd() ) return false;
	return true;
}

//*****
// Parse a physics model description
bool SceneParser::ParseModel(PhysicsObject& object)
{
	// Start a new model
	Verify(m_physics_object_builder.Begin());

	if( !m_loader.FindSectionStart() ) return false;
	while( m_loader.GetKeyword(m_keyword) )
	{
		if( str::EqualsNoCase(m_keyword, "Primitive") )
		{
			Primitive prim;
			if( !ParsePrimitive(prim) ) return false;
			Verify(m_physics_object_builder.AddPrimitive(prim));
		}
	}
	if( !m_loader.FindSectionEnd() ) return false;

	// Complete the model and record its index in the model list
	Verify(m_physics_object_builder.End(&object.m_physics_model_index));
	return true;
}

//*****
// Parse a physics model primitive
bool SceneParser::ParsePrimitive(Primitive& prim)
{
	// Give the primitive default values
	prim.m_type					= Primitive::Box;
	prim.m_radius[0]			= 1.0f;
	prim.m_radius[1]			= 1.0f;
	prim.m_radius[2]			= 1.0f;
	prim.m_material_index		= 0;
	prim.m_primitive_to_object	= m4x4Identity;
	
	// Extract details about the primitive
	if( !m_loader.FindSectionStart() ) return false;
	while( m_loader.GetKeyword(m_keyword) )
	{
		if( str::EqualsNoCase(m_keyword, "Type") )
		{
			std::string type_str;
			if( !m_loader.ExtractString(type_str) ) return false;
				 if( str::EqualsNoCase(type_str, "box") )		prim.m_type = Primitive::Box;
			else if( str::EqualsNoCase(type_str, "cylinder") )	prim.m_type = Primitive::Cylinder;
			else if( str::EqualsNoCase(type_str, "sphere") )	prim.m_type = Primitive::Sphere;
			else return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Dim") )
		{
			if( !m_loader.ExtractFloat(prim.m_radius[0]) ) return false;
			if( !m_loader.ExtractFloat(prim.m_radius[1]) ) return false;
			if( !m_loader.ExtractFloat(prim.m_radius[2]) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Material") )
		{
			if( !m_loader.ExtractUInt(prim.m_material_index, 10) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Transform") )
		{
			if( !ParseTransform(prim.m_primitive_to_object) ) return false;
		}
	}
	if( !m_loader.FindSectionEnd() ) return false;
	return true;
}

//*****
// Parse a transform
bool SceneParser::ParseTransform(m4x4& transform)
{
	if( !m_loader.FindSectionStart() ) return false;
	if( !m_loader.Extractm4x4(transform) ) return false;
	if( !m_loader.FindSectionEnd() ) return false;
	return true;
}

//*****
// Parse an event
bool SceneParser::ParseEvent(Event& event)
{
	if( !m_loader.FindSectionStart() ) return false;
	while( m_loader.GetKeyword(m_keyword) )
	{
		if( str::EqualsNoCase(m_keyword, "Type") )
		{
			std::string type;
			if( !m_loader.ExtractString(type) ) return false;
				 if( str::EqualsNoCase(type, "Impulse") )	event.m_type = Event::eImpulse;
			else if( str::EqualsNoCase(type, "Moment") )	event.m_type = Event::eMoment;
			else return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Obj") )
		{
			if( !m_loader.ExtractString(event.m_target_name) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Pos") )
		{
			if( !m_loader.ExtractVector3(event.m_position, 1.0f) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Dir") )
		{
			if( !m_loader.ExtractVector3(event.m_direction, 0.0f) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Mag") )
		{
			if( !m_loader.ExtractFloat(event.m_magnitude) ) return false;
		}
		else if( str::EqualsNoCase(m_keyword, "Time") )
		{
			if( !m_loader.ExtractFloat(event.m_time) ) return false;
		}
	}
	if( !m_loader.FindSectionEnd() ) return false;
	return true;
}

//*****
// Search the object list for an object that matches 'name'
PhysicsObject* SceneParser::FindObject(std::string& name)
{
	// Complete the physics objects
	for( uint i = 0; i < m_object->size(); ++i )
	{
		if( str::EqualsNoCase(name, (*m_object)[i].m_name) ) return &(*m_object)[i];
	}
	return 0;
}
