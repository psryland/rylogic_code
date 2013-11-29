using System.ComponentModel;
using pr.extn;

namespace Rylogic.VSExtension
{
	/// <summary>Represents a set of patterns at all align</summary>
	internal class AlignGroup
	{
		/// <summary>The name of the group</summary>
		public string Name { get; set; }

		/// <summary>True if the aligned group should have a leading whitespace</summary>
		public bool LeadingSpace { get; set; }

		/// <summary>The patterns belonging to the group</summary>
		public BindingList<AlignPattern> Patterns { get; set; }

		public AlignGroup() :this(string.Empty, true) {}
		public AlignGroup(string name, bool leading_space, params AlignPattern[] patterns)
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
