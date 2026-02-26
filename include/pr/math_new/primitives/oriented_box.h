//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/types/vector4.h"
#include "pr/math_new/types/matrix4x4.h"
#include "pr/math_new/types/matrix3x4.h"
#include "pr/math_new/primitives/bsphere.h"

namespace pr::math
{
	template <ScalarType S>
	struct OBox
	{
		using Mat4 = Mat4x4<S>;
		using Mat3 = Mat3x4<S>;
		using Vec4 = Vec4<S>;

		enum { Point = 1 << 0, Edge = 1 << 1, Face = 1 << 2, Bits = 1 << 3, Mask = Bits - 1 };

		Mat4 m_box_to_world;
		Vec4 m_radius;

		OBox() = default;
		OBox(Vec4 centre, Vec4 radii, Mat3 const& ori)
			:m_box_to_world(ori, centre)
			,m_radius(radii)
		{}
		OBox(Mat4 const& box_to_world, Vec4 radii)
			:m_box_to_world(box_to_world)
			,m_radius(radii)
		{}

		// Width of the box
		S SizeX() const
		{
			return S(2) * m_radius.x;
		}

		// Height of the box
		S SizeY() const
		{
			return S(2) * m_radius.y;
		}

		// Length of the box
		S SizeZ() const
		{
			return S(2) * m_radius.z;
		}

		// The centre position of the box
		Vec4 Centre() const
		{
			return m_box_to_world.pos;
		}

		// Diagonal size of the box
		S DiametreSq() const
		{
			return S(4) * LengthSq(m_radius);
		}
		S Diametre() const
		{
			return Sqrt(DiametreSq());
		}

		// Constants
		static constexpr OBox Unit()
		{
			return OBox{ Identity<Mat4>(), Vec4{S(0.5), S(0.5), S(0.5), S(1)} };
		}
		static constexpr OBox Reset()
		{
			return { Identity<Mat4>(), Zero<Vec4>() };
		}

		#pragma region Operators
		friend bool operator == (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
		friend bool operator != (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
		friend bool operator <  (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
		friend bool operator >  (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
		friend bool operator <= (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
		friend bool operator >= (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
		friend OBox& pr_vectorcall operator += (OBox& lhs, Vec4 offset)
		{
			lhs.m_box_to_world.pos += offset;
			return lhs;
		}
		friend OBox& pr_vectorcall operator -= (OBox& lhs, Vec4 offset)
		{
			lhs.m_box_to_world.pos -= offset;
			return lhs;
		}
		friend OBox pr_vectorcall operator + (OBox const& lhs, Vec4 offset)
		{
			auto ob = lhs;
			return ob += offset;
		}
		friend OBox pr_vectorcall operator - (OBox const& lhs, Vec4 offset)
		{
			auto ob = lhs;
			return ob -= offset;
		}
		friend OBox pr_vectorcall operator * (Mat4 const& m, OBox const& ob)
		{
			OBox obox;
			obox.m_box_to_world = m * ob.m_box_to_world;
			obox.m_radius = ob.m_radius;
			return obox;
		}
		#pragma endregion
	};
	static_assert(std::is_trivially_copyable_v<OBox<float>>, "Should be a pod type");
	static_assert(std::alignment_of_v<OBox<float>> == 16, "Should be 16 byte aligned");

	#pragma region Functions

	// Return the volume of the box
	template <ScalarType S> constexpr S Volume(OBox<S> const& ob)
	{
		return ob.SizeX() * ob.SizeY() * ob.SizeZ();
	}

	// Return the bounding sphere for the box
	template <ScalarType S> constexpr BSphere<S> GetBSphere(OBox<S> const& ob)
	{
		return BSphere(ob.m_box_to_world.pos, Length(ob.m_radius));
	}

	#pragma endregion
}
