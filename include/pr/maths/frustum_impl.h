//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_FRUSTUM_IMPL_H
#define PR_MATHS_FRUSTUM_IMPL_H

#include "pr/maths/frustum.h"

namespace pr
{
	// Type methods
	inline Frustum  Frustum::makeWH(float width, float height, float z, float zfar) { Frustum f; return f.setWH(width, height, z, zfar); }
	inline Frustum  Frustum::makeFA(float fovY, float aspect, float zfar)           { Frustum f; return f.setFA(fovY, aspect, zfar); }
	inline Frustum  Frustum::makeHV(float horz_angle, float vert_angle, float zfar) { Frustum f; return f.setHV(horz_angle, vert_angle, zfar); }

	inline Frustum& Frustum::setWH(float width, float height, float z, float zfar)
	{
		m_Tnorms.x.set(    z, 0.0f, -width  * 0.5f, 0.0f); // left
		m_Tnorms.y.set(   -z, 0.0f, -width  * 0.5f, 0.0f); // right
		m_Tnorms.z.set( 0.0f,   -z, -height * 0.5f, 0.0f); // top
		m_Tnorms.w.set( 0.0f,    z, -height * 0.5f, 0.0f); // bottom
		m_Tnorms.x = Normalise3(m_Tnorms.x);
		m_Tnorms.y = Normalise3(m_Tnorms.y);
		m_Tnorms.z = Normalise3(m_Tnorms.z);
		m_Tnorms.w = Normalise3(m_Tnorms.w);
		m_Tnorms = Transpose4x4_(m_Tnorms);
		ZDist(zfar);
		return *this;
	}
	inline Frustum& Frustum::setFA(float fovY, float aspect, float zfar)
	{
		float h = 2.0f * Tan(fovY * 0.5f);
		float w = h * aspect;
		return setWH(w, h, 1.0f, zfar);
	}
	inline Frustum& Frustum::setHV(float horz_angle, float vert_angle, float zfar)
	{
		float h = 2.0f * pr::Tan(vert_angle * 0.5f);
		float w = 2.0f * pr::Tan(horz_angle * 0.5f);
		return setWH(w, h, 1.0f, zfar);
	}

	// Get/Set the z position of the sharp end.
	// This is the distance to the far clip plane
	inline void Frustum::ZDist(float zfar)
	{
		m_Tnorms.w = -zfar * m_Tnorms.z;
	}
	inline float Frustum::ZDist() const
	{
		return -m_Tnorms.w.x / m_Tnorms.z.x;
	}

	// Return the frustum width/height at the far plane
	inline float Frustum::Width() const
	{
		return m_Tnorms.w.x/m_Tnorms.x.x - m_Tnorms.w.y/m_Tnorms.x.y;
	}
	inline float Frustum::Height() const
	{
		return m_Tnorms.w.w/m_Tnorms.y.w - m_Tnorms.w.z/m_Tnorms.y.z;
	}

	// Return the Y field of view
	inline float Frustum::FovY() const
	{
		float dot = Clamp(m_Tnorms.y.z*m_Tnorms.y.w + m_Tnorms.z.z*m_Tnorms.z.w, -1.0f, 1.0f);
		return maths::tau_by_2 - ACos(dot);
	}
	inline float Frustum::Aspect() const
	{
		// Not using Width()/Height() here because if ZDist() is zero it
		// will be 0/0 even though the aspect ratio is still actually valid
		return (m_Tnorms.z.y/m_Tnorms.x.y - m_Tnorms.z.x/m_Tnorms.x.x) /
		       (m_Tnorms.z.z/m_Tnorms.y.z - m_Tnorms.z.w/m_Tnorms.y.w);
	}

	// Return a vector containing the dimensions of the frustum
	// half_width, half_height, zfar
	inline pr::v4 Frustum::Dim() const
	{
		return pr::v4::make(0.5f*Width(), 0.5f*Height(), ZDist(), 0);
	}

	// Return a plane of the frustum
	inline Plane Frustum::Plane(int plane_index) const
	{
		switch (plane_index)
		{
		default: assert(false && "Invalid plane index"); return pr::v4ZAxis;
		case XPos: return Plane::make(m_Tnorms.x.x, m_Tnorms.y.x, m_Tnorms.z.x, m_Tnorms.w.x);
		case XNeg: return Plane::make(m_Tnorms.x.y, m_Tnorms.y.y, m_Tnorms.z.y, m_Tnorms.w.y);
		case YPos: return Plane::make(m_Tnorms.x.z, m_Tnorms.y.z, m_Tnorms.z.z, m_Tnorms.w.z);
		case YNeg: return Plane::make(m_Tnorms.x.w, m_Tnorms.y.w, m_Tnorms.z.w, m_Tnorms.w.w);
		case ZFar: return pr::v4ZAxis;
		}
	}

	// Return a matrix containing the inward pointing normals as the x,y,z,w vectors
	// where: x=left, y=right, z=top, w=bottom. Note, the far plane normal isn't included
	inline pr::m4x4 Frustum::Normals() const
	{
		return Transpose4x4_(m_Tnorms);
	}

	// Return the (inward pointing) plane vector for a face of the frustum [0,5)
	inline pr::v4 Frustum::Normal(int plane_index) const
	{
		return Plane(plane_index).w0();
	}

	// Binary operators
	inline Frustum operator * (m4x4 const& m, Frustum const& rhs)
	{
		Frustum f = {rhs.m_Tnorms * Transpose4x4_(m)}; // = Transpose(m * Transpose(rhs))
		return f;
	}

	// Equality operators
	inline bool operator == (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Return true if 'point' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, v4 const& point)
	{
		v4 dots = frustum.m_Tnorms * point;
		return dots.x >= 0.0f && dots.y >= 0.0f && dots.z >= 0.0f && dots.w >= 0.0f;
	}

	// Return true if any part of 'bbox' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, BBox const& bbox)
	{
		return IsWithin(frustum, GetBoundingSphere(bbox));
	}

	// Return true if any part of 'bsphere' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, BSphere const& sphere)
	{
		v4 dots = frustum.m_Tnorms * sphere.Centre();
		return dots.x >= -sphere.Radius() && dots.y >= -sphere.Radius() && dots.z >= -sphere.Radius() && dots.w >= -sphere.Radius();
	}

	// Intersect the line passing through 's' and 'e' to 'frustum' returning
	// parametric values 't0' and 't1'. Note: this is an accumulative function,
	// 't0' and 't1' must be initialised. Returns true if t0 < t1 i.e. some of
	// the line is within the frustum.
	inline bool Intersect(Frustum const& frustum, v4 const& s, v4 const& e, float& t0, float& t1, bool include_far_plane)
	{
		// Clip to the far plane
		if (include_far_plane)
		{
			if (!pr::FEql(s.z, e.z))
			{
				if (e.z > s.z) t0 = Max(t0, -s.z / (e.z - s.z));
				else           t1 = Min(t1, -s.z / (e.z - s.z));
			}
			else if (s.z < 0)
			{
				t1 = t0;
				return false;
			}
		}

		// Clip to the frustum planes
		pr::v4 d0 = frustum.m_Tnorms * s;
		pr::v4 d1 = frustum.m_Tnorms * e;
		pr::v4 d  = d1 - d0;
		for (int i = 0; i != 4; ++i)
		{
			if (!pr::FEqlZero(d[i])) // if the line is not parallel to this plane
			{
				if (d1[i] > d0[i]) t0 = Max(t0, -d0[i] / d[i]);
				else               t1 = Min(t1, -d0[i] / d[i]);
			}
			else if (d0[i] < 0) { t1 = t0; break; } // If behind the plane, then wholely clipped
		}

		// Return true if any portion of the line is within the frustum
		return t0 < t1;
	}

	// Returns the corners of the frustum *in frustum space*
	// Order: (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)
	// Note: this function puts the frustum pointy end at (0,0,0)
	// with the wide end down the -z axis.
	inline void GetCorners(Frustum const& frustum, v4 (&corners)[4], float z_)
	{
		auto z = frustum.ZDist();
		auto x = frustum.Width()  * 0.5f * z_ / z;
		auto y = frustum.Height() * 0.5f * z_ / z;
		corners[0].set(-x, -y, -z_, 1.0f);
		corners[1].set( x, -y, -z_, 1.0f);
		corners[2].set( x,  y, -z_, 1.0f);
		corners[3].set(-x,  y, -z_, 1.0f);
	}
	inline void GetCorners(Frustum const& frustum, v4 (&corners)[4])
	{
		GetCorners(frustum, corners, frustum.ZDist());
	}
}

#endif
