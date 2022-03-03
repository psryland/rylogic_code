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
		// Notes:
		//  - The frustum grows down the negative z axis, i.e. the z value of the apex is more
		//    positive than the far plane. This is because cameras generally look down the -z axis
		//    in right-handed space so that the x axis is to the right and the y axis is up.
		//  - The frustum plane normals are stored with the far plane at (0,0,0). However, it is
		//    more convenient to have the apex at (0,0,0) so the public interface should expect
		//    the apex at (0,0,0) and internally offset by 'zfar'.
		//  - 'zfar' is a positive value when within the frustum and negative when behind, to be
		//    consistent with camera near/far planes. 'zfar' is 'the distance to', not 'the z value of'.
		//  - There is no reason the frustum needs to be symmetrical, any 4 inward normals are valid.
		//    However, the -Z axis is still considered the main axis of the frustum, even if a face
		//    normal has a positive z component.
		//  - This type expects left/right to have no Y component, and bottom/top to have no X
		//    component. There are optimisations based on this assumption.
		//  - "Left" is from the apex looking down the -z axis. The left plane normal of frustum
		//    typically has a positive X component and -Z component.
		//  - There is no 'Frustum operator * (m4x4, Frustum)' because this type assumes the -z
		//    is the main axes, and the left/right planes have no Y component, etc. Transforming
		//    the planes doesn't work as expected anyway.

		// The order of planes in the frustum
		enum class EPlane
		{
			XPos = 0, // left
			XNeg = 1, // right
			YPos = 2, // bottom
			YNeg = 3, // top
			ZFar = 4, // far plane
			NumberOf,
		};

		// The inward pointing planes of the faces of the frustum, transposed.
		m4x4 m_Tplanes;

		// Create a frustum from 'width' and 'height' at 'z' from the apex
		static Frustum MakeWH(float width, float height, float z, float zfar)
		{
			assert(z > 0 && "The focus plane should be a positive distance from the apex");
			assert(zfar >= 0 && "The far plane should be a positive distance from the apex");

			Frustum f;
			f.m_Tplanes.x = v4(+z, 0.0f, -width * 0.5f, 0.0f); // left
			f.m_Tplanes.y = v4(-z, 0.0f, -width * 0.5f, 0.0f); // right
			f.m_Tplanes.z = v4(0.0f, +z, -height * 0.5f, 0.0f); // bottom
			f.m_Tplanes.w = v4(0.0f, -z, -height * 0.5f, 0.0f); // top
			f.m_Tplanes.x = Normalise(f.m_Tplanes.x);
			f.m_Tplanes.y = Normalise(f.m_Tplanes.y);
			f.m_Tplanes.z = Normalise(f.m_Tplanes.z);
			f.m_Tplanes.w = Normalise(f.m_Tplanes.w);
			f.m_Tplanes = Transpose4x4(f.m_Tplanes);
			f.zfar(zfar);
			return f;
		}
		static Frustum MakeWH(v2 area, float z, float zfar)
		{
			return MakeWH(area.x, area.y, z, zfar);
		}

		// Create a frustum from vertical 'field-of-view' and an 'aspect' ratio
		static Frustum MakeFA(float fovY, float aspect, float zfar)
		{
			auto h = 2.0f * tan(0.5f * fovY);
			return MakeWH(aspect * h, h, 1.0f, zfar);
		}

		// Create a frustum from horizontal and vertical fields of view
		static Frustum MakeFovXY(float fovX, float fovY, float zfar)
		{
			auto w = 2.0f * tan(0.5f * fovX);
			auto h = 2.0f * tan(0.5f * fovY);
			return MakeWH(w, h, 1.0f, zfar);
		}

		// Create an orthographic "frustum" that has the zfar at infinity and parallel side planes
		static Frustum MakeOrtho(float width, float height)
		{
			Frustum f;
			f.m_Tplanes.x = v4(+1.0f, 0.0f, 0.0f, width * 0.5f); // left
			f.m_Tplanes.y = v4(-1.0f, 0.0f, 0.0f, width * 0.5f); // right
			f.m_Tplanes.z = v4(0.0f, +1.0, 0.0f, height * 0.5f); // bottom
			f.m_Tplanes.w = v4(0.0f, -1.0, 0.0f, height * 0.5f); // top
			f.m_Tplanes = Transpose4x4(f.m_Tplanes);
			return f;
		}
		static Frustum MakeOrtho(v2 area)
		{
			return MakeOrtho(area.x, area.y);
		}

		// Create from a projection matrix
		static Frustum MakeFromProjection(m4x4 const& c2s)
		{
			Frustum f;
			if (c2s.w.w == 1) // If orthographic
			{
				auto w = 2.0f / c2s.x.x;
				auto h = 2.0f / c2s.y.y;
				f = MakeOrtho(w, h);
			}
			else // Otherwise perspective
			{
				auto rh = -Sign(c2s.z.w);
				auto zn = rh * c2s.w.z / c2s.z.z;
				auto zf = zn * c2s.z.z / (rh + c2s.z.z);
				auto w = 2.0f * zn / c2s.x.x;
				auto h = 2.0f * zn / c2s.y.y;
				f = MakeWH(w, h, zn, zf);
			}
			return f;
		}

		// Return the projection matrix for this Frustum
		m4x4 projection(float zn, float zf) const
		{
			auto wh = area(zn);
			return orthographic()
				? m4x4::ProjectionOrthographic(wh.x, wh.y, zn, zf, true)
				: m4x4::ProjectionPerspective(wh.x, wh.y, zn, zf, true);
		}
		m4x4 projection(v2 nf) const
		{
			return projection(nf.x, nf.y);
		}

		// True if this is an orthographic frustum
		bool orthographic() const
		{
			// If none of the plane normals have a z component,
			// then they are all parallel to the z axis.
			return m_Tplanes.z == v4Zero;
		}

		// Get/Set the distance to the far clip plane.
		float zfar() const
		{
			// Orthographic frusta don't have a far plane
			return !orthographic()
				? -ComponentSum(m_Tplanes.w / m_Tplanes.z) / 4.0f
				: maths::float_inf;
		}
		void zfar(float z)
		{
			if (!orthographic())
				m_Tplanes.w = -z * m_Tplanes.z;
		}

		// Get/Set the X field of view
		float fovX() const
		{
			// The FovX is the angle between the left/right plane normals: Cos(ang) = dot(l,r)
			auto l = v4(m_Tplanes.x.x, m_Tplanes.y.x, m_Tplanes.z.x, 0);
			auto r = v4(m_Tplanes.x.y, m_Tplanes.y.y, m_Tplanes.z.y, 0);
			auto fov = ACos(Clamp(Dot(l, -r), -1.0f, 1.0f));
			return fov;
		}

		// Get the Y field of view
		float fovY() const
		{
			// The FovY is the angle between the bottom/top plane normals: Cos(ang) = dot(b,t)
			auto b = v4(m_Tplanes.x.z, m_Tplanes.y.z, m_Tplanes.z.z, 0);
			auto t = v4(m_Tplanes.x.w, m_Tplanes.y.w, m_Tplanes.z.w, 0);
			auto fov = ACos(Clamp(Dot(b, -t), -1.0f, 1.0f));
			return fov;
		}

		// Get/Set the aspect ratio for the frustum
		float aspect() const
		{
			// Not using width/height here because if zfar is zero it will be
			// 0/0 even though the aspect ratio is still actually valid.
			if (orthographic())
			{
				return
					(m_Tplanes.w.x + m_Tplanes.w.y) /
					(m_Tplanes.w.z + m_Tplanes.w.w);
			}
			else
			{
				return
					(m_Tplanes.z.y / m_Tplanes.x.y - m_Tplanes.z.x / m_Tplanes.x.x) /
					(m_Tplanes.z.w / m_Tplanes.y.w - m_Tplanes.z.z / m_Tplanes.y.z);
			}
		}

		// Get the frustum width/height at 'z' distance from the apex.
		v2 area(float z) const
		{
			auto w = m_Tplanes.w.x / m_Tplanes.x.x - m_Tplanes.w.y / m_Tplanes.x.y;
			auto h = m_Tplanes.w.z / m_Tplanes.y.z - m_Tplanes.w.w / m_Tplanes.y.w;
			if (orthographic())
			{
				return v2(w, h);
			}
			else
			{
				auto z0 = zfar();
				return z0 != 0 ? v2(w * z / z0, h * z / z0) : v2Zero;
			}
		}

		// Return the planes of the frustum
		m4x4 planes() const
		{
			return Transpose4x4(m_Tplanes);
		}
		Plane plane(EPlane plane_index) const
		{
			switch (plane_index)
			{
				case EPlane::XPos: return Plane(m_Tplanes.x.x, m_Tplanes.y.x, m_Tplanes.z.x, m_Tplanes.w.x);
				case EPlane::XNeg: return Plane(m_Tplanes.x.y, m_Tplanes.y.y, m_Tplanes.z.y, m_Tplanes.w.y);
				case EPlane::YPos: return Plane(m_Tplanes.x.z, m_Tplanes.y.z, m_Tplanes.z.z, m_Tplanes.w.z);
				case EPlane::YNeg: return Plane(m_Tplanes.x.w, m_Tplanes.y.w, m_Tplanes.z.w, m_Tplanes.w.w);
				case EPlane::ZFar: return Plane{0, 0, 1, 0};
				default: throw std::runtime_error("Invalid plane index");
			}
		}

		// Return a matrix containing the inward pointing face normals or the frustum sides.
		// Order is the same as EPlane (i.e. x=left, y=right, z=bottom, w=top). The far plane is always (0,0,1,0)
		m4x4 face_normals() const
		{
			auto norms = m_Tplanes;
			norms.w = v4Zero;
			return Transpose4x4(norms);
		}
		v4 face_normal(EPlane plane_index) const
		{
			auto p = plane(plane_index);
			return v4(p.x, p.y, p.z, 0);
		}

		// Return a matrix containing the direction vectors of the frustum edges. These are the four ray directions
		// that start at the camera and lie at the intersections of the frustum planes.
		m4x4 edges() const
		{
			// Direction vectors for the corners
			auto norms = face_normals();
			return m4x4(
				Cross(norms.z, norms.x),  // BL
				Cross(norms.x, norms.w),  // TL
				Cross(norms.w, norms.y),  // TR
				Cross(norms.y, norms.z)); // BR
		}

		// Clip the infinite line that passes through 's' with direction 'd' to this frustum.
		// 's' and 'd' must be in frustum space where the frustum apex is at (0,0,0) and grows down the -z axis. (i.e. camera space).
		// 'accumulative' means 't0' and 't1' are expected to be initialised already, otherwise they are initialised to an infinite line.
		// Returns the parametric values 't0' and 't1' and true if t0 < t1 i.e. some of the line is within the frustum.
		bool pr_vectorcall clip(v4_cref<> s, v4_cref<> d, bool accumulative, float& t0, float& t1, bool include_zfar) const
		{
			// Frustum stores the clip planes such that the far plane is actually at (0,0,0) and the apex is at zfar along the +Z axis.
			// Shift 's' and 'd' by 'zfar' so that callers can treat the frustum apex as if it was at (0,0,0).
			auto z = zfar();
			auto a = s + v4(0, 0, z, 0);
			auto b = a + d;

			// Initialise the parametric values if this is not an accumulative clip
			if (!accumulative)
			{
				t0 = maths::float_lowest;
				t1 = maths::float_max;
			}

			// Clip to the far plane. Orthographic frustums don't have a far plane and are really a rectilinear channel.
			if (include_zfar && z != 0)
			{
				// If the line is not parallel to the far plane
				if (!FEql(a.z, b.z))
				{
					if (b.z > a.z) t0 = Max(t0, -a.z / (b.z - a.z));
					else           t1 = Min(t1, -a.z / (b.z - a.z));
				}
				else if (a.z < 0)
				{
					t1 = t0;
					return false;
				}
			}

			// Get the dot products of a section of the line agains the frustum planes
			auto d0 = (*this) * a;
			auto d1 = (*this) * b;
			auto interval = d1 - d0;

			// Reduce the parametric interval
			for (int i = 0; i != 4; ++i)
			{
				// If the line is not parallel to the far plane
				if (!FEql(interval[i], 0))
				{
					if (d1[i] > d0[i]) t0 = Max(t0, -d0[i] / interval[i]);
					else               t1 = Min(t1, -d0[i] / interval[i]);
				}
				// If behind the plane, then wholly clipped
				else if (d0[i] < 0)
				{
					t1 = t0;
					break;
				}
			}

			// Return true if any portion of the line is within the frustum
			return t0 < t1;
		}

		#pragma region Operators
		friend bool operator == (Frustum const& lhs, Frustum const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) == 0;
		}
		friend bool operator != (Frustum const& lhs, Frustum const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) != 0;
		}
		friend bool operator <  (Frustum const& lhs, Frustum const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) < 0;
		}
		friend bool operator >  (Frustum const& lhs, Frustum const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) > 0;
		}
		friend bool operator <= (Frustum const& lhs, Frustum const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0;
		}
		friend bool operator >= (Frustum const& lhs, Frustum const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0;
		}
		friend v4 operator * (Frustum const& lhs, v4 const& rhs)
		{
			// Returns the signed distance of 'rhs' from each face of the frustum
			return lhs.m_Tplanes * rhs;
		}
		// Do not implement Frustum = m4x4 * Frustum. Frustums cannot be rotated or moved.
		#pragma endregion
	};

	// Floating point comparisons. *WARNING* 'tol' is an absolute tolerance. Returns true if a is in the range (b-tol,b+tol)
	constexpr bool FEqlAbsolute(Frustum const& a, Frustum const& b, float tol)
	{
		return FEqlAbsolute<>(a.m_Tplanes, b.m_Tplanes, tol);
	}

	// *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
	constexpr bool FEqlRelative(Frustum const& a, Frustum const& b, float tol)
	{
		return FEqlRelative<>(a.m_Tplanes, b.m_Tplanes, tol);
	}

	// FEqlRelative using 'tinyf'. Returns true if a in the range (b - max(a,b)*tiny, b + max(a,b)*tiny)
	constexpr bool FEql(Frustum const& a, Frustum const& b)
	{
		// Don't add a 'tol' parameter because it looks like the function should perform a == b +- tol, which isn't what it does.
		return FEql<>(a.m_Tplanes, b.m_Tplanes);
	}

	// Returns the corners of the frustum (in frustum space) at a given 'z' distance (i.e. apex at (0,0,0). Far plane at (0,0,-zfar)). Return order: x=lb, y=lt, z=rt, w=rb
	inline m4x4 Corners(Frustum const& frustum, float z)
	{
		assert(z >= 0 && "'z' should be a positive distance from the apex");
		auto edges = frustum.edges();
		
		if (frustum.orthographic())
		{
			return m4x4(
				v4(-frustum.m_Tplanes.w.x, -frustum.m_Tplanes.w.z, -z, 1),
				v4(-frustum.m_Tplanes.w.x, +frustum.m_Tplanes.w.w, -z, 1),
				v4(+frustum.m_Tplanes.w.y, +frustum.m_Tplanes.w.w, -z, 1),
				v4(+frustum.m_Tplanes.w.y, -frustum.m_Tplanes.w.z, -z, 1));
		}
		else
		{
			// Each edge vector has length == 1. Find the length of each edge
			// when projected onto the Z axis then scale by z.
			auto lengths = Transpose4x4(edges) * v4(0, 0, -1, 0);
			auto corners = CompMul(edges, z / lengths) + m4x4(v4Origin, v4Origin, v4Origin, v4Origin);
			return corners;
		}
	}

	// Returns the corners of the frustum at the zfar plane.
	inline m4x4 Corners(Frustum const& frustum)
	{
		// Warning: calling this on orthographic frusta is probably a bug.. unless Z isn't used.
		return Corners(frustum, frustum.zfar());
	}

	// Return true if any part of a point, box, or sphere is within 'frustum'.
	// Use 'zf == 0' to use the frustum's far plane, or 'zf == -1' to ignore the far plane.
	inline bool pr_vectorcall IsWithin(Frustum const& frustum, v4_cref<> point, float radius, v2 nf = v2Zero)
	{
		auto pt = point;
		auto frustum_apex = 0.0f;
		auto znear = 0.0f, zfar = 0.0f;

		// Orthographic frusta don't have a zfar distance
		if (!frustum.orthographic())
		{
			// Remember zn and zf are "the distance to", not "the z coordinate of"
			frustum_apex = frustum.zfar();

			// Shift 'pt' so that the frustum apex is at (0,0,frustum_apex)
			pt.z += frustum_apex;

			// Get the z coordinate of the clip planes
			znear = frustum_apex - nf.x;
			zfar = nf.y > 0 ? frustum_apex - nf.y : nf.y == 0 ? 0.0f : -maths::float_inf;
		}
		// Orthographic frusta have their "apex" at (0,0,0)
		else
		{
			// Get the z coordinate of the clip planes
			znear = -nf.x;
			zfar = nf.y > 0 ? -nf.y : -maths::float_inf;
		}

		// Test against the near plane
		if (pt.z - radius > znear)
			return false;

		// Test against the far plane (only if given)
		if (pt.z + radius < zfar)
			return false;

		// Dot product of 'point' with each plane gives the signed distance to each plane.
		// Increase each distance by 'radius'. This is not strictly correct because we're
		// effectively expanding the frustum by 'radius' not the sphere. It doesn't work near edges.
		auto dots = frustum.m_Tplanes * pt + v4(radius);

		// If all dot products are >= 0 then some part of the sphere is within the frustum
		return dots == Abs(dots); 
	}
	inline bool pr_vectorcall IsWithin(Frustum const& frustum, BSphere_cref bsphere, v2 nf = v2Zero)
	{
		assert(bsphere.valid() && "Invalid bsphere used in 'IsWithin' test against frustum");
		return IsWithin(frustum, bsphere.Centre(), bsphere.Radius(), nf);
	}
	inline bool pr_vectorcall IsWithin(Frustum const& frustum, BBox_cref bbox, v2 nf = v2Zero)
	{
		assert(bbox.valid() && "Invalid bbox used in 'IsWithin' test against frustum");

		auto bb = bbox;
		auto frustum_apex = 0.0f;
		auto znear = 0.0f, zfar = 0.0f;

		// Orthographic frusta don't have a zfar distance
		if (!frustum.orthographic())
		{
			// Remember zn and zf are "the distance to", not "the z coordinate of"
			frustum_apex = frustum.zfar();

			// Shift 'bbox' so that the frustum apex is at (0,0,frustum_apex)
			bb.m_centre.z += frustum_apex;

			// Get the z coordinate of the clip planes
			znear = frustum_apex - nf.x;
			zfar = nf.y > 0 ? frustum_apex - nf.y : nf.y == 0 ? 0.0f : -maths::float_inf;
		}
		// Orthographic frusta have their "apex" at (0,0,0)
		else
		{
			// Get the z coordinate of the clip planes
			znear = -nf.x;
			zfar = nf.y > 0 ? -nf.y : -maths::float_inf;
		}

		// Test against the near plane
		if (bb.LowerZ() > znear)
			return false;

		// Test against the far plane (only if given)
		if (bb.UpperZ() < zfar)
			return false;

		// The bbox and frustum are both axis aligned, so the test is basically a 2D quad intersection test.
		// Only need to test the cross section of the bbox and the frustum at the minimum z value.
		auto z = Clamp(bb.LowerZ(), zfar, znear);
		auto wh = 0.5f * frustum.area(frustum_apex - z);

		// This assumes the frustum is symmetric....
		return
			bb.LowerX() >= -wh.x && bb.UpperX() <= +wh.x &&
			bb.LowerY() >= -wh.y && bb.UpperY() <= +wh.y;
	}

	// Grow a frustum (i.e. move it along +f2w.Z growing zfar while preserving fov/aspect) so that 'ws_pt','ws_bbox', or 'ws_sphere' are within the frustum.
	inline void pr_vectorcall Grow(Frustum& frustum, m4x4& f2w, v2& nf, v4_cref<> ws_pt, float radius)
	{
		// By similar triangles:
		//   zfar1 / zfar0 = (n.w + Dot(n,pt)) / n.w
		// where:
		//   zfar0 = the current zfar distance
		//   zfar1 = the new zfar distance needed to enclose 'pt'
		//   n.w   = is a frustum plane distance from the origin
		//   n     = is a frustum plane 4-vector
		// However, zfar0 can be 0.0.
		// So instead, make a copy of 'frustum' and set zfar0 to 1.0.
		// Calculate the zfar1 value for all planes and choose the maximum.
		assert(!frustum.orthographic() && "No amount of shifting along z can change what is within an orthographic frustum");

		// The caller assumes (0,0,0) is the apex and the far plane is (0,0,-zfar).
		// Transform 'ws_pt' to frustum space, then offset to be relative to (0,0,zfar)
		auto frustum_zfar = frustum.zfar();
		auto pt = InvertFast(f2w) * ws_pt;
		pt.z += frustum_zfar;
		
		// If 'pt' is beyond the far plane, extend the far plane
		if (pt.z - radius < 0)
		{
			frustum_zfar -= pt.z - radius;
			pt.z = radius;
		}

		// Take a copy of 'frustum' and set its zfar to 1.0 (i.e. zfar0 == 1)
		auto tnorms = frustum.m_Tplanes;
		tnorms.w = -tnorms.z;

		// Get the signed distance to all frustum planes
		auto planes = Transpose4x4(tnorms);
		auto dst = v4{
			Dot(planes.x, pt - radius * planes.x.w0()),
			Dot(planes.y, pt - radius * planes.y.w0()),
			Dot(planes.z, pt - radius * planes.z.w0()),
			Dot(planes.w, pt - radius * planes.w.w0()),
		};

		// Get the new zfar distance according to each plane
		auto zfar4 = (tnorms.w - dst) / tnorms.w;
		auto zfar = MaxElement(zfar4);
		auto dzfar = Max(0.0f, zfar - frustum_zfar);
		frustum_zfar += dzfar;
		frustum.zfar(frustum_zfar);

		// Update the f2w transform and clip planes
		f2w.pos += dzfar * f2w.z;
		nf.x = Min(nf.x + dzfar, frustum_zfar - (pt.z + radius));
		nf.y = Max(nf.y + dzfar, frustum_zfar - (pt.z - radius));
	}
	inline void pr_vectorcall Grow(Frustum& frustum, m4x4& f2w, v2& nf, BSphere_cref ws_bsphere)
	{
		assert(!frustum.orthographic() && "No amount of shifting along z can change what is within an orthographic frustum");
		return Grow(frustum, f2w, nf, ws_bsphere.Centre(), ws_bsphere.Radius());
	}
	inline void pr_vectorcall Grow(Frustum& frustum, m4x4& f2w, v2& nf, BBox_cref ws_bbox)
	{
		assert(!frustum.orthographic() && "No amount of shifting along z can change what is within an orthographic frustum");

		// The caller assumes (0,0,0) is the apex and the far plane is (0,0,-zfar).
		// Transform 'ws_bbox' to frustum space, then offset to be relative to (0,0,zfar)
		auto frustum_zfar = frustum.zfar();
		auto bbox = InvertFast(f2w) * ws_bbox;
		bbox.m_centre.z += frustum_zfar;
		
		// If 'pt' is beyond the far plane, extend the far plane
		auto pt = SupportPoint(bbox, -v4ZAxis);
		if (pt.z < 0)
		{
			frustum_zfar -= pt.z;
			bbox.m_centre.z = bbox.m_radius.z;
		}

		// Take a copy of 'frustum' and set its zfar to 1.0 (i.e. zfar0 == 1)
		auto tnorms = frustum.m_Tplanes;
		tnorms.w = -tnorms.z;

		// Get the signed distance to all frustum planes
		auto planes = Transpose4x4(tnorms);
		auto dst = v4{
			Dot(planes.x, SupportPoint(bbox, -planes.x.w0())),
			Dot(planes.y, SupportPoint(bbox, -planes.y.w0())),
			Dot(planes.z, SupportPoint(bbox, -planes.z.w0())),
			Dot(planes.w, SupportPoint(bbox, -planes.w.w0())),
		};

		// Get the new zfar distance according to each plane
		auto zfar4 = (tnorms.w - dst) / tnorms.w;
		auto zfar = MaxElement(zfar4);
		auto dzfar = Max(0.0f, zfar - frustum_zfar);
		frustum_zfar += dzfar;
		frustum.zfar(frustum_zfar);

		// Update the f2w transform
		f2w.pos += dzfar * f2w.z;
		nf.x = Min(nf.x + dzfar, frustum_zfar - bbox.UpperZ());
		nf.y = Max(nf.y + dzfar, frustum_zfar - bbox.LowerZ());
	}
	inline void pr_vectorcall Grow(Frustum& frustum, m4x4& f2w, v4_cref<> ws_pt, float radius)
	{
		auto nf = v2Zero;
		Grow(frustum, f2w, nf, ws_pt, radius);
	}
	inline void pr_vectorcall Grow(Frustum& frustum, m4x4& f2w, BSphere_cref ws_bsphere)
	{
		auto nf = v2Zero;
		Grow(frustum, f2w, nf, ws_bsphere);
	}
	inline void pr_vectorcall Grow(Frustum& frustum, m4x4& f2w, BBox_cref ws_bbox)
	{
		auto nf = v2Zero;
		Grow(frustum, f2w, nf, ws_bbox);
	}

	// Include 'f2w * frustum' in 'bbox'
	inline BBox& pr_vectorcall Grow(BBox& bbox, Frustum const& frustum, m4x4 const& f2w = m4x4Identity)
	{
		auto corner = Corners(frustum);
		Grow(bbox, f2w.pos);
		Grow(bbox, f2w * corner.x);
		Grow(bbox, f2w * corner.y);
		Grow(bbox, f2w * corner.z);
		Grow(bbox, f2w * corner.w);
		return bbox;
	}
}

