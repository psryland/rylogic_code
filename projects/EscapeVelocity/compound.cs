using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace EscapeVelocity
{
	public interface IKnownCompound
	{
		/// <summary>The common name of the Compound</summary>
		string CommonName { get; }

		/// <summary>The scientific name of the Compound</summary>
		string ScientificName { get; }

		/// <summary>The name to display for the Compound</summary>
		string Fullname { get; }
	}

	[TypeConverter(typeof(Compound))]
	public class Compound :GenericTypeConverter<Compound> ,IKnownCompound 
	{
		private const string UnknownCompoundName = "unknown compound";
		private const string UnknownCompoundSymName = "??";
		private static readonly string[] NumPrefix = new[]{ // 0 - 23
			"","mono","di","tri","tetra","penta","hexa","hepta","octa","nona","deca",
			"undeca","dodeca","trideca","tetradeca","pentadeca","hexadeca","heptadeca","octadeca","nonadeca",
			"icosa","heicosa","docosa","tricosa"};

		/// <summary>The elements that this Compound is made of: Elem1 * Count1 + Elem2 * Count2</summary>
		public Element Elem1 { get; private set; }
		public Element Elem2 { get; private set; }
		public int Count1 { get; private set; }
		public int Count2 { get; private set; }

		/// <summary>The name assigned by the player for this Compound</summary>
		public string CommonName { get; set; }   // What the layman call it

		/// <summary>The name of the Compound (derived from the elements)</summary>
		public string ScientificName { get; private set; } // Its full chemical name

		/// <summary>The sumbollic name</summary>
		public string SymbolicName { get; private set; }
		
		/// <summary>The name to display for the Compound</summary>
		public string Fullname { get { return Elem1.Known && Elem2.Known ? ScientificName + " ("+CommonName+")" : CommonName; } }

		/// <summary>The configuration of the Compound</summary>
		public readonly Bond[] Bonds;

		/// <summary>The mass of one mole of this stuff (kg/mol)</summary>
		public double MolarMass { get; private set; }

		/// <summary>A measure of how ionic the Compound is</summary>
		public double Ionicity { get; private set; }

		/// <summary>A measure of how strongly bonded molecules of this Compound are to each other (N/m², Pa)</summary>
		public double Strength { get; private set; }

		/// <summary>The measure of how strongly bonded this Compound is</summary>
		public double Enthalpy { get; private set; }

		/// <summary>The melting point of this Compound (K)</summary>
		public double MeltingPoint { get; private set; }

		/// <summary>The boiling point of this Compound (K)</summary>
		public double BoilingPoint { get; private set; }

		/// <summary>The density of this Compound when it's a solid (assuming constant)(kg/m³)</summary>
		private double m_solid_density;
		/// <summary>The density of this Compound at the solid/liquid phase transition(kg/m³)</summary>
		private double m_liquid_density0;
		/// <summary>The density of this Compound at the liquid/gas phase transition(kg/m³)</summary>
		private double m_liquid_density1;

		/// <summary>True if this Compound is known to the player</summary>
		public bool Discovered { get; set; }

		///<summary>
		/// Returns a unique index for a Compound combination
		/// The order of elem1/elem2 does not effect the index</summary>
		public static int Index(int elem1_atomic_number, int elem2_atomic_number)
		{
			return TriTable.Index(elem1_atomic_number - 1, elem2_atomic_number - 1, TriTable.EType.Inclusive);
		}
		public static int Index(Element elem1, Element elem2)
		{
			return Index(elem1.AtomicNumber, elem2.AtomicNumber);
		}

		public override string ToString() { return ScientificName + "("+SymbolicName+")" + " known as '" + CommonName + "'"; }

		public Compound()
		{
			Elem1           = new Element();
			Elem2           = new Element();
			Count1          = 0;
			Count2          = 0;
			CommonName      = UnknownCompoundName;
			ScientificName  = UnknownCompoundName;
			SymbolicName    = UnknownCompoundSymName;
			Bonds           = Util.NewArray<Bond>(EPerm.Perm2Count);
			Ionicity        = 0;
			Discovered    = false;
		}
		public Compound(Element e1, Element e2, GameConstants consts) :this()
		{
			// Nobal elements cannot form materials
			if (e1.IsNobal || e2.IsNobal)
				return;

			// Order the elements by least valence electrons first
			Elem1 = e1.ValenceElectrons < e2.ValenceElectrons ? e1 : e2;
			Elem2 = e1.ValenceElectrons < e2.ValenceElectrons ? e2 : e1;

			// Find the bond strengths for all possible permutations of elem1/elem2
			Bonds = Bond.Permutations(Elem1, Elem2, consts);

			// Determine the configuration of the molecule
			CalcBondConfiguration(consts);

			// The bond energy of the Compound is the sum of the individual bond energies
			Enthalpy = Bonds.Enthalpy();

			CommonName      = UnknownCompoundName;
			ScientificName  = UnknownCompoundName;
			SymbolicName    = UnknownCompoundSymName;
			UpdateName();

			// The molar mass of the Compound is just the sum of its parts
			MolarMass = Elem1.MolarMass * Count1 + Elem2.MolarMass * Count2;

			var rnd = new Random(unchecked (consts.GameSeed ^ Elem1.Seed ^ Elem2.Seed));

			// Determine how ionic this Compound is based on the bond configuration
			// Ionic materials tend to form strong macro structures (e.g. crystal lattices)
			// The individual bonds may be ionic but symmetry of the molecule can cause
			// the molecule to be less ionic. The result should be the vector sum of each
			// bond but there isn't a 3D model of the molecule.
			// Use the ionicity of the most dominant bond
			Ionicity = Bonds[EPerm.AB].Ionicity * rnd.NextDoubleRange(0.7, 1.0);

			// Determine the density values.
			CalculateDensity(consts, rnd);
		}

		/// <summary>Generates a bond configuration using the bond information</summary>
		private void CalcBondConfiguration(GameConstants consts)
		{
			// We are making a compound from e1 and e2, it doesn't matter if e1-e1 or e2-e2 are more stable,
			// those combinations will have a higher enthalpy and are created by calling Compound(e1,e1,consts) or Compound(e2,e2,consts).

			// This is the ratio of elements needed to create a stable molecule
			Count1 = Elem2.ValenceHoles     / Maths.GreatestCommonFactor(Elem1.ValenceElectrons, Elem2.ValenceHoles);
			Count2 = Elem1.ValenceElectrons / Maths.GreatestCommonFactor(Elem1.ValenceElectrons, Elem2.ValenceHoles);
			int common = Math.Min(Count1, Count2);

			// The valence electrons to be used
			int electrons = Count1 * Elem1.ValenceElectrons;
			Debug.Assert(electrons == Count2 * Elem2.ValenceHoles);

			// Get the bonds in order of strength from strongest to weakest
			var order = Bonds.StrengthOrder();

			// Add the A-B bonds first to ensure...
			for (int start = 0;;)
			{
				// Find the index of the first A-B bond
				var ab_idx = order.IndexOf(x => x.Bond.Perm == EPerm.AB, start);
				Debug.Assert(ab_idx != -1);

			{// Create 'common' A-B bonds
				var b = order[ab_idx].Bond;
				var o = order[ab_idx].Order;
				Debug.Assert(o * common <= electrons); // There should be enough electrons 
				for (int i = 0; i != common; ++i)
				{
					b.Count[o - 1] ++order[ab_idx].Bond.Count[order[i].Order - 1]++;
					electrons -= order[i].Order;
				}
			}
			// Generate bonds in order of strength until there are no free electrons left
			for (int i = 0; i != order.Length; ++i)
			{
				while (electrons >= order[i].Order)
				{
					if (Bonds[EPerm.AB].TotalCount == 0 && order[i].Bond.Perm != EPerm.AB)
						break;

					order[i].Bond.Count[order[i].Order - 1]++;
					electrons -= order[i].Order;
				}
			}

			// Add instances of bonds until all of the valence electrons have been used up
			switch (order[0].Bond.Perm)
			{
			case EPerm.AA:
			case EPerm.BB: 
				{
					// 'AA' assumed to be the strongest in these comments, but works for AA or BB
					int aa = order[0].Bond.Perm;
					int count1 = aa == EPerm.AA ? Count1 : Count2;
					int count2 = aa == EPerm.AA ? Count2 : Count1;

					// The maximum number of electrons available for AA bonds
					// Need to leave at least 'reserved' electrons for AB bonds
					int aa_electrons = electrons - count2;
					int reserved = count2;

					break;
				}
			case EPerm.AB:
				{
					int pairs = Math.Min(Count1,Count2);
					if ()
					{
					}
					break;
				}
			}
			Debug.Assert(e == 0);

			
			
			
			// All structures are basically long chains with the other element hanging off
			// The 'backbone' of the molecule is defined by whichever is the strongest bond
			// e.g. if A-A is the strongest then:
			//  B - A - A - A - B
			//      |   |   |
			//      B   B   B

			// If there are N electrons, M holes, and single bonds only, we need M of Elem1,
			// and N of Elem2 giving M*N electrons and N*M holes
			Count1 = Elem2.ValenceHoles;
			Count2 = Elem1.ValenceElectrons;

			var strongest = Bonds.Strongest();
			switch (strongest.Perm)
			{
			case EPerm.AA:
				Bonds[EPerm.AA].Count[0] = Count1 - 1; // A - A - A - A ..., A = A - A
				Bonds[EPerm.AB].Count[0] = Count2;     // B   B   B          B   B   B
				Bonds[EPerm.BB].Count[0] = 0;
				break;
			case EPerm.BB:
				Bonds[EPerm.BB].Count[0] = Count2 - 1; // B - B - B - B ...
				Bonds[EPerm.AB].Count[0] = Count1;     // A   A   A
				Bonds[EPerm.AA].Count[0] = 0;
				break;
			case EPerm.AB:
				int c = Math.Min(Count1, Count2);      // A-B = 1, A-B-A-B = 3, A-B-A-B-A-B = 5 ...
				Bonds[EPerm.AB].Count[0] = c * 2 - 1   // A - B - A - B ...
					+ (Count1-c) + (Count2-c);         // B       B
				Bonds[EPerm.AA].Count[0] = 0;
				Bonds[EPerm.BB].Count[0] = 0;
				break;
			}

			// If any of the bonds are multiple bonds, create them by combining lower order bonds and reducing the number of atoms involved
			foreach (var b in Bonds)
			{
				var removed = b.Promote();
				if (removed == 0) continue;
				switch (b.Perm)
				{
				case EPerm.AA:
					Count1 -= removed;
					break;
				case EPerm.BB:
					Count2 -= removed;
					break;
				case EPerm.AB:
					for (int i = 0; i != removed; ++i)
					{
						if (Count1 > Count2) --Count1;
						else                 --Count2;
					}
					break;
				}
			}

			// Sanity check - check that the bond counts account for all electrons and all holes
			int e = 0; // The sum of electrons involved in bonds
			foreach (var b in Bonds)
			for (int i = 0; i != b.Count.Length; ++i) e += b.Order * b.Count[i];
			Debug.Assert(Count1 * Elem1.ValenceElectrons == e, "Wrong number of electrons");
			Debug.Assert(Count2 * Elem2.ValenceHoles     == e, "Wrong number of holes");
		}

		/// <summary>Determine the density limits</summary>
		private void CalculateDensity(GameConstants consts, Random rnd)
		{
			// Factors influency density:
			// -Bigger molar mass = higher density
			// -Stronger bonds = higher density
			// -More Ionic = higher density
			// -Smaller atomic radii = higher density
			var norm_ionicity = Maths.Frac(consts.MinElectronegativity, Ionicity, consts.MaxElectronegativity);

			var mass_adj = Maths.Frac(1, MolarMass / (Count1+Count2), consts.MaxMolarMass); // normalised average molar mass
			var ionicity_adj = 1.0 + 0.5 * Math.Pow(norm_ionicity, 8); // no significant ionicity until about 0.7
			//var bond_adj = dominant_bond.Strength;
			//var radii = Elem1.ValenceOrbitalRadius + Elem2.ValenceOrbitalRadius;

			var scaler = mass_adj * ionicity_adj;

			Debug.Assert(scaler >= 0 && scaler < 10);
			m_solid_density = Maths.Lerp(consts.MinSolidMaterialDensity, consts.MaxSolidMaterialDensity, scaler);
			

			// The liquid_density0 value can only be lower than the solid density when
			// the Compound is strongly ionic such that it forms a crystals
			var ionicity_solid_density_scaler = 0.1 * Maths.Max(0, norm_ionicity - 0.75);
			m_liquid_density0 = m_solid_density * rnd.NextDoubleRange(1.0,1.0-ionicity_solid_density_scaler);
			m_liquid_density1 = m_liquid_density0 * rnd.NextDoubleCentred(0.8,0.0); // at boiling point, density is roughly 20% less
			
		}

		/// <summary>Call to update the name</summary>
		public void UpdateName()
		{
			// A Compound can be discovered independently of its elements.
			// In this case the name is arbitrary (assigned by player?)
			// If one element is known it gets a partial name 'Something-hydride', 'Sodium-something'
			bool e1_known = (Elem1.KnownProperties & EElemProp.Existence) != 0;
			bool e2_known = (Elem2.KnownProperties & EElemProp.Existence) != 0;

			// If both elements are known, use it's standard name
			if (e1_known && e2_known)
			{
				Func<char,bool> IsVowel = x => x == 'a' || x == 'e' || x == 'i' || x == 'o' || x == 'u' || x == 'y';
				bool flip = (Elem1.AtomicNumber == 1 && Elem2.IsMetal) || Elem2.ValenceElectrons < Elem1.ValenceElectrons;
				var e1 = flip ? Elem2  : Elem1;
				var e2 = flip ? Elem1  : Elem2;
				var c1 = flip ? Count2 : Count1;
				var c2 = flip ? Count1 : Count2;

				var name = new StringBuilder();
				if (Elem1.AtomicNumber == Elem2.AtomicNumber)
				{
					name.Append(e1.Name.Fullname);
				}
				else
				{
					if (c1 > 1)
					{
						name.Append(NumPrefix[c1]);
						if (IsVowel(e1.Name.Fullname[0]) && c1 > 3)
							name.Length--;
					}
					name.Append(e1.Name.Fullname);
					name.Append(" ");
					if (c2 > 1 && !e1.IsMetal)
					{
						name.Append(NumPrefix[c2]);
						if (IsVowel(e2.Name.SuffixForm[0]) && (c2 != 2 || c2 != 3))
							name.Length--;
					}
					name.Append(e2.Name.SuffixForm);
					name.Append("ide");
				}
				var sym  = new StringBuilder();
				if (Elem1.AtomicNumber == Elem2.AtomicNumber)
				{
					sym.Append(e1.Name.Symbol).Append(c1+c2 == 2 ? (c1+c2).ToString(CultureInfo.CurrentCulture) : string.Empty);
				}
				else
				{
					sym
						.Append(e1.Name.Symbol).Append(c1 > 1 ? c1.ToString(CultureInfo.CurrentCulture) : string.Empty)
						.Append(e2.Name.Symbol).Append(c2 > 1 ? c2.ToString(CultureInfo.CurrentCulture) : string.Empty);
				}
				
				ScientificName = name.ToString();
				SymbolicName   = sym.ToString();
			}
			else if (e1_known || e2_known)
			{
				var elem  = e1_known ? Elem1 : Elem2;
				ScientificName = "{0}-??".Fmt(elem.Name.Fullname);
				SymbolicName = "{0}-??".Fmt(elem.Name.Symbol);
			}
			else
			{
				ScientificName = UnknownCompoundName;
				SymbolicName = "??";
			}
		}

		/// <summary>The matter state of this Compound at the given temperature/pressure</summary>
		public EPhase Phase(double temperature, double pressure)
		{
			//todo
			return EPhase.Solid;
		}

		/// <summary>The density of the Compound at the given temperature/pressure (kg/m³)</summary>
		public double Density(double temperature, double pressure)
		{
			switch (Phase(temperature, pressure))
			{
			default: throw new ArgumentException();
			case EPhase.Gas: return 0.0;
			case EPhase.Solid: return m_solid_density;
			case EPhase.Liquid:
				var f = Maths.Frac(MeltingPoint, temperature, BoilingPoint);
				return Maths.Lerp(m_liquid_density0, m_liquid_density1, f);
			}
		}

		///// <summary>A measure of the strength of this Compound as a function of thickness (N)</summary>
		//public double CompressiveStrength(double thickness)
		//{
		//	return 0;
		//}

	}

	public class CompoundCont :IEnumerable<Compound>
	{
		private readonly TriTable<Compound> m_table;

		public CompoundCont(int item_count, TriTable.EType type)
		{
			m_table = new TriTable<Compound>(item_count, type);
		}

		/// <summary>Return the Compound made from the given pair of elements</summary>
		public Compound this[Element e1, Element e2]
		{
			get { return m_table[e1.AtomicNumber - 1, e2.AtomicNumber - 1]; }
			set { m_table[e1.AtomicNumber - 1, e2.AtomicNumber - 1] = value; }
		}

		/// <summary>Returns all materials that contain 'elem'</summary>
		public IEnumerable<Compound> Related(Element elem)
		{
			return m_table.Row(elem.AtomicNumber - 1);
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		public IEnumerator<Compound> GetEnumerator()
		{
			return ((IEnumerable<Compound>)m_table).GetEnumerator();
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}