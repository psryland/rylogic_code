using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Rylogic.Common;

namespace RyLogViewer.Options
{
	public class Format :SettingsSet<Format>
	{
		public Format()
		{
			Formatter = Constants.DefaultFormatter;
			ColumnCount = 1;
			Encoding = string.Empty; // empty means auto detect
			LineEnding = string.Empty; // stored in humanised form, empty means auto detect
			ColDelimiter = string.Empty; // stored in humanised form, empty means auto detect
			RowHeight = Constants.RowHeightDefault;
			AlternateLineColours = true;
			LineSelectBackColour = Colors.DarkGreen;
			LineSelectForeColour = Colors.White;
			LineBackColour1 = Colors.WhiteSmoke;
			LineBackColour2 = Colors.White;
			LineForeColour1 = Colors.Black;
			LineForeColour2 = Colors.Black;
		}

		/// <summary></summary>
		public string Formatter
		{
			get { return get<string>(nameof(Formatter)); }
			set { set(nameof(Formatter), value); }
		}

		/// <summary></summary>
		public int ColumnCount
		{
			get { return get<int>(nameof(ColumnCount)); }
			set { set(nameof(ColumnCount), value); }
		}

		/// <summary>File encoding. Blank means auto detect</summary>
		public string Encoding
		{
			get { return get<string>(nameof(Encoding)); }
			set { set(nameof(Encoding), value); }
		}

		/// <summary></summary>
		public string LineEnding
		{
			get { return get<string>(nameof(LineEnding)); }
			set { set(nameof(LineEnding), value); }
		}

		/// <summary></summary>
		public string ColDelimiter
		{
			get { return get<string>(nameof(ColDelimiter)); }
			set { set(nameof(ColDelimiter), value); }
		}

		/// <summary></summary>
		public int RowHeight
		{
			get { return get<int>(nameof(RowHeight)); }
			set { set(nameof(RowHeight), value); }
		}

		/// <summary>Alternating line colours in the main view</summary>
		public bool AlternateLineColours
		{
			get { return get<bool>(nameof(AlternateLineColours)); }
			set { set(nameof(AlternateLineColours), value); }
		}

		/// <summary></summary>
		public Color LineSelectBackColour
		{
			get { return get<Color>(nameof(LineSelectBackColour)); }
			set { set(nameof(LineSelectBackColour), value); }
		}

		/// <summary></summary>
		public Color LineSelectForeColour
		{
			get { return get<Color>(nameof(LineSelectForeColour)); }
			set { set(nameof(LineSelectForeColour), value); }
		}

		/// <summary></summary>
		public Color LineBackColour1
		{
			get { return get<Color>(nameof(LineBackColour1)); }
			set { set(nameof(LineBackColour1), value); }
		}

		/// <summary></summary>
		public Color LineBackColour2
		{
			get { return get<Color>(nameof(LineBackColour2)); }
			set { set(nameof(LineBackColour2), value); }
		}

		/// <summary></summary>
		public Color LineForeColour1
		{
			get { return get<Color>(nameof(LineForeColour1)); }
			set { set(nameof(LineForeColour1), value); }
		}

		/// <summary></summary>
		public Color LineForeColour2
		{
			get { return get<Color>(nameof(LineForeColour2)); }
			set { set(nameof(LineForeColour2), value); }
		}

		/// <summary>Validate settings</summary>
		public override Exception Validate()
		{
			int column_count = ColumnCount;
			if (column_count < Constants.ColumnCountMin || column_count > Constants.ColumnCountMax)
				ColumnCount = Constants.ColumnCountDefault;

			// Row height within range
			int row_height = RowHeight;
			if (row_height < Constants.RowHeightMinHeight || row_height > Constants.RowHeightMaxHeight)
				RowHeight = Constants.RowHeightDefault;

			return null;
		}
	}
}
