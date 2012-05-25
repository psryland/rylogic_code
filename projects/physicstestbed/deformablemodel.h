//******************************************
// CollisionModel
//******************************************
#pragma once
#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
#	include "PR/Geometry/DeformableMesh.h"
#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
#	include "Physics/Include/Physics.h"
#	include "Physics/Include/PhysicsDev.h"
#endif

struct DeformableModel
{
	DeformableModel()
	:m_model(0)
	,m_name("deformable")
	,m_anchor_colour(pr::Colour32Red)
	,m_spring_colour(pr::Colour32Blue)
	,m_beam_colour(pr::Colour32Red)
	,m_velocity_colour(pr::Colour32Yellow)
	,m_show_velocity(false)
	,m_convex_tolerance(0.01f)
	,m_model_to_CoMframe(pr::m4x4Identity)
	,m_CoMframe_to_model(pr::m4x4Identity)
	,m_inertia_tensor(pr::m3x3Identity)
	,m_ms_bbox(pr::BBoxUnit)
	,m_mass(1.0f)
	{}

	#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
	pr::deformable::Mesh*	m_model;
	#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
	PHdeformable::Instance	m_model_buffer;
	PHdeformable::Instance*	m_model;
	#endif
	pr::TBinaryData		m_buffer;

	std::string			m_name;
	pr::Colour32		m_anchor_colour;
	pr::Colour32		m_spring_colour;
	pr::Colour32		m_beam_colour;
	pr::Colour32		m_velocity_colour;
	bool				m_show_velocity;
	float				m_convex_tolerance;

	// Mass properties
	pr::m4x4			m_model_to_CoMframe;
	pr::m4x4			m_CoMframe_to_model;
	pr::m3x3			m_inertia_tensor;
	pr::BoundingBox		m_ms_bbox;
	float				m_mass;
};
