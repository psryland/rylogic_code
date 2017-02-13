//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/vector4.h"
#include "pr/maths/bsphere.h"
#include "pr/maths/bbox.h"

namespace pr
{
	struct alignas(16) Frustum
	{
		// The inward pointing normals of the faces of the frustum
		// Note: the frustum grows down the negative z axis, i.e. the
		// z value of the pointy end is more positive than the base end.
		// This is because cameras generally look down the negative z axis
		// in right-handed space so that the x axis is to the right and the y axis is up.
		m4x4 m_Tnorms;

		// Create a frustum from Width/Height at 'z' from the apex
		static Frustum makeWH(float width, float height, float z, float zfar = 0)
		{
			Frustum f = {};
			f.m_Tnorms.x = v4(    z, 0.0f, -width  * 0.5f, 0.0f); // left
			f.m_Tnorms.y = v4(   -z, 0.0f, -width  * 0.5f, 0.0f); // right
			f.m_Tnorms.z = v4( 0.0f,   -z, -height * 0.5f, 0.0f); // top
			f.m_Tnorms.w = v4( 0.0f,    z, -height * 0.5f, 0.0f); // bottom
			f.m_Tnorms.x = Normalise3(f.m_Tnorms.x);
			f.m_Tnorms.y = Normalise3(f.m_Tnorms.y);
			f.m_Tnorms.z = Normalise3(f.m_Tnorms.z);
			f.m_Tnorms.w = Normalise3(f.m_Tnorms.w);
			f.m_Tnorms = Transpose4x4(f.m_Tnorms);
			f.ZDist(zfar);
			return f;
		}

		// Create a frustum from vertical field of view and an aspect ratio
		static Frustum makeFA(float fovY, float aspect, float zfar = 0)
		{
			auto h = 2.0f * Tan(fovY * 0.5f);
			auto w = h * aspect;
			return makeWH(w, h, 1.0f, zfar);
		}

		// Create a frustum from horizontal and vertical fields of view
		static Frustum makeHV(float horz_angle, float vert_angle, float zfar = 0)
		{
			auto h = 2.0f * pr::Tan(vert_angle * 0.5f);
			auto w = 2.0f * pr::Tan(horz_angle * 0.5f);
			return makeWH(w, h, 1.0f, zfar);
		}

		// Get/Set the z position of the sharp end or equivalently, the distance to the far clip plane.
		float ZDist() const
		{
			return -m_Tnorms.w.x / m_Tnorms.z.x;
		}
		void ZDist(float zfar)
		{
			m_Tnorms.w = -zfar * m_Tnorms.z;
		}

		// Get the frustum width/height at the far plane
		float Width() const
		{
			return m_Tnorms.w.x/m_Tnorms.x.x - m_Tnorms.w.y/m_Tnorms.x.y;
		}
		float Height() const
		{
			return m_Tnorms.w.w/m_Tnorms.y.w - m_Tnorms.w.z/m_Tnorms.y.z;
		}

		// Get the Y field of view
		float FovY() const
		{
			auto dot = Clamp(m_Tnorms.y.z*m_Tnorms.y.w + m_Tnorms.z.z*m_Tnorms.z.w, -1.0f, 1.0f);
			return float(maths::tau_by_2 - ACos(dot));
		}

		// Get the aspect ratio for the frustum
		float Aspect() const
		{
			// Not using Width()/Height() here because if ZDist() is zero it
			// will be 0/0 even though the aspect ratio is still actually valid
			return (m_Tnorms.z.y/m_Tnorms.x.y - m_Tnorms.z.x/m_Tnorms.x.x) /
				   (m_Tnorms.z.z/m_Tnorms.y.z - m_Tnorms.z.w/m_Tnorms.y.w);
		}

		// Return a vector containing the dimensions of the frustum: v4(width, height, zfar, longest_edge_length)
		v4 Dim() const
		{
			return v4(0.5f*Width(), 0.5f*Height(), ZDist(), 0);
		}

		// Return a plane of the frustum
		Plane Plane(int plane_index) const
		{
			switch (plane_index)
			{
			default: assert(false && "Invalid plane index"); return pr::v4ZAxis;
			case XPos: return pr::Plane(m_Tnorms.x.x, m_Tnorms.y.x, m_Tnorms.z.x, m_Tnorms.w.x);
			case XNeg: return pr::Plane(m_Tnorms.x.y, m_Tnorms.y.y, m_Tnorms.z.y, m_Tnorms.w.y);
			case YPos: return pr::Plane(m_Tnorms.x.z, m_Tnorms.y.z, m_Tnorms.z.z, m_Tnorms.w.z);
			case YNeg: return pr::Plane(m_Tnorms.x.w, m_Tnorms.y.w, m_Tnorms.z.w, m_Tnorms.w.w);
			case ZFar: return pr::v4ZAxis;
			}
		}
		enum EPlane {XPos = 0, XNeg = 1, YPos = 2, YNeg = 3, ZFar = 4, EPlane_NumberOf};

		// Return a matrix containing the inward pointing normals as the x,y,z,w vectors, where: x=left, y=right, z=top, w=bottom
		m4x4 Normals() const
		{
			return Transpose4x4(m_Tnorms);
		}

		// Return the (inward pointing) plane vector for a face of the frustum [0,5)
		v4 Normal(int plane_index) const
		{
			return Plane(plane_index).w0();
		}
	};

	#pragma region Operators
	inline bool operator == (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (Frustum const& lhs, Frustum const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	inline Frustum operator * (m4x4 const& m, Frustum const& rhs)
	{
		Frustum f = {rhs.m_Tnorms * Transpose4x4(m)}; // = Transpose(m * Transpose(rhs))
		return f;
	}
	#pragma endregion

	#pragma region Functions

	// Return true if 'point' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, v4 const& point)
	{
		v4 dots = frustum.m_Tnorms * point;
		return dots.x >= 0.0f && dots.y >= 0.0f && dots.z >= 0.0f && dots.w >= 0.0f;
	}

	// Return true if any part of 'bsphere' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, BSphere const& sphere)
	{
		v4 dots = frustum.m_Tnorms * sphere.Centre();
		return dots.x >= -sphere.Radius() && dots.y >= -sphere.Radius() && dots.z >= -sphere.Radius() && dots.w >= -sphere.Radius();
	}

	// Return true if any part of 'bbox' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, BBox const& bbox)
	{
		return IsWithin(frustum, GetBSphere(bbox));
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
			if (!pr::FEql(d[i],0)) // if the line is not parallel to this plane
			{
				if (d1[i] > d0[i]) t0 = Max(t0, -d0[i] / d[i]);
				else               t1 = Min(t1, -d0[i] / d[i]);
			}
			else if (d0[i] < 0) { t1 = t0; break; } // If behind the plane, then wholly clipped
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
		corners[0] = v4(-x, -y, -z_, 1.0f);
		corners[1] = v4( x, -y, -z_, 1.0f);
		corners[2] = v4( x,  y, -z_, 1.0f);
		corners[3] = v4(-x,  y, -z_, 1.0f);
	}
	inline void GetCorners(Frustum const& frustum, v4 (&corners)[4])
	{
		GetCorners(frustum, corners, frustum.ZDist());
	}

	#pragma endregion
}
