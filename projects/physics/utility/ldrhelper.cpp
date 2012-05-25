//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "physics/utility/ldrhelper_private.h"
#include "physics/utility/assert.h"
#include "physics/utility/debug.h"

//inline void PhPrimitive(const char* name, const char* colour, const ph::Primitive& prim, m4x4 const& obj_to_world)
//{
//	m4x4 prim_to_world = prim.m_primitive_to_model * obj_to_world;

//	switch( prim.m_type )
//	{
//	case ph::EPrimitive_Box:
//		{
//			fprintf(Output(),	"*Box %s %s "
//								"{ "
//									"%0.3f %0.3f %0.3f "
//									"*Transform "
//									"{ "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//									"} "
//								"}\n",
//								name, colour,
//								prim.m_radius.x * 2.0f, prim.m_radius.y * 2.0f, prim.m_radius.z * 2.0f,
//								prim_to_world[0][0], prim_to_world[1][0], prim_to_world[2][0], prim_to_world[3][0], 
//								prim_to_world[0][1], prim_to_world[1][1], prim_to_world[2][1], prim_to_world[3][1], 
//								prim_to_world[0][2], prim_to_world[1][2], prim_to_world[2][2], prim_to_world[3][2], 
//								prim_to_world[0][3], prim_to_world[1][3], prim_to_world[2][3], prim_to_world[3][3]);
//		}break;
//	case ph::EPrimitive_Cylinder:
//		{
//			fprintf(Output(),	"*CylinderHR %s %s "
//								"{ "
//									"%0.3f %0.3f "
//									"*Transform "
//									"{ "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//									"} "
//								"}\n",
//								name, colour,
//								prim.m_radius.z * 2.0f, prim.m_radius.x,
//								prim_to_world[0][0], prim_to_world[1][0], prim_to_world[2][0], prim_to_world[3][0], 
//								prim_to_world[0][1], prim_to_world[1][1], prim_to_world[2][1], prim_to_world[3][1], 
//								prim_to_world[0][2], prim_to_world[1][2], prim_to_world[2][2], prim_to_world[3][2], 
//								prim_to_world[0][3], prim_to_world[1][3], prim_to_world[2][3], prim_to_world[3][3]);
//		}break;
//	case ph::EPrimitive_Sphere:
//		{
//			fprintf(Output(),	"*Sphere %s %s "
//								"{ "
//									"%0.3f "
//									"*Transform "
//									"{ "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//										"%0.3f %0.3f %0.3f %0.3f "
//									"} "
//								"}\n",
//								name, colour,
//								prim.m_radius.x,
//								prim_to_world[0][0], prim_to_world[1][0], prim_to_world[2][0], prim_to_world[3][0], 
//								prim_to_world[0][1], prim_to_world[1][1], prim_to_world[2][1], prim_to_world[3][1], 
//								prim_to_world[0][2], prim_to_world[1][2], prim_to_world[2][2], prim_to_world[3][2], 
//								prim_to_world[0][3], prim_to_world[1][3], prim_to_world[2][3], prim_to_world[3][3]);
//		}break;
//	};
//}
//inline void PhModel(const char* name, const char* colour, const ph::Model& model, m4x4 const& obj_to_world)
//{
//	fprintf(Output(),	"*Group %s %s\n{\n", name, colour);
//	for( const ph::Primitive *p = model.prim_begin(), *p_end = model.prim_end(); p != p_end; ++p )
//	{
//		PhPrimitive(name, colour, *p, obj_to_world);
//	}
//	fprintf(Output(),	"}\n ");
//}
//inline void PhInst(const char* name, const char* colour, const ph::Instance& instance)
//{
//	PhModel(name, colour, instance.GetModel(), instance.ObjectToWorld());
//}

//PSR...	inline void Man(const char* name, const char* colour, const MAv4& position, float height = 2.0f)
//PSR...	{
//PSR...		fprintf(Output(),	"*Group %s %s "
//PSR...							"{ "
//PSR...								"*Sphere head %s { 0.2 *Position { 0 %0.3f 0 } } "
//PSR...								"*Line body %s {  0    %0.3f 0   0     %0.3f 0 } "
//PSR...								"*Line arms %s { %0.3f %0.3f 0   %0.3f %0.3f 0 } "
//PSR...								"*Line Lleg %s { %0.3f     0 0   0     %0.3f 0 } "
//PSR...								"*Line Rleg %s { %0.3f     0 0   0     %0.3f 0 } "
//PSR...								"*Position { %0.3f %0.3f %0.3f } "
//PSR...							"}\n",
//PSR...							name, colour, 
//PSR...							colour,  height,
//PSR...							colour,  height * 0.5f,  height * 0.9f,
//PSR...							colour, -height * 0.25f, height * 0.8f, height * 0.25, height * 0.8f,
//PSR...							colour, -height * 0.25f, height * 0.5f,
//PSR...							colour,  height * 0.25f, height * 0.5f,
//PSR...							position[0], position[1], position[2]
//PSR...							);
//PSR...	}
//PSR...	inline void TannerVolume(const char* name, const char* colour, const MAv3& position, float height, float radius)
//PSR...	{
//PSR...		fprintf(Output(),	"*CylinderHR %s %s "
//PSR...							"{ "
//PSR...								"%0.3f %0.3f "
//PSR...								"*Transform "
//PSR...								"{ "
//PSR...									"1 0 0 %0.3f "
//PSR...									"0 0 1 %0.3f "
//PSR...									"0 1 0 %0.3f "
//PSR...									"0 0 0 1 "
//PSR...								"} "
//PSR...							"}\n",
//PSR...							name, colour,
//PSR...							height, radius,
//PSR...							position[0], position[1], position[2]
//PSR...							);
//PSR...	}
