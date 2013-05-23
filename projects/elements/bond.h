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

		Bond() :m_perm() ,m_strength() {}
		Bond(int perm, double strength) :m_perm(perm) ,m_strength(strength) {}
	};

	// Order an array of Bond objects by bond strength
	template <size_t Len> inline void OrderByStrength(Bond (&bonds)[Len])
	{
		std::sort(bonds, bonds + Len, [](Bond const& lhs, Bond const& rhs){ return lhs.m_strength > rhs.m_strength; });
	}
}
