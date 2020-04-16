//#define PR_CB_TRACE
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;
using Rylogic.Interop.Win32;

namespace Rylogic.Gui.WinForms
{
	[Serializable]
	[DebuggerDisplay("Value={Value} Text={Text} Valid={Valid}")]
	public class ComboBox :System.Windows.Forms.ComboBox
	{
		// Notes:
		//
		// ValueBox is very similar to this class, maintain both.
		//
		// ComboBox has two use cases:
		//  - DropDownList - where the selection must be one of the values from the drop down list.
		//  - DropDown - The value may be one from the drop down list, but doesn't have to be.
		//  The tricky one is the second case, because the ComboBox only displays strings but
		//  the items in the ComboBox are unknown types. When the ComboBox text is changed it
		//  should match at item from the list if possible.
		//
		// 'SelectedItem' is the item from the list,
		// 'Text' is the displayed text in the ComboBox.
		// 'Value' is either 'SelectedItem', 'Text' converted to the value type, or a default value type.
		//
		// Notes:
		// - Replacement for the forms combo box that doesn't throw a first chance exception
		//   when the data source is empty.
		// - When binding a DropDown style combo box, the BS position changes then an
		//   item is selected from the drop down list. But, as soon as the text is changed,
		//   the cb.SelectedIndex and cb.SelectedItem become -1/null (the BS position
		//   is not changed, and no SelectedIndexChanged event raised). Restoring the
		//   text does not restore cb.SelectedIndex and cb.SelectedItem.
		// - When the BS position is changed, the CB text is changed before the SelectedIndex
		//   so TextChanged is raised, then SelectedIndexChanged is raised.
		// - SelectedIndex/SelectedItem only accept values that are legal according to the Items
		//   collection. When using a DataSource, however, the items collection isn't updated until
		//   the control has a windows handle and a parent window (it seems).
		// - For DropDownList style combo boxes, use the 'DropDownClosed' event to trigger changes
		//   of SelectedItem. 'SelectedIndexChanged' and other events all trigger on mouse over of
		//   items in the drop down list.
		// - For DropDownList style combo boxes, you can update the available objects on drop down
		//   like this, if you use 'DropDownClosed' for committing a selection:
		//      m_cb.DropDown += (s,a) =>
		//      {
		//          using (m_cb.PreserveSelectedItem())
		//              m_cb.DataSource = GetDataSource();
		//      };
		// - To use the text field to update the bound item do this:
		//      m_cb.TextChanged += (s,a) =>
		//      {
		//          // The selected item becomes null when the text is changed by the user.
		//          // Without this test, changing the selection causes the previously selected
		//          // item to have it's text changed because TextChanged is raised before the
		//          // binding source position and 'SelectedIndex' are changed.
		//          if (m_cb.SelectedItem == null)
		//              m_bs.Current.Name = m_cb.Text;
		//      };
		//
		// Test cases:
		//  - Select from drop down
		//     SelectedItem should be the item from the drop down
		//     Text should match selected item
		//     Value should be 'SelectedItem'
		//
		//  - Type text -> auto complete -> tab or enter
		//     SelectedItem should be the item from the drop down
		//     Text should match selected item
		//     Value should be 'SelectedItem'
		//
		//  - Type full text to match an item in drop down
		//     SelectedItem should be the first matching item in the drop down
		//     Text should match selected item
		//     Value should be 'SelectedItem'
		//
		//  - Type text not matching an item in drop down and not convertible to the value type
		//     SelectedItem should be null
		//     Text should be as typed
		//     Value should be a default value type
		//
		//  - Type text that is convertible to the type but not matching an entry in the drop down
		//     SelectedItem should be null
		//     Text should be as typed
		//     Value should be a valid value
		//
		//  - Programmatically assign to 'SelectedItem'
		//     SelectedItem should be the assigned value if it exists in the drop down, otherwise null
		//     Text should be 'SelectedItem' converted to a string ("" if null)
		//     Value should be 'SelectedItem' (possibly null)
		//
		//  - Programmatically assign to 'Text'
		//     SelectedItem should be the first matching item in the drop down
		//     Text should be the assigned value
		//     Value should be 'SelectedItem' if not null, or 'Text' converted to a value type, or a default value type.
		//
		//  - Programmatically assign to 'Value'
		//     SelectedItem should be the assigned value if it exists in the drop down, otherwise null
		//     Text should be the assigned value converted to a string
		//     Value should be the assigned value 

		private int m_internal_set_text;

		public ComboBox()
			:base()
		{
			// Ensure the combo box has a binding context so that assigning
			// to the DataSource property correctly initialises the Items collection. 
			BindingContext = new BindingContext();

			ValueType = typeof(object);
			UseValidityColours = true;
			CommitValueOnFocusLost = true;
			PreserveSelectionThruFocusChange = true;
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
				if (m.Msg == Win32.WM_KEYDOWN && Win32.ToVKey(m.WParam) == EKeyCodes.Return)
					TextToValueIfValid(Text);
			}

			return base.PreProcessMessage(ref m);
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
			Trace($"OnTextUpdate");
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
			Trace($"OnTextChanged");
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				// Update the selection whenever the text changes
				if (PreserveSelectionThruFocusChange)
					m_selection = SaveTextSelection();

				if (ValueToText(Value) != Text)
					TextToValueIfValid(Text);
			}

			base.OnTextChanged(e);
		}
		protected override void OnSelectionChangeCommitted(EventArgs e)
		{
			Trace($"OnSelectionChangeCommitted");
			base.OnSelectionChangeCommitted(e);
			SetValue(SelectedItem);
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
			Trace($"OnLostFocus");
			base.OnLostFocus(e);

			// Commit the value on focus lost
			if (DropDownStyle != ComboBoxStyle.DropDownList && CommitValueOnFocusLost && !ContainsFocus)
			{
				// If the current value does not match the current text, try to convert the text to a value
				if (ValueToText(Value) != Text)
					TextToValueIfValid(Text);

				// Notify value committed
				OnValueCommitted(new ValueEventArgs(Value));
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

		/// <summary>The type of 'Value'</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Type ValueType { get; set; }

		/// <summary>The property of the data bound items to display</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		public Color ForeColorValid { get; set; }

		/// <summary>The background color for valid values</summary>
		public Color BackColorValid { get; set; }

		/// <summary>The text color for invalid values</summary>
		public Color ForeColorInvalid { get; set; }

		/// <summary>The background color for invalid values</summary>
		public Color BackColorInvalid { get; set; }

		/// <summary>Get/Set whether the background colours are set based on value validity</summary>
		public bool UseValidityColours { get; set; }

		/// <summary>Control whether focus lost results in a ValueCommited event</summary>
		public bool CommitValueOnFocusLost { get; set; }

		/// <summary>The value represented in the control</summary>
		public object Value
		{
			get
			{
				// Always return the selected item if valid
				if (SelectedItem != null)
					return SelectedItem;

				// Otherwise, return the assigned value, or a default value
				return m_value ?? ValueType.DefaultInstance();
			}
			set
			{
				if (Equals(Value, value)) return;
				SetValue(value);
			}
		}
		private object m_value;

		/// <summary>Set the value explicitly (i.e. not ignored if equal to the current value)</summary>
		public void SetValue(object value, bool notify = true)
		{
			if (m_in_set_value != 0) return;
			using (Scope.Create(() => ++m_in_set_value, () => --m_in_set_value))
			{
				// Adopt the type from 'value' if 'ValueType' is currently 'object'
				if (ValueType == typeof(object) && value != null)
					ValueType = value.GetType();

				// Null is equivalent to the default type for structs
				if (ValueType.IsValueType && value == null)
					value = ValueType.DefaultInstance();

				// Check the assigned value has the correct type
				if (value != null && !ValueType.IsAssignableFrom(value.GetType()))
					throw new ArgumentException($"Cannot assign to 'Value', argument has the wrong type. Expected: {ValueType.Name}  Received: {value.GetType().Name} with value '{value}'");

				// Save the assigned value
				m_value = value;

				// Try assign the value to the SelectedItem property.
				// If 'value' is not a drop down item, this will be silently ignored
				SelectedItem = value;

				// Set 'Text' to match the value
				ValueToTextIfNotFocused(value);

				// Notify value changed
				if (notify)
					OnValueChanged(new ValueEventArgs(Value));

				Trace($"SetValue to '{value}'");
			}
		}
		private int m_in_set_value;

		/// <summary>True if a valid value is selected</summary>
		public bool Valid => Value != null;

		/// <summary>Raised when the value is changed to a valid value by Enter pressed or focus lost</summary>
		public event EventHandler<ValueEventArgs> ValueCommitted;
		protected virtual void OnValueCommitted(ValueEventArgs args)
		{
			ValueCommitted?.Invoke(this, args);
		}

		/// <summary>Raised when the value changes</summary>
		public event EventHandler<ValueEventArgs> ValueChanged;
		protected virtual void OnValueChanged(ValueEventArgs args)
		{
			ValueChanged?.Invoke(this, args);
		}

		/// <summary>Convert the text to a value and assign to 'Value' if valid</summary>
		private void TextToValueIfValid(string text)
		{
			if (!CanConvertTextToValue)
				return;

			// Prevent reentrancy
			if (m_internal_set_text != 0) return;
			using (Scope.Create(() => ++m_internal_set_text, () => --m_internal_set_text))
				Value = ValidateText(text) ? TextToValue(text) : null;

			UpdateValidationColours();
		}

		/// <summary>Convert the value to text and update 'Text' if not focused</summary>
		private void ValueToTextIfNotFocused(object value)
		{
			// Only update the text when not focused to prevent
			// the text changing while the user is typing.
			if (Focused || DropDownStyle == ComboBoxStyle.DropDownList)
				return;

			// Prevent reentrancy
			if (m_internal_set_text != 0) return;
			using (Scope.Create(() => ++m_internal_set_text, () => --m_internal_set_text))
				Text = ValueToText(value);

			UpdateValidationColours();
		}

		/// <summary>Set the Fore and Back colours for the value box based on the current text</summary>
		private void UpdateValidationColours()
		{
			// Ignore if not possible
			if (DropDownStyle == ComboBoxStyle.DropDownList || !UseValidityColours || this.IsInDesignMode())
				return;

			ForeColor = Valid ? ForeColorValid : ForeColorInvalid;
			BackColor = Valid ? BackColorValid : BackColorInvalid;
		}

		/// <summary>True if the value type can be constructed from Text</summary>
		private bool CanConvertTextToValue
		{
			get
			{
				switch (ValueType.Name)
				{
				default:
					if (ValueType.IsEnum) return true;
					return m_text_to_value != null;
				case nameof(String): return true;
				case nameof(Byte): return true;
				case nameof(Char): return true;
				case nameof(Int16): return true;
				case nameof(UInt16): return true;
				case nameof(Int32): return true;
				case nameof(UInt32): return true;
				case nameof(Int64): return true;
				case nameof(UInt64): return true;
				case nameof(Single): return true;
				case nameof(Double): return true;
				case nameof(Decimal): return true;
				}
			}
		}

		/// <summary>Returns true if the text in the control represents a valid value</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<string, bool> ValidateText
		{
			get
			{
				return m_validate_text ?? DefaultValidateText;
				bool DefaultValidateText(string text)
				{
					// For built in types, validate the text. For user types, assume the text is valid
					// (even tho we may not be able to convert the text to an instance of the user type).
					switch (ValueType.Name)
					{
					default:
						if (ValueType.IsEnum) return Enum.IsDefined(ValueType, text);
						return text.HasValue();
					case nameof(String): return true;
					case nameof(Byte): return text.HasValue() && byte.TryParse(text, out var b);
					case nameof(Char): return text.HasValue() && char.TryParse(text, out var c);
					case nameof(Int16): return text.HasValue() && short.TryParse(text, out var s);
					case nameof(UInt16): return text.HasValue() && ushort.TryParse(text, out var us);
					case nameof(Int32): return text.HasValue() && int.TryParse(text, out var i);
					case nameof(UInt32): return text.HasValue() && uint.TryParse(text, out var ui);
					case nameof(Int64): return text.HasValue() && long.TryParse(text, out var l);
					case nameof(UInt64): return text.HasValue() && ulong.TryParse(text, out var ul);
					case nameof(Single): return text.HasValue() && float.TryParse(text, out var f);
					case nameof(Double): return text.HasValue() && double.TryParse(text, out var d);
					case nameof(Decimal): return text.HasValue() && decimal.TryParse(text, out var ld);
					}
				}
			}
			set
			{
				m_validate_text = value;
				TextToValueIfValid(Text);
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
				return m_text_to_value ?? DefaultTextToValue;
				object DefaultTextToValue(string text)
				{
					// For built in types, convert the text to the type.
					// For user types, assume the text is a description of the type and not convertible to the type.
					// Don't try to match the text to an entry in 'Items' because that will always match the first
					// item and doesn't allow for duplicate entries in the Items collection.
					switch (ValueType.Name)
					{
					default:
						if (!text.HasValue()) return null;
						if (ValueType == typeof(string)) return text;
						if (ValueType == typeof(object)) return text;
						if (ValueType.IsEnum) return Enum.Parse(ValueType, text);

						// Note: this function should only be called if 'x' passes 'ValidateText' so
						// this exception means some text passed 'ValidateText' but is not actually
						// valid, or 'Valid' has not been invalidated.
						throw new Exception($"Cannot convert '{text}' to a value of type {ValueType.Name}");

					case nameof(String): return text ?? string.Empty;
					case nameof(Byte): return text.HasValue() ? (object)byte.Parse(text) : null;
					case nameof(Char): return text.HasValue() ? (object)char.Parse(text) : null;
					case nameof(Int16): return text.HasValue() ? (object)short.Parse(text) : null;
					case nameof(UInt16): return text.HasValue() ? (object)ushort.Parse(text) : null;
					case nameof(Int32): return text.HasValue() ? (object)int.Parse(text) : null;
					case nameof(UInt32): return text.HasValue() ? (object)uint.Parse(text) : null;
					case nameof(Int64): return text.HasValue() ? (object)long.Parse(text) : null;
					case nameof(UInt64): return text.HasValue() ? (object)ulong.Parse(text) : null;
					case nameof(Single): return text.HasValue() ? (object)float.Parse(text) : null;
					case nameof(Double): return text.HasValue() ? (object)double.Parse(text) : null;
					case nameof(Decimal): return text.HasValue() ? (object)decimal.Parse(text) : null;
					}
				}
			}
			set
			{
				m_text_to_value = value;
				TextToValueIfValid(Text);
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
				return m_value_to_text ?? DefaultValueToText;
				string DefaultValueToText(object x)
				{
					if (x == null) return string.Empty;
					if (DisplayMember.HasValue())
					{
						var mi = x.GetType().GetProperty(DisplayMember).GetGetMethod();
						return mi.Invoke(x, null).ToString();
					}
					return x.ToString();
				}
			}
			set
			{
				m_value_to_text = value;
				ValueToTextIfNotFocused(Value);
			}
		}
		private Func<object, string> m_value_to_text;

		/// <summary>The index of the selected item in the drop down list</summary>
		public override int SelectedIndex
		{
			get { return Items.Count > 0 ? base.SelectedIndex : -1; }
			set
			{
				if (value == base.SelectedIndex || !value.Within(0, Items.Count)) return;
				base.SelectedIndex = value;
			}
		}

		/// <summary>Set the selected item to an item in the drop down list</summary>
		public new object SelectedItem
		{
			get { return base.SelectedItem; }
			set
			{
				base.SelectedItem = value;

				if (base.SelectedItem != null)
					Value = base.SelectedItem;

				// For drop down lists, if 'value' isn't in the collection then
				// then the call to SelectedItem is ignored, silently.
				if (!Equals(SelectedItem, value))
					UnknownItemSelected?.Invoke(this, new UnknownItemSelectedEventArgs(value));
			}
		}

		/// <summary>
		/// Raised whenever an attempt to change the selected item to an unknown item is made.
		/// I.e. whenever the combo box ignores a call to 'SelectedItem = value'</summary>
		public event EventHandler<UnknownItemSelectedEventArgs> UnknownItemSelected;

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
		private RangeI m_selection; // don't use a Scope for this. We save selection more than restoring it and disposed old scopes will restore the selection.

		/// <summary>Preserve the selection in the combo</summary>
		public Scope PreserveTextSelection()
		{
			return Scope.Create(
				() => SaveTextSelection(),
				sr => RestoreTextSelection(sr));
		}

		/// <summary>Restore the selection</summary>
		public void RestoreTextSelection(RangeI selection)
		{
			// Only allow selection setting for editable combo box styles
			if (DropDownStyle != ComboBoxStyle.DropDownList)
			{
				//System.Diagnostics.Trace.WriteLine($"Restoring Selection: [{selection.Begi},{ selection.Sizei}]");
				Select(selection.Begi, selection.Sizei);
				//System.Diagnostics.Trace.WriteLine($"Selection is now: [{SelectionStart},{ SelectionLength}]");
			}
		}

		/// <summary>Save the current selection</summary>
		public RangeI SaveTextSelection()
		{
			var selection = RangeI.FromStartLength(SelectionStart, SelectionLength);
			//System.Diagnostics.Trace.WriteLine("Selection Saved: [{0},{1}]\n\t{2}"
			//	.Fmt(selection.Value.Begi, selection.Value.Sizei,
			//	string.Join("\n\t", new System.Diagnostics.StackTrace().GetFrames().Take(5).Select(x => x.GetMethod()))));
			return selection;
		}

		/// <summary>Wrapper for the dynamically created combo box drop down list</summary>
		internal class DropDownListControl : NativeWindow
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

		/// <summary>For debugging this bastard...</summary>
		[Conditional("PR_CB_TRACE")]
		private void Trace(string message)
		{
			Debug.WriteLine($"{message}\n\tValue={Value}\n\tText={Text}\n\tSelectedItem={SelectedItem}({SelectedIndex})");
		}
	}

	#region EventArgs
	public class UnknownItemSelectedEventArgs :EventArgs
	{
		public UnknownItemSelectedEventArgs(object unknown_item)
		{
			UnknownItem =  unknown_item;
		}

		/// <summary>The item that was attempted as the selected item</summary>
		public object UnknownItem { get; private set; }
	}
	#endregion
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Gui.WinForms;

	[TestFixture] public class TestComboBox
	{
		[Test] public void DataBinding()
		{
			var cb = new ComboBox();
			var arr = new[]{ "Hello","World" };

			{// Test the data source works even when the window handle hasn't been created
				cb.DataSource = arr;
				cb.SelectedItem = "Hello";

				Assert.Equal(cb.SelectedIndex, 0);
				Assert.Equal(cb.SelectedItem, "Hello");

				cb.DataSource = new string[0];
				Assert.Equal(cb.SelectedIndex, -1);
				Assert.Equal(cb.SelectedItem, null);

				cb.DataSource = null;
				Assert.Equal(cb.SelectedIndex, -1);
				Assert.Equal(cb.SelectedItem, null);
			}

			{// Test the combo box still works when added to a form
				var f = new Form();
				f.Controls.Add(cb);
				cb.DataSource = arr;
				cb.SelectedItem = "Hello";

				Assert.Equal(cb.SelectedIndex, 0);
				Assert.Equal(cb.SelectedItem, "Hello");

				cb.DataSource = null;
				Assert.Equal(cb.SelectedIndex, -1);
				Assert.Equal(cb.SelectedItem, null);
			}
		}
	}
}
#endif
