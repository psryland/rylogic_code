//#define PR_VB_TRACE
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;
using Rylogic.Interop.Win32;

namespace Rylogic.Gui.WinForms
{
	[Serializable]
	[DebuggerDisplay("Value={Value} Text={Text} Valid={Valid}")]
	public class ValueBox : System.Windows.Forms.TextBox
	{
		// Notes:
		//
		//  ComboBox is very similar to this class, maintain both.
		//
		// This is the typical issue with normal text boxes when used to modify a value:
		// - User types in the text box
		// - TextChanged handler
		// - Convert the text to a value
		// - Set the value
		// - ValueChanged handler or UpdateUI
		// - Convert the value to text
		// - Set the text box Text property
		// - TextChanged handler
		// - repeat ad infinitum
		//
		// The problems are:
		// - Updating the text box Text field while the user is typing resets the caret
		//   position and can change the value while the person is typing.
		// - Can be made not to repeat by testing for 'Modified' in the TextChanged handler
		//   but this still doesn't work if the value is not a valid value.
		//
		// Fixes:
		// - Only update the text box Text property when the text doesn't have focus.
		// - Only update the value when the text can be converted to a valid value.
		// - Use colour to indicate a mismatch between the value and the current text
		// - On focus lost, update the text to the actual value
		//
		// Uses:
		// - A field that displays a value, but can also be edited and used to update a value
		//   m_tb.Value = MyValue;
		//   m_tb.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0 && v.Value < Math_.TauBy2; };
		//   m_tb.ValueChanged += (s,a) =>
		//   {
		//       if (!m_tb.Focused) return;
		//       MyValue = (float)m_tb.Value;
		//   };

		private int m_internal_set_text;

		public ValueBox()
			:base()
		{
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
			// Commit on return
			if (m.Msg == Win32.WM_KEYDOWN && Win32.ToVKey(m.WParam) == EKeyCodes.Return && !Multiline)
				TextToValueIfValid(Text);

			return base.PreProcessMessage(ref m);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			// Save the selection after it has been changed by mouse selection
			if (PreserveSelectionThruFocusChange)
				m_selection = SaveTextSelection();
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			// Save the selection after it has been changed by key presses
			if (PreserveSelectionThruFocusChange)
				m_selection = SaveTextSelection();
		}
		protected override void OnGotFocus(EventArgs e)
		{
			// Restore the selection on focus gained
			// Note, don't save on lost focus, the selection has already been reset to 0,0 by then
			if (PreserveSelectionThruFocusChange)
				RestoreTextSelection(m_selection);

			base.OnGotFocus(e);
		}
		protected override void OnTextChanged(EventArgs e)
		{
			Trace($"OnTextChanged");

			// Update the selection whenever the text changes
			if (PreserveSelectionThruFocusChange)
				m_selection = SaveTextSelection();

			if (ValueToText(Value) != Text)
				TextToValueIfValid(Text);

			base.OnTextChanged(e);
		}
		protected override void OnLostFocus(EventArgs e)
		{
			Trace($"OnLostFocus");
			base.OnLostFocus(e);

			if (CommitValueOnFocusLost && !ContainsFocus)
			{
				// If the current value does not match the current text, try to convert the text to a value
				if (ValueToText(Value) != Text)
					TextToValueIfValid(Text);

				// Notify value committed
				OnValueCommitted(new ValueEventArgs(Value));
			}
		}

		/// <summary>The type of 'Value'</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Type ValueType { get; set; }

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

		/// <summary>The value represented in the text box</summary>
		public object Value
		{
			get { return m_value ?? ValueType.DefaultInstance(); }
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
			// Adopt the type from the value if 'ValueType' is currently 'object'
			if (ValueType == typeof(object) && value != null)
				ValueType = value.GetType();

			// Null is equivalent to the default type for structs
			if (!ValueType.IsClass && value == null)
				value = ValueType.DefaultInstance();

			// Check the assigned value has the correct type
			if (value != null && !ValueType.IsAssignableFrom(value.GetType()))
				throw new ArgumentException($"Cannot assign to 'Value', argument has the wrong type. Expected: {ValueType.Name}  Received: {value.GetType().Name} with value '{value}'");

			// Save the assigned value
			m_value = value;


			// Set 'Text' to match the value
			ValueToTextIfNotFocused(value);

			// Notify value changed
			if (notify)
				OnValueChanged(new ValueEventArgs(Value));

			Trace($"SetValue to '{value}'");
		}

		/// <summary>True if a valid value is selected</summary>
		public bool Valid => Value != null || Nullable.GetUnderlyingType(ValueType) != null;

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
			if (Focused)
				return;

			// Prevent reentrancy
			if (m_internal_set_text != 0) return;
			using (Scope.Create(() => ++m_internal_set_text, () => --m_internal_set_text))
				Text = ValueToText(value);

			UpdateValidationColours();
		}

		/// <summary>Set the Fore and Back colours for the value box based on the current text</summary>
		public void UpdateValidationColours()
		{
			if (!UseValidityColours || this.IsInDesignMode())
				return;

			// Set the text colour based on whether the value matches the text
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
		public new void AppendText(string text)
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

		/// <summary>Set to true to have the text selection preserved while the control doesn't have focus</summary>
		public bool PreserveSelectionThruFocusChange
		{
			get { return m_preserve_selection; }
			set
			{
				if (m_preserve_selection == value) return;
				m_preserve_selection = value;
			}
		}
		private bool m_preserve_selection;
		private RangeI m_selection; // don't use a Scope for this. We save selection more than restoring it and disposed old scopes will restore the selection.

		/// <summary>Preserve the selection in the control</summary>
		public Scope PreserveTextSelection()
		{
			return Scope.Create(
				() => SaveTextSelection(),
				sr => RestoreTextSelection(sr));
		}

		/// <summary>Restore the selection</summary>
		public void RestoreTextSelection(RangeI selection)
		{
			//System.Diagnostics.Trace.WriteLine($"Restoring Selection: [{selection.Begi},{ selection.Sizei}]");
			Select(selection.Begi, selection.Sizei);
			//System.Diagnostics.Trace.WriteLine($"Selection is now: [{SelectionStart},{ SelectionLength}]");
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

		/// <summary>For debugging this bastard...</summary>
		[Conditional("PR_VB_TRACE")]
		private void Trace(string message)
		{
			Debug.WriteLine($"{message}\n\tValue={Value}\n\tText={Text}");
		}
	}
}
