using System;
using Rylogic.Extn;

namespace Rylogic.Gui.WinForms
{
	public static class DateTimePicker_
	{
		/// <summary>
		/// Set the MinDate, MaxDate, and Value members all at once, avoiding out of range exceptions.
		/// val, min, max must all have a specified 'Kind' value.
		/// Values are clamped to the valid range for a DateTimePicker</summary>
		public static void Set(this System.Windows.Forms.DateTimePicker dtp, DateTime val, DateTime min, DateTime max)
		{
			if (val.Kind == DateTimeKind.Unspecified) throw new Exception("DateTimePicker Value has an unspecified time-zone");
			if (min.Kind == DateTimeKind.Unspecified) throw new Exception("DateTimePicker Minimum Value has an unspecified time-zone");
			if (max.Kind == DateTimeKind.Unspecified) throw new Exception("DateTimePicker Maximum Value has an unspecified time-zone");
			if (min > max)
				throw new Exception("Minimum date/time value is greater than the maximum date/time value");
			if (val != val.Clamp(min,max))
				throw new Exception("Date/time value is not within the given range of date/time values");

			// Clamp the values to the valid range for 'DateTimePicker'
			val = val.Clamp(DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);
			min = min.Clamp(DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);
			max = max.Clamp(DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);

			// Setting to MinimumDateTime/MaximumDateTime first avoids problems if min > MaxDate or max < MinDate
			dtp.MinDate = DateTimePicker.MinimumDateTime.As(val.Kind);
			dtp.MaxDate = DateTimePicker.MaximumDateTime.As(val.Kind);

			// Setting Value before MinDate/MaxDate avoids setting Value twice when Value < MinDate or Value > MaxDate
			dtp.Value = val;
			dtp.MinDate = min;
			dtp.MaxDate = max;
		}

		/// <summary>Sets the MinDate, MaxDate, and Value members all to 'kind'.</summary>
		public static void To(this System.Windows.Forms.DateTimePicker dtp, DateTimeKind kind)
		{
			var min = dtp.MinDate.To(kind);
			var max = dtp.MaxDate.To(kind);
			var val = dtp.Value.To(kind);
			if (dtp is DateTimePicker rdtp) rdtp.Kind = kind;
			dtp.Set(val, min, max);
		}
	}
}