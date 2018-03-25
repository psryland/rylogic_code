using System.ComponentModel;
using Rylogic.Extn;

namespace Rylogic.VSExtension
{
	/// <summary>Represents a set of patterns that all align together</summary>
	internal class AlignGroup
	{
		/// <summary>The name of the group</summary>
		public string Name { get; set; }

		/// <summary>The number leading whitespaces the group should have</summary>
		public int LeadingSpace { get; set; }

		/// <summary>The patterns belonging to the group</summary>
		public BindingList<AlignPattern> Patterns { get; set; }

		public AlignGroup() :this(string.Empty, 1) {}
		public AlignGroup(string name, int leading_space, params AlignPattern[] patterns)
		{
			Name = name;
			LeadingSpace = leading_space;
			Patterns = new BindingList<AlignPattern>{AllowNew = true, AllowRemove = true, AllowEdit = true, RaiseListChangedEvents = true};
			Patterns.AddRange(patterns);
		}
		public override string ToString()
		{
			return Name;
		}
	}
}
