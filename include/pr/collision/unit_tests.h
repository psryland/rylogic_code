//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/ldraw/ldr_helper.h"
#include "pr/collision/ldraw.h"

namespace pr::collision
{
	PRUnitTest(CollisionBoxVsBoxTests)
	{
		auto lhs = ShapeBox{v4(0.3f, 0.4f, 0.5f, 0.0f)};
		auto rhs = ShapeBox{v4(0.3f, 0.4f, 0.5f, 0.0f)};
		m4x4 l2w_[] =
		{
			m4x4Identity,
		};
		m4x4 r2w_[] =
		{
			m4x4::Transform(float(maths::tau_by_8), 0, 0, v4(0.2f, 0.3f, 0.1f, 1.0f)),
			m4x4::Transform(0, float(maths::tau_by_8), 0, v4(0.2f, 0.3f, 0.1f, 1.0f)),
			m4x4::Transform(0, 0, float(maths::tau_by_8), v4(0.2f, 0.3f, 0.1f, 1.0f)),
			m4x4::Transform(0, 0, -3*float(maths::tau_by_8), v4(0.2f, 0.3f, 0.1f, 1.0f)),
		};

		std::default_random_engine rng;
		for (int i = 0; i != 20; ++i)
		{
			Contact c;
			m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : Random4x4(rng, v4Origin, 0.5f);
			m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : Random4x4(rng, v4Origin, 0.5f);

			std::string s;
			ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
			ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
			//ldr::Write(s, L"collision_unittests.ldr");
			if (BoxVsBox(lhs, l2w, rhs, r2w, c))
			{
				ldr::LineD(s, "sep_axis", Colour32Yellow, c.m_point, c.m_axis);
				ldr::Box(s, "pt0", Colour32Yellow, 0.01f, c.m_point - 0.5f*c.m_depth*c.m_axis);
				ldr::Box(s, "pt1", Colour32Yellow, 0.01f, c.m_point + 0.5f*c.m_depth*c.m_axis);
			}
			//ldr::Write(s, L"collision_unittests.ldr");
		}
	}
	PRUnitTest(CollisionBoxVsLine)
	{
		auto lhs = ShapeBox{v4{0.3f, 0.5f, 0.2f, 0.0f}};
		auto rhs = ShapeLine{3.0f};
		m4x4 l2w_[] =
		{
			m4x4Identity,
		};
		m4x4 r2w_[] =
		{
			m4x4::Transform(float(maths::tau_by_8), float(maths::tau_by_8), float(maths::tau_by_8), v4(0.2f, 0.3f, 0.1f, 1.0f)),
		};

		std::default_random_engine rng;
		for (int i = 0; i != 20; ++i)
		{
			Contact c;
			m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : Random4x4(rng, v4Origin, 0.3f);
			m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : Random4x4(rng, v4Origin, 0.3f);

			std::string s;
			ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
			ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
			//ldr::Write(s, L"collision_unittests.ldr");
			if (!BoxVsLine(lhs, l2w, rhs, r2w, c))
				continue;

			ldr::LineD(s, "sep_axis", Colour32Yellow, c.m_point - 0.5f*c.m_depth*c.m_axis, c.m_axis);
			ldr::Box(s, "pt0", Colour32Yellow, 0.002f, c.m_point - 0.5f*c.m_depth*c.m_axis);
			ldr::Box(s, "pt1", Colour32Yellow, 0.002f, c.m_point + 0.5f*c.m_depth*c.m_axis);
			//ldr::Write(s, L"collision_unittests.ldr");
		}
	}
	PRUnitTest(CollisionSphereVsBox)
	{
		auto lhs = ShapeSphere{0.3f};
		auto rhs = ShapeBox   {v4{0.3f, 0.4f, 0.5f, 0.0f}};
		m4x4 l2w_[] =
		{
			m4x4Identity,
		};
		m4x4 r2w_[] =
		{
			m4x4::Transform(float(maths::tau_by_8), float(maths::tau_by_8), float(maths::tau_by_8), v4(0.2f, 0.3f, 0.1f, 1.0f)),
		};

		std::default_random_engine rng;
		for (int i = 0; i != 20; ++i)
		{
			Contact c;
			m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : Random4x4(rng, v4Origin, 0.5f);
			m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : Random4x4(rng, v4Origin, 0.5f);

			std::string s;
			ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
			ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
			//ldr::Write(s, "collision_unittests.ldr");
			if (SphereVsBox(lhs, l2w, rhs, r2w, c))
			{
				ldr::LineD(s, "sep_axis", Colour32Yellow, c.m_point, c.m_axis);
				ldr::Box(s, "pt0", Colour32Yellow, 0.01f, c.m_point - 0.5f*c.m_depth*c.m_axis);
				ldr::Box(s, "pt1", Colour32Yellow, 0.01f, c.m_point + 0.5f*c.m_depth*c.m_axis);
			}
			//ldr::Write(s, "collision_unittests.ldr");
		}
	}
	PRUnitTest(CollisionSphereVsSphere)
	{
		auto lhs = ShapeSphere{0.3f};
		auto rhs = ShapeSphere{0.4f};
		m4x4 l2w_[] =
		{
			m4x4Identity,
		};
		m4x4 r2w_[] =
		{
			m4x4::Transform(float(maths::tau_by_8), float(maths::tau_by_8), float(maths::tau_by_8), v4(0.2f, 0.3f, 0.1f, 1.0f)),
		};

		std::default_random_engine rng;
		for (int i = 0; i != 20; ++i)
		{
			Contact c;
			m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : Random4x4(rng, v4Origin, 0.5f);
			m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : Random4x4(rng, v4Origin, 0.5f);

			std::string s;
			ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
			ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
			//ldr::Write(s, L"collision_unittests.ldr");
			if (SphereVsSphere(lhs, l2w, rhs, r2w, c))
			{
				ldr::LineD(s, "sep_axis", Colour32Yellow, c.m_point, c.m_axis);
				ldr::Box(s, "pt0", Colour32Yellow, 0.01f, c.m_point - 0.5f*c.m_depth*c.m_axis);
				ldr::Box(s, "pt1", Colour32Yellow, 0.01f, c.m_point + 0.5f*c.m_depth*c.m_axis);
			}
			//ldr::Write(s, L"collision_unittests.ldr");
		}
	}
}
#endif