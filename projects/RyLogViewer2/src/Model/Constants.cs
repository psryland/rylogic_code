using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Rylogic.Utility;

namespace RyLogViewer
{
	public static class Constants
	{
		public const string SupportEmail = "support@rylogic.co.nz";
		public const string FreeLicence = "Free Edition Licence";
		//public const string AppIdentifier = "rylogviewer";
		//public const string Purchase = "purchase";
		//public const string StoreLink = "http://www.rylogic.co.nz/rylogviewer/index.php";

		//public const string PatternSetVersion = "v1.0";

		public static readonly string LogFileFilter = Util.FileDialogFilter("Text Files", "*.txt", "*.log", "Comma Separated Values Files", "*.csv", "JSON Files", "*.json", "All files", "*.*");
		public static readonly string SettingsFileFilter = Util.FileDialogFilter("Settings Files", "*.xml", "All files", "*.*");
		//public static readonly string XmlOrCsvFileFilter = Util.FileDialogFilter("XML Files", "*.xml", "CSV Files", "*.csv");
		public static readonly string PatternSetFilter = Util.FileDialogFilter("Pattern Set Files", "*.pattern_set");
		public static readonly string ExecutablesFilter = Util.FileDialogFilter("Executables", "*.exe", "*.bat", "*.cmd", "*.com", "All files", "*.*");

		public const string DefaultFormatter = "Single Lines";
		public const int MaxHistoryDefault = 10;
		public const int PortNumberMin = 0;
		public const int PortNumberWebProxyDefault = 8080;
		public const int PortNumberMax = 65535;
		public const int FileBufSizeMin = 256 * OneKB;
		public const int FileBufSizeDefault = 1 * OneMB;
		public const int FileBufSizeMax = 100 * OneMB;
		public const int MaxLineLengthMin = 1 * OneKB;
		public const int MaxLineLengthDefault = 16 * OneKB;
		public const int MaxLineLengthMax = 128 * OneKB;
		public const int LineCacheCountMin = 3;
		public const int LineCacheCountDefault = 1000;
		public const int LineCacheCountMax = 99999999;
		public const int ColumnCountMin = 1;
		public const int ColumnCountDefault = 1;
		public const int ColumnCountMax = 256;
		public const int FileScrollMinWidth = 16;
		public const int FileScrollMaxWidth = 200;
		public const int FileScrollWidthDefault = 20;
		public const int RowHeightMinHeight = 1;
		public const int RowHeightMaxHeight = 200;
		public const int RowHeightDefault = 18;
		public const int FilePollingRate = 100;
		public const int MaxProgramHistoryLength = 10;
		public const int MaxNetConnHistoryLength = 10;
		public const int MaxSerialConnHistoryLength = 1;
		public const int MaxOutputFileHistoryLength = 10;
		public const int MaxFindHistory = 10;
		public const int OneKB = 1024;
		public const int OneMB = 1024 * 1024;

		public static readonly Color[] BkColors = new[]
		{
			Colors.LightGreen, Colors.LightBlue, Colors.LightCoral, Colors.LightSalmon, Colors.Violet, Colors.LightSkyBlue,
			Colors.Aquamarine, Colors.Yellow, Colors.Orchid, Colors.GreenYellow, Colors.PaleGreen, Colors.Goldenrod, Colors.MediumTurquoise
		};
	}
}
