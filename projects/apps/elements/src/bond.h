#pragma once

#include "elements/forward.h"

namespace ele
{
	// The stuff that all materials are made of
	struct Bond
	{
		// The permutation identifier assocated with the bond (one of EPerm2/4)
		int m_perm;

		// The bond strength
		double m_strength;

		// The number of bonds of this permutation
		size_t m_count;

		Bond(int perm = 0, double strength = 0, size_t count = 0)
			:m_perm(perm)
			,m_strength(strength)
			,m_count(count)
		{}
	};

	// Order an array of Bond objects by bond strength
	template <size_t Len> inline void OrderByStrength(Bond (&bonds)[Len])
	{
		std::sort(bonds, bonds + Len, [](Bond const& lhs, Bond const& rhs){ return lhs.m_strength > rhs.m_strength; });
	}

	// Order an array of Bond objects by bond counts
	template <size_t Len> inline void OrderByCount(Bond (&bonds)[Len])
	{
		std::sort(bonds, bonds + Len, [](Bond const& lhs, Bond const& rhs){ return lhs.m_count > rhs.m_count; });
	}
}
