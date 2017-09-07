using System.ComponentModel;
using pr.extn;
using pr.maths;
using pr.util;

namespace EscapeVelocity
{
	public class ShipSpec
	{
		/// <summary>The number of people on the ship</summary>
		[Description("The number of people that the ship is designed to carry")]
		public int PassengerCount { get; set; }

		/// <summary>Fuel is comprised of two components, this represents a component</summary>
		[TypeConverter(typeof(FuelSpec))]
		public class FuelSpec :GenericTypeConverter<FuelSpec>
		{
			/// <summary>The Compound to use for fuel</summary>
			[Description("The component to use as a fuel component")]
			public Compound FuelComponent { get; set; }

			/// <summary>The initial mass of this fuel (kg)</summary>
			[Description("The quantity of the fuel component")]
			public double Mass { get; set; }

			/// <summary>The pressure used to contain this fuel component (N/m²)</summary>
			[Description("The pressure used to contain the fuel component")]
			public double ContainmentPressure { get; set; }

			/// <summary>The Compound to build the fuel tank out of</summary>
			[Description("The material to build the fuel tank out of")]
			public Compound TankCompound { get; set; }

			/// <summary>The volume that the given mass of this fuel occupies</summary>
			public double Volume(GameConstants consts, WorldState world)
			{
				// If the fuel is a gas at the local temperature/pressure, use the ideal gas equation
				if (FuelComponent.Phase(world.AverageLocalTemperature, ContainmentPressure) == EPhase.Gas)
				{
					// PV = nRT, V = nRT / P
					return FuelComponent.MolarMass * consts.GasConstant * world.AverageLocalTemperature / ContainmentPressure;
				}

				// Otherwise assume an incompressible liquid/solid; Density = Mass/Volume, V = M/D
				return Mass / FuelComponent.Density(world.AverageLocalTemperature, ContainmentPressure);
			}

			/// <summary>Returns the thickness of the wall of a container needed to hold the fuel at the containment pressure</summary>
			public double TankWallThickness(double fuel_volume)
			{
				// The container Compound has a strength (measured in Pa) which is the pressure
				// at which the Compound breaks (tensile strength). Assume that the breaking
				// force scales linearly with the cross sectional area, i.e. N/m² = pressure in Pa
				// So to contain pressure, p, the cross-sectional area = p / Compound.strength
				double xs_area = ContainmentPressure / TankCompound.Strength;

				// The inner radius of the spherical container: v = 2/3τr³ => r = ³root(3v/2τ)
				double inner = Maths.SphereRadius(fuel_volume);

				// The cross sectional area is a cross section through the spherical container
				// so the area = 0.5τ(r1² - r0²). r0 = inner container radius => r1 = sqrt(2a/τ + r0²)
				double outer = Maths.Sqrt(2.0 * xs_area / Maths.Tau + Maths.Sqr(inner));

				// The difference is the wall thickness
				return outer - inner;
			}

			/// <summary>Returns the mass of the container</summary>
			public double TankMass(double fuel_volume, WorldState world)
			{
				// v = 2/3 τ(r1³ - r0³)
				var inner = Maths.SphereRadius(fuel_volume);
				var outer = inner + TankWallThickness(fuel_volume);
				var volume = (2.0/3.0) * Maths.Tau * (Maths.Cubed(outer) - Maths.Cubed(inner));
				return volume * TankCompound.Density(world.AverageLocalTemperature, 0.0);
			}

			/// <summary>Returns the volume of the tank including the fuel</summary>
			public double TankAndFuelVolume(double fuel_volume, WorldState world)
			{
				return Maths.SphereRadius(fuel_volume) + TankWallThickness(fuel_volume);
			}

			public FuelSpec()
			{
				FuelComponent = new Compound();
				TankCompound = new Compound();
			}
		}

		/// <summary>The materials that will be combined to use as fuel</summary>
		[Description("The reactants to combine as fuel")]
		public FuelSpec[] Fuel { get; private set; }

		/// <summary>
		/// The rate at which the fuel will be burnt (kg/s).
		/// This is the total mass flow rate. Fuel A and B will be combined at the ideal
		/// ratio so this property defines how long the fuel can be burnt before being
		/// used up. If the ratio of FuelMass A and B are not chosen correctly, the
		/// remainder will be left on the ship contributing to it's weight</summary>
		[Description("How fast the fuel should be burnt")]
		public double FuelBurnRate { get; set; }

		/// <summary>
		/// The Compound that the ship is made out of.
		/// This property effects:
		///   total mass of the ship
		///   max fuel containment pressure -> effects fuel volume -> fuel matter state
		/// </summary>
		[Description("The material from which to build the main hull containing the passengers")]
		public Compound HullCompound
		{
			get { return m_hull_compound; }
			set { m_hull_compound = Fuel[0].TankCompound = Fuel[1].TankCompound = value; }
		}
		private Compound m_hull_compound;

		public ShipSpec()
		{
			Fuel = Array_.New<FuelSpec>(2);
			HullCompound = new Compound();
		}
	}
}
