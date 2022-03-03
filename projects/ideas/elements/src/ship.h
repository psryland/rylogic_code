#pragma once

#include "elements/forward.h"
#include "elements/material.h"
#include "elements/game_constants.h"

namespace ele
{
	// Used to simulate a space craft design
	struct Ship
	{
		struct Engine
		{
			// How fast particles leaving the engine are travelling
			// A function of the
			double m_exhaust_speed;
		};

		// The number of people on the ship
		size_t m_passenger_count;

		// The material to use for fuel and how much is onboard
		Material m_fuel;
		pr::kilograms_t m_fuel_mass;
		pr::kilograms_p_sec_t m_fuel_burn_rate;

		// The material that the ship is made out of 
		Material m_structure;

		// The material that the ships computer systems are made out of
		Material m_systems;

		// The material used to create the shield and how much is onboard
		Material m_shield;
		pr::kilograms_t m_shield_mass;

		// Derived values

		// The time needed to build the ship
		pr::days_t m_construction_time;

		// The volume of space required to house all the passengers, fuel, shields, and computer systems
		pr::metres³_t m_total_volume;

		// The initial mass of the ship including passengers, fuel, shields, and systems
		pr::kilograms_t m_total_mass;

		// The mass of just the ship, not including everything in it
		pr::kilograms_t m_structural_mass;

		pr::seconds_t m_max_burn_time;
		//// The energy required to get the ship to escape velocity
		//joules_t m_energy_requirement;

		Ship();

		// Returns the total mass of the ship at time 't'
		pr::kilograms_t TotalMass(pr::seconds_t t) const;

		// Calculate the derived fields from the given materials
		void CalculateDerivedFields(GameConstants constants);

		// Run a simulation of the ship to determine it's viability
		void Simulate();
	};
}
