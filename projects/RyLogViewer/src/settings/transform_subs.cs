using System;

namespace RyLogViewer
{
	public class SubNoChange :TransformSubstitutionBase
	{
		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public override Guid Guid { get { return new Guid("89CFA4F1-D0BA-4118-B3FD-13F56F91E611"); } }

		/// <summary>
		/// The name that appears in the transform column dropdown
		/// for this text transformation.</summary>
		public override string DropDownName { get { return "No Change"; }  }

		/// <summary>
		/// Returns the result of applying this text transform to 'captured_text'.
		/// This method provides the functionality of the text transform and should
		/// be efficiently implemented.</summary>
		public override string Result(string captured_text) { return captured_text; }
	}

	public class SubToLower :TransformSubstitutionBase
	{
		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public override Guid Guid { get { return new Guid("6766D085-E345-413A-B781-CEF7B3400A4F"); } }

		/// <summary>
		/// The name that appears in the transform column dropdown
		/// for this text transformation.</summary>
		public override string DropDownName { get { return "Lower Case"; } }

		/// <summary>
		/// Returns the result of applying this text transform to 'captured_text'.
		/// This method provides the functionality of the text transform and should
		/// be efficiently implemented.</summary>
		public override string Result(string elem) { return elem.ToLower(); }
	}

	public class SubToUpper :TransformSubstitutionBase
	{
		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public override Guid Guid { get { return new Guid("F6A4E40B-21A2-4DFC-A299-32F244718DC1"); } }

		/// <summary>
		/// The name that appears in the transform column dropdown
		/// for this text transformation.</summary>
		public override string DropDownName { get { return "Upper Case"; } }

		/// <summary>
		/// Returns the result of applying this text transform to 'captured_text'.
		/// This method provides the functionality of the text transform and should
		/// be efficiently implemented.</summary>
		public override string Result(string elem) { return elem.ToUpper(); }
	}
}
