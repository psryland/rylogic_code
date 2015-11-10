using System;
using System.Windows.Forms;
using pr.extn;

namespace pr.gui
{
	/// <summary>Replacement for the forms list box that doesn't throw a first chance exception when the data source is empty</summary>
	public class ListBox :System.Windows.Forms.ListBox
	{
		public ListBox() :base()
		{
			m_last_hovered_item = null;
		}

		/// <summary>Gets or Sets the zero-based index of the currently selected item. Ignores indices outside the value range</summary>
		public override int SelectedIndex
		{
			get { return base.SelectedIndex; }
			set
			{
				if (value < 0 || value >= Items.Count) return;
				base.SelectedIndex = value;
			}
		}

		/// <summary>Raised whenever the mouse moves over an item in the list</summary>
		public event EventHandler<HoveredItemEventArgs> HoveredItemChanged;
		protected virtual void OnHoveredItemChanged(HoveredItemEventArgs args)
		{
			HoveredItemChanged.Raise(this, args);
		}
		public class HoveredItemEventArgs :EventArgs
		{
			public HoveredItemEventArgs(int index, object item)
			{
				Index = index;
				Item = item;
			}

			/// <summary>The index of the hovered item</summary>
			public int Index { get; private set; }

			/// <summary>The Item under the mouse</summary>
			public object Item { get; private set; }
		}

		/// <summary>Use mouse move to update the hovered item</summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var pos = e.Location;
			int idx = IndexFromPoint(pos); // Returns 65535 sometimes (i.e. (short)-1)
			var hovered_item = idx >= 0 && idx < Items.Count ? Items[idx] : null;
			if (m_last_hovered_item != hovered_item)
			{
				m_last_hovered_item = hovered_item;
				OnHoveredItemChanged(new HoveredItemEventArgs(idx, hovered_item));
			}
		}
		private object m_last_hovered_item;
	}
}
