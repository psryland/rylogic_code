﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;
using Rylogic.Windows32;

namespace Rylogic.Gui
{
	// Notes:
	//  ComboBox is very similar to this class, maintain both.

	[Serializable]
	[DebuggerDisplay("Value={Value} Text={Text} Valid={Valid}")]
	public class ValueBox :TextBox
	{
		// Notes:
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
		private int m_in_set_text;

		public ValueBox()
			:base()
		{
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
			// Commit on return
			if (m.Msg == Win32.WM_KEYDOWN && Win32.ToVKey(m.WParam) == Keys.Return && !Multiline)
				TryCommitValue();

			return base.PreProcessMessage(ref m);
		}
		protected override void OnTextChanged(EventArgs e)
		{
			// Invalidate the cached 'valid' state whenever the text changes
			m_valid = null;

			// Prevent reentrancy
			if (m_in_set_text != 0) return;
			using (Scope.Create(() => ++m_in_set_text, () => --m_in_set_text))
				TextToValueIfValid();

			base.OnTextChanged(e);
		}
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			if (CommitValueOnFocusLost && !ContainsFocus)
			{
				TryCommitValue();
				var text = ValueToText(Value);
				Text = text;
			}
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			m_valid = null;
			base.OnInvalidated(e);
		}

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
					return x?.ToString() ?? string.Empty;
				});
			}
			set
			{
				m_value_to_text = value;
				ValueToTextIfNotFocused();
			}
		}
		private Func<object, string> m_value_to_text;

		/// <summary>The value represented in the text box</summary>
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
			if (!ValueType.IsClass && value == null)
				value = ValueType.DefaultInstance();

			// Check the assigned value has the correct type
			if (value != null && !ValueType.IsAssignableFrom(value.GetType()))
				throw new ArgumentException($"Cannot assign to 'Value', argument has the wrong type. Expected: {ValueType.Name}  Received: {value.GetType().Name}");

			// Assign the value
			m_value = value;

			// Only update the text when not focused to prevent
			// the text changing while the user is typing.
			ValueToTextIfNotFocused();

			// Notify value changed
			if (notify)
				OnValueChanged(new ValueEventArgs(Value));
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
			if (!UseValidityColours)
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
	}
}