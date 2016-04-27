//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	struct alignas(16) BSphere
	{
		v4 m_ctr_rad; // x,y,z = position, 'w' = radius

		BSphere() = default;
		BSphere(v4 const& centre, float radius)
			:m_ctr_rad(centre.xyz, radius)
		{}

		// Reset this bsphere to invalid
		BSphere& reset()
		{
			m_ctr_rad = -v4Origin;
			return *this;
		}

		// Returns true if this bsphere does not bound anything
		bool empty() const
		{
			return m_ctr_rad.w < 0;
		}

		// Set this bsphere to a unit sphere centred on the origin
		BSphere& unit()
		{
			m_ctr_rad = v4Origin;
			return *this;
		}

		// The centre of the bsphere
		v4 Centre() const
		{
			return m_ctr_rad.w1();
		}

		// The radius of the bsphere
		float RadiusSq() const
		{
			return Sqr(Radius());
		}
		float Radius() const
		{
			return m_ctr_rad.w;
		}

		// The diameter of the bsphere
		float DiametreSq() const
		{
			return Sqr(Diametre());
		}
		float Diametre() const
		{
			return 2.0f * m_ctr_rad.w;
		}
	};
	static_assert(std::is_pod<BSphere>::value || _MSC_VER < 1900, "Should be a pod type");
	static_assert(std::alignment_of<BSphere>::value == 16, "Should be 16 byte aligned");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	using BSphere_cref = BSphere const;
	#else
	using BSphere_cref = BSphere const&;
	#endif

	#pragma region Constants
	static BSphere const BSphereZero  = {v4Zero, 0.0f};
	static BSphere const BSphereUnit  = {v4Origin, 1.0f};
	static BSphere const BSphereReset = {v4Origin, -1.0f};
	#pragma endregion

	#pragma region Operators
	inline bool operator == (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	inline BSphere& pr_vectorcall operator += (BSphere& lhs, v4_cref offset)
	{
		assert(offset.w == 0.0f);
		lhs.m_ctr_rad += offset;
		return lhs;
	}
	inline BSphere& pr_vectorcall operator -= (BSphere& lhs, v4_cref offset)
	{
		assert(offset.w == 0.0f);
		lhs.m_ctr_rad -= offset;
		return lhs;
	}
	inline BSphere& operator *= (BSphere& lhs, float s)
	{
		lhs.m_ctr_rad.w *= s;
		return lhs;
	}
	inline BSphere& operator /= (BSphere& lhs, float s)
	{
		lhs *= (1.0f / s);
		return lhs;
	}
	inline BSphere pr_vectorcall operator + (BSphere_cref bsph, v4_cref offset)
	{
		auto bs = bsph;
		return bs += offset;
	}
	inline BSphere pr_vectorcall operator - (BSphere_cref bsph, v4_cref offset)
	{
		auto bs = bsph;
		return bs -= offset;
	}
	inline BSphere pr_vectorcall operator * (BSphere_cref bsph, float s)
	{
		auto bs = bsph;
		return bs *= s;
	}
	inline BSphere pr_vectorcall operator * (float s, BSphere_cref bsph)
	{
		auto bs = bsph;
		return bs *= s;
	}
	inline BSphere pr_vectorcall operator * (m4x4_cref m, BSphere_cref bsph)
	{
		return BSphere(m * bsph.Centre(), bsph.m_ctr_rad.w);
	}
	#pragma endregion

	#pragma region Functions

	// The volume of the bsphere
	inline float Volume(BSphere const& bsph)
	{
		return 4.188790f * bsph.m_ctr_rad.w * bsph.m_ctr_rad.w * bsph.m_ctr_rad.w; // (2/3)*tau*r^3
	}

	// Encompass 'point' within 'bsphere' and re-centre the centre point.
	inline BSphere& pr_vectorcall Encompass(BSphere& bsphere, v4_cref point)
	{
		if (bsphere.Radius() < 0.0f)
		{
			// Centre on this point, since it's the first
			bsphere.m_ctr_rad = v4(point.x, point.y, point.z, 0.0f);
		}
		else
		{
			// Only grow if outside the current bounds
			auto len_sq = Length3Sq(point - bsphere.Centre());
			if (len_sq > bsphere.RadiusSq())
			{
				// Move the centre and increase the radius by the minimum
				// amount to include the existing bsphere and 'point'
				auto separation = Sqrt(len_sq);
				auto new_radius = (separation + bsphere.Radius()) * 0.5f;
				bsphere += (point - bsphere.Centre()) * ((new_radius - bsphere.Radius()) / separation);
				bsphere.m_ctr_rad.w = new_radius;
			}
		}
		return bsphere;
	}
	inline BSphere pr_vectorcall Encompass(BSphere const& bsphere, v4_cref point)
	{
		auto bsph = bsphere;
		return Encompass(bsph, point);
	}

	// Encompass 'rhs' in 'lhs'
	inline BSphere& pr_vectorcall Encompass(BSphere& lhs, BSphere_cref rhs)
	{
		if (lhs.Radius() < 0.0f)
		{
			// If this is the first thing, just adopt 'rhs'
			lhs = rhs;
		}
		else
		{
			// Only grow if 'rhs' extends beyond the current radius
			auto separation = Length3(rhs.Centre() - lhs.Centre());
			if (separation + rhs.Radius() > lhs.Radius())
			{
				// Move the centre and increase the radius by the minimum
				// amount to include the existing bsphere and 'rhs'
				auto new_radius = (separation + lhs.Radius() + rhs.Radius()) * 0.5f;
				lhs += (rhs.Centre() - lhs.Centre()) * ((new_radius - lhs.Radius()) / separation);
				lhs.m_ctr_rad.w = new_radius;
			}
		}
		return lhs;
	}
	inline BSphere pr_vectorcall Encompass(BSphere const& lhs, BSphere_cref rhs)
	{
		auto bsph = lhs;
		return Encompass(bsph, rhs);
	}

	// Encompass 'point' within 'bsphere' without moving the centre point.
	inline BSphere& pr_vectorcall EncompassLoose(BSphere& bsphere, v4_cref point)
	{
		if (bsphere.m_ctr_rad.w < 0.0f)
		{
			bsphere.m_ctr_rad = v4(point.x, point.y, point.z, 0.0f);
		}
		else
		{
			auto len_sq = Length3Sq(point - bsphere.Centre());
			if (len_sq > bsphere.RadiusSq())
				bsphere.m_ctr_rad.w = Sqrt(len_sq);
		}
		return bsphere;
	}
	inline BSphere pr_vectorcall EncompassLoose(BSphere const& bsphere, v4_cref point)
	{
		auto bsph = bsphere;
		return EncompassLoose(bsph, point);
	}

	// Encompass 'rhs' in 'lhs' without moving the centre point
	inline BSphere& pr_vectorcall EncompassLoose(BSphere& lhs, BSphere_cref rhs)
	{
		if (lhs.Radius() < 0.0f)
		{
			lhs = rhs;
		}
		else
		{
			auto new_radius = Length3(rhs.Centre() - lhs.Centre()) + rhs.Radius();
			if (new_radius > lhs.Radius())
				lhs.m_ctr_rad.w = new_radius;
		}
		return lhs;
	}
	inline BSphere pr_vectorcall EncompassLoose(BSphere const& lhs, BSphere_cref rhs)
	{
		auto bsph = lhs;
		return EncompassLoose(bsph, rhs);
	}

	// Return true if 'point' is within the bounding sphere
	inline bool pr_vectorcall IsWithin(BSphere_cref bsphere, v4_cref point, float tol = 0)
	{
		return Length3Sq(point - bsphere.Centre()) <= bsphere.RadiusSq() + tol;
	}
	inline bool pr_vectorcall IsWithin(BSphere_cref bsphere, BSphere_cref test, float tol = 0)
	{
		return Length3Sq(test.Centre() - bsphere.Centre()) <= Sqr(bsphere.Radius() - test.Radius() + tol);
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool pr_vectorcall IsIntersection(BSphere_cref lhs, BSphere_cref rhs)
	{
		return Length3Sq(rhs.Centre() - lhs.Centre()) < Sqr(lhs.Radius() + rhs.Radius());
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_bsphere)
		{
			v4 pt[] =
			{
				{0, 0, 0, 1},
				{1, 1, 1, 1},
				{0, 0, 1, 1},
				{0, 1, 0, 1},
				{0, 1, 1, 1},
				{1, 0, 0, 1},
				{1, 0, 1, 1},
				{1, 1, 0, 1},
			};
			auto bsph = BSphereReset;
			for (auto& p : pt)
				pr::Encompass(bsph, p);
			PR_CHECK(FEql(bsph.Centre(), v4(0.5f, 0.5f, 0.5f, 1)), true);
			PR_CHECK(FEql(bsph.Radius(), 0.8660254f), true);
		}
	}
}
#endif