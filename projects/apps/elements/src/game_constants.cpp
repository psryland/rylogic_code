#include "src/forward.h"
#include "src/game_constants.h"

namespace ele
{
	// The element names, using real names for now for testing
	ElementName const g_element_names[] =
	{
		{"hydrogen"   , "H" , "hydr"    },
		{"helium"     , "He", "hel"     },
		{"lithium"    , "Li", "lithim"  },
		{"beryllium"  , "Be", "beryll"  },
		{"boron"      , "B" , "bor"     },
		{"carbon"     , "C" , "carbon"  },
		{"nitrogen"   , "N" , "nitr"    },
		{"oxygen"     , "O" , "ox"      },
		{"fluorine"   , "F" , "fluor"   },
		{"neon"       , "Ne", "neon"    },
		{"sodium"     , "Na", "sodim"   },
		{"magnesium"  , "Mg", "magnesim"},
		{"aluminium"  , "Al", "alumin"  },
		{"silicon"    , "Si", "silic"   },
		{"phosphorus" , "P" , "phosph"  },
		{"sulfur"     , "S" , "sulf"    },
		{"chlorine"   , "Cl", "chlor"   },
		{"argon"      , "Ar", "argon"   },
		{"potassium"  , "K" , "potassim"},
		{"calcium"    , "Ca", "calc"    },
	};

	// Contains the randomly generated constants for an instance of the game
	GameConstants::GameConstants(int seed, bool real_chemistry)
	{
		std::default_random_engine rng(seed);

		m_max_game_duration                 = 30 * 60 * 60; // 30 minutes
		m_start_time_till_nova              = 365 * 24 * 60 * 60;
		m_start_time_till_nova_error_margin = 20 * 24 * 60 * 60;
		m_time_scaler                       = m_start_time_till_nova / m_max_game_duration;
		m_speed_of_light                    = 2.99792458e8;
		m_gravitational_constant            = 6.6738e-11;
		m_coulomb_constant                  = 1;
		m_proton_mass                       = 1.67262178e-27;
		m_zeffective_scaler                 = 0.3;

		m_element_count = PR_COUNTOF(g_element_names);
		m_element_name  = &g_element_names[0];

		static_assert(PR_COUNTOF(m_valence_levels) > 2, "");
		if (real_chemistry)
		{
			// The total numbers of electrons at each orbital level
			m_valence_levels[0] = 0;
			m_valence_levels[1] = 2;
			m_valence_levels[2] = 10;
			m_valence_levels[3] = 18;
			m_valence_levels[4] = 36;
			m_valence_levels[5] = 54;
			m_valence_levels[6] = 86;
			m_valence_levels[6] = 118;
		}
		else
		{
			std::uniform_int_distribution<size_t> rng_valence_levels(1, 4);

			// The total numbers of electrons at each orbital level
			m_valence_levels[0] = 0;
			m_valence_levels[1] = rng_valence_levels(rng);
			for (size_t i = 2; i != PR_COUNTOF(m_valence_levels); ++i)
			{
				size_t v = 1 + m_valence_levels[i-1];

				std::uniform_real_distribution<double> rdist(1.3*v, 2.9*v);
				m_valence_levels[i] = size_t(rdist(rng));
			}
		}
		for (size_t i = 0; i != PR_COUNTOF(m_orbital_radius); ++i)
		{
			m_orbital_radius[i] = (double)m_valence_levels[i]; // will do for now...
		}

		// Pick a star mass approximately the same as the sun
		const pr::kilograms_t suns_mass = 2.0e30;
		std::uniform_real_distribution<double> rng_star_mass(suns_mass, suns_mass * 0.25);
		m_star_mass = rng_star_mass(rng);

		// Pick a distance from the star, somewhere between mercury and mars
		const pr::metres_t sun_to_mercury = 5.79e10;
		const pr::metres_t sun_to_mars = 2.279e11;
		std::uniform_real_distribution<double> rng_star_distance(sun_to_mercury, sun_to_mars);
		m_star_distance = rng_star_distance(rng);

		// The acceleration due to the star's gravity at the given distance
		m_star_gravitational_acceleration = m_gravitational_constant * m_star_mass / pr::Sqr(m_star_distance);

		// Calculate the required escape velocity (speed)
		// Escape Velocity = Sqrt(2 * G * M / r), G = 6.67x10^-11 m^3kg^-1s^-2, M = star mass, r = distance from star
		m_escape_velocity = ::sqrt(2.0 * m_gravitational_constant * m_star_mass / m_star_distance);

		// Set up per passenger constants
		std::uniform_real_distribution<double> rng_average_passenger_weight(80.0, 10.0);
		m_average_passenger_weight = rng_average_passenger_weight(rng);
		std::uniform_real_distribution<double> rng_average_passenger_personal_space(2.0, 0.5);
		m_average_passenger_personal_space = rng_average_passenger_personal_space(rng);
		std::uniform_real_distribution<double> rng_average_passenger_required_systems_volume(5.0, 1.0);
		m_average_passenger_required_systems_volume = rng_average_passenger_required_systems_volume(rng);

		// The total number of people available to work
		std::uniform_int_distribution<int> rng_total_man_power(10000, 0);
		m_total_man_power = rng_total_man_power(rng);

		// The ship is roughly 10% bigger than the volume of it's contents
		std::uniform_real_distribution<double> rng_ship_volume_scaler(1.11, 0.1);
		m_ship_volume_scaler = rng_ship_volume_scaler(rng);
		std::uniform_real_distribution<double> rng_ship_construction_rate(10.0, 2.0);
		m_ship_construction_rate = rng_ship_construction_rate(rng);

		// The total man days needed to discover the star mass
		std::uniform_real_distribution<double> rng_star_mass_discovery_effort(1000, 0.0);
		m_star_mass_discovery_effort = rng_star_mass_discovery_effort(rng);

		// The rate at which the star distance can be discovered proportional to the main hours assigned
		std::uniform_real_distribution<double> rng_star_distance_discovery_effort(1000, 0.0);
		m_star_distance_discovery_effort = rng_star_distance_discovery_effort(rng);
	}
}
