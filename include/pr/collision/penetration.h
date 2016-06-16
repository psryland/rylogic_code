//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once
#include "pr/maths/maths.h"

namespace pr
{
	// Base class for penetration function objects
	struct Penetration
	{
		// Notes:
		//  Calculate depth as: 'just-contacting-distance - actual-distance' = positive if overlapping
		//  Initialize 'm_depth_sq' with "max penetration" since we typically want the minimum penetration.
		//  We expect to test at least one separating axis which ensures 'm_depth_sq' is always set to a valid value.

		v4    m_axis;        // The axis of minimum penetration (not normalised)
		float m_axis_len_sq; // The square of the separating axis length
		float m_depth_sq;    // The signed square of the depth of penetration

		Penetration()
			:m_axis(v4Zero)
			,m_axis_len_sq(0.0f)
			,m_depth_sq(maths::float_inf)
		{}

		// Boolean test of penetration
		bool Contact() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return m_depth_sq > 0;
		}

		// Return the depth of penetration
		float Depth() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return SignedSqrt(m_depth_sq);
		}

		// The direction of minimum penetration
		v4 SeparatingAxis() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return m_axis / Sqrt(m_axis_len_sq);
		}

		// Implemented by derived types.
		// 'depth' is positive if there is penetration.
		// 'sep_axis' is a function that returns the separating axis (for lazy evaluation)
		// The returned separating axis does not have to be normalised but 'depth' is assumed to be in multiples of the separating axis length.
		// Return false to 'quick-out' of collision detection.
		template <typename SepAxis> bool operator()(float depth, SepAxis sep_axis) = delete;
	};

	// For boolean 'is penetrating' tests.
	struct TestPenetration :Penetration
	{
		// 'depth' should be positive if there is penetration.
		template <typename SepAxis> bool operator()(float depth, SepAxis)
		{
			m_depth_sq = Sign(depth);

			// Stop as soon as non-contact is detected
			return m_depth_sq >= 0;
		}
	};

	// Find the separating axis with the minimum penetration (i.e. the most negative depth)
	// This also records the nearest non-penetration (indicated by depth() < 0)
	struct MinPenetration :Penetration
	{
		// 'depth' is positive if there is penetration.
		// 'sep_axis' does not have to be normalised but 'depth' is assumed to be in multiples of the 'sep_axis' length.
		template <typename SepAxis> bool operator()(float depth, SepAxis sep_axis)
		{
			// Defer the sqrt by comparing squared depths.
			// Need to preserve the sign however.
			auto axis = sep_axis();
			auto len_sq = Length3Sq(axis);
			auto d_sq = SignedSqr(depth) / len_sq;
			if (d_sq < m_depth_sq)
			{
				m_axis = axis;
				m_axis_len_sq = len_sq;
				m_depth_sq = d_sq;
			}

			// Never quick out, test all separating axes to get the closest point
			return true;
		}
	};

	// Records the deepest penetration, ignoring non-contact.
	struct ContactPenetration :MinPenetration
	{
		// 'depth' is positive if there is penetration.
		// 'sep_axis' does not have to be normalised but 'depth' is assumed to be in multiples of the 'sep_axis' length.
		template <typename SepAxis> bool operator()(float depth, SepAxis sep_axis)
		{
			if (depth >= 0)
				MinPenetration::operator()(depth, sep_axis);
			else
				m_depth_sq = -1.0f;

			// Stop as soon as non-contact is detected
			return m_depth_sq >= 0;
		}
	};
}