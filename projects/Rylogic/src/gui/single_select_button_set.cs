using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;

namespace Rylogic.Gui
{
	/// <summary>A helper for creating sets of buttons for which only one can be checked</summary>
	public class SingleSelectButtonSet :Component ,IDisposable
	{
		public SingleSelectButtonSet()
		{
			m_items = new List<ToolStripButton>();
		}
		protected override void Dispose(bool disposing)
		{
			if (m_items == null) return;
			foreach (var b in m_items)
				b.Click -= HandleClicked;
			m_items = null;
		}

		/// <summary>Readonly access to the buttons</summary>
		public IEnumerable<ToolStripButton> Items { get { return m_items; } }
		private List<ToolStripButton> m_items;

		/// <summary>Add/Remove a button from the set</summary>
		public void Add(ToolStripButton btn)
		{
			btn.CheckOnClick = false;
			btn.Click += HandleClicked;
			m_items.Add(btn);
		}
		public void Remove(ToolStripButton btn)
		{
			btn.Click -= HandleClicked;
			m_items.Remove(btn);
		}

		/// <summary>The currently selected button in the set</summary>
		public ToolStripButton Selected
		{
			get { return m_impl_selected; }
			set
			{
				if (m_impl_selected == value) return;
				if (m_items.IndexOf(value) == -1) throw new Exception("Can only set Selected to an item in this collection");
				var previous = m_impl_selected;
				m_impl_selected = value;
				SelectedChanged.Raise(this, new SelectedChangedEventArgs(value, previous));
			}
		}
		private ToolStripButton m_impl_selected;

		/// <summary>Raised whenever the selected button changes</summary>
		public event EventHandler<SelectedChangedEventArgs> SelectedChanged;
		public class SelectedChangedEventArgs :EventArgs
		{
			/// <summary>The button that was selected</summary>
			public ToolStripButton Button { get; private set; }

			/// <summary>The button that was previously selected</summary>
			public ToolStripButton Previous { get; private set; }

			public SelectedChangedEventArgs(ToolStripButton btn, ToolStripButton prev)
			{
				Button = btn;
				Previous = prev;
			}
		}

		/// <summary>Handle a button in the set being clicked</summary>
		private void HandleClicked(object sender, EventArgs e)
		{
			// Uncheck all buttons except 'sender'
			foreach (var b in m_items.OfType<ToolStripButton>())
				b.Checked = ReferenceEquals(b, sender);

			Selected = (ToolStripButton)sender;
		}
	}
}
