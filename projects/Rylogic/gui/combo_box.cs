using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;

namespace pr.gui
{
	/// <summary>Replacement for the forms combo box that doesn't throw a first chance exception when the data source is empty</summary>
	public class ComboBox :System.Windows.Forms.ComboBox
	{
		// Notes:
		// - When binding a DropDown style combo box, the BS position changes then an
		//   item is selected from the drop down list. But, as soon as the text is changed,
		//   the cb.SelectedIndex and cb.SelectedItem become -1/null (the BS position
		//   is not changed, and no SelectedIndexChanged event raised). Restoring the
		//   text does not restore cb.SelectedIndex and cb.SelectedItem.
		// - When the BS position is changed, the CB text is changed before the SelectedIndex
		//   so TextChanged is raised, then SelectedIndexChanged is raised.
		// - SelectedIndex/SelectedItem only accept values that are legal according to the Items
		//   collection. When using a DataSource, however, the items collection isn't updated until
		//   the control has a windows handle and a parent window (it seems)
		//
		// To use the text field to update the bound item do this:
		//	m_cb.TextChanged += (s,a) =>
		//	{
		//		// The selected item becomes null when the text is changed by the user.
		//		// Without this test, changing the selection causes the previously selected
		//		// item to have it's text changed because TextChanged is raised before the
		//		// binding source position and 'SelectedIndex' are changed.
		//		if (m_cb.SelectedItem == null)
		//			m_bs.Current.Name = m_cb.Text;
		//	};
		public ComboBox()
			:base()
		{
			// Ensure the combo box has a binding context so that assigning
			// to the DataSource property correctly initialises the Items collection. 
			BindingContext = new BindingContext();
		}

		/// <summary>The index of the selected item in the drop down list</summary>
		public override int SelectedIndex
		{
			get { return Items.Count > 0 ? base.SelectedIndex : -1; }
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

		/// <summary>Preserves the selected index in the combo</summary>
		public Scope SelectedIndexScope()
		{
			return Scope.Create(
				() => SelectedIndex,
				si => SelectedIndex = si);
		}

		/// <summary>Preserves the selected item in the combo</summary>
		public Scope SelectedItemScope()
		{
			return Scope.Create(
				() => SelectedItem,
				si => SelectedItem = si);
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
					RestoreTextSelection(m_selection);

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
					m_selection = SaveTextSelection();
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
					m_selection = SaveTextSelection();
			}
		}

		/// <summary>A smarter set text that does sensible things with the selection position</summary>
		public void SetText(string text)
		{
			if (PreserveSelectionThruFocusChange)
				RestoreTextSelection(m_selection);

			var idx = SelectionStart;
			SelectedText = text;
			SelectionStart = idx + text.Length;
		}

		/// <summary>A smarter set text that does sensible things with the selection position</summary>
		public void AppendText(string text)
		{
			if (PreserveSelectionThruFocusChange)
				RestoreTextSelection(m_selection);

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
		public bool PreserveSelectionThruFocusChange
		{
			get { return m_preserve_selection && DropDownStyle != ComboBoxStyle.DropDownList; }
			set
			{
				if (m_preserve_selection == value) return;
				m_preserve_selection = value;
			}
		}
		private bool m_preserve_selection;
		private Range m_selection; // don't use a Scope for this. We save selection more than restoring it and disposed old scopes will restore the selection.

		/// <summary>The property of the data bound items to display</summary>
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

		/// <summary>True when the text in the combo was last modified by the user (like the TextBox.Modified property)</summary>
		public bool Modified { get; private set; }

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

		/// <summary>Restore the selection on focus gained</summary>
		protected override void OnGotFocus(EventArgs e)
		{
			// Note, don't save on lost focus, the selection has already been reset to 0,0 by then
			if (PreserveSelectionThruFocusChange)
				RestoreTextSelection(m_selection);
	
			base.OnGotFocus(e);
		}

		/// <summary>Update the selection whenever the text changes</summary>
		protected override void OnTextUpdate(EventArgs e)
		{
			Modified = true;
			base.OnTextUpdate(e);
			if (PreserveSelectionThruFocusChange)
				m_selection = SaveTextSelection();
		}

		/// <summary>Clear the Modified flag after text changed</summary>
		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			Modified = false;
		}

		/// <summary>Save the selection after it has been changed by mouse selection</summary>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (PreserveSelectionThruFocusChange)
				m_selection = SaveTextSelection();
		}

		/// <summary>Save the selection after it has been changed by key presses</summary>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (PreserveSelectionThruFocusChange)
				m_selection = SaveTextSelection();
		}

		/// <summary>Preserve the selection in the combo</summary>
		public Scope TextSelectionScope()
		{
			return Scope.Create(
				() => SaveTextSelection(),
				sr => RestoreTextSelection(sr));
		}

		/// <summary>Restore the selection</summary>
		public void RestoreTextSelection(Range selection)
		{
			// Only allow selection setting for editable combo box styles
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				//System.Diagnostics.Trace.WriteLine("Restoring Selection: [{0},{1}]".Fmt(selection.Begini, selection.Sizei));
				Select(selection.Begini, selection.Sizei);
				//System.Diagnostics.Trace.WriteLine("Selection is now: [{0},{1}]".Fmt(SelectionStart, SelectionLength));
			}
		}

		/// <summary>Save the current selection</summary>
		public Range SaveTextSelection()
		{
			var selection = Range.FromStartLength(SelectionStart, SelectionLength);
			//System.Diagnostics.Trace.WriteLine("Selection Saved: [{0},{1}]\n\t{2}"
			//	.Fmt(selection.Value.Begini, selection.Value.Sizei,
			//	string.Join("\n\t", new System.Diagnostics.StackTrace().GetFrames().Take(5).Select(x => x.GetMethod()))));
			return selection;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestComboBox
	{
		[Test] public void DataBinding()
		{
			var cb = new pr.gui.ComboBox();
			var arr = new[]{ "Hello","World" };

			{// Test the data source works even when the window handle hasn't been created
				cb.DataSource = arr;
				cb.SelectedItem = "Hello";

				Assert.AreEqual(cb.SelectedIndex, 0);
				Assert.AreEqual(cb.SelectedItem, "Hello");

				cb.DataSource = new string[0];
				Assert.AreEqual(cb.SelectedIndex, -1);
				Assert.AreEqual(cb.SelectedItem, null);

				cb.DataSource = null;
				Assert.AreEqual(cb.SelectedIndex, -1);
				Assert.AreEqual(cb.SelectedItem, null);
			}

			{// Test the combo box still works when added to a form
				var f = new Form();
				f.Controls.Add(cb);
				cb.DataSource = arr;
				cb.SelectedItem = "Hello";

				Assert.AreEqual(cb.SelectedIndex, 0);
				Assert.AreEqual(cb.SelectedItem, "Hello");

				cb.DataSource = null;
				Assert.AreEqual(cb.SelectedIndex, -1);
				Assert.AreEqual(cb.SelectedItem, null);
			}
		}
	}
}
#endif
