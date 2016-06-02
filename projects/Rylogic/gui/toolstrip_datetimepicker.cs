using System;
using System.Windows.Forms;
using pr.extn;

namespace pr.gui
{
	public class ToolStripDateTimePicker :ToolStripControlHost
	{
		public ToolStripDateTimePicker()
			:this(string.Empty)
		{}
		public ToolStripDateTimePicker(string name)
			:base(new DateTimePicker{Name = name + "_hosted"}, name)
		{}

		/// <summary>The hosted control</summary>
		public DateTimePicker DateTimePicker { get { return Control.As<DateTimePicker>(); } }

		/// <summary>The default DateTime.Kind</summary>
		public DateTimeKind Kind
		{
			get { return DateTimePicker.Kind; }
			set { DateTimePicker.Kind = value; }
		}

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

		///<summary>Gets or sets the date/time value assigned to the control.</summary>
		public DateTime Value
		{
			get { return DateTimePicker.Value; }
			set { DateTimePicker.Value = value; }
		}

		///<summary>Gets or sets the minimum date and time that can be selected in the control.</summary>
		public DateTime MinDate
		{
			get { return DateTimePicker.MinDate; }
			set { DateTimePicker.MinDate = value; }
		}

		///<summary>Gets or sets the maximum date and time that can be selected in the control.</summary>
		public DateTime MaxDate
		{
			get { return DateTimePicker.MaxDate; }
			set { DateTimePicker.MaxDate = value; }
		}
	}
}
