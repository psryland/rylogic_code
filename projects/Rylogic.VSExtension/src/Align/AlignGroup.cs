using System.ComponentModel;
using pr.extn;

namespace Rylogic.VSExtension
{
	/// <summary>Represents a set of patterns at all align</summary>
	public class AlignGroup
	{
		/// <summary>The name of the group</summary>
		public string Name { get; set; }

		/// <summary>The patterns belonging to the group</summary>
		public BindingList<AlignPattern> Patterns { get; set; }

		public AlignGroup()
		{
			Name = string.Empty;
			Patterns = new BindingList<AlignPattern>{AllowNew = true, AllowRemove = true, AllowEdit = true, RaiseListChangedEvents = true};
		}
		public AlignGroup(string name, params AlignPattern[] patterns)
		{
			Name = name;
			Patterns = new BindingList<AlignPattern>{AllowNew = true, AllowRemove = true, AllowEdit = true, RaiseListChangedEvents = true};
			Patterns.AddRange(patterns);
		}
		public override string ToString()
		{
			return Name;
		}
	}
}
