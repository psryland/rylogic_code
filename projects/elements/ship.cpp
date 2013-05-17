#include "elements/stdafx.h"
#include "elements/ship.h"

namespace ele
{
	Ship::Ship()
		:m_passenger_count(0)
		,m_fuel()
		,m_fuel_mass(0)
		,m_structure()
		,m_systems()
		,m_shield()
		,m_shield_mass(0)
		,m_construction_time(0)
		,m_total_volume(0)
		,m_total_mass(0)
	{}

	// Calculate the derived fields from the given materials
	void Ship::CalculateDerivedFields(GameConstants constants)
	{
		pr::kilograms_t passenger_mass = m_passenger_count * constants.m_average_passenger_weight;
		pr::kilograms_t fuel_mass      = m_fuel_mass;
		pr::kilograms_t systems_mass   = m_passenger_count * constants.m_average_passenger_required_systems_volume * m_systems.m_density;
		pr::kilograms_t shield_mass    = m_shield_mass;
		pr::metres³_t passenger_volume = m_passenger_count * constants.m_average_passenger_personal_space;
		pr::metres³_t fuel_volume      = m_fuel_mass / m_fuel.m_density;
		pr::metres³_t systems_volume   = m_passenger_count * constants.m_average_passenger_required_systems_volume;
		pr::metres³_t shield_volume    = m_shield_mass / m_shield.m_density;

		// Find the volume of the ship
		pr::metres³_t contents_volume = passenger_volume + fuel_volume + systems_volume + shield_volume;
		m_total_volume = contents_volume * constants.m_ship_volume_scaler;

		// Determine the mass of structural material needed and the total ship mass
		m_structural_mass = (m_total_volume - contents_volume) * m_structure.m_density;
		m_total_mass = passenger_mass + fuel_mass + systems_mass + shield_mass + m_structural_mass;

		// Construction time is a function of how big the ship is
		m_construction_time = m_total_volume / constants.m_ship_construction_rate;

		// Shields

		// The radius of the ship if it was a spherical ball
		// volume = (2 * tau * r³)/3
		// r = ³root((3*volume)/(2*tau))
		pr::metres_t radius = cubert(1.5 * m_total_volume / pr::maths::tau);

		// The unboosted field strength at this radius
		pr::field_strength_t strength = 1.0 / (m_shield.m_field_falloff * sqr(radius) + 1.0);

		// How much the field needs boosting to reach the required strength
		double required_field_gain = std::max(1.0, constants.m_shield_protective_field_strength / strength);

		// The energy required by the shields to have the required strength at the ships surface
		pr::joules_t shield_energy = required_field_gain / constants.m_field_boost_scaler;

		// The energy required by the ships computer systems
		pr::joules_t system_energy = systems_volume * constants.m_systems_energy_requirement;

		// Find the required exhaust speed of the ships rocket engine
		// delta_v = exhaust_speed * ln(M0/M1), M0 = initial mass, M1 = final mass
		// exhaust_speed = delta_v / ln(M0/M1)
		// say delta_v = escape_velocity
		pr::metres_p_sec_t delta_v = constants.m_escape_velocity;
		pr::metres_p_sec_t exhaust_speed = delta_v / ln(m_total_mass / (m_total_mass - fuel_mass));


		
		//// Acceleration is provided by thrust
		//// F = m' * v, net thrust = mass flow rate * effective exhaust velocity
		//required_thrust = m_ship.f


		//kilograms_p_sec_t mass_flow_rate;

		//// Theoretical change in velocity obtainable by burning all fuel
		//metres_p_sec_t delta_v = exhaust_speed * ln(m_total_mass / (m_total_mass - fuel_mass));



		//seconds_t specific_impulse = 1;
		//
		//metres_p_sec_t exhaust_velocity = specific_impulse * constants.m_star_gravitational_acceleration;


		//m_energy_requirement = 0.5 * m_total_mass * sqr(constants.m_escape_velocity);
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
