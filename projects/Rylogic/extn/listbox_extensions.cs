using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pr.extn
{
	public static class ListBoxExtensions
	{
		/// <summary>Listbox select all implementation.</summary>
		public static void SelectAll(this ListBox lb)
		{
			if (lb.SelectionMode == SelectionMode.MultiSimple ||
				lb.SelectionMode == SelectionMode.MultiExtended)
				Enumerable.Range(0, lb.Items.Count).ForEach(i => lb.SetSelected(i, true));
		}

		/// <summary>Listbox select none implementation.</summary>
		public static void SelectNone(this ListBox lb)
		{
			Enumerable.Range(0, lb.Items.Count).ForEach(i => lb.SetSelected(i, false));
		}

		/// <summary>ListBox copy implementation. Returns true if something was added to the clip board</summary>
		public static bool Copy(this ListBox lb)
		{
			var d = string.Join("\n", lb.SelectedItems.Cast<object>().Select(x => x.ToString()));
			if (!d.HasValue()) return false;
			Clipboard.SetDataObject(d);
			return true;
		}

		/// <summary>Select all rows. Attach to the KeyDown handler</summary>
		public static void SelectAll(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var lb = (ListBox)sender;
			if (!e.Control || e.KeyCode != Keys.A) return;
			SelectAll(lb);
			e.Handled = true;
		}

		/// <summary>Copy selected items to the clipboard. Attach to the KeyDown handler</summary>
		public static void Copy(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var lb = (ListBox)sender;
			if (!e.Control || e.KeyCode != Keys.C) return;
			if (!Copy(lb)) return;
			e.Handled = true;
		}
	}
}
