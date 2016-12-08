#pragma once

#include "physics2/src/forward.h"

struct Body :pr::physics::RigidBody
{
	// Collision shape instance
	pr::physics::ShapeBox m_shape;

	// Graphics for the object
	View3DObject m_gfx;

	static std::default_random_engine& rng()
	{
		static std::default_random_engine r;
		return r;
	}
	static wchar_t const* Desc()
	{
		return pr::FmtS(L"*Box b %08X { 1 1 1 }", pr::RandomRGB(rng()).argb);
	}

	Body()
		:pr::physics::RigidBody(&m_shape)
		,m_shape(pr::v4(1,1,1,0))
		,m_gfx(View3D_ObjectCreateLdr(Desc(), false, false, nullptr, nullptr))
	{
		O2W(pr::Random4x4(rng(), pr::v4Origin, 10.0f));
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