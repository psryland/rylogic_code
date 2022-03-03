//******************************************
// CollisionModel
//******************************************
#pragma once
#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
	#include "pr/Physics/Physics.h"
#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
	#include "Physics/Include/Physics.h"
	#include "Physics/Include/PhysicsDev.h"
#else
#endif

struct CollisionModel
{
	CollisionModel()
	:m_shape(0)
	,m_shape_id()
	,m_buffer()
	,m_name("col_model")
	,m_colour(pr::Colour32White)
	,m_model_to_CoMframe(pr::m4x4Identity)
	,m_CoMframe_to_model(pr::m4x4Identity)
	,m_inertia_tensor(pr::m3x3Identity)
	,m_mass(1.0f)
	{}

	#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
	pr::ph::Shape*		m_shape;
	pr::uint			m_shape_id;
	pr::TBinaryData		m_buffer;
	#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
	PHcollisionModel*	m_shape;
	PHmodelReference	m_shape_id;
	PHcollision::ModelBuffer<10000> m_buffer;
	#endif

	std::string			m_name;
	pr::Colour32		m_colour;

	// Mass properties
	pr::m4x4			m_model_to_CoMframe;
	pr::m4x4			m_CoMframe_to_model;
	pr::m3x3			m_inertia_tensor;
	pr::BoundingBox		m_ms_bbox;
	float				m_mass;
};
