using System;
using pr.extn;
using pr.maths;

namespace EscapeVelocity
{
	public class GameConstants
	{
		public static readonly ElementName[] ElementNames = new[]
			{
				new ElementName("hydrogen"   , "H" , "hydr"     ),
				new ElementName("helium"     , "He", "hel"      ),
				new ElementName("lithium"    , "Li", "lithim"   ),
				new ElementName("beryllium"  , "Be", "beryll"   ),
				new ElementName("boron"      , "B" , "bor"      ),
				new ElementName("carbon"     , "C" , "carbon"   ),
				new ElementName("nitrogen"   , "N" , "nitr"     ),
				new ElementName("oxygen"     , "O" , "ox"       ),
				new ElementName("fluorine"   , "F" , "fluor"    ),
				new ElementName("neon"       , "Ne", "neon"     ),
				new ElementName("sodium"     , "Na", "sodim"    ),
				new ElementName("magnesium"  , "Mg", "magnesim" ),
				new ElementName("aluminium"  , "Al", "alumin"   ),
				new ElementName("silicon"    , "Si", "silic"    ),
				new ElementName("phosphorus" , "P" , "phosph"   ),
				new ElementName("sulfur"     , "S" , "sulf"     ),
				new ElementName("chlorine"   , "Cl", "chlor"    ),
				new ElementName("argon"      , "Ar", "argon"    ),
				new ElementName("potassium"  , "K" , "potassim" ),
				new ElementName("calcium"    , "Ca", "calc"     ),
				new ElementName("scandium"   , "Sc", "Scandim"  ),
				new ElementName("titanium"   , "Ti", "titan"    ),
				new ElementName("vanadium"   , "V" , "vanad"    ),
				new ElementName("chromium"   , "Cr", "chrom"    ),
				new ElementName("manganese"  , "Mn", "mangan"   ),
				new ElementName("iron"       , "Fe", "iron"     ),
				new ElementName("cobalt"     , "Co", "coball"   ),
				new ElementName("nickel"     , "Ni", "nickel"   ),
				new ElementName("copper"     , "Cu", "coppen"   ),
				new ElementName("zinc"       , "Zn", "zinc"     ),
				new ElementName("gallium"    , "Ga", "gall"     ),
				new ElementName("germanium"  , "Ge", "german"   ),
				new ElementName("arsenic"    , "As", "arsen"    ),
				new ElementName("selenium"   , "Se", "selen"    ),
				new ElementName("bromine"    , "Br", "brom"     ),
				new ElementName("krypton"    , "Kr", "krypton"  ),
				new ElementName("rubidium"   , "Rb", "rubid"    ),
				new ElementName("strontium"  , "Sr", "stront"   ),
				new ElementName("yttrium"    , "Y" , "yttr"     ),
				new ElementName("zirconium"  , "Zr", "zircon"   ),
				new ElementName("niobium"    , "Nb", "niobim"   ),
				new ElementName("molybdenum" , "Mo", "molybden" ),
				new ElementName("technetium" , "Tc", "technet"  ),
				new ElementName("ruthenium"  , "Ru", "ruthen"   ),
				new ElementName("rhodium"    , "Rh", "rhod"     ),
				new ElementName("palladium"  , "Pd", "palladin" ),
				new ElementName("silver"     , "Ag", "silv"     ),
				new ElementName("cadmium"    , "Cd", "cadm"     ),
				new ElementName("indium"     , "In", "ind"      ),
				new ElementName("tin"        , "Sn", "tin"      ),
				new ElementName("antimony"   , "Sb", "antim"    ),
				new ElementName("tellurium"  , "Te", "tellur"   ),
				new ElementName("iodine"     , "I" , "iod"      ),
				new ElementName("xenon"      , "Xe", "xenon"    ),
				new ElementName("caesium"    , "Cs", "caes"     ),
				new ElementName("barium"     , "Ba", "bar"      ),
				new ElementName("lanthanum"  , "La", "lanth"    ),
				new ElementName("cerium"     , "Ce", "cerim"    ),
			};

		/// <summary>The seed used to generate this game instance</summary>
		public int GameSeed { get; private set; }

		/// <summary>The maximum time a game should take (in seconds)</summary>
		public readonly double MaxGameDuration;

		/// <summary>The starting time till the star goes nova (in seconds)</summary>
		public readonly double InitialTimeTillNova;

		/// <summary>The error margin for the time till the star goes nova (in seconds)</summary>
		public readonly double InitialTimeTillNovaErrorMargin;

		/// <summary>The countdown till nova is a large value but we want each game to last a
		/// fixed time. This scales game seconds to make the nova time = max_game_time</summary>
		public readonly double m_time_scaler;

		/// <summary>The universal speed of light (m/s)</summary>
		public readonly double m_speed_of_light;

		/// <summary>The constant that scales the gravitational force</summary>
		public readonly double m_gravitational_constant;

		/// <summary>The constant that scales the electro static force Nm²/C²</summary>
		public double CoulombConstant { get; private set; }

		/// <summary>The electro-static charge on an electron</summary>
		public double ElectronCharge { get; private set; }

		/// <summary>The mass of a proton (kg)</summary>
		public double ProtonMass { get; private set; }

		/// <summary>The ideal gas equation gas constant (Joules/(mol Kelvin))</summary>
		public double GasConstant { get; private set; }

		/// <summary>The number of elements in the universe</summary>
		public readonly int ElementCount;

		/// <summary>The number of elements in a shell that makes it stable, 8 for real chemisty</summary>
		public readonly int StableShellCount;

		/// <summary>
		/// The valence levels of the elements, these are the number of electrons for a stable element.
		///  E.g 2, 10, 18, 36, etc corresponding to orbit electron counts 2, 8, 8, 18, etc
		/// Note: ValenceLevels[0] is always 0.</summary>
		public readonly int[] ValenceLevels;

		/// <summary>
		/// The "radii" of the orbitals, i.e. the maximum distance of the radial probability
		/// distribution functions for each orbital level. Orbital radii decreases from bottom left
		/// to top right of the periodic table, i.e He is the smallest atom, Cs the biggest (in picometres)</summary>
		public readonly Range[] OrbitalRadii;

		/// <summary>The approximate max molar mass (i.e. molar mass of the largest element)</summary>
		public double MaxMolarMass { get; private set; }

		/// <summary>The minimum electro negativity value</summary>
		public double MinElectronegativity { get; private set; }

		/// <summary>The maximum electro negativity value</summary>
		public double MaxElectronegativity { get; private set; }

		/// <summary>The minimum density of any solid (kg/m³)</summary>
		public double MinSolidMaterialDensity { get; private set; }

		/// <summary>The maximum density of any solid (kg/m³)</summary>
		public double MaxSolidMaterialDensity { get; private set; }

		/// <summary>The mass of the star that the space craft needs to escape (kg)</summary>
		public readonly double StarMass;

		/// <summary>The distance from the star (m)</summary>
		public readonly double StarDistance;

		/// <summary>The acceleration due to the star's gravity at the given distance (m/s²)</summary>
		public readonly double m_star_gravitational_acceleration;

		/// <summary>The speed required to escape the star (m/s)</summary>
		public readonly double EscapeVelocity;

		/// <summary>The average weight of a passenger (kg)</summary>
		public readonly double AveragePassengerWeight;

		/// <summary>The space required by each passenger (m³)</summary>
		public readonly double AveragePassengerPersonalSpace;

		/// <summary>The pressure in the space ship needed for the </summary>
		public readonly double CabinPressure;

		/// <summary>The ships volume is this much bigger than it's contents</summary>
		public readonly double ShipVolumeScaler;

		/// <summary>A limit on the available resources, to be divided amoung the research efforts</summary>
		public readonly int m_total_man_power;

		/// <summary>How quickly the ship can be built (m³/s)</summary>
		public readonly double ShipConstructionRate;

		/// <summary>The total man days needed to discover the star mass (man-days)</summary>
		public readonly double m_star_mass_discovery_effort;

		/// <summary>The total man days needed to discover the star distance (man-days)</summary>
		public readonly double m_star_distance_discovery_effort;

		public GameConstants(int seed, bool real_chemistry)
		{
			GameSeed = seed;
			ElementCount = ElementNames.Length;

			// Universal constants
			MaxGameDuration                = 30 * 60 * 60; // 30 minutes
			InitialTimeTillNova            = 365 * 24 * 60 * 60;
			InitialTimeTillNovaErrorMargin = 20 * 24 * 60 * 60;
			m_time_scaler                  = InitialTimeTillNova / MaxGameDuration;
			m_speed_of_light               = 2.99792458e8;
			m_gravitational_constant       = 6.6738e-11;
			CoulombConstant                = 8.987551e9;
			ElectronCharge                 = 1.60217657e-19;
			ProtonMass                     = 1.67262178e-27;
			GasConstant                    = 8.3144621;
			MaxMolarMass                   = 2.0 * ElementCount;
			MinElectronegativity           = 0.7;
			MaxElectronegativity           = 4.0;
			MinSolidMaterialDensity        = 350.0;
			MaxSolidMaterialDensity        = 25000.0;

			var rnd = new Random(GameSeed);

			ValenceLevels = new int[10];
			if (real_chemistry)
			{
				StableShellCount = 8;

				// The total numbers of electrons at each orbital level
				ValenceLevels[0] = 0;
				ValenceLevels[1] = 2;
				ValenceLevels[2] = 10;
				ValenceLevels[3] = 18;
				ValenceLevels[4] = 36;
				ValenceLevels[5] = 54;
				ValenceLevels[6] = 86;
				ValenceLevels[7] = 118;
			}
			else
			{
				StableShellCount = rnd.Next(6,11); // [6,10]

				// The total numbers of electrons at each orbital level
				ValenceLevels[0] = 0;
				ValenceLevels[1] = rnd.Next(1,4);
				for (int i = 2; i != ValenceLevels.Length; ++i)
				{
					int v = 1 + ValenceLevels[i-1];
					ValenceLevels[i] = (int)rnd.NextDouble(1.3*v, 2.9*v);
				}
			}

			OrbitalRadii = new Range[ValenceLevels.Length];
			OrbitalRadii[0] = new Range(   0.0 ,   0.0);
			OrbitalRadii[1] = new Range(  31.0 ,  53.0);
			OrbitalRadii[2] = new Range(  38.0 , 167.0);
			OrbitalRadii[3] = new Range(  71.0 , 190.0);
			OrbitalRadii[4] = new Range(  88.0 , 243.0);
			OrbitalRadii[5] = new Range( 108.0 , 265.0);
			OrbitalRadii[6] = new Range( 120.0 , 298.0);
			OrbitalRadii[7] = new Range( 132.0 , 341.0);
			OrbitalRadii[8] = new Range( 140.0 , 390.0);
			OrbitalRadii[9] = new Range( 144.0 , 450.0);

			// Pick a star mass approximately the same as the sun
			const double suns_mass = 2.0e30;
			StarMass = rnd.NextDoubleCentred(suns_mass, suns_mass * 0.25);

			// Pick a distance from the star, somewhere between mercury and mars
			const double sun_to_mercury = 5.79e10;
			const double sun_to_mars    = 2.279e11;
			StarDistance = rnd.NextDouble(sun_to_mercury, sun_to_mars);

			// The acceleration due to the star's gravity at the given distance
			m_star_gravitational_acceleration = m_gravitational_constant * StarMass / Maths.Sqr(StarDistance);

			// Calculate the required escape velocity (speed)
			// Escape Velocity = Sqrt(2 * G * M / r), G = 6.67x10^-11 m³kg^-1s^-2, M = star mass, r = distance from star
			EscapeVelocity = Maths.Sqrt(2.0 * m_gravitational_constant * StarMass / StarDistance);

			// Set up per passenger constants
			AveragePassengerWeight = rnd.NextDoubleCentred(80.0, 10.0);
			AveragePassengerPersonalSpace = rnd.NextDoubleCentred(2.0, 0.5);
			CabinPressure = 1000;

			// The total number of people available to work
			m_total_man_power = rnd.NextCentred(10000, 0);

			// The ship is roughly 10% bigger than the volume of it's contents
			ShipVolumeScaler = rnd.NextDoubleCentred(1.11,0.0);
			ShipConstructionRate = rnd.NextDoubleCentred(0.1,0.0);

			// The total man days needed to discover the star mass
			m_star_mass_discovery_effort = rnd.NextDoubleCentred(1000,0.0);

			// The rate at which the star distance can be discovered proportional to the main hours assigned
			m_star_distance_discovery_effort = rnd.NextDoubleCentred(1000,0.0);
		}
	}
}