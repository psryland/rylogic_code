//***********************************************
// SI units
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#pragma once

namespace pr
{
	typedef double scaler_t;                       // A dimensionless unit used to scale
	typedef double fraction_t;                     // A value between 0 and 1
	typedef double seconds_t;
	typedef double kilograms_t;
	typedef double kilograms_p_sec_t;
	typedef double kilograms_p_metre³_t;
	typedef double kilogram_metres_p_sec_t;
	typedef double metres_t;
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
}
