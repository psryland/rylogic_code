#include "elements/stdafx.h"
#include "elements/ship.h"

namespace ele
{
	Ship::Ship()
		:m_passenger_count(0)
		,m_fuel()
		,m_fuel_mass(0)
		,m_fuel_burn_rate(1)
		,m_structure()
		,m_systems()
		,m_shield()
		,m_shield_mass(0)
		,m_construction_time(0)
		,m_total_volume(0)
		,m_total_mass(0)
		,m_structural_mass(0)
		,m_max_burn_time(0)
	{}

	// Returns the total mass of the ship at time 't'
	pr::kilograms_t Ship::TotalMass(pr::seconds_t t) const
	{
		auto burnt_fuel = t * m_fuel_burn_rate;
		return (burnt_fuel >= m_fuel_mass)
			? m_total_mass - m_fuel_mass
			: m_total_mass - burnt_fuel;
	}

	// Calculate the derived fields from the given materials
	void Ship::CalculateDerivedFields(GameConstants constants)
	{
		// Determine the size and mass of the ship
		pr::kilograms_t passenger_mass = m_passenger_count * constants.m_average_passenger_weight;
		pr::kilograms_t fuel_mass      = m_fuel_mass;
		pr::kilograms_t systems_mass   = m_passenger_count * constants.m_average_passenger_required_systems_volume * m_systems.Density();
		pr::kilograms_t shield_mass    = m_shield_mass;
		pr::metres³_t passenger_volume = m_passenger_count * constants.m_average_passenger_personal_space;
		pr::metres³_t fuel_volume      = m_fuel_mass / m_fuel.Density();
		pr::metres³_t systems_volume   = m_passenger_count * constants.m_average_passenger_required_systems_volume;
		pr::metres³_t shield_volume    = m_shield_mass / m_shield.Density();

		// Find the volume of the ship
		pr::metres³_t contents_volume = passenger_volume + fuel_volume + systems_volume + shield_volume;
		m_total_volume = contents_volume * constants.m_ship_volume_scaler;

		// The radius of the ship if it was a spherical ball
		// volume = (2 * tau * r³)/3
		// r = ³root((3*volume)/(2*tau))
		pr::metres_t radius = cubert(1.5 * m_total_volume / pr::maths::tau);
		(void)radius;

		// Determine the mass of structural material needed and the total ship mass
		m_structural_mass = (m_total_volume - contents_volume) * m_structure.Density();
		m_total_mass = passenger_mass + fuel_mass + systems_mass + shield_mass + m_structural_mass;

		// Construction time is a function of how big the ship is
		m_construction_time = m_total_volume / constants.m_ship_construction_rate;

	}

	// Run a simulation of the ship to determine it's viability
	void Ship::Simulate()
	{

	}

	//// How fast 
	//metres_p_sec_t ExhaustSpeed(GameConstants constants) const
	//{
	//	kilograms_t mass_converted_to_energy = m_reaction_ratio;
	//	kilograms_t mass_ejected = 1.0 - m_reaction_ratio;

	//	// Calculate the energy produced by reacting 1 unit of this material
	//	joules_t energy = mass_converted_to_energy * sqr(constants.m_speed_of_light); // E = mc²

	//	// This energy corresponds to a velocity for mass_ejected
	//	// E = mc²(gamma - 1)
	//	// v = c * sqrt(1 - 1/(E/mc² + 1)²)
	//	auto E = energy;
	//	auto m = mass_ejected;
	//	auto c = constants.m_speed_of_light;
	//	auto c² = sqr(c);
	//	metres_p_sec_t exhaust_speed = c * sqrt(1.0 - 1.0/sqr(E/(m*c²) + 1.0));

	//	// particles travel this fast
	//}
}
