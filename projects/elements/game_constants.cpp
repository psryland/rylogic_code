#include "elements/stdafx.h"
#include "elements/game_constants.h"

namespace ele
{
	// The element names, using real names for now for testing
	ElementName const g_element_names[] =
	{
		{""           , ""  , ""        },
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
	GameConstants::GameConstants(int seed)
	{
		pr::Rnd rnd(seed);

		// The universal speed of light
		m_speed_of_light = 2.99792458e8;

		// The universal gravitational constant
		m_gravitational_constant = 6.6738e-11;

		// The mass of a proton
		m_proton_mass = 1.67262178e-27;


		// The collection of element names
		m_element_count = PR_COUNTOF(g_element_names);
		m_element_name  = &g_element_names[0];

		// The valency levels of the elements
		static_assert(PR_COUNTOF(m_valency_levels) > 2, "");
		m_valency_levels[0] = 0;
		m_valency_levels[1] = size_t(rnd.int1(1,4));
		for (size_t i = 2; i != PR_COUNTOF(m_valency_levels); ++i)
		{
			size_t v = 1 + m_valency_levels[i-1];
			m_valency_levels[i] = size_t(rnd.dbl1(1.3*v, 2.9*v));
		}

		// Pick a star mass approximately the same as the sun
		const pr::kilograms_t suns_mass = 2.0e30;
		m_star_mass = rnd.dbl2(suns_mass, suns_mass * 0.25);

		// Pick a distance from the star, somewhere between mercury and mars
		const pr::metres_t sun_to_mercury = 5.79e10;
		const pr::metres_t sun_to_mars = 2.279e11;
		m_star_distance = rnd.dbl1(sun_to_mercury, sun_to_mars);

		// The acceleration due to the star's gravity at the given distance
		m_star_gravitational_acceleration = m_gravitational_constant * m_star_mass / pr::Sqr(m_star_distance);

		// Calculate the required escape velocity (speed)
		// Escape Velocity = Sqrt(2 * G * M / r), G = 6.67x10^-11 m³kg^-1s^-2, M = star mass, r = distance from star
		m_escape_velocity = ::sqrt(2.0 * m_gravitational_constant * m_star_mass / m_star_distance);

		// Set up per passenger constants
		m_average_passenger_weight                  = rnd.dbl2(80.0, 10.0);
		m_average_passenger_personal_space          = rnd.dbl2(2.0, 0.5);
		m_average_passenger_required_systems_volume = rnd.dbl2(5.0, 1.0);

		// The ship is roughly 10% bigger than the volume of it's contents
		m_ship_volume_scaler = rnd.dbl2(1.11, 0.1);
		m_ship_construction_rate = rnd.dbl2(10.0, 2.0);
	}
}
