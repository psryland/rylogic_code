namespace RyLogViewer
{
	public class SubNoChange :TransformSubstitutionBase
	{
		/// <summary>The name of the substitution (must be unique)</summary>
		public override string Name { get { return "No Change"; } }
	}

	public class SubToLower :TransformSubstitutionBase
	{
		/// <summary>The name of the substitution (must be unique)</summary>
		public override string Name { get { return "Lower Case"; } }

		/// <summary>Returns 'elem' transformed</summary>
		public override string Result(string elem) { return elem.ToLower(); }
	}

	public class SubToUpper :TransformSubstitutionBase
	{
		/// <summary>The name of the substitution (must be unique)</summary>
		public override string Name { get { return "Upper Case"; } }

		/// <summary>Returns 'elem' transformed</summary>
		public override string Result(string elem) { return elem.ToUpper(); }
	}
}
