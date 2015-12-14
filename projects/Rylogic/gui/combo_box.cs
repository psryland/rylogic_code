using System;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;

namespace pr.gui
{
	/// <summary>Replacement for the forms combo box that doesn't throw a first chance exception when the data source is empty</summary>
	public class ComboBox :System.Windows.Forms.ComboBox
	{
		/// <summary>The index of the selected item in the drop down list</summary>
		public override int SelectedIndex
		{
			get { return base.SelectedIndex; }
			set
			{
				if (value < 0 || value >= Items.Count) return;
				base.SelectedIndex = value;
			}
		}

		/// <summary>Set the selected item.</summary>
		public new object SelectedItem
		{
			get { return base.SelectedItem; }
			set
			{
				base.SelectedItem = value;

				// For drop down lists, if 'value' isn't in the collection then
				// then the call to SelectedItem is ignored, silently.
				if (!Equals(SelectedItem, value))
					UnknownItemSelected.Raise(this, new UnknownItemSelectedEventArgs(value));
			}
		}

		/// <summary>
		/// Raised whenever an attempt to change the selected item to an unknown item is made.
		/// I.e. whenever the combo box ignores a call to 'SelectedItem = value'</summary>
		public event EventHandler<UnknownItemSelectedEventArgs> UnknownItemSelected;
		public class UnknownItemSelectedEventArgs :EventArgs
		{
			public UnknownItemSelectedEventArgs(object unknown_item)
			{
				UnknownItem =  unknown_item;
			}

			/// <summary>The item that was attempted as the selected item</summary>
			public object UnknownItem { get; private set; }
		}

		/// <summary>Get/Set the selected text</summary>
		public new string SelectedText
		{
			get { return base.SelectedText; }
			set
			{
				if (PreserveSelectionThruFocusChange && !Focused)
					RestoreSelection();

				base.SelectedText = value;
			}
		}

		/// <summary>Get/Set the start of the selection range</summary>
		public new int SelectionStart
		{
			get { return base.SelectionStart; }
			set
			{
				base.SelectionStart = value;
				if (PreserveSelectionThruFocusChange)
					SaveSelection();
			}
		}

		/// <summary>Get/Set the length of the selection range</summary>
		public new int SelectionLength
		{
			get { return base.SelectionLength; }
			set
			{
				base.SelectionLength = value;
				if (PreserveSelectionThruFocusChange)
					SaveSelection();
			}
		}

		/// <summary>A smarter set text that does sensible things with the selection position</summary>
		public void SetText(string text)
		{
			if (PreserveSelectionThruFocusChange)
				RestoreSelection();

			var idx = SelectionStart;
			SelectedText = text;
			SelectionStart = idx + text.Length;
		}

		/// <summary>A smarter set text that does sensible things with the selection position</summary>
		public void AppendText(string text)
		{
			if (PreserveSelectionThruFocusChange)
				RestoreSelection();

			var carot_at_end = SelectionStart == Text.Length && SelectionLength == 0;
			if (carot_at_end)
			{
				SelectedText = text;
				SelectionStart = Text.Length;
			}
			else
			{
				using (this.SelectionScope())
					AppendText(text);
			}
		}

		/// <summary>Set to true to have the text selection preserved while the combo doesn't have focus</summary>
		public bool PreserveSelectionThruFocusChange { get; set; }

		/// <summary>Restore the selection on focus gained</summary>
		protected override void OnGotFocus(EventArgs e)
		{
			// Note, don't save on lost focus, the selection has already been reset to 0,0 by then
			if (PreserveSelectionThruFocusChange)
				RestoreSelection();
	
			base.OnGotFocus(e);
		}

		/// <summary>Update the selection whenever the text changes</summary>
		protected override void OnTextUpdate(EventArgs e)
		{
			base.OnTextUpdate(e);
			if (PreserveSelectionThruFocusChange)
				SaveSelection();
		}

		///// <summary>Update the selection whenever the text changes</summary>
		//protected override void OnTextChanged(EventArgs e)
		//{
		//	base.OnTextChanged(e);
		//	if (PreserveSelectionThruFocusChange)
		//		SaveSelection();
		//}

		/// <summary>Save the selection after it has been changed by mouse selection</summary>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (PreserveSelectionThruFocusChange)
				SaveSelection();
		}

		/// <summary>Save the selection after it has been changed by key presses</summary>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (PreserveSelectionThruFocusChange)
				SaveSelection();
		}

		/// <summary>Restore the selection</summary>
		public void RestoreSelection()
		{
			// Only allow selection setting for editable combo box styles
			if (DropDownStyle != ComboBoxStyle.DropDownList && m_selection != null)
			{
				System.Diagnostics.Trace.WriteLine("Restoring Selection: [{0},{1}]".Fmt(m_selection.Value.Begini, m_selection.Value.Sizei));
				Select(m_selection.Value.Begini, m_selection.Value.Sizei);
				m_selection = null;
				System.Diagnostics.Trace.WriteLine("Selection is now: [{0},{1}]".Fmt(SelectionStart, SelectionLength));
			}
		}

		/// <summary>Save the current selection</summary>
		public void SaveSelection()
		{
			// Only allow selection setting for editable combo box styles
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				m_selection = Range.FromStartLength(SelectionStart, SelectionLength);
				System.Diagnostics.Trace.WriteLine("Selection Saved: [{0},{1}]\n\t{2}".Fmt(m_selection.Value.Begini, m_selection.Value.Sizei,
					string.Join("\n\t", new System.Diagnostics.StackTrace().GetFrames().Take(5).Select(x => x.GetMethod()))));
			}
		}

		/// <summary>Used to preserve the selection while the control doesn't have focus</summary>
		private Range? m_selection; // don't use a scope, because we save selection more than restoring it and disposed old scope will restore the selection.
	}
}
