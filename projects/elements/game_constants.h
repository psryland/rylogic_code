#pragma once

#include "elements/forward.h"

namespace ele
{
	// Contains the randomly generated constants for an instance of the game
	struct GameConstants
	{
		// The maximum time a game should last
		pr::seconds_t m_max_game_duration;

		// The starting time till the star goes nova
		pr::seconds_t m_start_time_till_nova;

		// The error margin for the time till the star goes nova
		pr::seconds_t m_start_time_till_nova_error_margin;

		// The countdown till nova is a large value but we want each game to last a
		// fixed time. This scales game seconds to make the nova time = max_game_time
		double m_time_scaler;

		// The universal speed of light
		pr::metres_p_sec_t m_speed_of_light;

		// The constant that scales the gravitational force
		pr::metres³_p_kilogram_p_sec²_t m_gravitational_constant;

		// The constant that scales the electro static force
		double m_coulomb_constant;

		// The mass of a proton
		pr::kilograms_t m_proton_mass;

		// The number of elements in the universe
		size_t m_element_count;

		// A table of the element names, of length m_element_count
		ElementName const* m_element_name;

		// The valence levels of the elements, these are the number of
		// electrons for a stable element.
		// E.g 2, 10, 18, 36, etc corresponding to orbit electron counts 2, 8, 8, 18, etc
		size_t m_valence_levels[10];

		// The "radii" of the orbitals, i.e. the maximum distance of the radial probability
		// distribution functions for each orbital level.
		double m_orbital_radius[10];

		// The amount of positive charge the valence electrons "let through".
		// i.e. (1 - Zeff) = the amount of positive charge shielded.
		// e.g. Carbon = 6 protons, 6 electrons, 4 of which are valence electrons
		//  => charge = 6 - 2 - 4*Zeff
		double m_zeffective_scaler;

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

		// A limit on the available resources, to be divided amoung the research efforts
		size_t m_total_man_power;

		// How quickly the ship can be built
		pr::metres³_p_day_t m_ship_construction_rate;

		// The total man days needed to discover the star mass
		man_days_t m_star_mass_discovery_effort;

		// The total man days needed to discover the star distance
		man_days_t m_star_distance_discovery_effort;

		GameConstants(int seed, bool real_chemistry);
	};
}
