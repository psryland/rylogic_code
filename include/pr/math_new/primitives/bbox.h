//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/types/vector4.h"
#include "pr/math_new/types/matrix3x4.h"
#include "pr/math_new/types/matrix4x4.h"
#include "pr/math_new/primitives/bsphere.h"
//#include "pr/maths/constants.h"
//#include "pr/maths/plane.h"

namespace pr::math
{
	template <ScalarType S>
	struct BBox
	{
		using Vec4 = Vec4<S>;

		// Faces of the bounding box
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

		Vec4 m_centre;
		Vec4 m_radius;

		BBox() = default;
		constexpr BBox(Vec4 centre, Vec4 radius)
			: m_centre(centre)
			, m_radius(radius)
		{
			// Catch invalid bbox radii (make an exception for the 'Reset' bbox)
			pr_assert("Invalid bounding box" && (
				(radius.x >= +S(0) && radius.y >= +S(0) && radius.z >= +S(0)) ||
				(radius.x == -S(1) && radius.y == -S(1) && radius.z == -S(1))));
		}

		// Reset this bbox to an invalid interval
		BBox& reset()
		{
			m_centre = Origin<Vec4>();
			m_radius = Vec4{ S(-1), S(-1), S(-1), S(0) };
			return *this;
		}

		// Returns true if the bbox is valid
		bool valid() const
		{
			return m_radius.x >= 0 && m_radius.y >= 0 && m_radius.z >= 0 &&
				IsFinite(LengthSq(m_radius)) && IsFinite(LengthSq(m_centre));
		}

		// Returns true if this bbox encloses a single point
		bool is_point() const
		{
			return m_radius == Zero<Vec4>();
		}

		// Returns true if all of the radii are non zero
		bool has_volume() const
		{
			return m_radius.x != 0 && m_radius.y != 0 && m_radius.z != 0;
		}

		// Set this bbox to a unit cube centred on the origin
		BBox& unit()
		{
			m_centre = Origin<Vec4>();
			m_radius = Vec4{ S(0.5), S(0.5), S(0.5), S(0) };
			return *this;
		}

		// The centre of the bbox
		Vec4 Centre() const
		{
			// This method exists for uniformity with BSphere
			return m_centre;
		}

		// The radius on each axis of the bbox
		Vec4 RadiusSq() const
		{
			return Sqr(Radius());
		}
		Vec4 Radius() const
		{
			return m_radius;
		}

		// Diagonal size of the bbox
		S DiametreSq() const
		{
			return S(4) * LengthSq(m_radius);
		}
		double Diametre() const
		{
			return Sqrt(static_cast<double>(DiametreSq()));
		}

		// Return the lower corner (-x,-y,-z) of the bounding box
		S LowerX() const
		{
			return m_centre.x - m_radius.x;
		}
		S LowerY() const
		{
			return m_centre.y - m_radius.y;
		}
		S LowerZ() const
		{
			return m_centre.z - m_radius.z;
		}
		S Lower(int axis) const
		{
			return m_centre[axis] - m_radius[axis];
		}
		Vec4 Lower() const
		{
			return m_centre - m_radius;
		}

		// Return the upper corner (+x,+y,+z) of the bounding box
		S UpperX() const
		{
			return m_centre.x + m_radius.x;
		}
		S UpperY() const
		{
			return m_centre.y + m_radius.y;
		}
		S UpperZ() const
		{
			return m_centre.z + m_radius.z;
		}
		S Upper(int axis) const
		{
			return m_centre[axis] + m_radius[axis];
		}
		Vec4 Upper() const
		{
			return m_centre + m_radius;
		}

		// Return the size of the bounding box
		S SizeX() const
		{
			return S(2) * m_radius.x;
		}
		S SizeY() const
		{
			return S(2) * m_radius.y;
		}
		S SizeZ() const
		{
			return S(2) * m_radius.z;
		}
		S Size(int axis) const
		{
			return S(2) * m_radius[axis];
		}

		// Grows the bbox to include 'rhs'. Note: prefer the free function versions.
		// There are two variations of 'Encompass':
		//   1) Grow = modifies the provided instance returning the point enclosed,
		//   2) Union = operates on a const BBox returning a new BBox that includes 'point'
		constexpr Vec4 Grow(Vec4 point)
		{
			pr_assert("BBox Grow. Point must have w = 1" && point.w == 1.0f);

			auto fallback = [&]() constexpr
			{
				for (int i = 0; i != 3; ++i)
				{
					if (m_radius[i] < 0)
					{
						m_centre[i] = point[i];
						m_radius[i] = 0;
					}
					else
					{
						auto signed_dist = point[i] - m_centre[i];
						auto length = Abs(signed_dist);
						if (length > m_radius[i])
						{
							auto new_radius = (length + m_radius[i]) / 2;
							m_centre[i] += signed_dist * (new_radius - m_radius[i]) / length;
							m_radius[i] = new_radius;
						}
					}
				}
			};
			auto optimised = [&]()
			{
				__m128 const zero = _mm_set_ps1(+0.0f);
				__m128 const half = _mm_set_ps1(+0.5f);
				auto init = _mm_cmplt_ps(m_radius.vec, zero);                               // init = radius == -1 ? FFFF... : 0000... 
				auto lwr = _mm_sub_ps(m_centre.vec, m_radius.vec);                          // lwr  = centre - radius
				auto upr = _mm_add_ps(m_centre.vec, m_radius.vec);                          // upr  = centre + radius
				auto init_pt = _mm_and_ps(init, point.vec);                                 // init_pt = init & point
				lwr = _mm_or_ps(init_pt, _mm_andnot_ps(init, _mm_min_ps(lwr, point.vec))); // lwr  = init_pt | (~init & min(lwr, point))
				upr = _mm_or_ps(init_pt, _mm_andnot_ps(init, _mm_max_ps(upr, point.vec))); // upr  = init_pt | (~init & max(upr, point))
				m_centre.vec = _mm_mul_ps(_mm_add_ps(upr, lwr), half);                       // center = (upr + lwr) / 2;
				m_radius.vec = _mm_mul_ps(_mm_sub_ps(upr, lwr), half);                       // radius = (upr - lwr) / 2;
			};

			if consteval
			{
				fallback();
			}
			else
			{
				if constexpr (Vec4::IntrinsicF)
					optimised();
				else
					fallback();
			}
			return point;
		}

		#pragma region Operators
		friend bool pr_vectorcall operator == (BBox lhs, BBox rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
		friend bool pr_vectorcall operator != (BBox lhs, BBox rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
		friend bool pr_vectorcall operator <  (BBox lhs, BBox rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
		friend bool pr_vectorcall operator >  (BBox lhs, BBox rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
		friend bool pr_vectorcall operator <= (BBox lhs, BBox rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
		friend bool pr_vectorcall operator >= (BBox lhs, BBox rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
		friend BBox& pr_vectorcall operator += (BBox& lhs, Vec4 offset)
		{
			lhs.m_centre += offset;
			return lhs;
		}
		friend BBox& pr_vectorcall operator -= (BBox& lhs, Vec4 offset)
		{
			lhs.m_centre -= offset;
			return lhs;
		}
		friend BBox& pr_vectorcall operator *= (BBox& lhs, S s)
		{
			lhs.m_radius *= s;
			return lhs;
		}
		friend BBox& pr_vectorcall operator /= (BBox& lhs, S s)
		{
			lhs *= (1.0f / s);
			return lhs;
		}
		friend BBox pr_vectorcall operator + (BBox lhs, Vec4 offset)
		{
			auto bb = lhs;
			return bb += offset;
		}
		friend BBox pr_vectorcall operator - (BBox lhs, Vec4 offset)
		{
			auto bb = lhs;
			return bb -= offset;
		}
		friend BBox pr_vectorcall operator * (Mat4x4<S> const& m, BBox rhs)
		{
			pr_assert("m4x4 * BBox: Transform is not affine" && IsAffine(m));
			pr_assert("Transforming an invalid bounding box" && rhs.valid());

			BBox bb(m.pos, Vec4::Zero());
			auto mat = Transpose3x3(m);
			for (int i = 0; i != 3; ++i)
			{
				bb.m_centre[i] += Dot(    mat[i] , rhs.m_centre);
				bb.m_radius[i] += Dot(Abs(mat[i]), rhs.m_radius);
			}
			return bb;
		}
		friend BBox pr_vectorcall operator * (Mat3x4<S> const& m, BBox rhs)
		{
			pr_assert("Transforming an invalid bounding box" && rhs.valid());

			BBox bb(Vec4::Origin(), Vec4::Zero());
			auto mat = Transpose(m);
			for (int i = 0; i != 3; ++i)
			{
				bb.m_centre[i] += Dot(    mat[i] , rhs.m_centre);
				bb.m_radius[i] += Dot(Abs(mat[i]), rhs.m_radius);
			}
			return bb;
		}
		#pragma endregion

		// Constants
		static constexpr BBox Unit()
		{
			return BBox{ math::Origin<Vec4>(), S(0.5) * math::One<Vec4>().w0() };
		}
		static constexpr BBox Reset()
		{
			return BBox{ math::Origin<Vec4>(), -math::One<Vec4>().w0() };
		}
		static constexpr BBox Infinity()
		{
			return BBox{ math::Origin<Vec4>(), math::Infinity<Vec4>().w0() };
		}

		// Create a bounding box from lower/upper corners
		static BBox Make(Vec4 lower, Vec4 upper)
		{
			return BBox((upper + lower) * 0.5f, (upper - lower) * 0.5f);
		}

		// Create a bounding box from a collection of points
		static BBox Make(std::ranges::input_range auto&& points)
		{
			auto bbox = Reset();
			for (auto& point : points) bbox.Grow(point);
			return bbox;
		}

		//// Create a bounding box from a list of verts
		//template <typename Vert> static BBox Make(std::initializer_list<Vert> verts)
		//{
		//	auto bbox = Reset();
		//	for (auto& vert : verts) pr::Grow(bbox, vert);
		//	return bbox;
		//}
	};
	static_assert(std::is_trivially_copyable_v<BBox<float>>, "Should be a pod type");
	static_assert(std::alignment_of_v<BBox<float>> == 16, "Should be 16 byte aligned");

	#pragma region Functions

	// Return a corner of the bounding box
	template <ScalarType S> constexpr Vec4<S> pr_vectorcall Corner(BBox<S> bbox, int corner)
	{
		pr_assert("Invalid corner index" && corner >= 0 && corner < 8);
		auto x = ((corner >> 0) & 0x1) * 2 - 1;
		auto y = ((corner >> 1) & 0x1) * 2 - 1;
		auto z = ((corner >> 2) & 0x1) * 2 - 1;
		return Vec4<S>(
			bbox.m_centre.x + x * bbox.m_radius.x,
			bbox.m_centre.y + y * bbox.m_radius.y,
			bbox.m_centre.z + z * bbox.m_radius.z,
			S(1));
	}

	// Return the corners of the bounding box
	template <ScalarType S> constexpr std::array<Vec4<S>,8> pr_vectorcall Corners(BBox<S> bbox)
	{
		auto& c = bbox.m_centre;
		auto& r = bbox.m_radius;
		std::array<Vec4<S>, 8> corners;
		corners[0] = Vec4<S>{c.x - r.x, c.y - r.y, c.z - r.z, 1};
		corners[1] = Vec4<S>{c.x + r.x, c.y - r.y, c.z - r.z, 1};
		corners[2] = Vec4<S>{c.x - r.x, c.y + r.y, c.z - r.z, 1};
		corners[3] = Vec4<S>{c.x + r.x, c.y + r.y, c.z - r.z, 1};
		corners[4] = Vec4<S>{c.x - r.x, c.y - r.y, c.z + r.z, 1};
		corners[5] = Vec4<S>{c.x + r.x, c.y - r.y, c.z + r.z, 1};
		corners[6] = Vec4<S>{c.x - r.x, c.y + r.y, c.z + r.z, 1};
		corners[7] = Vec4<S>{c.x + r.x, c.y + r.y, c.z + r.z, 1};
		return corners;
	}

	// Return the volume of a bounding box
	template <ScalarType S> constexpr S pr_vectorcall Volume(BBox<S> bbox)
	{
		return bbox.SizeX() * bbox.SizeY() * bbox.SizeZ();
	}

	// Include 'point' within 'bbox'.
	template <ScalarType S> [[nodiscard]] constexpr BBox<S> pr_vectorcall Union(BBox<S> bbox, Vec4<S> point)
	{
		// Const version returns lhs, non-const returns rhs!
		auto bb = bbox;
		bb.Grow(point);
		return bb;
	}
	template <ScalarType S> constexpr Vec4<S> pr_vectorcall Grow(BBox<S>& bbox, Vec4<S> point)
	{
		return bbox.Grow(point);
	}

	// Include 'rhs' in 'lhs'
	template <ScalarType S> [[nodiscard]] constexpr BBox<S> pr_vectorcall Union(BBox<S> lhs, BBox<S> rhs)
	{
		// Const version returns lhs, non-const returns rhs!
		// Don't treat !rhs.valid() as an error, it's the only way to grow an empty bbox
		auto bb = lhs;
		if (!rhs.valid()) return bb;
		bb.Grow(rhs.m_centre + rhs.m_radius);
		bb.Grow(rhs.m_centre - rhs.m_radius);
		return bb;
	}
	template <ScalarType S> constexpr BBox<S> pr_vectorcall Grow(BBox<S>& lhs, BBox<S> rhs)
	{
		// Const version returns lhs, non-const returns rhs!
		// Don't treat !rhs.valid() as an error, it's the only way to grow an empty bbox
		if (!rhs.valid()) return rhs;
		lhs.Grow(rhs.m_centre + rhs.m_radius);
		lhs.Grow(rhs.m_centre - rhs.m_radius);
		return rhs;
	}

	// Returns the most extreme point in the direction of 'separating_axis'
	template <ScalarType S> constexpr Vec4<S> pr_vectorcall SupportPoint(BBox<S> bbox, Vec4<S> separating_axis)
	{
		return bbox.m_centre + Sign(separating_axis, false) * bbox.m_radius;
	}

	#if 0 // Todo
	// Return a plane corresponding to a side of the bounding box. Returns inward facing planes
	inline Plane pr_vectorcall GetPlane(BBox_cref bbox, BBox::EPlane side)
	{
		switch (side)
		{
		default: pr_assert(false && "Unknown side index"); return Plane();
		case BBox::EPlane::Lx: return plane::make( 1.0f,  0.0f,  0.0f, bbox.m_centre.x + bbox.m_radius.x);
		case BBox::EPlane::Ux: return plane::make(-1.0f,  0.0f,  0.0f, bbox.m_centre.x + bbox.m_radius.x);
		case BBox::EPlane::Ly: return plane::make( 0.0f,  1.0f,  0.0f, bbox.m_centre.y + bbox.m_radius.y);
		case BBox::EPlane::Uy: return plane::make( 0.0f, -1.0f,  0.0f, bbox.m_centre.y + bbox.m_radius.y);
		case BBox::EPlane::Lz: return plane::make( 0.0f,  0.0f,  1.0f, bbox.m_centre.z + bbox.m_radius.z);
		case BBox::EPlane::Uz: return plane::make( 0.0f,  0.0f, -1.0f, bbox.m_centre.z + bbox.m_radius.z);
		}
	}
	#endif

	// Return a bounding sphere that bounds the bounding box
	template <ScalarType S> [[nodiscard]] BSphere<S> pr_vectorcall GetBSphere(BBox<S> bbox)
	{
		return BSphere<S>(bbox.m_centre, Length(bbox.m_radius));
	}

	// Include 'rhs' in 'lhs'
	template <ScalarType S> [[nodiscard]] constexpr BBox<S> Union(BBox<S> lhs, BSphere<S> rhs)
	{
		// Don't treat rhs.empty() as an error, it's the only way to grow an empty bsphere
		auto bb = lhs;
		if (!rhs.valid()) return bb;
		auto radius = Vec4<S>(rhs.Radius(), rhs.Radius(), rhs.Radius(), 0);
		bb.Grow(rhs.Centre() + radius);
		bb.Grow(rhs.Centre() - radius);
		return bb;
	}
	template <ScalarType S> constexpr BSphere<S> Grow(BBox<S>& lhs, BSphere<S> rhs)
	{
		// Don't treat rhs.empty() as an error, it's the only way to grow an empty bsphere
		if (!rhs.valid()) return rhs;
		auto radius = Vec4<S>(rhs.Radius(), rhs.Radius(), rhs.Radius(), 0);
		lhs.Grow(rhs.Centre() + radius);
		lhs.Grow(rhs.Centre() - radius);
		return rhs;
	}

	// Returns true if 'point' is within the bounding volume
	template <ScalarType S> constexpr bool pr_vectorcall IsWithin(BBox<S> bbox, Vec4<S> point, S tol = 0)
	{
		return
			Abs(point.x - bbox.m_centre.x) <= bbox.m_radius.x + tol &&
			Abs(point.y - bbox.m_centre.y) <= bbox.m_radius.y + tol &&
			Abs(point.z - bbox.m_centre.z) <= bbox.m_radius.z + tol;
	}

	// Returns true if 'test' is within the bounding volume
	template <ScalarType S> constexpr bool pr_vectorcall IsWithin(BBox<S> bbox, BBox<S> test)
	{
		return
			Abs(test.m_centre.x - bbox.m_centre.x) <= (bbox.m_radius.x - test.m_radius.x) &&
			Abs(test.m_centre.y - bbox.m_centre.y) <= (bbox.m_radius.y - test.m_radius.y) &&
			Abs(test.m_centre.z - bbox.m_centre.z) <= (bbox.m_radius.z - test.m_radius.z);
	}

	// Multiply the bounding box by a non-affine transform
	template <ScalarType S> constexpr BBox<S> pr_vectorcall MulNonAffine(Mat4x4<S> const& m, BBox<S> rhs)
	{
		pr_assert("Transforming an invalid bounding box" && rhs.valid());

		auto bb = BBox<S>::Reset();
		for (auto& c : Corners(rhs))
		{
			auto cnr = m * c;
			bb.Grow(cnr / cnr.w);
		}
		return bb;
	}
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::math
{
	PRUnitTestClass(BoundingBoxTests)
	{
		PRUnitTestMethod(Grow)
		{
			Vec4<float> pt[] =
			{
				{+1,+1,+1,1},
				{-1,+0,+1,1},
				{+1,+1,+1,1},
				{+0,-2,-1,1},
			};
	
			auto bbox = BBox<float>::Reset();
			for (auto& p : pt)
				Grow(bbox, p);

			PR_EXPECT(bbox.Lower().x == -1.0f);
			PR_EXPECT(bbox.Lower().y == -2.0f);
			PR_EXPECT(bbox.Lower().z == -1.0f);
			PR_EXPECT(bbox.Lower().w == +1.0f);
			PR_EXPECT(bbox.Upper().x == +1.0f);
			PR_EXPECT(bbox.Upper().y == +1.0f);
			PR_EXPECT(bbox.Upper().z == +1.0f);
			PR_EXPECT(bbox.Upper().w == +1.0f);
		}
	};
}
#endif