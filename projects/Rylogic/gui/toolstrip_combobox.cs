using System;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.gui
{
	/// <summary>
	/// A replacement for ToolStripComboBox that preserves the text selection across focus lost/gained
	/// and also doesn't throw a first chance exception when then the combo box data source is set to null</summary>
	public class ToolStripComboBox :System.Windows.Forms.ToolStripControlHost
	{
		public ToolStripComboBox() :this(string.Empty) {}
		public ToolStripComboBox(string name) :base(new pr.gui.ComboBox(), name)
		{
			ComboBox.TextChanged += (s,a) => OnTextChanged(a);
			ComboBox.TextUpdate += (s,a) => OnTextUpdate(a);
		}

		/// <summary>The hosted combobox</summary>
		public ComboBox ComboBox
		{
			get { return (ComboBox)Control; }
		}

		/// <summary>The items displayed in the combo box</summary>
		public ComboBox.ObjectCollection Items
		{
			get { return ComboBox.Items; }
		}

		/// <summary>Get/Set the selected item</summary>
		public int SelectedIndex
		{
			get { return ComboBox.SelectedIndex; }
			set { ComboBox.SelectedIndex = value; }
		}

		/// <summary>Raised when the selected index changes</summary>
		public event EventHandler SelectedIndexChanged
		{
			add { ComboBox.SelectedIndexChanged += value; }
			remove { ComboBox.SelectedIndexChanged -= value; }
		}

		/// <summary>The selected item</summary>
		public object SelectedItem
		{
			get { return ComboBox.SelectedItem; }
			set { ComboBox.SelectedItem = value; }
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public string SelectedText
		{
			get { return ComboBox.SelectedText; }
			set
			{
				//System.Diagnostics.Trace.Write("SelectedText: ");
				RestoreSelection();
				ComboBox.SelectedText = value;
			}
		}

		/// <summary>Gets/Sets the style of the combo box</summary>
		public ComboBoxStyle DropDownStyle
		{
			get { return ComboBox.DropDownStyle; }
			set { ComboBox.DropDownStyle = value; }
		}

		/// <summary>Get/Set the appearance of the combo box</summary>
		public FlatStyle FlatStyle
		{
			get { return ComboBox.FlatStyle; }
			set { ComboBox.FlatStyle = value; }
		}

		/// <summary>Restore the selection on focus gained</summary>
		protected override void OnGotFocus(EventArgs e)
		{
			//System.Diagnostics.Trace.Write("OnGotFocus: ");
			RestoreSelection();
			base.OnGotFocus(e);
			// Note, don't save on lost focus, the selection has already been reset to 0,0 by then
		}

		/// <summary>Update the selection whenever the text changes</summary>
		protected virtual void OnTextUpdate(EventArgs e)
		{
			//base.OnTextUpdate(e);
			//System.Diagnostics.Trace.Write("OnTextUpdate: ");
			SaveSelection();
		}

		/// <summary>Update the selection whenever the text changes</summary>
		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			//System.Diagnostics.Trace.Write("OnTextChanged: ");
			SaveSelection();
		}

		/// <summary>Save the selection after it has been changed by mouse selection</summary>
		protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
		{
			base.OnMouseUp(e);
			//System.Diagnostics.Trace.Write("OnMouseUp: ");
			SaveSelection();
		}

		/// <summary>Save the selection after it has been changed by key presses</summary>
		protected override void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
		{
			base.OnKeyUp(e);
			//System.Diagnostics.Trace.Write("OnKeyUp: ");
			SaveSelection();
		}

		/// <summary>Restore the selection</summary>
		private void RestoreSelection()
		{
			// Only allow selection setting for editable combo box styles
			if (ComboBox.DropDownStyle != System.Windows.Forms.ComboBoxStyle.DropDownList)
				Util.Dispose(ref m_selection_scope);
			//System.Diagnostics.Trace.WriteLine("Selection Restored: [{0},{1}] -> [{2},{3}]".Fmt(m_selection.Begini, m_selection.Sizei, SelectionStart, SelectionLength));
		}

		/// <summary>Save the current selection</summary>
		private void SaveSelection()
		{
			// Only allow selection setting for editable combo box styles
			if (ComboBox.DropDownStyle != System.Windows.Forms.ComboBoxStyle.DropDownList)
			{
				Util.Dispose(ref m_selection_scope);
				m_selection_scope = ComboBox.SelectionScope();
				//System.Diagnostics.Trace.WriteLine("Selection Saved: [{0},{1}]\n\t{2}".Fmt(m_selection.Begini, m_selection.Sizei, string.Join("\n\t", new StackTrace().GetFrames().Take(5).Select(x => x.GetMethod()))));
			}
		}

		/// <summary>Used to preserve the selection while the control doesn't have focus</summary>
		private Scope m_selection_scope;
	}
}
