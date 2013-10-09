using RyLogViewer;

namespace ExamplePlugin
{
	/// <summary>A substitution type that converts elements to lower case</summary>
	[TransformSubstitution]
	public class ExampleSubstitution :TransformSubstitutionBase
	{
		/// <summary>The name of the substitution (must be unique)</summary>
		public override string Name { get { return "Plugin-ExampleSubstitution"; } }

		/// <summary>Returns 'elem' transformed</summary>
		public override string Result(string elem)
		{
			return "To create plugins that provide substitutions, see help";
		}
	}
}
