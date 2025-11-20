//***********************************************
// SI units
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#pragma once

namespace pr
{
	using scaler_t = double;                       // A dimensionless unit used to scale
	using fraction_t = double;                     // A value between 0 and 1
	using seconds_t = double;
	using kilograms_t = double;
	using kilograms_p_sec_t = double;
	using kilograms_p_metre2_t = double;
	using kilogram_metres_p_sec_t = double;
	using metres_t = double;
	using metres_p_sec_t = double;
	using metres_p_sec2_t = double;
	using metres3_t = double;
	using metres3_p_day_t = double;
	using days_t = double;
	using joules_t = double;
	using kelvin_t = double;
	using celsius_t = double;
	using joules_p_metres3_t = double;
	using joules_p_kilogram_t = double;
	using metres3_p_kilogram_p_sec2_t = double;

	// Conversions
	inline kelvin_t CelsiusToKelvin(celsius_t c)
	{
		return c + 273.15;
	}
	inline celsius_t KelvinToCelsius(kelvin_t c)
	{
		return c - 273.15;
	}
}
