#pragma once

#include "physics2/src/forward.h"

struct Body :physics::RigidBody
{
	// Collision shape instance
	physics::ShapeBox m_shape;

	// Graphics for the object
	View3DObject m_gfx;

	static std::default_random_engine& rng()
	{
		static std::default_random_engine r;
		return r;
	}
	static wchar_t const* Desc()
	{
		return pr::FmtS(L"*Box b %08X { 1 1 1 }", RandomRGB(rng()).argb);
	}

	Body()
		:physics::RigidBody(&m_shape)
		,m_shape(v4{1,1,1,0})
		,m_gfx(View3D_ObjectCreateLdr(Desc(), false, nullptr, nullptr))
	{
		using namespace physics;

		// Set the inertia
		auto mp = CalcMassProperties(m_shape, 10.0f);
		SetMassProperties(physics::Inertia{mp}, mp.m_centre_of_mass);

		// Update the graphics
		UpdateGfx();
	}
	~Body()
	{
		if (m_gfx)
			View3D_ObjectDelete(m_gfx);
	}

	// Position the graphics at the rigid body location
	void UpdateGfx()
	{
		View3D_ObjectO2WSet(m_gfx, view3d::To<View3DM4x4>(m_o2w), nullptr);
	}
};