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
	inline BoundingSphere  BoundingSphere::make(v4 const& centre, float radius)     { BoundingSphere s = {{centre.x, centre.y, centre.z, radius}}; return s; }
	inline BoundingSphere& BoundingSphere::set(v4 const& centre, float radius)      { m_ctr_rad = centre; m_ctr_rad.w = radius; return *this; }
	inline BoundingSphere& BoundingSphere::zero()                                   { m_ctr_rad = v4Zero; return *this; }
	inline BoundingSphere& BoundingSphere::unit()                                   { m_ctr_rad = v4Origin; return *this; }
	inline BoundingSphere& BoundingSphere::reset()                                  { m_ctr_rad = -v4Origin; return *this; }
	inline bool            BoundingSphere::IsValid() const                          { return Volume(*this) >= 0.0f; }
	inline v4              BoundingSphere::Centre() const                           { return m_ctr_rad.w1(); }
	inline float           BoundingSphere::Radius() const                           { return m_ctr_rad.w; }
	inline float           BoundingSphere::RadiusSq() const                         { return Sqr(m_ctr_rad.w); }
	inline float           BoundingSphere::Diametre() const                         { return 2.0f * m_ctr_rad.w; }
	inline float           BoundingSphere::DiametreSq() const                       { return Sqr(Diametre()); }
	
	// Assignment operators
	inline BoundingSphere& operator += (BoundingSphere& lhs, v4 const& offset)      { PR_ASSERT(PR_DBG_MATHS, offset.w == 0.0f, ""); lhs.m_ctr_rad += offset; return lhs; }
	inline BoundingSphere& operator -= (BoundingSphere& lhs, v4 const& offset)      { PR_ASSERT(PR_DBG_MATHS, offset.w == 0.0f, ""); lhs.m_ctr_rad -= offset; return lhs; }
	inline BoundingSphere& operator *= (BoundingSphere& lhs, float s)               { lhs.m_ctr_rad.w *= s; return lhs; }
	inline BoundingSphere& operator /= (BoundingSphere& lhs, float s)               { lhs *= (1.0f / s); return lhs; }
	
	// Binary operators
	inline BoundingSphere operator + (BoundingSphere const& bsph, v4 const& offset) { BoundingSphere bs = bsph; return bs += offset; }
	inline BoundingSphere operator - (BoundingSphere const& bsph, v4 const& offset) { BoundingSphere bs = bsph; return bs -= offset; }
	inline BoundingSphere operator * (BoundingSphere const& bsph, float s)          { BoundingSphere bs = bsph; return bs *= s; }
	inline BoundingSphere operator * (float s, BoundingSphere const& bsph)          { BoundingSphere bs = bsph; return bs *= s; }
	inline BoundingSphere operator * (m4x4 const& m, BoundingSphere const& bsph)    { BoundingSphere bs; bs.m_ctr_rad = m * bsph.Centre(); bs.m_ctr_rad.w = bsph.m_ctr_rad.w; return bs; }
	
	// Equality operators
	inline bool operator == (BoundingSphere const& lhs, BoundingSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (BoundingSphere const& lhs, BoundingSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (BoundingSphere const& lhs, BoundingSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (BoundingSphere const& lhs, BoundingSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (BoundingSphere const& lhs, BoundingSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (BoundingSphere const& lhs, BoundingSphere const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	
	// Functions
	inline float Volume(BoundingSphere const& bsph)
	{
		return 4.188790f * bsph.m_ctr_rad.w * bsph.m_ctr_rad.w * bsph.m_ctr_rad.w; // (4/3)*pi*r^3
	}
	
	// Encompase 'point' within 'bsphere' and re-centre the centre point.
	inline BoundingSphere& Encompase(BoundingSphere& bsphere, v4 const& point)
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
	inline BoundingSphere Encompase(BoundingSphere const& bsphere, v4 const& point)
	{
		BoundingSphere bsph = bsphere;
		return Encompase(bsph, point);
	}
	
	// Encompase 'rhs' in 'lhs' and re-centre the centre point
	inline BoundingSphere& Encompase(BoundingSphere& lhs, BoundingSphere const& rhs)
	{
		if (lhs.Radius() < 0.0f) { lhs = rhs; return lhs; }
		
		float separation = Length3(rhs.Centre() - lhs.Centre());
		if (separation + rhs.Radius() <= lhs.Radius()) return lhs;
		
		float new_radius = (separation + lhs.Radius() + rhs.Radius()) * 0.5f;
		lhs += (rhs.Centre() - lhs.Centre()) * ((new_radius - lhs.Radius()) / separation);
		lhs.m_ctr_rad.w = new_radius;
		return lhs;
	}
	inline BoundingSphere Encompase(BoundingSphere const& lhs, BoundingSphere const& rhs)
	{
		BoundingSphere bsph = lhs;
		return Encompase(bsph, rhs);
	}
	
	// Encompase 'point' within 'bsphere' without moving the centre point.
	inline BoundingSphere& EncompaseLoose(BoundingSphere& bsphere, v4 const& point)
	{
		if (bsphere.m_ctr_rad.w < 0.0f) { bsphere.m_ctr_rad.set(point.x, point.y, point.z, 0.0f); return bsphere; }
		
		float len_sq = Length3Sq(point - bsphere.Centre());
		if (len_sq <= bsphere.RadiusSq()) return bsphere; 
		bsphere.m_ctr_rad.w = pr::Sqrt(len_sq);
		return bsphere;
	}
	inline BoundingSphere EncompaseLoose(BoundingSphere const& bsphere, v4 const& point)
	{
		BoundingSphere bsph = bsphere;
		return EncompaseLoose(bsph, point);
	}
	
	// Encompase 'rhs' in 'lhs' without moving the centre point
	inline BoundingSphere& EncompaseLoose(BoundingSphere& lhs, BoundingSphere const& rhs)
	{
		if (lhs.Radius() < 0.0f) { lhs = rhs; return lhs; }
		
		float new_radius = Length3(rhs.Centre() - lhs.Centre()) + rhs.Radius();
		if (new_radius <= lhs.Radius()) return lhs;
		lhs.m_ctr_rad.w = new_radius;
		return lhs;
	}
	inline BoundingSphere EncompaseLoose(BoundingSphere const& lhs, BoundingSphere const& rhs)
	{
		BoundingSphere bsph = lhs;
		return EncompaseLoose(bsph, rhs);
	}
	
	// Return true if 'point' is within the bounding sphere
	inline bool IsWithin(BoundingSphere const& bsphere, v4 const& point)
	{
		return Length3Sq(point - bsphere.Centre()) < bsphere.RadiusSq();
	}
	inline bool IsWithin(BoundingSphere const& bsphere, BoundingSphere const& test)
	{
		return Length3(test.Centre() - bsphere.Centre()) + test.Radius() < bsphere.Radius();
	}
	
	// Returns true if 'lhs' and 'rhs' intersect
	inline bool IsIntersection(BoundingSphere const& lhs, BoundingSphere const& rhs)
	{
		return Length3(rhs.Centre() - lhs.Centre()) < lhs.Radius() + rhs.Radius();
	}
}

#endif
