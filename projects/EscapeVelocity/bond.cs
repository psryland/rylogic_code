using System;
using System.Collections.Generic;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace EscapeVelocity
{
	public class Bond
	{
		/// <summary>The permutation identifier assocated with the bond (one of EPerm2/4)</summary>
		public int Perm { get; private set; }

		/// <summary>The base single-bond strength</summary>
		public double BaseStrength { get; private set; }

		/// <summary>The number of electrons shared in the bond ie. single bond, double bond, etc</summary>
		public int Order { get; private set; }

		/// <summary>The difference in electronegativity between the bonded elements</summary>
		public double Ionicity { get; private set; }

		/// <summary>The number of bonds of this permutation (per order)</summary>
		public int[] Count { get; private set; }

		public override string ToString() { return "[Bond] Perm:{0} BaseStrength:{1} Order:{2} Counts:({3}) Ionicity:{4} Enthalpy:{5}".Fmt(EPerm.ToString(Perm),BaseStrength,Order,string.Join(",",Count),Ionicity,Enthalpy); }

		public Bond() { Count = new int[0]; }
		public Bond(int perm, Element elem1, Element elem2, GameConstants consts)
		{
			Perm = perm;

			// The electro-static force between two charged objects is F = k*Q*q/r²
			// Assume elem1 and elem2 are separated such that their outermost electron shells just touch
			// The total bond strength is the sum of the electro static forces:
			//  P1 - P2 (repulsive), E1 - E2 (repulsive), P1 - E2 (attractive), P2 - E1 (attractive)
		
			// Assuming ionic/covalent bonding only, P1 and P2 can share electrons in their outer orbital.
			// The proton charges are the effective (Zeff) positive charge, the electron charge is
			// the charge of the maximum number of electrons that can be borrowed when trying to fill the
			// the outer orbital.

			// Find the number of electrons available to be shared between elem1 and elem2 in a covalent bond
			var shareable = Shareable(elem1, elem2);

			// Constant to make the strength reasonable numbers
			var scaler = 1e11 * consts.CoulombConstant * Math_.Sqr(consts.ElectronCharge) / Math_.Sqr(10e-12);

			// Assume these electrons sit between the two atoms, calculate the electro-static
			// force between the electrons and each element
			var strength = 0.0;
			Order = 0;// Order is the number of electrons actually shared in the bond
			for (int i = 0; i != shareable; ++i)
			{
				// This is the charge experienced by each element in relation to shared electron 'i'
				var charge1 = elem1.EffectiveCharge(consts, elem1.AtomicNumber + i + 1);
				var charge2 = elem2.EffectiveCharge(consts, elem2.AtomicNumber + i + 1);
				
				// If either element experiences a negative charge, then this shared election does
				// not contribute to the bond and no further shared electrons will either
				if (charge1 < 0 || charge2 < 0)
					break;

				// Accumulate the attractive forces
				strength += scaler * charge1 * 1.0 / Math_.Sqr(elem1.ValenceOrbitalRadius);
				strength += scaler * charge2 * 1.0 / Math_.Sqr(elem2.ValenceOrbitalRadius);
				++Order;
			}
			{// Remove the effective charge repulsive force
				var charge1 = elem1.EffectiveCharge(consts, elem1.AtomicNumber + Order);
				var charge2 = elem2.EffectiveCharge(consts, elem2.AtomicNumber + Order);
				strength -= scaler * charge1 * charge2 / Math_.Sqr(elem1.ValenceOrbitalRadius + elem2.ValenceOrbitalRadius);
			}

			// Scale the strength by 'Order'
			// Relationship (got from Carbon) is Strength = BaseStrength * Order ^ 0.8
			Count = new int[Math.Max(Order,1)];
			BaseStrength = strength * Math.Pow(Order, -0.8);

			// Ionicity > ~1.8 (Paulie scale) results in an ionic bond (as opposed to covalent bond).
			// The atoms of covalent materials are bound tightly to each other in stable molecules, but those
			// molecules are generally not very strongly attracted to other molecules in the Compound. On the
			// other hand, the atoms (ions) in ionic materials show strong attractions to other ions in their
			// vicinity. This generally leads to low melting points for covalent solids, and high melting points
			// for ionic solids.
			Ionicity = Math.Abs(elem2.Electronegativity - elem1.Electronegativity);
		}

		/// <summary>Return the total count of bonds of this type</summary>
		public int TotalCount
		{
			get
			{
				int sum = 0;
				for (int i = 0; i != Count.Length; ++i)
					sum += Count[i];
				return sum;
			}
		}

		/// <summary>Return the total enthalpy of all of the bonds of this type</summary>
		public double Enthalpy
		{
			get
			{
				double sum = 0.0;
				for (int i = 0; i != Count.Length; ++i)
					sum += Count[i] * Strength(i+1);
				return sum;
			}
		}

		/// <summary>Returns an array of the bond order distribution given 'valence_electrons' available electrons</summary>
		public int[] Distribution(int valence_electrons, int[] count = null)
		{
			count = count ?? new int[Order];
			for (int i = Order; i-- != 0;)
			{
				int c = valence_electrons / (i+1);
				valence_electrons -= c * (i+1);
				count[i] += c;
			}
			return count;
		}

		/// <summary>Add to Count[] for enough bonds to use 'valence_electrons', prefering higher order bonds first</summary>
		public void Make(int valence_electrons)
		{
			Distribution(valence_electrons, Count);
		}

		/// <summary>Return the strength of the bond of a given order</summary>
		public double Strength(int order)
		{
			return BaseStrength * Math.Pow(order, 0.8);
		}

		/// <summary>Returns the maximum number of electrons that can be shared between 'elem1' and 'elem2'</summary>
		public static int Shareable(Element elem1, Element elem2)
		{
			// Either elem2 can fill elem1's holes or visa versa.
			// The preferred option will be the one that involves the least number of electrons
			// being shared because effectivity, one element is ionising the other
			return Math.Min(Math.Min(elem1.ValenceHoles, elem2.ValenceElectrons), Math.Min(elem2.ValenceHoles, elem1.ValenceElectrons));
		}

		/// <summary>Returns details of the bonds for all permutations of elem1,elem2</summary>
		public static Bond[] Permutations(Element elem1, Element elem2, GameConstants consts)
		{
			var bonds = new Bond[EPerm.Perm2Count];
			bonds[EPerm.AA] = new Bond(EPerm.AA, elem1, elem1, consts);
			bonds[EPerm.AB] = new Bond(EPerm.AB, elem1, elem2, consts);
			bonds[EPerm.BB] = new Bond(EPerm.BB, elem2, elem2, consts);
			return bonds;
		}

		/// <summary>Returns details of the bonds for all permutations of mat1,mat2</summary>
		public static Bond[] Permutations(Compound mat1, Compound mat2, GameConstants consts)
		{
			var bonds = new Bond[EPerm.Perm4Count];
			var a = mat1.Elem1;
			var b = mat1.Elem2;
			var c = mat2.Elem1;
			var d = mat2.Elem2;

			bonds[EPerm.AA] = new Bond(EPerm.AA, a,a,consts);
			bonds[EPerm.AB] = new Bond(EPerm.AB, a,b,consts);
			bonds[EPerm.AC] = new Bond(EPerm.AC, a,c,consts);
			bonds[EPerm.AD] = new Bond(EPerm.AD, a,d,consts);
			bonds[EPerm.BB] = new Bond(EPerm.BB, b,b,consts);
			bonds[EPerm.BC] = new Bond(EPerm.BC, b,c,consts);
			bonds[EPerm.BD] = new Bond(EPerm.BD, b,d,consts);
			bonds[EPerm.CC] = new Bond(EPerm.CC, c,c,consts);
			bonds[EPerm.CD] = new Bond(EPerm.CD, c,d,consts);
			bonds[EPerm.DD] = new Bond(EPerm.DD, d,d,consts);
			return bonds;
		}

		/// <summary>Comparer for bond strengths, -ve means stronger</summary>
		public static int CompareStrength(Bond lhs, Bond rhs)
		{
			return CompareStrength(lhs, lhs.Order, rhs, rhs.Order);
		}
		public static int CompareStrength(Bond lhs, int lorder, Bond rhs, int rorder)
		{
			// If the strengths are equal, sort by permutation number
			var s1 = lhs.Strength(lorder);
			var s2 = rhs.Strength(rorder);
			if (Math.Abs(s1 - s2) < double.Epsilon) return -lhs.Perm.CompareTo(rhs.Perm);
			return -s1.CompareTo(s2);
		}
	}

	public struct BondOrder
	{
		public Bond Bond;
		public int  Order;
	}

	public static class BondExtn
	{
		/// <summary>Return the strongest bond in this array of Bond objects</summary>
		public static Bond Strongest(this Bond[] bonds)
		{
			if (bonds.Length == 0)
				return null;
			
			int strongest = 0;
			for (int i = 1; i < bonds.Length; ++i)
			{
				if (Bond.CompareStrength(bonds[i], bonds[strongest]) >= 0) continue;
				strongest = i;
			}
			return bonds[strongest];
		}

		/// <summary>Returns the total enthalpy for all bonds in this array</summary>
		public static double Enthalpy(this Bond[] bonds)
		{
			if (bonds.Length == 0)
				return 0.0;

			double sum = 0.0;
			foreach (var b in bonds)
				sum += b.Enthalpy;
			return sum;
		}

		/// <summary>Returns an array of bonds and order listed in order from strongest to weakest</summary>
		public static BondOrder[] StrengthOrder(this IEnumerable<Bond> bonds)
		{
			// Generate a list containing all bonds for all available orders
			var order = new List<BondOrder>();
			foreach (var b in bonds)
				for (int i = 0; i != b.Order; ++i)
					order.Add(new BondOrder{Bond = b, Order = i+1});

			// Sort the order list from strongest to weakest
			order.Sort((l,r) => Bond.CompareStrength(l.Bond, l.Order, r.Bond, r.Order));
			return order.ToArray();
		}
	}
}