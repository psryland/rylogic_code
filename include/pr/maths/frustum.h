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
			assert(zfar > 0 && "The far plane should be a positive distance from the apex");

			Frustum f;
			f.m_Tplanes.x = v4(+z, 0.0f, -width * 0.5f, 0.0f); // left
			f.m_Tplanes.y = v4(-z, 0.0f, -width * 0.5f, 0.0f); // right
			f.m_Tplanes.z = v4(0.0f, +z, -height * 0.5f, 0.0f); // bottom
			f.m_Tplanes.w = v4(0.0f, -z, -height * 0.5f, 0.0f); // top
			f.m_Tplanes.x = Normalise3(f.m_Tplanes.x);
			f.m_Tplanes.y = Normalise3(f.m_Tplanes.y);
			f.m_Tplanes.z = Normalise3(f.m_Tplanes.z);
			f.m_Tplanes.w = Normalise3(f.m_Tplanes.w);
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
			return MakeWH(aspect*h, h, 1.0f, zfar);
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
			auto fov = ACos(Clamp(Dot(l,-r), -1.0f, 1.0f));
			return fov;
		}

		// Get the Y field of view
		float fovY() const
		{
			// The FovY is the angle between the bottom/top plane normals: Cos(ang) = dot(b,t)
			auto b = v4(m_Tplanes.x.z, m_Tplanes.y.z, m_Tplanes.z.z, 0);
			auto t = v4(m_Tplanes.x.w, m_Tplanes.y.w, m_Tplanes.z.w, 0);
			auto fov = ACos(Clamp(Dot(b,-t), -1.0f, 1.0f));
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
				return v2(w * z / z0, h * z / z0);
			}
		}

		// Return a plane of the frustum
		Plane plane(EPlane plane_index) const
		{
			switch (plane_index)
			{
			case EPlane::XPos: return pr::Plane(m_Tplanes.x.x, m_Tplanes.y.x, m_Tplanes.z.x, m_Tplanes.w.x);
			case EPlane::XNeg: return pr::Plane(m_Tplanes.x.y, m_Tplanes.y.y, m_Tplanes.z.y, m_Tplanes.w.y);
			case EPlane::YPos: return pr::Plane(m_Tplanes.x.z, m_Tplanes.y.z, m_Tplanes.z.z, m_Tplanes.w.z);
			case EPlane::YNeg: return pr::Plane(m_Tplanes.x.w, m_Tplanes.y.w, m_Tplanes.z.w, m_Tplanes.w.w);
			case EPlane::ZFar: return pr::Plane{0, 0, 1, 0};
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
			auto interval  = d1 - d0;

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
		//friend Frustum operator * (m4x4 const& m, Frustum const& rhs) // Do not implement this. Frustums cannot be rotated or moved.
		#pragma endregion
	};

	#pragma region Functions

	// Return true if any part of a sphere around 'point' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, v4 const& point, float radius, bool include_zfar)
	{
		auto pt = point;
		
		// Orthographic frusta don't have a zfar distance
		if (!frustum.orthographic())
		{
			// Shift the point so that zfar is at (0,0,0)
			pt.z += frustum.zfar();

			// Test against the far plane
			if (include_zfar && pt.z + radius < 0)
				return false;
		}
		else
		{
			// Orthographic frusta start at (0,0,0)
			if (pt.z - radius > 0)
				return false;
		}

		// Dot product of 'point' with each plane gives the signed distance to each plane.
		// Increase each distance by 'radius'. This is not strictly correct because we're
		// effectively expanding the frustum by 'radius' not the sphere. It doesn't work
		// near edges.
		auto dots = frustum.m_Tplanes * pt + v4(radius);

		// If all dot products are >= 0 then some part of the sphere is within the frustum
		return dots == Abs(dots); 
	}

	// Return true if any part of 'bsphere' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, BSphere const& sphere, bool include_zfar)
	{
		return IsWithin(frustum, sphere.Centre(), sphere.Radius(), include_zfar);
	}

	// Return true if any part of 'bbox' is within 'frustum'
	inline bool IsWithin(Frustum const& frustum, BBox const& bbox, bool include_zfar)
	{
		// Shift 'bbox' to that zfar is at (0,0,0)
		auto bb = bbox;
		bb.m_centre.z += frustum.zfar();

		// Test against the far plane
		if (include_zfar && bb.m_centre.z + Abs(bb.m_radius.z) < 0)
			return false;

		// todo: Test for intersection of 'bb' with the four planes of the frustum
		// for now:
		return IsWithin(frustum, GetBSphere(bbox), include_zfar);
	}

	// Returns the corners of the frustum (in frustum space) at a given 'z' distance (i.e. apex at (0,0,0). Far plane at (0,0,-zfar)).
	// Return order: x=lb, y=lt, z=rt, w=rb
	inline m4x4 GetCorners(Frustum const& frustum, float z)
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
	inline m4x4 GetCorners(Frustum const& frustum)
	{
		// Warning: calling this on orthographic frusta is probably a bug.. unless Z isn't used.
		return GetCorners(frustum, frustum.zfar());
	}

	// Encompass 'f2w * frustum' in 'bbox'
	inline BBox& pr_vectorcall Encompass(BBox& bbox, Frustum const& frustum, m4x4 const& f2w = m4x4Identity)
	{
		auto corner = GetCorners(frustum);
		Encompass(bbox, f2w.pos);
		Encompass(bbox, f2w * corner.x);
		Encompass(bbox, f2w * corner.y);
		Encompass(bbox, f2w * corner.z);
		Encompass(bbox, f2w * corner.w);
		return bbox;
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(FrustumTests)
	{
		{ // FA Frustum
			auto f = Frustum::MakeFA(maths::tau_by_8f, 1.75f, 10.0f);
			
			// zdist
			f.zfar(5.0f);
			PR_CHECK(f.zfar(), 5.0f);
			f.zfar(10.0f);
			PR_CHECK(f.zfar(), 10.0f);

			// aspect = tan(fovX/2) / tan(fovY/2)
			PR_CHECK(FEql(f.aspect(), 1.75f), true);

			// fov
			PR_CHECK(FEql(f.fovY(), maths::tau_by_8f), true);
			PR_CHECK(FEql(f.fovX(), 2 * ATan(f.aspect() * Tan(f.fovY()/2))), true);

			// width/height
			auto wh = f.area(1.0f);
			PR_CHECK(FEql(wh.x, 2 * Tan(f.fovX() / 2)), true);
			PR_CHECK(FEql(wh.y, 2 * Tan(f.fovY() / 2)), true);

			f.zfar(2.0f);

			// IsWithin
			// In front/behind test
			PR_CHECK(IsWithin(f, v4(0, 0, -1.0f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(0, 0, +0.1f, 1), 0.0f, true), false);

			// zfar test
			PR_CHECK(IsWithin(f, v4(0, 0, -2.1f, 1), 0.0f, true), false);
			PR_CHECK(IsWithin(f, v4(0, 0, -2.1f, 1), 0.0f, false), true);

			// Random points
			PR_CHECK(IsWithin(f, v4(+0.4f, -0.2f, -0.8f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(-0.3f, +0.4f, -0.8f, 1), 0.0f, true), false);

			// Radius test
			PR_CHECK(IsWithin(f, v4(+0.6f, -0.4f, -0.8f, 1), 0.07f, true), true);
			PR_CHECK(IsWithin(f, v4(+0.6f, -0.4f, -0.8f, 1), 0.06f, true), false);

			// GetCorners
			auto corners = GetCorners(f, 1.0f);
			PR_CHECK(FEql(corners.x, v4(-0.724874f, -0.414214f, -1.0f, 1)), true);
			PR_CHECK(FEql(corners.y, v4(-0.724874f, +0.414214f, -1.0f, 1)), true);
			PR_CHECK(FEql(corners.z, v4(+0.724874f, +0.414214f, -1.0f, 1)), true);
			PR_CHECK(FEql(corners.w, v4(+0.724874f, -0.414214f, -1.0f, 1)), true);
		}
		{ // WH Frustum
			auto f = Frustum::MakeWH(v2(16.0f, 9.0f), 10.0f, 20.0f);
			
			// zdist
			PR_CHECK(f.zfar(), 20.0f);

			// aspect = tan(fovX/2) / tan(fovY/2)
			PR_CHECK(FEql(f.aspect(), 16.0f/9.0f), true);

			// fov
			PR_CHECK(FEql(f.fovX(), 2 * ATan(8.0f / 10.0f)), true);
			PR_CHECK(FEql(f.fovY(), 2 * ATan(4.5f / 10.0f)), true);

			// width/height
			auto wh = f.area(1.0f);
			PR_CHECK(FEql(wh.x, 1.6f), true);
			PR_CHECK(FEql(wh.y, 0.9f), true);

			f.zfar(2.0f);

			// IsWithin
			// In front/behind test
			PR_CHECK(IsWithin(f, v4(0, 0, -1.0f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(0, 0, +0.1f, 1), 0.0f, true), false);
			
			// zfar test
			PR_CHECK(IsWithin(f, v4(0, 0, -2.1f, 1), 0.0f, true), false);
			PR_CHECK(IsWithin(f, v4(0, 0, -2.1f, 1), 0.0f, false), true);
			
			// Random points
			PR_CHECK(IsWithin(f, v4(+0.6f, -0.2f, -0.8f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(+0.8f, -0.5f, -0.7f, 1), 0.0f, true), false);
			
			// Radius test
			PR_CHECK(IsWithin(f, v4(+0.8f, -0.5f, -0.7f, 1), 0.23f, true), true);
			PR_CHECK(IsWithin(f, v4(+0.8f, -0.5f, -0.7f, 1), 0.20f, true), true); // This should be false but isn't because 'radius' actually expands the frustum not the sphere.
			
			// GetCorners
			auto corners = GetCorners(f, 2.0f);
			PR_CHECK(FEql(corners.x, v4(-1.6f, -0.9f, -2.0f, 1)), true);
			PR_CHECK(FEql(corners.y, v4(-1.6f, +0.9f, -2.0f, 1)), true);
			PR_CHECK(FEql(corners.z, v4(+1.6f, +0.9f, -2.0f, 1)), true);
			PR_CHECK(FEql(corners.w, v4(+1.6f, -0.9f, -2.0f, 1)), true);
		}
		{// Ortho frustum
			auto f = Frustum::MakeOrtho(v2(1.6f, 0.9f));

			// zdist
			PR_CHECK(FEql(f.zfar(), maths::float_inf), true);

			// aspect = tan(fovX/2) / tan(fovY/2)
			PR_CHECK(FEql(f.aspect(), 1.6f/0.9f), true);

			// fov
			PR_CHECK(FEql(f.fovX(), 0.0f), true);
			PR_CHECK(FEql(f.fovY(), 0.0f), true);

			// width/height
			auto wh = f.area(1.0f);
			PR_CHECK(FEql(wh.x, 1.6f), true);
			PR_CHECK(FEql(wh.y, 0.9f), true);

			// IsWithin
			// In front/behind test
			PR_CHECK(IsWithin(f, v4(0, 0, -1.0f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(0, 0, +0.1f, 1), 0.0f, true), false);
			
			// zfar test
			PR_CHECK(IsWithin(f, v4(0, 0, -2.1f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(0, 0, -2.1f, 1), 0.0f, false), true);
			
			// Random points
			PR_CHECK(IsWithin(f, v4(+0.3f, -0.44f, -0.8f, 1), 0.0f, true), true);
			PR_CHECK(IsWithin(f, v4(+0.3f, -0.46f, -0.8f, 1), 0.0f, true), false);
			
			// Radius test
			PR_CHECK(IsWithin(f, v4(+0.3f, -0.5f, -0.8f, 1), 0.06f, true), true);
			PR_CHECK(IsWithin(f, v4(+0.3f, -0.5f, -0.8f, 1), 0.04f, true), false);

			// GetCorners
			auto corners = GetCorners(f, 2.0f);
			PR_CHECK(FEql(corners.x, v4(-0.8f, -0.45f, -2.0f, 1)), true);
			PR_CHECK(FEql(corners.y, v4(-0.8f, +0.45f, -2.0f, 1)), true);
			PR_CHECK(FEql(corners.z, v4(+0.8f, +0.45f, -2.0f, 1)), true);
			PR_CHECK(FEql(corners.w, v4(+0.8f, -0.45f, -2.0f, 1)), true);
		}
		{
			auto DumpFrustum = [](Frustum const& f)
			{
				(void)f;
				//auto c = GetCorners(f);
				//
				//std::string s;
				//ldr::Triangle(s, "left", 0xFFFFFFFF, v4Origin, c.x, c.y);
				//ldr::Triangle(s, "top", 0xFFFFFFFF, v4Origin, c.y, c.z);
				//ldr::Triangle(s, "right", 0xFFFFFFFF, v4Origin, c.z, c.w);
				//ldr::Triangle(s, "bottom", 0xFFFFFFFF, v4Origin, c.w, c.x);
				//ldr::Plane(s, "left", 0xFFFFFFFF, f.plane(Frustum::EPlane::XPos), v4Origin, zfar);
				//ldr::Plane(s, "right", 0xFFFFFFFF, f.plane(Frustum::EPlane::XNeg), v4Origin, zfar);
				//ldr::Plane(s, "bottom", 0xFFFFFFFF, f.plane(Frustum::EPlane::YPos), v4Origin, zfar);
				//ldr::Plane(s, "top", 0xFFFFFFFF, f.plane(Frustum::EPlane::YNeg), v4Origin, zfar);
				//ldr::Write(s, "P:\\dump\\frustum.ldr");
			};

			//auto f0 = Frustum::MakeFA(maths::tau_by_8f, 1.75f, 10.0f);
			//DumpFrustum(f0);
		}
	}
}
#endif
