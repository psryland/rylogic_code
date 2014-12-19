//***********************************************
// SI units
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#pragma once
#include <ratio>

namespace pr
{
	typedef double scaler_t;                       // A dimensionless unit used to scale
	typedef double fraction_t;                     // A value between 0 and 1
	typedef double seconds_t;
	typedef double kilograms_t;
	typedef double kilograms_p_sec_t;
	typedef double kilograms_p_metre³_t;
	typedef double kilogram_metres_p_sec_t;
	//typedef double metres_t;
	typedef double metres_p_sec_t;
	typedef double metres_p_sec²_t;
	typedef double metres³_t;
	typedef double metres³_p_day_t;
	typedef double days_t;
	typedef double joules_t;
	typedef double kelvin_t;
	typedef double celsius_t;
	typedef double joules_p_metres³_t;
	typedef double joules_p_kilogram_t;
	typedef double metres³_p_kilogram_p_sec²_t;

	// Conversions
	inline kelvin_t CelsiusToKelvin(celsius_t c) { return c + 273.15; }
	inline celsius_t KelvinToCelsius(kelvin_t c) { return c - 273.15; }

	//namespace units
	//{
	//	// Unit types come in families, e.g.
	//	//  metres, millimetres, kilometres
	//	//  seconds, milliseconds, etc
	//	// Units of the same family can be added, subtracted, cast between
	//	// Units of different families can be multipled, and result in a different unit

	//	// Families
	//	enum class EFamily
	//	{
	//		Id       = 1 << 0,
	//		Time     = 1 << 1,
	//		Distance = 1 << 2,
	//	};
	//	inline EFamily operator | (EFamily lhs, EFamily rhs) { return EFamily(int(lhs) | int(rhs)); }
	//	inline EFamily operator & (EFamily lhs, EFamily rhs) { return EFamily(int(lhs) & int(rhs)); }

	//	// A unit is a ratio of quantities, i.e. m^2/s, kgm^3/s^2
	//	template <EFamily Num, int C0, EFamily Den, int C1> struct Unit
	//	{
	//		static_assert(impl::is_quantity<Num>::value, "");
	//		static_assert(impl::is_quantity<Den>::value, "");
	//	};

	//	namespace impl
	//	{
	//		template <class T>                  struct is_ratio                    { enum { value = false }; };
	//		template <intmax_t R1, intmax_t R2> struct is_ratio<std::ratio<R1,R2>> { enum { value = true  }; };
	//	}

	//	// Define a unit type
	//	// 'BaseType' is the underlying type to hold the value
	//	// 'ScaleToSI' is a multiplying value that converts the type to the SI standard
	//	template <typename BaseType, EFamily Fam, class Ratio>
	//	struct Unit
	//	{
	//		static_assert(impl::is_ratio<Ratio>::value, "");
	//		BaseType value;

	//		template <typename R> operator Unit<BaseType,Fam,R>() const
	//		{
	//			static_assert(impl::is_ratio<R>::value, "");
	//			return Unit<BaseType,Fam,R>{(value*Ratio::num*R::den) / (Ratio::den*R:num)};
	//		}
	//	};

	//	using seconds_t    = Unit<double , EFamily::Time     , std::ratio<1,1>   >;
	//	using metres_t     = Unit<double , EFamily::Distance , std::ratio<1,1>   >;
	//	using kilometres_t = Unit<double , EFamily::Distance , std::ratio<1000,1>>;

	//	// Unary negation
	//	template <typename BT, EFamily F0, typename R0> inline Unit<BT,F0,R0> operator -(Unit<BT,F0,R0> x)
	//	{
	//		return Unit<BT,F0,R0>{-x.value};
	//	}

	//	// Equality
	//	template <typename BT, EFamily F0, EFamily F1, typename R0, typename R1> inline bool operator == (Unit<BT,F0,R0> lhs, Unit<BT,F1,R1> rhs)
	//	{
	//		static_assert(impl::is_ratio<R0>::value && impl::is_ratio<R1>::value, "");
	//		return lhs.value * R0::num * R1::den == rhs.value * R1::num * R0::den;
	//	}
	//	template <typename BT, EFamily F0, EFamily F1, typename R0, typename R1> inline bool operator != (Unit<BT,F0,R0> lhs, Unit<BT,F1,R1> rhs)
	//	{
	//		return !(lhs == rhs);
	//	}

	//	// Addition
	//	template <typename BT, EFamily F, typename R0, typename R1, class RR = std::ratio<R0::num*R1::num, R0::den*R1::den>>
	//	inline Unit<BT,F,RR> operator + (Unit<BT,F,R0> lhs, Unit<BT,F,R1> rhs)
	//	{
	//		static_assert(impl::is_ratio<R0>::value && impl::is_ratio<R1>::value, "");
	//		return Unit<BT,F,RR>{(lhs.value*R0::num*R1::den + rhs.value*R1::num/R0::den)/(R0::den*R1::den)};
	//	}
	//	template <typename BT, EFamily F, typename R0, typename R1, class RR = std::ratio<R0::num*R1::num, R0::den*R1::den>>
	//	inline Unit<BT,F,RR> operator - (Unit<BT,F,R0> lhs, Unit<BT,F,R1> rhs)
	//	{
	//		return lhs + (-rhs);
	//	}

	//	// Multiplication
	//	template <typename BT, EFamily F0, EFamily F1, typename R0, typename R1, class RR = std::ratio<R0::num*R1::num, R0::den*R1::den>>
	//	inline Unit<BT,F0,RR> operator - (Unit<BT,F,R0> lhs, Unit<BT,F,R1> rhs)
	//	{
	//		return lhs + (-rhs);
	//	}
	//}
}

#if PR_UNITTESTS
#include <chrono>
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_si_unit)
		{

			//auto d1 = metres_t{43.2};
			//auto d2 = kilometres_t{0.32};
			//auto d3 = d1 + d2;
			//auto d4 = kilometres_t(d3);

			//PR_CHECK(d3 == metres_t{363.2}, true);
			//PR_CHECK(d3 == kilometres_t{0.3632}, true);
		}
	}
}
#endif