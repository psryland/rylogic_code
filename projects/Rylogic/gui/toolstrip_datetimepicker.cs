using System;
using System.Windows.Forms;
using pr.extn;

namespace pr.gui
{
	public class ToolStripDateTimePicker :ToolStripControlHost
	{
		public ToolStripDateTimePicker() :base(new DateTimePicker()) {}
		public ToolStripDateTimePicker(string name) :base(new DateTimePicker{Name = name + "_hosted"}, name) {}

		/// <summary>The hosted control</summary>
		public DateTimePicker DateTimePicker { get { return Control.As<DateTimePicker>(); } }

		/// <summary>Gets or sets the custom date/time format string.</summary>
		public string CustomFormat
		{
			get { return DateTimePicker.CustomFormat; }
			set { DateTimePicker.CustomFormat = value; }
		}

		/// <summary>Gets or sets the format of the date and time displayed in the control.</summary>
		public DateTimePickerFormat Format
		{
			get { return DateTimePicker.Format; }
			set { DateTimePicker.Format = value; }
		}

		/// <summary>
		/// Gets or sets the minimum date and time that can be selected in the control.
		/// If 'Value' is outside the new [MinDate,MaxDate] range it gets clamped to the new range</summary>
		public DateTimeOffset MinDate
		{
			get { return DateTimePicker.MinDate; }
			set { DateTimePicker.MinDate = DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc); }
		}

		/// <summary>
		/// Gets or sets the maximum date and time that cann be selected in the control.
		/// If 'Value' is outside the new [MinDate,MaxDate] range it gets clamped to the new range</summary>
		public DateTimeOffset MaxDate
		{
			get { return DateTimePicker.MaxDate; }
			set { DateTimePicker.MaxDate = DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc); }
		}

		/// <summary>
		/// The selected date time value.
		/// Value must be within [MinDate,MaxDate] or an exception is thrown</summary>
		public DateTimeOffset Value
		{
			get { return DateTimePicker.Value; }
			set { DateTimePicker.Value = DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc); }
		}
	}
}
