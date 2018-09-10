using System;
using Rylogic.Extn;

namespace Rylogic.Gui.WinForms
{
	/// <summary>DateTimePicker with support for UTC/Local</summary>
	public class DateTimePicker :System.Windows.Forms.DateTimePicker
	{
		///<remarks>
		/// The DateTime values in the base.DateTimePicker have 'Kind' as unspecified since the control
		/// does not provide a way for the user to select UTC/Local. This sub-classed control adds the 'Kind'
		/// property and ensures the value,min,max properties are get/set is this kind.
		/// Notes:
		///  Conversion from DTO to DT leaves Kind as unspecified.
		///  Conversion from DT to DTO sets Offset. If DT.Kind is unspecified, the local time-zone offset is assumed
		///</remarks>

		public DateTimePicker()
		{
			// Default behaviour is the same is System.Windows.Forms.DateTimePicker
			Kind = DateTimeKind.Unspecified;
		}

		/// <summary>
		/// The default DateTime.Kind.
		/// DateTime values in the base.DateTimePicker have 'Kind' as unspecified since the control
		/// does not provide a way for the user to select UTC/Local. This value sets the Kind value
		/// used in all control values. Changing this value only changes the 'Kind' property on the
		/// MinData, MaxDate, and Value properties</summary>
		public DateTimeKind Kind
		{
			get { return m_kind; }
			set
			{
				if (m_kind == value) return;
				var min = DateTime.SpecifyKind(MinDate, value);
				var max = DateTime.SpecifyKind(MaxDate, value);
				var val = DateTime.SpecifyKind(Value  , value);
				m_kind = value;
				this.Set(val, min, max);
			}
		}
		private DateTimeKind m_kind;

		///<summary>
		/// Gets or sets the minimum date and time that can be selected in the control.
		/// Returns: The minimum date and time that can be selected in the control. The default is 1/1/1753 00:00:00.
		/// Exceptions:
		///   System.ArgumentException: The value assigned is not less than the System.Windows.Forms.DateTimePicker.MaxDate value.
		///   System.SystemException:   The value assigned is less than the System.Windows.Forms.DateTimePicker.MinDateTime value.
		/// Note: Implicit conversion of this value to a DateTimeOffset should correctly set 'Offset' based on 'Kind'
		///</summary>
		public new DateTime MinDate
		{
			get { return base.MinDate.As(Kind); }
			set
			{
				if (value.Kind != Kind) throw new Exception("DateTime.Kind value incorrect");
				value = DateTime_.MaskByFormat(value, CustomFormat);
				base.MinDate = value.As(DateTimeKind.Unspecified);
			}
		}

		///<summary>
		/// Gets or sets the maximum date and time that can be selected in the control.
		/// Returns: The maximum date and time that can be selected in the control. The default is 12/31/9998 23:59:59.
		/// Exceptions:
		///   System.ArgumentException: The value assigned is less than the System.Windows.Forms.DateTimePicker.MinDate value.
		///   System.SystemException:   The value assigned is greater than the System.Windows.Forms.DateTimePicker.MaxDateTime value.
		/// Note: Implicit conversion of this value to a DateTimeOffset should correctly set 'Offset' based on 'Kind'
		///</summary>
		public new DateTime MaxDate
		{
			get { return base.MaxDate.As(Kind); }
			set
			{
				if (value.Kind != Kind) throw new Exception("DateTime.Kind value incorrect");
				value = DateTime_.MaskByFormat(value, CustomFormat);
				base.MaxDate = value.As(DateTimeKind.Unspecified);
			}
		}

		/// <summary>
		/// Gets or sets the date/time value assigned to the control.
		/// Returns: The System.DateTime value assign to the control.
		/// Exceptions:
		///   System.ArgumentOutOfRangeException: The set value is less than System.Windows.Forms.DateTimePicker.MinDate or more than System.Windows.Forms.DateTimePicker.MaxDate.
		/// Note: Implicit conversion of this value to a DateTimeOffset should correctly set 'Offset' based on 'Kind'
		///</summary>
		public new DateTime Value
		{
			get { return base.Value.As(Kind); }
			set
			{
				if (value.Kind != Kind) throw new Exception("DateTime.Kind value incorrect");
				value = DateTime_.MaskByFormat(value, CustomFormat);
				base.Value = value.As(DateTimeKind.Unspecified);
			}
		}
	}
}
