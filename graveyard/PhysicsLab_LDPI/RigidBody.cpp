//*******************************************************************
//
//	A physics object displayed in the PhysicsLab LineDrawer plugin
//
//*******************************************************************
#include "Stdafx.h"
#include "RigidBody.h"

void RigidBody::GenerateLdrString()
{
	PR_ASSERT(m_physics.m_physics_object);
	m_ldr_string.Clear();

	m_ldr_string += Fmt("Group %s %8.8X\n{\n", m_name.c_str(), m_colour);
	for( uint p = 0; p < m_physics.m_physics_object->m_num_primitives; ++p )
	{
		Primitive& prim = m_physics.m_physics_object->m_primitive[p];
		switch( prim.m_type )
		{
		case Primitive::Box:
			{
				m_ldr_string += Fmt("BoxWHD b %8.8X "
									"{ "
										"%0.3f %0.3f %0.3f "
										"Transform "
										"{ "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
										"} "
									"}\n",
									m_colour,
									prim.m_radius[0] * 2.0f, prim.m_radius[1] * 2.0f, prim.m_radius[2] * 2.0f,
									prim.m_primitive_to_object[0][0], prim.m_primitive_to_object[0][1], prim.m_primitive_to_object[0][2], prim.m_primitive_to_object[0][3], 
									prim.m_primitive_to_object[1][0], prim.m_primitive_to_object[1][1], prim.m_primitive_to_object[1][2], prim.m_primitive_to_object[1][3], 
									prim.m_primitive_to_object[2][0], prim.m_primitive_to_object[2][1], prim.m_primitive_to_object[2][2], prim.m_primitive_to_object[2][3], 
									prim.m_primitive_to_object[3][0], prim.m_primitive_to_object[3][1], prim.m_primitive_to_object[3][2], prim.m_primitive_to_object[3][3]);
			}break;
		case Primitive::Cylinder:
			{
				m_ldr_string += Fmt("CylinderHR c %8.8X "
									"{ "
										"%0.3f %0.3f "
										"Transform "
										"{ "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
										"} "
									"}\n",
									m_colour,
									prim.m_radius[2] * 2.0f, prim.m_radius[0],
									prim.m_primitive_to_object[0][0], prim.m_primitive_to_object[0][1], prim.m_primitive_to_object[0][2], prim.m_primitive_to_object[0][3], 
									prim.m_primitive_to_object[1][0], prim.m_primitive_to_object[1][1], prim.m_primitive_to_object[1][2], prim.m_primitive_to_object[1][3], 
									prim.m_primitive_to_object[2][0], prim.m_primitive_to_object[2][1], prim.m_primitive_to_object[2][2], prim.m_primitive_to_object[2][3], 
									prim.m_primitive_to_object[3][0], prim.m_primitive_to_object[3][1], prim.m_primitive_to_object[3][2], prim.m_primitive_to_object[3][3]);
			}break;
		case Primitive::Sphere:
			{
				m_ldr_string += Fmt("SphereR s %8.8X "
									"{ "
										"%0.3f "
										"Transform "
										"{ "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
											"%0.3f %0.3f %0.3f %0.3f "
										"} "
									"}\n",
									m_colour,
									prim.m_radius[0],
									prim.m_primitive_to_object[0][0], prim.m_primitive_to_object[0][1], prim.m_primitive_to_object[0][2], prim.m_primitive_to_object[0][3], 
									prim.m_primitive_to_object[1][0], prim.m_primitive_to_object[1][1], prim.m_primitive_to_object[1][2], prim.m_primitive_to_object[1][3], 
									prim.m_primitive_to_object[2][0], prim.m_primitive_to_object[2][1], prim.m_primitive_to_object[2][2], prim.m_primitive_to_object[2][3], 
									prim.m_primitive_to_object[3][0], prim.m_primitive_to_object[3][1], prim.m_primitive_to_object[3][2], prim.m_primitive_to_object[3][3]);
			}break;
		};
	}
	m_ldr_string += "}\n ";
}
