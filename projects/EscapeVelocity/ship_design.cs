using pr.maths;

namespace EscapeVelocity
{
	public class ShipDesign
	{
		private readonly GameConstants Consts;
		private readonly WorldState World;

		/// <summary>The current ship spec being simulated</summary>
		public ShipSpec Spec { get; set; }

		/// <summary>The mass of people being transported</summary>
		public double PassengerMass { get; private set; }

		/// <summary>The total mass of fuel</summary>
		public double FuelMass { get; private set; }

		/// <summary>The space that the fuel occupies</summary>
		public double FuelVolume { get; private set; }

		/// <summary>The total mass of the fuel tanks</summary>
		public double FuelTankMass { get; private set; }

		/// <summary>How fast particles leaving the engine are travelling</summary>
		public double ExhaustSpeed { get; private set; }

		/// <summary>The time needed to build the ship (s)</summary>
		public double ConstructionTime { get; private set; }

		/// <summary>The volume of the space required to house all the passengers and fuel (m³)</summary>
		public double TotalVolume { get; private set; }

		/// <summary>The initial mass of the ship including passengers and fuel (kg)</summary>
		public double TotalInitialMass { get; private set; }

		/// <summary>The mass of just the ship, not including passengers and fuel (kg)</summary>
		public double HullMass { get; private set; }

		//pr::seconds_t m_max_burn_time;
		//// The energy required to get the ship to escape velocity
		//joules_t m_energy_requirement;

		public ShipDesign(GameConstants consts, WorldState world)
		{
			Consts = consts;
			World = world;
			UseSpec(new ShipSpec());
		}

		/// <summary>Update the derived fields from the given spec</summary>
		public void UseSpec(ShipSpec spec)
		{
			Spec = spec;

			// Determine the size and mass of the ship
			FuelMass = 0;
			FuelVolume = 0;
			FuelTankMass = 0;
			double tank_volume = 0;
			foreach (var f in spec.Fuel)
			{
				double v;
				FuelMass += f.Mass;
				FuelVolume += v = f.Volume(Consts, World);
				FuelTankMass += f.TankMass(v, World);
				tank_volume += f.TankAndFuelVolume(v, World);
			}

			PassengerMass = Spec.PassengerCount * Consts.AveragePassengerWeight;
			double passenger_volume = Spec.PassengerCount * Consts.AveragePassengerPersonalSpace;

			// Assume the ship is a spherical ball housing the passengers only
			double xs_area = Consts.CabinPressure / Spec.HullCompound.Strength;
;			double ship_inner = Maths.SphereRadius(passenger_volume);
			double ship_outer = Maths.Sqrt(2.0 * xs_area / Maths.Tau + Maths.Sqr(ship_inner));
			var hull_volume = (2.0/3.0) * Maths.Tau * (Maths.Cubed(ship_outer) - Maths.Cubed(ship_inner));

			HullMass         = hull_volume * Spec.HullCompound.Density(World.AverageLocalTemperature, 0.0);
			TotalVolume      = hull_volume + tank_volume;
			TotalInitialMass = HullMass + FuelTankMass + PassengerMass;

			// Construction time is a function of how big the ship is
			ConstructionTime = TotalVolume / Consts.ShipConstructionRate;
		}

		///// <summary>Returns the total mass of the ship at time 't' during the flight (kg)</summary>
		//public double FlightMass(double t)
		//{
		//	var burnt_fuel = t * Spec.FuelBurnRate;
		//	return (burnt_fuel >= Spec.FuelMass)
		//		? TotalInitialMass - Spec.FuelMass
		//		: TotalInitialMass - burnt_fuel;
		//}

		// Run a simulation of the ship to determine it's viability
		void Simulate()
		{
		}
	}
}
