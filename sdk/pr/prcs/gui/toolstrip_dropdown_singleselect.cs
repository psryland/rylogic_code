using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pr.gui
{
	public class ToolStripDropDownSingleSelect :ToolStripDropDown
	{
		/// <summary>Get the menu items in the drop down list</summary>
		public IEnumerable<ToolStripMenuItem> MenuItems
		{
			get { return Items.OfType<ToolStripMenuItem>(); }
		}

		/// <summary>Return the item currently selected in the drop down</summary>
		public ToolStripItem Selected
		{
			get { return MenuItems.FirstOrDefault(x => x.Checked); }
			set
			{
				foreach (var item in MenuItems)
					item.Checked = ReferenceEquals(item,value);
			}
		}

		protected override void OnItemClicked(ToolStripItemClickedEventArgs e)
		{
			base.OnItemClicked(e);
			Selected = e.ClickedItem;
		}
	}
}
