//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/plane.h"
#include "pr/maths/bsphere.h"

namespace pr
{
	struct BBox
	{
		enum class EPlane
		{
			Lx = 0,
			Ux = 1,
			Ly = 2,
			Uy = 3,
			Lz = 4,
			Uz = 5,
			NumberOf = 6
		};

		v4 m_centre;
		v4 m_radius;

		BBox() = default;
		BBox(v4 const& centre, v4 const& radius)
			:m_centre(centre)
			,m_radius(radius)
		{}

		// Reset this bbox to an invalid interval
		BBox& reset()
		{
			m_centre = v4Origin;
			m_radius = v4(-1.0f, -1.0f, -1.0f, 0.0f);
			return *this;
		}

		// Returns true if this bbox does not bound anything
		bool empty() const
		{
			return m_radius.x < 0 || m_radius.y < 0 || m_radius.z < 0;
		}

		// Set this bbox to a unit cube centred on the origin
		BBox& unit()
		{
			m_centre = v4Origin;
			m_radius = v4(0.5f, 0.5f, 0.5f, 0.0f);
			return *this;
		}

		// The centre of the bbox
		v4 Centre() const
		{
			// This method exists for uniformity with BSphere
			return m_centre;
		}

		// The radius on each axis of the bbox
		v4 RadiusSq() const
		{
			return Sqr(Radius());
		}
		v4 Radius() const
		{
			return m_radius;
		}

		// Diagonal size of the bbox
		float DiametreSq() const
		{
			return 4.0f * Length3Sq(m_radius);
		}
		float Diametre() const
		{
			return Sqrt(DiametreSq());
		}

		// Return the lower corner (-x,-y,-z) of the bounding box
		v4 Lower() const
		{
			return m_centre - m_radius;
		}
		float Lower(int axis) const
		{
			return m_centre[axis] - m_radius[axis];
		}

		// Return the upper corner (+x,+y,+z) of the bounding box
		v4 Upper() const
		{
			return m_centre + m_radius;
		}
		float Upper(int axis) const
		{
			return m_centre[axis] + m_radius[axis];
		}

		// Return the size of the bounding box
		float SizeX() const
		{
			return 2.0f * m_radius.x;
		}
		float SizeY() const
		{
			return 2.0f * m_radius.y;
		}
		float SizeZ() const
		{
			return 2.0f * m_radius.z;
		}
		float Size(int axis) const
		{
			return 2.0f * m_radius[axis];
		}

		// Create a bounding box from lower/upper corners
		static BBox Make(v4 const& lower, v4 const& upper)
		{
			return BBox((upper + lower) * 0.5f, (upper - lower) * 0.5f);
		}

		// Create a bounding box from a collection of verts
		template <typename VertCont> static BBox Make(VertCont const& verts)
		{
			auto bbox = BBoxReset;
			for (auto& vert : verts) Encompass(bbox, vert);
			return bbox;
		}

		// Create a bounding box from a list of verts
		template <typename Vert> static BBox Make(std::initializer_list<Vert> verts)
		{
			auto bbox = BBoxReset;
			for (auto& vert : verts) Encompass(bbox, vert);
			return bbox;
		}
	};
	static_assert(std::is_pod<BBox>::value, "Should be a pod type");
	static_assert(std::alignment_of<BBox>::value == 16, "Should be 16 byte aligned");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	using BBox_cref = BBox const;
	#else
	using BBox_cref = BBox const&;
	#endif

	#pragma region Constants
	static BBox const BBoxUnit  = {{0.0f,  0.0f,  0.0f, 1.0f}, {0.5f, 0.5f, 0.5f, 0.0f}};
	static BBox const BBoxReset = {{0.0f,  0.0f,  0.0f, 1.0f}, {-1.0f, -1.0f, -1.0f, 0.0f}};
	#pragma endregion

	#pragma region Operators
	inline bool	operator == (BBox const& lhs, BBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool	operator != (BBox const& lhs, BBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (BBox const& lhs, BBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (BBox const& lhs, BBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (BBox const& lhs, BBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (BBox const& lhs, BBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	inline BBox& pr_vectorcall operator += (BBox& lhs, v4_cref offset)
	{
		lhs.m_centre += offset;
		return lhs;
	}
	inline BBox& pr_vectorcall operator -= (BBox& lhs, v4_cref offset)
	{
		lhs.m_centre -= offset;
		return lhs;
	}
	inline BBox& operator *= (BBox& lhs, float s)
	{
		lhs.m_radius *= s;
		return lhs;
	}
	inline BBox& operator /= (BBox& lhs, float s)
	{
		lhs *= (1.0f / s);
		return lhs;
	}
	inline BBox pr_vectorcall operator + (BBox_cref lhs, v4_cref offset)
	{
		auto bb = lhs;
		return bb += offset;
	}
	inline BBox pr_vectorcall operator - (BBox_cref lhs, v4_cref offset)
	{
		auto bb = lhs;
		return bb -= offset;
	}
	inline BBox pr_vectorcall operator * (m4x4_cref m, BBox_cref rhs)
	{
		assert("Transforming an invalid bounding box" && !rhs.empty());

		BBox bb(m.pos, v4Zero);
		m4x4 mat = Transpose3x3(m);
		for (int i = 0; i != 3; ++i)
		{
			bb.m_centre[i] += Dot4(    mat[i] , rhs.m_centre);
			bb.m_radius[i] += Dot4(Abs(mat[i]), rhs.m_radius);
		}
		return bb;
	}
	inline BBox pr_vectorcall operator * (m3x4_cref m, BBox_cref rhs)
	{
		assert("Transforming an invalid bounding box" && !rhs.empty());

		BBox bb(v4Origin, v4Zero);
		auto mat = Transpose(m);
		for (int i = 0; i != 3; ++i)
		{
			bb.m_centre[i] += Dot4(    mat[i] , rhs.m_centre);
			bb.m_radius[i] += Dot4(Abs(mat[i]), rhs.m_radius);
		}
		return bb;
	}
	#pragma endregion

	#pragma region Functions

	// Return the volume of a bounding box
	inline float Volume(BBox const& bbox)
	{
		return bbox.SizeX() * bbox.SizeY() * bbox.SizeZ();
	}

	// Return a plane corresponding to a side of the bounding box. Returns inward facing planes
	inline Plane GetPlane(BBox const& bbox, BBox::EPlane side)
	{
		switch (side)
		{
		default: assert(false && "Unknown side index"); return Plane();
		case BBox::EPlane::Lx: return plane::make( 1.0f,  0.0f,  0.0f, bbox.m_centre.x + bbox.m_radius.x);
		case BBox::EPlane::Ux: return plane::make(-1.0f,  0.0f,  0.0f, bbox.m_centre.x + bbox.m_radius.x);
		case BBox::EPlane::Ly: return plane::make( 0.0f,  1.0f,  0.0f, bbox.m_centre.y + bbox.m_radius.y);
		case BBox::EPlane::Uy: return plane::make( 0.0f, -1.0f,  0.0f, bbox.m_centre.y + bbox.m_radius.y);
		case BBox::EPlane::Lz: return plane::make( 0.0f,  0.0f,  1.0f, bbox.m_centre.z + bbox.m_radius.z);
		case BBox::EPlane::Uz: return plane::make( 0.0f,  0.0f, -1.0f, bbox.m_centre.z + bbox.m_radius.z);
		}
	}

	// Return a corner of the bounding box
	inline v4 GetCorner(BBox const& bbox, int corner)
	{
		assert("Invalid corner index" && corner >= 0 && corner < 8);
		auto x = ((corner >> 0) & 0x1) * 2 - 1;
		auto y = ((corner >> 1) & 0x1) * 2 - 1;
		auto z = ((corner >> 2) & 0x1) * 2 - 1;
		return v4(
			bbox.m_centre.x + x * bbox.m_radius.x,
			bbox.m_centre.y + y * bbox.m_radius.y,
			bbox.m_centre.z + z * bbox.m_radius.z,
			1.0f);
	}

	// Return a bounding sphere that bounds the bounding box
	inline BSphere GetBSphere(BBox const& bbox)
	{
		return BSphere(bbox.m_centre, Length3(bbox.m_radius));
	}

	// Encompass 'point' within 'bbox'.
	inline BBox& pr_vectorcall Encompass(BBox& bbox, v4_cref point)
	{
		assert("BBox encompass point must have w = 1" && point.w == 1.0f);
		assert("'point' must be aligned to 16" && maths::is_aligned(&point));

		#if PR_MATHS_USE_INTRINSICS
		__m128 const zero = _mm_set_ps1(+0.0f);
		__m128 const half = _mm_set_ps1(+0.5f);
		auto init = _mm_cmplt_ps(bbox.m_radius.vec, zero);                          /// init = radius == -1 ? FFFF... : 0000... 
		auto lwr = _mm_sub_ps(bbox.m_centre.vec, bbox.m_radius.vec);                /// lwr  = centre - radius
		auto upr = _mm_add_ps(bbox.m_centre.vec, bbox.m_radius.vec);                /// upr  = centre + radius
		auto init_pt = _mm_and_ps(init, point.vec);                                 /// init_pt = init & point
		lwr = _mm_or_ps(init_pt , _mm_andnot_ps(init, _mm_min_ps(lwr, point.vec))); /// lwr  = init_pt | (~init & min(lwr, point))
		upr = _mm_or_ps(init_pt , _mm_andnot_ps(init, _mm_max_ps(upr, point.vec))); /// upr  = init_pt | (~init & max(upr, point))
		bbox.m_centre.vec = _mm_mul_ps(_mm_add_ps(upr,lwr), half);                  /// center = (upr + lwr) / 2;
		bbox.m_radius.vec = _mm_mul_ps(_mm_sub_ps(upr,lwr), half);                  /// radius = (upr - lwr) / 2;
		#else
		for (int i = 0; i != 3; ++i)
		{
			if (bbox.m_radius[i] < 0.0f)
			{
				bbox.m_centre[i] = point[i];
				bbox.m_radius[i] = 0.0f;
			}
			else
			{
				float signed_dist = point[i] - bbox.m_centre[i];
				float length      = Abs(signed_dist);
				if (length > bbox.m_radius[i])
				{
					float new_radius = (length + bbox.m_radius[i]) / 2.0f;
					bbox.m_centre[i] += signed_dist * (new_radius - bbox.m_radius[i]) / length;
					bbox.m_radius[i] = new_radius;
				}
			}
		}
		#endif
		return bbox;
	}
	inline BBox pr_vectorcall Encompass(BBox const& bbox, v4_cref point)
	{
		auto bb = bbox;
		return Encompass(bb, point);
	}

	// Encompass 'rhs' in 'lhs'
	inline BBox& pr_vectorcall Encompass(BBox& lhs, BBox_cref rhs)
	{
		// Don't treat rhs.empty() as an error, it's the only way to Encompass a empty bbox
		if (rhs.empty()) return lhs;
		Encompass(lhs, rhs.m_centre + rhs.m_radius);
		Encompass(lhs, rhs.m_centre - rhs.m_radius);
		return lhs;
	}
	inline BBox pr_vectorcall Encompass(BBox const& lhs, BBox_cref rhs)
	{
		auto bb = lhs;
		return Encompass(bb, rhs);
	}

	// Encompass 'rhs' in 'lhs'
	inline BBox& Encompass(BBox& lhs, BSphere const& rhs)
	{
		// Don't treat rhs.empty() as an error, it's the only way to Encompass a empty bsphere
		if (rhs.empty()) return lhs;
		auto radius = v4(rhs.Radius(), rhs.Radius(), rhs.Radius(), 0);
		Encompass(lhs, rhs.Centre() + radius);
		Encompass(lhs, rhs.Centre() - radius);
		return lhs;
	}

	// Returns true if 'point' is within the bounding volume
	inline bool pr_vectorcall IsWithin(BBox_cref bbox, v4_cref point, float tol = 0)
	{
		return
			Abs(point.x - bbox.m_centre.x) <= bbox.m_radius.x + tol &&
			Abs(point.y - bbox.m_centre.y) <= bbox.m_radius.y + tol &&
			Abs(point.z - bbox.m_centre.z) <= bbox.m_radius.z + tol;
	}

	// Returns true if 'test' is within the bounding volume
	inline bool pr_vectorcall IsWithin(BBox_cref bbox, BBox_cref test)
	{
		return
			Abs(test.m_centre.x - bbox.m_centre.x) <= (bbox.m_radius.x - test.m_radius.x) &&
			Abs(test.m_centre.y - bbox.m_centre.y) <= (bbox.m_radius.y - test.m_radius.y) &&
			Abs(test.m_centre.z - bbox.m_centre.z) <= (bbox.m_radius.z - test.m_radius.z);
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_bbox)
		{
			v4 pt[] =
			{
				{+1,+1,+1,1},
				{-1,+0,+1,1},
				{+1,+1,+1,1},
				{+0,-2,-1,1},
			};
			auto bbox = BBoxReset;
			for (auto& p : pt) pr::Encompass(bbox, p);
			PR_CHECK(bbox.Lower().x, -1.0f);
			PR_CHECK(bbox.Lower().y, -2.0f);
			PR_CHECK(bbox.Lower().z, -1.0f);
			PR_CHECK(bbox.Lower().w, +1.0f);
			PR_CHECK(bbox.Upper().x, +1.0f);
			PR_CHECK(bbox.Upper().y, +1.0f);
			PR_CHECK(bbox.Upper().z, +1.0f);
			PR_CHECK(bbox.Upper().w, +1.0f);
		}
	}
}
#endif