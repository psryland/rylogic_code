using System;
using System.Diagnostics;
using System.Globalization;
using Rylogic.Extn;
using Rylogic.Maths;

namespace EscapeVelocity
{
	/// <summary>Return the known properties of an element or ??</summary>
	public interface IElementKnown
	{
		/// <summary>The atomic number if known as a string, otherwise ??</summary>
		int AtomicNumber { get; }

		/// <summary>The element name</summary>
		string Fullname { get; }

		/// <summary>The percentage that this element is understood given the known properties of elements</summary>
		double PercentUnderstood { get; }
	}

	public class Element :IElementKnown
	{
		/// <summary>A game seed associated with this element</summary>
		public int Seed { get; private set; }

		/// <summary>Where this element lives in the period table</summary>
		public int AtomicNumber { get; private set; }
		int IElementKnown.AtomicNumber { get { return Bit.AllSet((int)KnownProperties, (int)EElemProp.AtomicNumber) ? AtomicNumber : 999; } }

		/// <summary>The name of the element</summary>
		public ElementName Name { get; private set; }
		string IElementKnown.Fullname { get { return Bit.AllSet((int)KnownProperties, (int)EElemProp.Name) ? Name.Fullname : ElementName.Unknown.Fullname; } }

		/// <summary>The number of free electrons this element has in its non-ionised state</summary>
		public int ValenceElectrons { get; private set; }

		/// <summary>The number of electrons needed to fill this electron shell (from its non-ionised state)</summary>
		public int ValenceHoles { get; private set; }

		/// <summary>The radius of the outer electron shell (m)</summary>
		public double ValenceOrbitalRadius { get; private set; }

		/// <summary>
		/// The period within the periodic table (i.e. row index).
		/// Note: ValenceElectrons + ValenceHoles = consts.ValenceLevels[Period]</summary>
		public int Period { get; private set; }

		/// <summary>The normalised horizontal position within the periodic table</summary>
		public double TableX { get; private set; }

		/// <summary>The normalised vertical position within the periodic table (0 = top)</summary>
		public double TableY { get; private set; }

		/// <summary>The mass of one mole of this element (kg/mol)</summary>
		public double MolarMass { get; private set; }

		/// <summary>
		/// A measure of the pull the element has on other electrons.
		/// In the real world, this increases from bottom left to top right of the periodic table
		/// with a range from ~0.5(Francium) to 4 (Fluorine). The Ionicity of a bond between two
		/// elements is determined from difference in electronegativity. On the 0.5->4.0 scale
		/// any bond with a difference > ~1.8 is considered ionic.</summary>
		public double Electronegativity { get; private set; }

		/// <summary>The density of this element as a solid (kg/m³)</summary>
		public double SolidDensity { get; private set; }

		/// <summary>The melting/boiling points of the element</summary>
		public double MeltingPoint { get; private set; }
		public double BoilingPoint { get; private set; }

		/// <summary>True if this element is known by the player</summary>
		public bool Known { get { return Bit.AllSet((int)KnownProperties, (int)EElemProp.Existence); } }

		/// <summary>The percentage that this element is understood given the known properties of elements</summary>
		public double PercentUnderstood { get { return Bit.CountBits((int)KnownProperties) * 100.0 / Bit.CountBits((int)KnownElementProperties); } }

		/// <summary>A bit mask of the property values that are known for this element</summary>
		public EElemProp KnownProperties { get; set; }

		/// <summary>The properties of any element currently able to be measured</summary>
		public static EElemProp KnownElementProperties { get; set; }

		public override string ToString()
		{
			return $"({AtomicNumber}) {Name.Fullname}";
		}

		// The stuff that all materials are made of
		public Element()
		{
			Seed              = 0;
			AtomicNumber      = 0;
			Name              = ElementName.Unknown;
			Period            = 0;
			TableX            = 0;
			TableY            = 0;
			ValenceElectrons  = 0;
			ValenceHoles      = 0;
			Electronegativity = 0;
			MeltingPoint      = 0;
			BoilingPoint      = 0;
			KnownProperties   = 0;
		}
		public Element(int atomic_number, GameConstants consts)
		{
			Debug.Assert(atomic_number > 0 && atomic_number <= consts.ElementCount, "");
			var rnd = new Random(consts.GameSeed + atomic_number);

			Seed         = rnd.Next();
			AtomicNumber = atomic_number;
			Name         = GameConstants.ElementNames[AtomicNumber - 1];
			MolarMass    = AtomicNumber != 1 ? 2*AtomicNumber*rnd.DoubleC(1.01,0.1) : 1;  // Roughly double the atomic number
			Period       = CalcPeriod(consts, atomic_number);
			TableX       = Math_.Frac(consts.ValenceLevels[Period-1] + 1, atomic_number, consts.ValenceLevels[Period]);
			TableY       = Math_.Frac(1.0, Period, 7.0);
			CalcValency(consts);
			CalcElectronegativity(consts);
			CalcSolidDensity(consts);
			CalcPhaseChangeTemperatures(consts);

			KnownProperties = EElemProp.Name; // All elements start with a name
			KnownProperties |= EElemProp.AtomicNumber;//hack
			
			Debug.Assert(ValenceElectrons >= 0 && ValenceElectrons <= consts.ValenceLevels[Period] - consts.ValenceLevels[Period-1], "");
			Debug.Assert(ValenceHoles     >= 0 && ValenceHoles     <= consts.ValenceLevels[Period] - consts.ValenceLevels[Period-1], "");
		}

		/// <summary>Returns true if this element is a nobal gas</summary>
		public bool IsNobal { get { return ValenceElectrons == 0; } }

		/// <summary>Return true if this element is classed as a metal</summary>
		public bool IsMetal { get { return AtomicNumber != 1 && TableX < 0.5 * TableY; } }

		/// <summary>Returns the periodic table row (i.e. period) for an element with the given atomic number</summary>
		private static int CalcPeriod(GameConstants consts, int atomic_number)
		{
			int i;
			for (i = 1; i != consts.ValenceLevels.Length && atomic_number > consts.ValenceLevels[i]; ++i) {}
			return i;
		}

		/// <summary>Sets the valency properties</summary>
		private void CalcValency(GameConstants consts)
		{
			// Noble gases have no valency
			if (AtomicNumber == consts.ValenceLevels[Period])
			{
				ValenceElectrons     = 0;
				ValenceHoles         = 0;
				ValenceOrbitalRadius = consts.OrbitalRadii[Period].Min;
				return;
			}

			ValenceElectrons = AtomicNumber - consts.ValenceLevels[Period-1];
			ValenceHoles     = consts.ValenceLevels[Period] - AtomicNumber;

			// Valency is never more than the stable shell count
			if (ValenceElectrons < consts.StableShellCount)
			{
				ValenceHoles = Math.Min(ValenceHoles, consts.StableShellCount - ValenceElectrons);
			}
			else if (ValenceHoles < consts.StableShellCount)
			{
				ValenceElectrons = Math.Min(ValenceElectrons, consts.StableShellCount - ValenceHoles);
			}
			else
			{
				const int MinValenceElectrons = 2;
				int MaxValenceElectrons = consts.StableShellCount - 1;
				for (int i = 0, iend = ValenceElectrons - MaxValenceElectrons; i < iend;)
				{
					ValenceElectrons = MaxValenceElectrons;
					for (; i < iend && ValenceElectrons > MinValenceElectrons; --ValenceElectrons, ++i) {}
					for (; i < iend && ValenceElectrons < MaxValenceElectrons; ++ValenceElectrons, ++i) {}
				}
				ValenceHoles = 8 - ValenceElectrons;
			}
	
			// Create an orbital radius for this element
			// Orbital radii decreases from bottom left to top right of the periodic table, i.e He is the smallest atom, Cs the biggest
			ValenceOrbitalRadius = Math_.Lerp(consts.OrbitalRadii[Period].Max, consts.OrbitalRadii[Period].Min, Math_.Sqrt(TableX));
		}

		/// <summary>Sets the electro negativity</summary>
		private void CalcElectronegativity(GameConstants consts)
		{
			if (AtomicNumber == 1)
			{
				Electronegativity = Math_.Lerp(consts.MinElectronegativity, consts.MaxElectronegativity, 0.5);
				return;
			}

			// This scale does not include the nobal gases so rescale TableX to make 1.0 = ValenceLevel[Period] - 1
			var row_count = consts.ValenceLevels[Period] - consts.ValenceLevels[Period - 1];
			var x = TableX * row_count / (row_count - 1);

			// Electronegativity increases from bottom left to top right of the
			// periodic table with a minor peak in the centre
			var frac = 0.03 * Period * Math.Sin(Math.Pow(x,Math_.Root2) * Math_.Tau) + x; // A normalised scaler with a hump in the middle that gets bigger for higher periods
			var min = consts.MinElectronegativity * Math_.Lerp(1.5,1.0,TableY);
			var max = consts.MaxElectronegativity* Math_.Lerp(1.15,0.45,TableY);
			Electronegativity = Math_.Lerp(min, max, frac);
		}

		/// <summary>Sets the density of the element in the solid state (assumed constant)</summary>
		private void CalcSolidDensity(GameConstants consts)
		{
			var frac = Math.Sin(TableX * 0.48 * Math_.Tau);
			var min = consts.MinSolidMaterialDensity * Math_.Lerp(1.0, 3.5, TableY);
			var max = consts.MaxSolidMaterialDensity * Math_.Lerp(0.015, 1.0, TableY);
			SolidDensity = Math_.Lerp(min, max, frac);
		}

		/// <summary>Sets the melting / boiling point for the element</summary>
		private void CalcPhaseChangeTemperatures(GameConstants consts)
		{
			MeltingPoint   = 0;
			BoilingPoint   = 0;
		}

		/// <summary>Returns the effective charge experienced by electron 'electron_number'</summary>
		public double EffectiveCharge(GameConstants consts, int electron_number)
		{
			// Basic Zeff approximation:
			//  Zeff = Z - non-valence electron count
			// (Z = atomic number)
			//
			// Slater’s rules assume imperfect shielding
			// Zeff = Z – s where s is calculated using Slater’s rules
			// 1. Group the orbitals in order:
			//  (1s) (2s,2p) (3s,3p) (3d) (4s,4p) (4d) (4f) (5s,5p)…
			// 2. To determine s, sum up the following contributions for the electron of interest:
			//  a. 0 (zero) for all electrons in groups outside (to the right of) the one being considered
			//  b. 0.35 for each of the other electrons in the same group (except for 1s group where 0.30 is used)
			//  c. If the electron is in a (ns,np) group, 0.85 for each electron in the next innermost (to the left) group
			//  d. If the electron is in a (nd) or (nf) group, 1.00 for each electron in the next innermost (to the left) group
			//  e. 1.00 for each electron in the still lower (farther in) groups

			var period = CalcPeriod(consts, electron_number);
			var ecount0 = period > 1 ? consts.ValenceLevels[period - 2] : 0;
			var ecount1 = consts.ValenceLevels[period - 1] - ecount0;
			var ecount2 = (electron_number - 1) - consts.ValenceLevels[period - 1];

			// Use something halfway between
			double charge = AtomicNumber;    // Atomic charge
			charge -= ecount0;               // Minus 100% of electrons more than 1 shell below
			charge -= 0.85 * ecount1;        // Minus 85% of electrons in the shell just below
			charge -= 0.35 * ecount2;        // Minus 35% of electrons in the current shell
			return charge;
		}
	}
}