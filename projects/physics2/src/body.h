#pragma once

#include "physics2/src/forward.h"

struct Body
{
	// Collision shape
	pr::physics::ShapeBox m_shape;

	// The rigid body for physics simulation
	pr::physics::RigidBody m_rb;

	// Graphics for the object
	View3DObject m_gfx;


	static pr::Rand& rng()
	{
		static pr::Rand r;
		return r;
	}
	static char const* Desc()
	{
		return pr::FmtS("*Box b %08X { 1 1 1 }", pr::RandomRGB(rng()).argb);
	}

	Body()
		:m_shape(pr::v4(1,1,1,0))
		,m_rb(&m_shape)
		,m_gfx(View3D_ObjectCreateLdr(Desc(), false, pr::GuidZero, false, nullptr))

	{
		m_rb.m_o2w = pr::Random4x4(rng(), pr::v4Origin, 10.0f); 
	}
	~Body()
	{
		if (m_gfx)
			View3D_ObjectDelete(m_gfx);
	}

	// Position the graphics at the rigid body location
	void UpdateGfx()
	{
		View3D_ObjectSetO2W(m_gfx, view3d::To<View3DM4x4>(m_rb.m_o2w), nullptr);
	}

	// Get/Set the body position
	pr::m4x4 const& O2W() const
	{
		return m_rb.m_o2w;
	}
	void O2W(pr::m4x4_cref o2w)
	{
		m_rb.m_o2w = o2w;
	}

	// Get/set mass
	float Mass() const
	{
		return m_rb.m_mass;
	}
	void Mass(float mass)
	{
		m_rb.m_mass = mass;
	}
};