using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.container;
using pr.maths;

namespace EscapeVelocity
{
	// Explanation of the physics of electron attractivity:
	// It all has to do with how much positive charge from the nucleus the electrons in the outer shell experience (which is usually called Z-effective or Zeff),
	// which depends on the principle quantum numbers of the electrons involved. The octet rule occurs mainly because an electron around a nucleus will not perfectly
	// "shield" another electron from the nucleus's positive charge, especially if the shielding electron and the incoming electron have the same principle quantum number.
	// 2p electrons can't shield other 2p electrons very well (and 3p can't shield other 3p very well, etc.). It's kind of complicated, but I will explain it as best I can.
	// Consider a helium atom and a hyrogen atom (a proton). If I only give my He atom one electron and give my proton no electrons, both my He and my proton will have a charge
	// of +1. Since they have the same charge, you might think that an electron would be equally attracted to either one - but that's not the case. An electron is much more
	// strongly attracted to an He+ atom than to a H+ atom. This occurs because even though the He+ and the H+ have the same charge, the He has two protons and the electron
	// that's already present won't perfectly shield the incoming electron from one whole unit of positive charge. The result is that an electron coming into an He+ atom will
	// experience a positive charge that's something like +1.3 insted of just +1. How well an electron shields an outer electron from the nucleus depends on the principle
	// quantum number and angular momentum quantum number of the electrons involved. If an electron has the same n and l value as the electron that it's trying to shield,
	// it won't be able to shield very well.
	// Consider a neutral carbon atom: it has 6 protons and 6 electrons (2 1s electrons, 2 2s electrons, and 2 2p electrons). If I add a new electron to make a C- anion,
	// the new electron that I'm adding will think that the atom has a charge of around +0.6 because each of the 2p electrons that are already there can only shield another
	// 2p electron from about .7 units of charge. But if I want to add an electron to a F atom to make F-, now my additional electron will see a charge of something like +1.5,
	// since there are already 5 2p electrons present that each allow +0.3 charge to "bleed through" their coverage of the nucleus. If I want to add another electron to my F-,
	// now I will have to add a 3s electron, and the 2p electrons that are already there will shield the 3s electron much better than they can shield other 2p electrons. So the
	// first extra electron that you add to F will see a charge of around +1.5, while the second will see a charge close to -1.
	// That is the main reason why atoms are more stable if they can get to 8 electrons to make an octet; so long as you are filling up a partly-filled p orbital, positive charge
	// from the nucleus will be able to get through to attract the extra electrons. Once you have filled the p orbital completely, you now have to add to the next level s orbital,
	// to which very little extra charge from the nucleus can get through. There are also a few issues with electrons being lower in energy if there are a lot of other electrons
	// around with the same n, l, and Ms values that contribute to the octet rule, but it mainly has to do with charge and charge shielding.
	// For simplicity, assume full valence shells shield 100% of the charge, valence electrons shield 60%

	public class ChemLab
	{
		private readonly GameConstants m_consts;
		private readonly List<Element> m_elements; // The elements in the world
		private readonly CompoundCont m_compounds; // Every possible Compound combination

		/// <summary>An event raised whenever a discovery is made</summary>
		public event EventHandler<DiscoveryEventArg> Discovery;
		public class DiscoveryEventArg :EventArgs
		{
			// Whichever isn't null was discovered
			public Element Element;
			public Compound Compound;
		}
		private void RaiseDiscovery(Element element)
		{
			if (Discovery == null) return;
			Discovery(this, new DiscoveryEventArg{Element = element});
		}
		private void RaiseDiscovery(Compound compound)
		{
			if (Discovery == null) return;
			Discovery(this, new DiscoveryEventArg{Compound = compound});
		}

		public ChemLab(GameConstants consts)
		{
			m_consts = consts;

			// Populate the container of elements
			m_elements = new List<Element>(m_consts.ElementCount);
			for (int i = 1; i <= m_consts.ElementCount; ++i)
				m_elements.Add(new Element(i, m_consts));

			// Populate the container of materials. Generate every possible combination
			m_compounds = new CompoundCont(consts.ElementCount, TriTable.EType.Inclusive);
			for (int i = 0; i != consts.ElementCount; ++i)
			for (int j = i; j != consts.ElementCount; ++j)
			{
				var e1 = m_elements[i];
				var e2 = m_elements[j];
				m_compounds[e1, e2] = new Compound(e1, e2, m_consts);
			}

			// The properties of the elements that the player knows about
			Element.KnownElementProperties |= EElemProp.Existence;
			Element.KnownElementProperties |= EElemProp.Name;
			Element.KnownElementProperties |= EElemProp.MeltingPoint;
			Element.KnownElementProperties |= EElemProp.BoilingPoint;

			// Set the display order collection
			UpdateKnown();
		}

		/// <summary>The known elements, sorted by atomic number where known, otherwise alphabetically</summary>
		public List<IElementKnown> KnownElements { get; private set; }

		/// <summary>The known Compound, sorted into the display order</summary>
		public List<Compound> KnownMaterials { get; private set; } 

		///<summary>
		/// Update the display order of the elements based on what the player
		/// currently knowns about them. Order is atomic number, alphabetical</summary>
		private void UpdateKnown()
		{
			// Only the elements that are known to exist are visible
			KnownElements = m_elements.Where(x => (x.KnownProperties & EElemProp.Existence) != 0).Cast<IElementKnown>().ToList();
			KnownElements.Sort((lhs,rhs) =>
				{
					var elhs = (Element)lhs;
					var erhs = (Element)rhs;
					bool al = (elhs.KnownProperties & EElemProp.AtomicNumber) != 0;
					bool ar = (erhs.KnownProperties & EElemProp.AtomicNumber) != 0;
					if (al != ar) return al ? -1 : 1;
					if (al) return lhs.AtomicNumber.CompareTo(rhs.AtomicNumber);
					return string.Compare(elhs.Name.Fullname, erhs.Name.Fullname, StringComparison.Ordinal);
				});

			// Only materials that have been discovered are visible
			KnownMaterials = m_compounds.Where(x => x.Discovered).ToList();
			KnownMaterials.Sort((lhs,rhs) =>
				{
					return string.Compare(lhs.Fullname, rhs.Fullname, StringComparison.Ordinal);
				});
		}

		/// <summary>Called to 'discover' a new element</summary>
		public void DiscoverElement(int atomic_number)
		{
			var element = m_elements[atomic_number - 1];
			Debug.Assert(!Bit.AllSet((int)element.KnownProperties, (int)EElemProp.Existence), "Element already discovered");
			element.KnownProperties |= EElemProp.Existence;
			element.KnownProperties |= EElemProp.Name;

var e = new Element(atomic_number,m_consts);


			// Update any materials that are based on this element
			foreach (var m in m_compounds.Related(element))
				m.UpdateName();

			// We now know of it's existence, and it's been named
			UpdateKnown();
			RaiseDiscovery(element);
		}

		/// <summary>Called to 'discover' a Compound</summary>
		public void DiscoverCompound(int elem_atomic_num1, int elem_atomic_num2)
		{
			var e1 = m_elements[elem_atomic_num1 - 1];
			var e2 = m_elements[elem_atomic_num2 - 1];

var m = new Compound(e1,e2,m_consts);
m.CommonName = "common";

			var compound = m_compounds[e1, e2];
			Debug.Assert(!compound.Discovered, "Compound already discovered");
			
			compound.Discovered = true;
			compound.UpdateName();
			
			UpdateKnown();
			RaiseDiscovery(compound);
		}

		/// <summary>Returns a collection of the materials related to 'elem'</summary>
		public IEnumerable<Compound> RelatedMaterials(Element element)
		{
			int num = element.AtomicNumber;
			return KnownMaterials.Where(x => x.Elem1.AtomicNumber == num || x.Elem2.AtomicNumber == num);
		}

		public void DumpElements()
		{
			var csv = new CSVData();
			var header = new pr.container.CSVData.Row();
			header.Add("Name").Add("AtomicNumber").Add("ValenceElectrons").Add("ValenceHoles").Add("ValenceOrbitalRadius").Add("MolarMass").Add("Electronegativity").Add("Melting Point").Add("Boiling Point").Add("SolidDensity");
			csv.Add(header);
			foreach (var e in m_elements)
			{
				var row = new CSVData.Row();
				row.Add(e.Name.Fullname).Add(e.AtomicNumber).Add(e.ValenceElectrons).Add(e.ValenceHoles).Add(e.ValenceOrbitalRadius).Add(e.MolarMass).Add(e.Electronegativity).Add(e.MeltingPoint).Add(e.BoilingPoint).Add(e.SolidDensity);
				csv.Add(row);
			}
			csv.Save(@"d:\deleteme\elements.csv");
		}

		public void DumpCompounds()
		{
			var csv = new CSVData ();
			var header = new CSVData.Row();
			header.Add("Scientific Name").Add("Symbolic Name").Add("Enthalpy").Add("Ionicity").Add("Melting Point").Add("Boiling Point").Add("Strength").Add("Density");
			csv.Add(header);
			foreach (var c in m_compounds)
			{
				var row = new CSVData.Row();
				row.Add(c.ScientificName).Add(c.SymbolicName).Add(c.Enthalpy).Add(c.Ionicity).Add(c.MeltingPoint).Add(c.BoilingPoint).Add(c.Strength).Add(c.Density(0,0));
				csv.Add(row);
			}
			csv.Save(@"d:\deleteme\compounds.csv");
		}
	}
}
