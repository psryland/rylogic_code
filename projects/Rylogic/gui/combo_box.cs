using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;
using pr.win32;

namespace pr.gui
{
	[Serializable]
	[DebuggerDisplay("Value={Value} Text={Text} Valid={Valid}")]
	public class ComboBox :System.Windows.Forms.ComboBox
	{
		// ValueBox is very similar to this class, maintain both.
		// Replacement for the forms combo box that doesn't throw a first chance
		// exception when the data source is empty
		//
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
		// - For DropDownList style combo boxes, use the 'DropDownClosed' event to trigger changes
		//   of SelectedItem. 'SelectedIndexChanged' and other events all trigger on mouse over of
		//   items in the drop down list.
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
		//

		private int m_in_set_text;

		public ComboBox()
			:base()
		{
			// Ensure the combo box has a binding context so that assigning
			// to the DataSource property correctly initialises the Items collection. 
			BindingContext = new BindingContext();

			ValueType = typeof(object);
			UseValidityColours = true;
			CommitValueOnFocusLost = true;
			ForeColorValid = Color.Black;
			BackColorValid = Color.White;
			ForeColorInvalid = Color.Gray;
			BackColorInvalid = Color.White;
			Value = null;
		}
		public override bool PreProcessMessage(ref Message m)
		{
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Commit on return
				if (m.Msg == Win32.WM_KEYDOWN && Win32.ToVKey(m.WParam) == Keys.Return)
					TryCommitValue();
			}

			return base.PreProcessMessage(ref m);
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			m_valid = null;
			base.OnInvalidated(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Save the selection after it has been changed by mouse selection
				if (PreserveSelectionThruFocusChange)
					m_selection = SaveTextSelection();
			}
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Save the selection after it has been changed by key presses
				if (PreserveSelectionThruFocusChange)
					m_selection = SaveTextSelection();
			}
		}
		protected override void OnGotFocus(EventArgs e)
		{
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
			    // Restore the selection on focus gained
			    // Note, don't save on lost focus, the selection has already been reset to 0,0 by then
				if (PreserveSelectionThruFocusChange)
					RestoreTextSelection(m_selection);
			}
	
			base.OnGotFocus(e);
		}
		protected override void OnTextUpdate(EventArgs e)
		{
			base.OnTextUpdate(e);

			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Update the selection whenever the text changes
				if (PreserveSelectionThruFocusChange)
					m_selection = SaveTextSelection();
			}
		}
		protected override void OnTextChanged(EventArgs e)
		{
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Update the selection whenever the text changes
				if (PreserveSelectionThruFocusChange)
					m_selection = SaveTextSelection();

				// Invalidate the cached 'valid' state whenever the text changes
				m_valid = null;

				// Prevent reentrancy
				if (m_in_set_text != 0) return;
				using (Scope.Create(() => ++m_in_set_text, () => --m_in_set_text))
					TextToValueIfValid();
			}

			base.OnTextChanged(e);
		}
		protected override void OnSelectionChangeCommitted(EventArgs e)
		{
			base.OnSelectionChangeCommitted(e);

			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Commit the value when the selection is changed
				Value = SelectedItem;
			}
		}
		protected override void SetItemsCore(IList list)
		{
			base.SetItemsCore(list);
			if (ValueType == typeof(object) && list != null)
			{
				var ty = list.GetType();
				if (ty.IsArray)
					ValueType = ty.GetElementType();
				else if (ty.IsGenericType)
					ValueType = ty.GetGenericArguments()[0];
				else if (list.Count != 0 && list[0] != null)
					ValueType = list[0].GetType();
			}
		}
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);

			if (DropDownStyle != ComboBoxStyle.DropDownList && CommitValueOnFocusLost && !ContainsFocus)
			{
				// Commit the value on focus lost
				TryCommitValue();
				var text = ValueToText(Value);
				Text = text;
			}
		}
		protected override void OnDropDown(EventArgs e)
		{
			 // Retrieve handle to the dynamically created drop-down list control
			var info = new Win32.COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(Win32.COMBOBOXINFO)) };
			Win32.SendMessage(Handle, Win32.CB_GETCOMBOBOXINFO, IntPtr.Zero, out info);
			DropDownListCtrl = new DropDownListControl(this, info.hwndList);

			base.OnDropDown(e);
		}
		protected override void OnDropDownClosed(EventArgs e)
		{
			DropDownListCtrl = null;
			base.OnDropDownClosed(e);
		}
		protected override void OnFormat(ListControlConvertEventArgs e)
		{
			// Display using the 'DisplayProperty' if specified
			if (DisplayProperty.HasValue())
			{
				m_disp_prop = m_disp_prop ?? e.ListItem.GetType().GetProperty(DisplayProperty, BindingFlags.Public|BindingFlags.Instance);
				e.Value = m_disp_prop.GetValue(e.ListItem).ToString();
			}

			// Otherwise, fall back to the other methods
			else
			{
				base.OnFormat(e);
			}
		}

		/// <summary>The property of the data bound items to display</summary>
		public string DisplayProperty
		{
			// Prefer this over 'DisplayMember' because DisplayMember throws
			// an exception when the data bound collection is empty.
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

		/// <summary>The text color for valid values</summary>
		public Color ForeColorValid
		{
			get;
			set;
		}

		/// <summary>The background color for valid values</summary>
		public Color BackColorValid
		{
			get;
			set;
		}

		/// <summary>The text color for invalid values</summary>
		public Color ForeColorInvalid
		{
			get;
			set;
		}

		/// <summary>The background color for invalid values</summary>
		public Color BackColorInvalid
		{
			get;
			set;
		}

		/// <summary>Get/Set whether the background colours are set based on value validity</summary>
		public bool UseValidityColours { get; set; }

		/// <summary>Control whether focus lost results in a ValueCommited event</summary>
		public bool CommitValueOnFocusLost { get; set; }

		/// <summary>The type of 'Value'</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Type ValueType
		{
			get;
			set;
		}

		/// <summary>Returns true if the text in the control represents a valid value</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<string, bool> ValidateText
		{
			get
			{
				return m_validate_text ?? (x =>
				{
					// For built in types, validate the text. For user types
					// assume the text is valid (even tho we may not be able to
					// convert the text to an instance of the user type).
					if (!x.HasValue()) return false;
					switch (ValueType.Name) {
					default: return true;
					case nameof(Byte   ): return byte   .TryParse(x, out var b);
					case nameof(Char   ): return char   .TryParse(x, out var c);
					case nameof(Int16  ): return short  .TryParse(x, out var s);
					case nameof(UInt16 ): return ushort .TryParse(x, out var us);
					case nameof(Int32  ): return int    .TryParse(x, out var i);
					case nameof(UInt32 ): return uint   .TryParse(x, out var ui);
					case nameof(Int64  ): return long   .TryParse(x, out var l);
					case nameof(UInt64 ): return ulong  .TryParse(x, out var ul);
					case nameof(Single ): return float  .TryParse(x, out var f);
					case nameof(Double ): return double .TryParse(x, out var d);
					case nameof(Decimal): return decimal.TryParse(x, out var ld);
					}
				});
			}
			set
			{
				m_validate_text = value;
				m_valid = null;
			}
		}
		private Func<string, bool> m_validate_text;

		/// <summary>Convert a string to the value type</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<string, object> TextToValue
		{
			get
			{
				return m_text_to_value ?? (x =>
				{
					// For built in types, convert the text to the type.
					// For user types, assume the text is a description of the type
					// and not convertible to the type.
					if (!x.HasValue()) return null;
					switch (ValueType.Name) {
					default: return Value;
					case nameof(String ): return x;
					case nameof(Byte   ): return byte   .Parse(x);
					case nameof(Char   ): return char   .Parse(x);
					case nameof(Int16  ): return short  .Parse(x);
					case nameof(UInt16 ): return ushort .Parse(x);
					case nameof(Int32  ): return int    .Parse(x);
					case nameof(UInt32 ): return uint   .Parse(x);
					case nameof(Int64  ): return long   .Parse(x);
					case nameof(UInt64 ): return ulong  .Parse(x);
					case nameof(Single ): return float  .Parse(x);
					case nameof(Double ): return double .Parse(x);
					case nameof(Decimal): return decimal.Parse(x);
					}
				});
			}
			set
			{
				m_text_to_value = value;
				TextToValueIfValid();
			}
		}
		private Func<string, object> m_text_to_value;

		/// <summary>Convert the value type to a string</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<object, string> ValueToText
		{
			get
			{
				return m_value_to_text ?? (x =>
				{
					if (x == null) return string.Empty;
					if (DisplayMember.HasValue())
					{
						var mi = x.GetType().GetProperty(DisplayMember).GetGetMethod();
						return mi.Invoke(x, null).ToString();
					}
					return x.ToString();
				});
			}
			set
			{
				m_value_to_text = value;
				ValueToTextIfNotFocused();
			}
		}
		private Func<object, string> m_value_to_text;

		/// <summary>The value represented in the control</summary>
		public object Value
		{
			get { return m_value ?? ValueType.DefaultInstance(); }
			set
			{
				if (Equals(m_value, value)) return;
				SetValue(value);
			}
		}
		private object m_value;

		/// <summary>Set the value explicitly (i.e. not ignored if equal to the current value)</summary>
		public void SetValue(object value, bool notify = true)
		{
			// Adopt the type from the value if 'ValueType' is currently 'object'
			if (ValueType == typeof(object) && value != null)
				ValueType = value.GetType();

			// Null is equivalent to the default type for structs
			if (ValueType.IsValueType && value == null)
				value = ValueType.DefaultInstance();

			// Check the assigned value has the correct type
			if (value != null && !ValueType.IsAssignableFrom(value.GetType()))
				throw new ArgumentException("Cannot assign to 'Value', argument has the wrong type. Expected: {0}  Received: {1}".Fmt(ValueType.Name, value.GetType().Name));

			// Assign the value
			m_value = value;

			// Only update the text when not focused to prevent
			// the text changing while the user is typing.
			ValueToTextIfNotFocused();

			// Notify value changed
			if (notify)
				OnValueChanged(new ValueEventArgs(Value));
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

		/// <summary>True if the control contains a valid value</summary>
		public bool Valid
		{
			get { return m_valid ?? (m_valid = ValidateText(Text)).Value; }
		}
		private bool? m_valid;

		/// <summary>Convert the text to a value and assign to 'Value' if valid</summary>
		private bool TextToValueIfValid()
		{
			// Set the text colour based on whether the value matches the text
			UpdateTextColours();

			// Update the value for valid text
			var r = Valid && ValueType != typeof(object);
			if (r) Value = TextToValue(Text);
			return r;
		}

		/// <summary>Convert the value to text and update 'Text' if not focused</summary>
		private bool ValueToTextIfNotFocused()
		{
			// Only update the text when not focused to prevent
			// the text changing while the user is typing.
			if (Focused) return false;
			Text = ValueToText(Value);
			return true;
		}

		/// <summary>If the current text is value, update the value, and notify of value committed</summary>
		public bool TryCommitValue()
		{
			var r = TextToValueIfValid();
			if (r) OnValueCommitted(new ValueEventArgs(Value));
			return r;
		}

		/// <summary>Set the Fore and Back colours for the value box based on the current text</summary>
		public void UpdateTextColours()
		{
			if (DropDownStyle == ComboBoxStyle.DropDownList ||
				!UseValidityColours ||
				this.IsInDesignMode())
				return;

			// Set the text colour based on whether the value matches the text
			ForeColor = Valid ? ForeColorValid : ForeColorInvalid;
			BackColor = Valid ? BackColorValid : BackColorInvalid;
		}

		/// <summary>Raised when the value is changed to a valid value by Enter pressed or focus lost</summary>
		public event EventHandler<ValueEventArgs> ValueCommitted;
		protected virtual void OnValueCommitted(ValueEventArgs args)
		{
			ValueCommitted.Raise(this, args);
		}

		/// <summary>Raised when the value changes</summary>
		public event EventHandler<ValueEventArgs> ValueChanged;
		protected virtual void OnValueChanged(ValueEventArgs args)
		{
			ValueChanged.Raise(this, args);
		}

		/// <summary>The dynamically created drop down list control</summary>
		internal DropDownListControl DropDownListCtrl
		{
			get { return m_dd_list_ctrl; }
			private set
			{
				if (m_dd_list_ctrl == value) return;
				m_dd_list_ctrl = value;
			}
		}
		private DropDownListControl m_dd_list_ctrl;

		/// <summary>Wrapper for the dynamically created combo box drop down list</summary>
		internal class DropDownListControl :NativeWindow
		{
			// Notes:
			//  - The list control is destroyed as soon as it loses focus. This
			//    means any behaviour while the list is displayed must be modal.
			//    Context menus cannot be modal, they use the parent window's
			//    message queue.
			private readonly ComboBox m_owner;
			internal DropDownListControl(ComboBox owner, IntPtr handle)
			{
				m_owner = owner;
				AssignHandle(handle);
			}
			protected override void WndProc(ref Message m)
			{
				// ToDo: handle window messages for the list box control
				base.WndProc(ref m);
			}
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
				Value = value;

				// For drop down lists, if 'value' isn't in the collection then
				// then the call to SelectedItem is ignored, silently.
				if (!Equals(SelectedItem, value))
					UnknownItemSelected.Raise(this, new UnknownItemSelectedEventArgs(value));
			}
		}

		/// <summary>Preserves the selected index in the combo</summary>
		public Scope PreserveSelectedIndex()
		{
			return Scope.Create(
				() => SelectedIndex,
				si => SelectedIndex = si);
		}

		/// <summary>Preserves the selected item in the combo</summary>
		public Scope PreserveSelectedItem(bool unless_null = false)
		{
			return Scope.Create(
				() => SelectedItem,
				si => SelectedItem = si != null || !unless_null ? si : SelectedItem);
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

		/// <summary>Preserve the selection in the combo</summary>
		public Scope PreserveTextSelection()
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
				//System.Diagnostics.Trace.WriteLine("Restoring Selection: [{0},{1}]".Fmt(selection.Begi, selection.Sizei));
				Select(selection.Begi, selection.Sizei);
				//System.Diagnostics.Trace.WriteLine("Selection is now: [{0},{1}]".Fmt(SelectionStart, SelectionLength));
			}
		}

		/// <summary>Save the current selection</summary>
		public Range SaveTextSelection()
		{
			var selection = Range.FromStartLength(SelectionStart, SelectionLength);
			//System.Diagnostics.Trace.WriteLine("Selection Saved: [{0},{1}]\n\t{2}"
			//	.Fmt(selection.Value.Begi, selection.Value.Sizei,
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
