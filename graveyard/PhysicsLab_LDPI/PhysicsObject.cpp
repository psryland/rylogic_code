//*******************************************************************
//
//	A physics object displayed in the PhysicsLab LineDrawer plugin
//
//*******************************************************************
#include "Stdafx.h"
#include "PR/Common/Fmt.h"
#include "PR/Common/StdString.h"
#include "PhysicsLab_LDPI/PhysicsObject.h"
#include "PhysicsLab_LDPI/PhysicsLab.h"

//*****
// Construction
PhysicsObject::PhysicsObject()
:m_name("")
,m_handle(InvalidObjectHandle)
,m_colour(0xFFFFFFFF)
,m_instance_to_world(m4x4Identity)
,m_bbox()
,m_physics()
,m_dynamic_object()
,m_physics_model_index(0xFFFFFFFF)
,m_velocity(InvalidObjectHandle)
,m_ang_velocity(InvalidObjectHandle)
,m_ang_momentum(InvalidObjectHandle)
{}

PhysicsObject::~PhysicsObject()
{}

//*****
// Update our transform in lineDrawer
void PhysicsObject::UpdateTransform()
{
	ldrSetObjectTransform(m_handle, m_instance_to_world);

	UnRegVelocity();
	if( PhysicsLab::Get().m_show_velocity ) RegVelocity();
	
	UnRegAngVelocity();
	if( PhysicsLab::Get().m_show_ang_velocity ) RegAngVelocity();

	UnRegAngMomentum();
	if( PhysicsLab::Get().m_show_ang_momentum ) RegAngMomentum();
}

//*****
// Show the velocity vector
void PhysicsObject::RegVelocity()
{
	const v4& pos = m_instance_to_world[3];
	const v4& vel = m_physics.m_velocity;
	std::string str = Fmt("*Line %s_vel FFFFFF00 { %f %f %f %f %f %f }\n",
							m_name.c_str(),
							pos[0], pos[1], pos[2],
							pos[0] + vel[0], pos[1] + vel[1], pos[2] + vel[2]);
	m_velocity = ldrRegisterObject(str.c_str(), (uint)str.length());
}

//*****
// Show the velocity vector
void PhysicsObject::RegAngVelocity()
{
	const v4& pos = m_instance_to_world[3];
	const v4& vel = m_physics.m_ang_velocity;
	std::string str = Fmt("*Line %s_ang_vel FF0000FF { %f %f %f %f %f %f }\n",
							m_name.c_str(),
							pos[0], pos[1], pos[2],
							pos[0] + vel[0], pos[1] + vel[1], pos[2] + vel[2]);
	m_ang_velocity = ldrRegisterObject(str.c_str(), (uint)str.length());
}

//*****
// Show the ang momentum vector
void PhysicsObject::RegAngMomentum()
{
	const v4& pos = m_instance_to_world[3];
	const v4& mom = m_physics.m_ang_momentum;
	std::string str = Fmt("*Line %s_ang_mom FFFF0000 { %f %f %f %f %f %f }\n",
							m_name.c_str(),
							pos[0], pos[1], pos[2],
							pos[0] + mom[0], pos[1] + mom[1], pos[2] + mom[2]);
	m_ang_momentum = ldrRegisterObject(str.c_str(), (uint)str.length());
}

//*****
// Compile a line drawer string for this object and register it with LineDrawer
void PhysicsObject::RegisterObject()
{
	std::string ldr_string;

	GenerateLdrString(ldr_string);

	m_handle = ldrRegisterObject(ldr_string.c_str(), (uint)ldr_string.length());
	ldrSetObjectTransform(m_handle, m_instance_to_world);
}

//*****
// Generate the line drawer string for this object
void PhysicsObject::GenerateLdrString(std::string& ldr_string)
{
	PR_ASSERT(PR_DBG_PHLAB, m_physics.m_physics_object);
	ldr_string.clear();

	ldr_string += Fmt("*Group %s %8.8X\n{\n", m_name.c_str(), m_colour);
	for( uint p = 0; p < m_physics.NumPrimitives(); ++p )
	{
		GenerateLdrStringPrimitive(ldr_string, m_physics.Primitive(p));
	}
	ldr_string += "}\n ";
}

//*****
// Add a primitive to the string
void PhysicsObject::GenerateLdrStringPrimitive(std::string& ldr_string, const Primitive& prim)
{
	const m4x4& prim_to_obj = prim.m_primitive_to_object;
	switch( prim.m_type )
	{
	case Primitive::Box:
		{
			ldr_string += Fmt(	"*BoxWHD b %8.8X "
								"{ "
									"%0.3f %0.3f %0.3f "
									"*Transform "
									"{ "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
									"} "
								"}\n",
								m_colour,
								prim.m_radius[0] * 2.0f, prim.m_radius[1] * 2.0f, prim.m_radius[2] * 2.0f,
								prim_to_obj[0][0], prim_to_obj[1][0], prim_to_obj[2][0], prim_to_obj[3][0], 
								prim_to_obj[0][1], prim_to_obj[1][1], prim_to_obj[2][1], prim_to_obj[3][1], 
								prim_to_obj[0][2], prim_to_obj[1][2], prim_to_obj[2][2], prim_to_obj[3][2], 
								prim_to_obj[0][3], prim_to_obj[1][3], prim_to_obj[2][3], prim_to_obj[3][3]);
		}break;
	case Primitive::Cylinder:
		{
			ldr_string += Fmt(	"*CylinderHR c %8.8X "
								"{ "
									"%0.3f %0.3f "
									"*Transform "
									"{ "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
									"} "
								"}\n",
								m_colour,
								prim.m_radius[2] * 2.0f, prim.m_radius[0],
								prim_to_obj[0][0], prim_to_obj[1][0], prim_to_obj[2][0], prim_to_obj[3][0], 
								prim_to_obj[0][1], prim_to_obj[1][1], prim_to_obj[2][1], prim_to_obj[3][1], 
								prim_to_obj[0][2], prim_to_obj[1][2], prim_to_obj[2][2], prim_to_obj[3][2], 
								prim_to_obj[0][3], prim_to_obj[1][3], prim_to_obj[2][3], prim_to_obj[3][3]);
		}break;
	case Primitive::Sphere:
		{
			ldr_string += Fmt(	"*SphereR s %8.8X "
								"{ "
									"%0.3f "
									"*Transform "
									"{ "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
										"%0.3f %0.3f %0.3f %0.3f "
									"} "
								"}\n",
								m_colour,
								prim.m_radius[0],
								prim_to_obj[0][0], prim_to_obj[1][0], prim_to_obj[2][0], prim_to_obj[3][0], 
								prim_to_obj[0][1], prim_to_obj[1][1], prim_to_obj[2][1], prim_to_obj[3][1], 
								prim_to_obj[0][2], prim_to_obj[1][2], prim_to_obj[2][2], prim_to_obj[3][2], 
								prim_to_obj[0][3], prim_to_obj[1][3], prim_to_obj[2][3], prim_to_obj[3][3]);
		}break;
	}
}
