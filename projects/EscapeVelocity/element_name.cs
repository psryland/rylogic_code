namespace EscapeVelocity
{
	// The following steps take you through the process of building a chemical name, using compound XaYb as an example:
	// 1 Is X hydrogen?
	//  If so, the compound is probably an acid and may use a common name. If X isn't hydrogen, proceed to Step 2.
	// 2 Is X a nonmetal or a metal?
	//  If X is a nonmetal, then the compound is molecular. For molecular compounds, use numeric prefixes before each
	//  element's name to specify the number of each element. If there's only one atom of element X, no prefix is
	//  required before the name of X. Use the suffix –ide after the element name for Y. If X is a metal, then the
	//  compound is ionic; proceed to Step 3.
	// 3 Is X a metal that has variable charge?
	//  If X has a variable charge (often, these are group B metals), you must specify its charge within the compound
	//  by using a Roman numeral within parentheses between the element names for X and Y. For example, use (II) for
	//  Fe2+ and (III) for Fe3+. Proceed to Step 4.
	// 4 Is Y a polyatomic ion?
	//  If Y is a polyatomic ion, use the appropriate name for that ion. Usually, polyatomic anions have an ending
	//  of –ate or –ite (corresponding to related ions that contain more or less oxygen, respectively). Another common
	//  ending for polyatomic ions is –ide, as in hydroxide (OH–) and cyanide (CN–). If Y is not a polyatomic ion,
	//  use the suffix –ide after the name of Y.

	public class ElementName
	{
		///<summary>
		/// Full element name (all lower case)
		/// e.g. hydrogen, sodium, iron, carbon, oxygen, sulfur, fluorine, argon</summary>
		public readonly string Fullname;

		/// <summary>Symbol e.g  H, Na, Fe, C, O, S, Ar</summary>
		public readonly string Symbol;

		/// <summary>
		/// The suffix form of the element name (only really needed for nonmetals)
		/// will have one of 'ide','ite','ate' appended
		/// e.g. hydr, sodim, ferr, carb, ox, sul, fluor, argon</summary>
		public readonly string SuffixForm;
	
		public ElementName(string fullname, string symbol, string suffix_form)
		{
			Fullname    = fullname;
			Symbol      = symbol;
			SuffixForm = suffix_form;
		}

		public override string ToString()
		{
			return Fullname + " (" + Symbol + ")";
		}
		/// <summary>Return an element name for an known element</summary>
		public static ElementName Unknown
		{
			get { return new ElementName("unnamed", "??", "??"); }
		}
	}
}