using System.Linq;
using System.Windows.Controls;

namespace Rylogic.Gui.WPF
{
	public static class Menu_
	{
		/// <summary>Search the item's of this menu for a menu item with header text matching 'header_string'</summary>
		public static MenuItem? FindSubMenu(this MenuItem menu, string header_string)
		{
			foreach (var item in menu.Items.OfType<MenuItem>())
			{
				if (!(item.Header is string hdr) || hdr != header_string) continue;
				return item;
			}
			return null;
		}
		public static MenuItem? FindSubMenu(this ContextMenu menu, string header_string)
		{
			foreach (var item in menu.Items.OfType<MenuItem>())
			{
				if (!(item.Header is string hdr) || hdr != header_string) continue;
				return item;
			}
			return null;
		}
	}
}
