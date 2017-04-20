using System;
using RyLogViewer;

namespace RyLogViewer.ExamplePlugin
{
	/// <summary>A example transform substitution, accessed via the Options->Transforms tab</summary>
	[pr.common.Plugin(typeof(ITransformSubstitution))]
	public class ExampleSubstitution :TransformSubstitutionBase
	{
		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public override Guid Guid { get { return new Guid("4E00B263-3F7B-4F65-84D5-5FE6BCE20045"); } }

		/// <summary>The name of the substitution (must be unique)</summary>
		public override string DropDownName { get { return "Reverse"; } }

		/// <summary>Returns 'elem' transformed</summary>
		public override string Result(string elem)
		{
			var arr = elem.ToCharArray();
			Array.Reverse(arr);
			return new string(arr);
		}
	}
}
