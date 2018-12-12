#pragma once

#include "physics2/src/forward.h"

static int BodyIndex;

struct Body :physics::RigidBody
{
	// Collision shape instance
	physics::ShapeBox m_shape_box;
	physics::ShapeSphere m_shape_sphere;

	// Graphics for the object
	View3DObject m_gfx;

	static std::wstring Desc(physics::Shape const& shape)
	{
		static std::default_random_engine rng;
		
		std::string str;
		ldr::Shape(str, "Body", RandomRGB(rng), shape, m4x4Identity);
		return ::pr::Widen(str);
		//return pr::FmtS(L"*Box b %08X { 1 1 1 }", RandomRGB(rng()).argb);
	}

	Body()
		:physics::RigidBody(BodyIndex++ == 0 ? &m_shape_sphere.m_base : &m_shape_box.m_base)
		,m_shape_box(v4{1,1,1,0})
		,m_shape_sphere(0.5f)
		,m_gfx(View3D_ObjectCreateLdr(Desc(*m_shape).c_str(), false, nullptr, nullptr))
	{
		using namespace physics;

		// Set the inertia
		auto mp = CalcMassProperties(*m_shape, 10.0f);
		mp.m_mass = 10.0f;
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