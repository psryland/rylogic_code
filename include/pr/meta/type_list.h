//******************************************
// Type list
//  Copyright (C) Rylogic Ltd 2010
//******************************************

#pragma once
#ifndef PR_META_TYPELIST_H
#define PR_META_TYPELIST_H

namespace pr
{
	namespace meta
	{
		struct tl_empty {};
		
		template <typename T0=tl_empty, typename T1=tl_empty, typename T2=tl_empty, typename T3=tl_empty, typename T4=tl_empty>
		struct typelist;
		
		template <typename T0, typename T1, typename T2, typename T3, typename T4>
		struct typelist
		{
			typedef T0 head;
			typedef typelist<T1,T2,T3,T4> tail;
			enum { length = tail::length + 1 };
		};
		
		// Specialisation to end the recursive typelist definition
		template <> struct typelist<tl_empty, tl_empty, tl_empty, tl_empty, tl_empty> { enum { length = 0 }; };

		// The number of parameters in a typelist
		template <typename TL>
		struct tl_length { enum { value = TL::length }; };

		// Access the 'Index'th element of the type list
		template<typename TL, int Index, int Rec, bool Stop = Index == Rec, bool OutOfRange = tl_length<TL>::value == 0>
		struct tl_get { typedef typename tl_get<typename TL::tail, Index, Rec+1>::type type; };
		
		// OutOfRange specialisation, if 'OutOfRange' is true, 'type' will be undefined, giving a compile error
		template <typename TL, int Index, int Rec, bool Stop>
		struct tl_get<TL, Index, Rec, Stop, true> { };

		// Element found specialisation
		template <typename TL, int Index, int Rec, bool OutOfRange>
		struct tl_get<TL, Index, Rec, true, OutOfRange> { typedef typename TL::head type; };
	}
}

#endif
