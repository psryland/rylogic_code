using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using Rylogic.Extn;

namespace Rylogic.VSExtension
{
	/// <summary>Represents a set of patterns that all align together</summary>
	internal class AlignGroup
	{
		public AlignGroup()
		{
			Name = string.Empty;
			LeadingSpace = 1;
			Patterns = new BindingList<AlignPattern> { AllowNew = true, AllowRemove = true, AllowEdit = true, RaiseListChangedEvents = true };
		}
		public AlignGroup(string name, int leading_space, params AlignPattern[] patterns)
			:this()
		{
			Name = name;
			LeadingSpace = leading_space;
			Patterns.AddRange(patterns);
		}
		public AlignGroup(XElement node)
			:this()
		{
			Name = node.Element(nameof(Name)).As<string>();
			LeadingSpace = node.Element(nameof(LeadingSpace)).As<int>();
			Patterns.AddRange(node.Elements(nameof(Patterns), nameof(AlignPattern)).Select(x => x.As<AlignPattern>()));
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(Name), Name, false);
			node.Add2(nameof(LeadingSpace), LeadingSpace, false);
			node.Add2(nameof(Patterns), nameof(AlignPattern), Patterns, false);
			return node;
		}

		/// <summary>The name of the group</summary>
		public string Name { get; set; }

		/// <summary>The number leading white spaces the group should have</summary>
		public int LeadingSpace { get; set; }

		/// <summary>The patterns belonging to the group</summary>
		public BindingList<AlignPattern> Patterns { get; set; }

		/// <summary></summary>
		public override string ToString()
		{
			return Name;
		}
	}
}
