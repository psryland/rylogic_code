using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Rylogic.Extn;

namespace Rylogic.Gui.WinForms
{
	/// <summary>Replacement for the forms list box that doesn't throw a first chance exception when the data source is empty</summary>
	public class ListBox :System.Windows.Forms.ListBox
	{
		public ListBox() :base()
		{
			m_last_hovered_item = null;
		}

		/// <summary>Set the draw mode for the list box</summary>
		public override DrawMode DrawMode
		{
			get { return base.DrawMode; }
			set
			{
				base.DrawMode = value;

				// Set some control styles on owner drawn mode to fix the flicker problems
				if (value == DrawMode.OwnerDrawFixed || value == DrawMode.OwnerDrawVariable)
				{
					SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
					SetStyle(ControlStyles.ResizeRedraw, true);
					SetStyle(ControlStyles.UserPaint, true);
				}
			}
		}

		/// <summary>Override OnPaint for owner drawn mode, to stop the flicker</summary>
		protected override void OnPaint(PaintEventArgs e)  
		{
			// Fill the clip area
			var region = new Region(e.ClipRectangle);  
			e.Graphics.FillRegion(new SolidBrush(BackColor), region);

			if (Items.Count == 0)
			{
				base.OnPaint(e);
				return;
			}

			if (DrawMode == DrawMode.OwnerDrawFixed || DrawMode == DrawMode.OwnerDrawVariable)
			{
				// Owner-Draw each item
				for (int i = 0; i < Items.Count; ++i)  
				{
					var rect = GetItemRectangle(i);  
					if (!e.ClipRectangle.IntersectsWith(rect))
						continue;

					var state = 
						(SelectionMode == SelectionMode.One && SelectedIndex == i) ||
						(SelectionMode == SelectionMode.MultiSimple && SelectedIndices.Contains(i)) ||
						(SelectionMode == SelectionMode.MultiExtended && SelectedIndices.Contains(i))
						? DrawItemState.Selected : DrawItemState.Default;
					OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, rect, i, state, ForeColor, BackColor));  
					region.Complement(rect);
				}
			}
			base.OnPaint(e);
		}

		/// <summary>The property of the data bound items to display</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string DisplayProperty
		{
			get { return m_impl_disp_prop; }
			set
			{
				if (m_impl_disp_prop == value) return;
				m_impl_disp_prop = value;
				m_disp_prop = null;
			}
		}
		private string m_impl_disp_prop;
		private PropertyInfo m_disp_prop;

		/// <summary>Display using the 'DisplayProperty' if specified</summary>
		protected override void OnFormat(ListControlConvertEventArgs e)
		{
			if (DisplayProperty.HasValue())
			{
				m_disp_prop = m_disp_prop ?? e.ListItem.GetType().GetProperty(DisplayProperty, BindingFlags.Public|BindingFlags.Instance);
				e.Value = m_disp_prop.GetValue(e.ListItem).ToString();
			}
			else
			{
				base.OnFormat(e);
			}
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
		public event EventHandler<ItemEventArgs> HoveredItemChanged;
		protected virtual void OnHoveredItemChanged(ItemEventArgs args)
		{
			HoveredItemChanged?.Invoke(this, args);
		}
		private object m_last_hovered_item;

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
				OnHoveredItemChanged(new ItemEventArgs(idx, hovered_item));
			}
		}

		/// <summary>Raised when an item is double clicked</summary>
		public event EventHandler<EventArgs> ItemDoubleClicked;
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			var idx = IndexFromPoint(e.Location);
			if (idx >= 0 && idx < Items.Count)
				ItemDoubleClicked?.Invoke(this, new ItemEventArgs(idx, Items[idx]));
		}

		/// <summary>Event args for events that involve an item</summary>
		public class ItemEventArgs :EventArgs
		{
			public ItemEventArgs(int index, object item)
			{
				Index = index;
				Item = item;
			}

			/// <summary>The index of the hovered item</summary>
			public int Index { get; private set; }

			/// <summary>The Item under the mouse</summary>
			public object Item { get; private set; }
		}
	}
}
