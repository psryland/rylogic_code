using System.Collections.Generic;

namespace Rylogic.Gui.WinForms
{
	public interface IFeatureTreeItem
	{
		/// <summary>The name of the feature displayed in the tree</summary>
		string Name { get; }

		/// <summary>All children of this feature</summary>
		IEnumerable<IFeatureTreeItem> Children { get; }

		/// <summary>A flag indicating when the feature is checked</summary>
		bool Allowed { get; set; }
	}
}
