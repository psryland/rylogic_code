//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_QUATERNION_IMPL_H
#define PR_MATHS_QUATERNION_IMPL_H

#include "pr/maths/quaternion.h"

namespace pr
{
	// Create from components
	inline Quat& Quat::set(float x_, float y_, float z_, float w_)
	{
		x = x_;
		y = y_;
		z = z_;
		w = w_;
		return *this;
	}
	
	// Create a quaternion from an axis and an angle
	inline Quat& Quat::set(v4 const& axis, float angle)
	{
		float s = pr::Sin(0.5f * angle);
		x = axis.x * s;
		y = axis.y * s;
		z = axis.z * s;
		w = pr::Cos(0.5f * angle);
		return *this;
	}
	
	// Create a quaternion from Euler angles
	inline Quat& Quat::set(float pitch, float yaw, float roll)
	{
		float cos_r = pr::Cos(roll  * 0.5f), sin_r = pr::Sin(roll  * 0.5f);
		float cos_p = pr::Cos(pitch * 0.5f), sin_p = pr::Sin(pitch * 0.5f);
		float cos_y = pr::Cos(yaw   * 0.5f), sin_y = pr::Sin(yaw   * 0.5f);
		x = cos_r * sin_p * cos_y + sin_r * cos_p * sin_y;
		y = cos_r * cos_p * sin_y - sin_r * sin_p * cos_y;
		z = sin_r * cos_p * cos_y - cos_r * sin_p * sin_y;
		w = cos_r * cos_p * cos_y + sin_r * sin_p * sin_y;
		return *this;
	}
	
	// Create a quaternion from a rotation matrix
	inline Quat& Quat::set(m3x3 const& m)
	{
		float trace = m.x.x + m.y.y + m.z.z;
		if (trace >= 0.0f)
		{
			float s = 0.5f * pr::Rsqrt1(1.0f + trace);
			return set((m.y.z - m.z.y) * s, (m.z.x - m.x.z) * s, (m.x.y - m.y.x) * s, 0.25f / s);
		}
		else if (m.x.x > m.y.y && m.x.x > m.z.z)
		{
			float s = 0.5f * pr::Rsqrt1(1.0f + m.x.x - m.y.y - m.z.z);
			return set(0.25f / s, (m.x.y + m.y.x) * s, (m.z.x + m.x.z) * s, (m.y.z - m.z.y) * s);
		}
		else if (m.y.y > m.z.z)
		{
			float s = 0.5f * pr::Rsqrt1(1.0f - m.x.x + m.y.y - m.z.z);
			return set((m.x.y + m.y.x) * s, 0.25f / s, (m.y.z + m.z.y) * s, (m.z.x - m.x.z) * s);
		}
		else
		{
			float s = 0.5f * pr::Rsqrt1(1.0f - m.x.x - m.y.y + m.z.z);
			return set((m.z.x + m.x.z) * s, (m.y.z + m.z.y) * s, 0.25f / s, (m.x.y - m.y.x) * s);
		}
	}
	
	// Create a quaternion from a rotation matrix
	inline Quat& Quat::set(m4x4 const& m)
	{
		#if PR_MATHS_USE_DIRECTMATH
		dxv4(*this) = DirectX::XMQuaternionRotationMatrix(dxm4(m));
		return *this;
		#else
		return set(cast_m3x3(m));
		#endif
	}
	
	// Construct a quaternion from two vectors represent start and end orientations
	inline Quat& Quat::set(v4 const& from, v4 const& to)
	{
		float d = Dot3(from, to);
		v4 axis = Cross3(from, to);
		float s = Sqrt(Length3Sq(from) * Length3Sq(to)) + d;
		if (FEql(s, 0.0f)) { axis = Perpendicular(to); s = 0.0f; }  // vectors are 180 degrees apart
		set(axis.x, axis.y, axis.z, s);
		return Normalise4(*this);
	}
	
	// Quaternion multiply
	// Note about quat multiply vs. r = q*v*conj(q):
	// To rotate a vector or another quaternion, use the "sandwich product"
	// However, combining rotations is done using q1 * q2.
	// This is because:
	//  r1 = a * v * conj(a)  - first rotation
	//  r2 = b * r1 * conj(b) - second rotation
	//  r2 = b * a * v * conj(a) * conj(b)
	//  r2 = (b*a) * v * conj(b*a)
	inline Quat operator * (Quat const& lhs, Quat const& rhs)
	{
		Quat q;
		q.x = lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y;
		q.y = lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x;
		q.z = lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w;
		q.w = lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z;
		return q;
	}
	
	// Functions
	inline bool IsZero(Quat const& quat)
	{
		return quat.x == 0.0f && quat.y == 0.0f && quat.z == 0.0f && quat.w == 0.0f;
	}
	inline bool IsFinite(Quat const& q)
	{
		return IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);
	}
	inline bool IsFinite(Quat const& q, float max_value)
	{
		return IsFinite(q.x, max_value) && IsFinite(q.y, max_value) && IsFinite(q.z, max_value) && IsFinite(q.w, max_value);
	}
	inline Quat& Conjugate(Quat& quat)
	{
		quat.x = -quat.x;
		quat.y = -quat.y;
		quat.z = -quat.z;
		return quat;
	}
	inline Quat GetConjugate(Quat const& quat)
	{
		Quat q = quat; return Conjugate(q);
	}
	
	// Return the axis and angle from a quaternion
	inline void AxisAngle(Quat const& quat, v4& axis, float& angle)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal4(quat), "quat isn't normalised");
		float w = pr::Clamp(quat.w, -1.0f, 1.0f);
		float s = pr::Sqrt(1.0f - w * w);
		if (FEql(s, 0.0f))  axis = pr::v4ZAxis; // axis arbitrary for angle = 0
		else                axis.set(quat.x/s, quat.y/s, quat.z/s, 0.0f);
		angle = 2.0f * pr::ACos(w);
	}
	
	// Spherically interpolate between quaternions
	inline Quat Slerp(Quat const& src, Quat const& dst, float frac)
	{
		if (FLessEql(frac, 0.0f)) { return src; }
		if (FGtrEql(frac, 1.0f)) { return dst; }
		
		// Calculate cosine
		Quat abs_dst;
		float cos_angle = src.x*dst.x + src.y*dst.y + src.z*dst.z + src.w*dst.w;
		if (cos_angle >= 0) { abs_dst =  dst; }
		else                { abs_dst = -dst; cos_angle = -cos_angle; }
		
		// Calculate coefficients
		if (FGtr(1.0f, cos_angle, 0.05f))
		{
			// standard case (slerp)
			float angle     = ACos(cos_angle);
			float sin_angle = Sin(angle);
			float scale0    = Sin((1.0f - frac) * angle);
			float scale1    = Sin((frac) * angle);
			return (scale0*src + scale1*abs_dst) * (1.0f / sin_angle);
		}
		else // "src" and "dst" quaternions are very close
		{
			return GetNormal4(Lerp(src, abs_dst, frac));
		}
	}
	
	// Rotate 'rotatee' by 'rotator'
	inline Quat Rotate(Quat const& rotator, Quat const& rotatee)
	{
		PR_ASSERT(PR_DBG_MATHS, FEql(Length4Sq(rotator), 1.0f), "Non-unit quaternion used for rotation");
		return rotator * rotatee * GetConjugate(rotator);
	}
	
	// Rotate a vector by a quaternion
	// This is an optimised version of: r = q*v*conj(q) for when v.w == 0
	inline v4 Rotate(Quat const& lhs, v4 const& rhs)
	{
		float xx = lhs.x*lhs.x; float xy = lhs.x*lhs.y; float xz = lhs.x*lhs.z; float xw = lhs.x*lhs.w;
		                        float yy = lhs.y*lhs.y; float yz = lhs.y*lhs.z; float yw = lhs.y*lhs.w;
		                                                float zz = lhs.z*lhs.z; float zw = lhs.z*lhs.w;
		                                                                        float ww = lhs.w*lhs.w;
		v4 r;
		r.x =   ww*rhs.x + 2*yw*rhs.z - 2*zw*rhs.y +   xx*rhs.x + 2*xy*rhs.y + 2*xz*rhs.z -   zz*rhs.x - yy*rhs.x;
		r.y = 2*xy*rhs.x +   yy*rhs.y + 2*yz*rhs.z + 2*zw*rhs.x -   zz*rhs.y +   ww*rhs.y - 2*xw*rhs.z - xx*rhs.y;
		r.z = 2*xz*rhs.x + 2*yz*rhs.y +   zz*rhs.z - 2*yw*rhs.x -   yy*rhs.z + 2*xw*rhs.y -   xx*rhs.z + ww*rhs.z;
		r.w = rhs.w;
		return r;
	}
	inline v3 Rotate(Quat const& lhs, v3 const& rhs)
	{
		return cast_v3(Rotate(lhs, v4::make(rhs, 0.0f)));
	}
}

#endif
