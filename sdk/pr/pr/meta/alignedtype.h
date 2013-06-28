//******************************************
// Aligned Type
//  Copyright © Rylogic Ltd 2010
//******************************************

#ifndef PR_META_ALIGNED_TYPE_H
#define PR_META_ALIGNED_TYPE_H
#pragma once

namespace pr
{
	namespace meta
	{
		// Note: if you get an error saying: "'type' : is not a member of 'pr::meta::aligned_type<Alignment>'"
		// Then it's probably because 'Alignment' is not one of the following specialisations
		template <std::size_t Alignment> struct aligned_type {};

		template <> struct aligned_type<1>  { struct type { char   a; }; };
		template <> struct aligned_type<2>  { struct type { short  a; }; };
		template <> struct aligned_type<4>  { struct type { int    a; }; };
		template <> struct aligned_type<8>  { struct type { double a; }; };
		template <> struct aligned_type<16> { __declspec(align(16)) struct type { char a[16]; }; };
	}
}

#endif

