#pragma once

#include "elements/forward.h"

namespace ele
{
	// Contains the randomly generated constants for an instance of the game
	struct GameConstants
	{
		// The number of elements in the universe
		size_t m_element_count;

		// A table of the element names, of length m_element_count
		ElementName const* m_element_name;

		// The valency levels of the elements
		size_t m_valency_levels[10];


		// The universal speed of light
		pr::metres_p_sec_t m_speed_of_light;

		// The universal gravitational constant
		pr::metres³_p_kilogram_p_sec²_t m_gravitational_constant;


		// The mass of a proton
		pr::kilograms_t m_proton_mass;

		// The mass of the star that the space craft needs to escape
		pr::kilograms_t m_star_mass;

		// The distance from the star
		pr::metres_t m_star_distance;

		// The acceleration due to the star's gravity at the given distance
		pr::metres_p_sec²_t m_star_gravitational_acceleration;

		// The speed required to escape the star
		pr::metres_p_sec_t m_escape_velocity;

		// The average weight of a passenger
		pr::kilograms_t m_average_passenger_weight;

		// The space required by each passenger
		pr::metres³_t m_average_passenger_personal_space;

		// The space required by each passenger for life support systems etc
		pr::metres³_t m_average_passenger_required_systems_volume;

		// The ships volume is this much bigger than it's contents
		double m_ship_volume_scaler;

		// How quickly the ship can be built
		pr::metres³_p_day_t m_ship_construction_rate;

		GameConstants(int seed);
	};
}
