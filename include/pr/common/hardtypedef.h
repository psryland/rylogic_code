//**********************************************************************************
// Hard Typedef
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************************
// Used mainly to give type safety between built-in types

#pragma once

#include <type_traits>

template <int S> class C;

namespace pr
{
	namespace impl
	{
		template<class T, class = decltype(std::declval<T>() < std::declval<T>() )> 
		std::true_type  supports_less_than_test(const T&);
		std::false_type supports_less_than_test(...);

template<class T> using supports_less_than = decltype(supports_less_than_test(std::declval<T>()));


		// Checks if type 'T' has a member
		// 'Check' is a SFINAE type that tests for the member in question
		template <typename T, typename Check> struct has_member
		{
		private: template <typename C> static char f(typename Check::template get<C>*);
		private: template <typename C> static long f(...);
		public:  static bool const value = sizeof(f<T>(0)) == sizeof(char);
		};

		struct check_has_bar      { template <typename T, int  (T::*)(float,float) const = &T::bar       > struct get {}; };
		struct check_has_pre_inc  { template <typename T, T&   (T::*)()                  = &T::operator++> struct get {}; };
		struct check_has_post_inc { template <typename T, T    (T::*)(int) const         = &T::operator++> struct get {}; };
	}

	// A wrapper class for creating hard typedefs of 'UnderlyingType'
	template <typename UnderlyingType> struct hardtypedef
	{
		using UT     = UnderlyingType;
		using mytype = hardtypedef<UnderlyingType>;
		static bool const is_builtin              = std::is_fundamental<UT>::value;
		static bool const has_default_constructor = is_builtin || std::has_default_constructor<UT>::value;
		static bool const has_copy_constructor    = is_builtin || std::has_copy_constructor<UT>::value;
		static bool const has_move_constructor    = is_builtin || std::has_move_constructor<UT>::value;
		static bool const has_pre_inc             = std::is_integral<UT>::value || impl::has_member<UT, impl::check_has_pre_inc>::value;
		static bool const has_post_inc            = std::is_integral<UT>::value || impl::has_member<UT, impl::check_has_post_inc>::value;

		UnderlyingType value;

		// default constructor
		template <class X = std::enable_if<has_default_constructor>>
		hardtypedef() :value() {}

		// construct from underlying type
		hardtypedef(UnderlyingType x) :value(x) {}

		// copy constructor
		template <class X = std::enable_if<has_copy_constructor>>
		hardtypedef(hardtypedef const& rhs) :value(rhs.value) {}

		// move constructor
		template <class X = std::enable_if<has_move_constructor>>
		hardtypedef(hardtypedef&& rhs) :value(std::move(rhs.value)) {}

		// explicit conversion to underlying type
		explicit operator UnderlyingType const&() const { return value; }
		explicit operator UnderlyingType&()             { return value; }

		// Operator ++
		mytype& operator ++()
		{
			++value;
			return *this;
		}
		mytype operator ++(int)
		{
			auto x = *this;
			++value;
			return x;
		}

		// Operator ==
		bool operator == (mytype const& rhs) const
		{
			return value == rhs.value;
		}
		bool operator != (mytype const& rhs) const
		{
			return value != rhs.value;
		}
	};
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_hardtypedef)
		{
			struct Point
			{
				int m_x, m_y;
				Point(int x, int y) :m_x(x) ,m_y(y) {}
			};

			using count_t = hardtypedef<int>;
			using pt0_t   = hardtypedef<Point>;
			using pt1_t   = hardtypedef<Point>;

			count_t c = 0;
			++c;
			c++;
			PR_CHECK(c == count_t{2}, true);

			//pt0_t pt0;
			//++pt0;
			//PR_CHECK()
		}
	}
}
#endif