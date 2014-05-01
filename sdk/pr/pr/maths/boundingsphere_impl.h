//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_BOUNDING_SPHERE_IMPL_H
#define PR_MATHS_BOUNDING_SPHERE_IMPL_H

#include "pr/maths/boundingsphere.h"

namespace pr
{
	// Type methods
	inline BSphere  BSphere::make(v4 const& centre, float radius)     { BSphere s = {{centre.x, centre.y, centre.z, radius}}; return s; }
	inline BSphere& BSphere::set(v4 const& centre, float radius)      { m_ctr_rad = centre; m_ctr_rad.w = radius; return *this; }
	inline BSphere& BSphere::zero()                                   { m_ctr_rad = v4Zero; return *this; }
	inline BSphere& BSphere::unit()                                   { m_ctr_rad = v4Origin; return *this; }
	inline BSphere& BSphere::reset()                                  { m_ctr_rad = -v4Origin; return *this; }
	inline bool     BSphere::IsValid() const                          { return Volume(*this) >= 0.0f; }
	inline v4       BSphere::Centre() const                           { return m_ctr_rad.w1(); }
	inline float    BSphere::Radius() const                           { return m_ctr_rad.w; }
	inline float    BSphere::RadiusSq() const                         { return Sqr(m_ctr_rad.w); }
	inline float    BSphere::Diametre() const                         { return 2.0f * m_ctr_rad.w; }
	inline float    BSphere::DiametreSq() const                       { return Sqr(Diametre()); }

	// Assignment operators
	inline BSphere& operator += (BSphere& lhs, v4 const& offset)      { assert(offset.w == 0.0f); lhs.m_ctr_rad += offset; return lhs; }
	inline BSphere& operator -= (BSphere& lhs, v4 const& offset)      { assert(offset.w == 0.0f); lhs.m_ctr_rad -= offset; return lhs; }
	inline BSphere& operator *= (BSphere& lhs, float s)               { lhs.m_ctr_rad.w *= s; return lhs; }
	inline BSphere& operator /= (BSphere& lhs, float s)               { lhs *= (1.0f / s); return lhs; }

	// Binary operators
	inline BSphere operator + (BSphere const& bsph, v4 const& offset) { BSphere bs = bsph; return bs += offset; }
	inline BSphere operator - (BSphere const& bsph, v4 const& offset) { BSphere bs = bsph; return bs -= offset; }
	inline BSphere operator * (BSphere const& bsph, float s)          { BSphere bs = bsph; return bs *= s; }
	inline BSphere operator * (float s, BSphere const& bsph)          { BSphere bs = bsph; return bs *= s; }
	inline BSphere operator * (m4x4 const& m, BSphere const& bsph)    { BSphere bs; bs.m_ctr_rad = m * bsph.Centre(); bs.m_ctr_rad.w = bsph.m_ctr_rad.w; return bs; }

	// Equality operators
	inline bool operator == (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (BSphere const& lhs, BSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	inline float Volume(BSphere const& bsph)
	{
		return 4.188790f * bsph.m_ctr_rad.w * bsph.m_ctr_rad.w * bsph.m_ctr_rad.w; // (4/3)*pi*r^3
	}

	// Encompass 'point' within 'bsphere' and re-centre the centre point.
	inline BSphere& Encompass(BSphere& bsphere, v4 const& point)
	{
		if (bsphere.Radius() < 0.0f) { bsphere.m_ctr_rad.set(point.x, point.y, point.z, 0.0f); return bsphere; }

		float len_sq = Length3Sq(point - bsphere.Centre());
		if (len_sq <= bsphere.RadiusSq()) return bsphere;

		float separation = pr::Sqrt(len_sq);
		float new_radius = (separation + bsphere.Radius()) * 0.5f;
		bsphere += (point - bsphere.Centre()) * ((new_radius - bsphere.Radius()) / separation);
		bsphere.m_ctr_rad.w = new_radius;
		return bsphere;
	}
	inline BSphere Encompass(BSphere const& bsphere, v4 const& point)
	{
		BSphere bsph = bsphere;
		return Encompass(bsph, point);
	}

	// Encompass 'rhs' in 'lhs' and re-centre the centre point
	inline BSphere& Encompass(BSphere& lhs, BSphere const& rhs)
	{
		if (lhs.Radius() < 0.0f) { lhs = rhs; return lhs; }

		float separation = Length3(rhs.Centre() - lhs.Centre());
		if (separation + rhs.Radius() <= lhs.Radius()) return lhs;

		float new_radius = (separation + lhs.Radius() + rhs.Radius()) * 0.5f;
		lhs += (rhs.Centre() - lhs.Centre()) * ((new_radius - lhs.Radius()) / separation);
		lhs.m_ctr_rad.w = new_radius;
		return lhs;
	}
	inline BSphere Encompass(BSphere const& lhs, BSphere const& rhs)
	{
		BSphere bsph = lhs;
		return Encompass(bsph, rhs);
	}

	// Encompass 'point' within 'bsphere' without moving the centre point.
	inline BSphere& EncompassLoose(BSphere& bsphere, v4 const& point)
	{
		if (bsphere.m_ctr_rad.w < 0.0f) { bsphere.m_ctr_rad.set(point.x, point.y, point.z, 0.0f); return bsphere; }

		float len_sq = Length3Sq(point - bsphere.Centre());
		if (len_sq <= bsphere.RadiusSq()) return bsphere;
		bsphere.m_ctr_rad.w = pr::Sqrt(len_sq);
		return bsphere;
	}
	inline BSphere EncompassLoose(BSphere const& bsphere, v4 const& point)
	{
		BSphere bsph = bsphere;
		return EncompassLoose(bsph, point);
	}

	// Encompass 'rhs' in 'lhs' without moving the centre point
	inline BSphere& EncompassLoose(BSphere& lhs, BSphere const& rhs)
	{
		if (lhs.Radius() < 0.0f) { lhs = rhs; return lhs; }

		float new_radius = Length3(rhs.Centre() - lhs.Centre()) + rhs.Radius();
		if (new_radius <= lhs.Radius()) return lhs;
		lhs.m_ctr_rad.w = new_radius;
		return lhs;
	}
	inline BSphere EncompassLoose(BSphere const& lhs, BSphere const& rhs)
	{
		BSphere bsph = lhs;
		return EncompassLoose(bsph, rhs);
	}

	// Return true if 'point' is within the bounding sphere
	inline bool IsWithin(BSphere const& bsphere, v4 const& point)
	{
		return Length3Sq(point - bsphere.Centre()) < bsphere.RadiusSq();
	}
	inline bool IsWithin(BSphere const& bsphere, BSphere const& test)
	{
		return Length3(test.Centre() - bsphere.Centre()) + test.Radius() < bsphere.Radius();
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool IsIntersection(BSphere const& lhs, BSphere const& rhs)
	{
		return Length3(rhs.Centre() - lhs.Centre()) < lhs.Radius() + rhs.Radius();
	}
}

#endif
