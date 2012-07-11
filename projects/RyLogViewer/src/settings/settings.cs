using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.common;

namespace RyLogViewer
{
	/// <summary>RyLog Viewer settings</summary>
	public sealed class Settings :SettingsBase<Settings>
	{
		public string RecentFiles
		{
			get { return get<string>("RecentFiles"); }
			set { set("RecentFiles", value); }
		}
		public Font   Font
		{
			get { return get<Font>("Font"); }
			set { set("Font", value); }
		}
		public bool   RestoreScreenLoc
		{
			get { return get<bool>("RestoreScreenLoc"); }
			set { set("RestoreScreenLoc", value); }
		}
		public Point  ScreenPosition
		{
			get { return get<Point>("ScreenPosition"); }
			set { set("ScreenPosition", value); }
		}
		public Size   WindowSize
		{
			get { return get<Size>("WindowSize"); }
			set { set("WindowSize", value); }
		}
		public bool   AlternateLineColours
		{
			get { return get<bool>("AlternateLineColours"); }
			set { set("AlternateLineColours", value); }
		}
		public Color  LineSelectBackColour
		{
			get { return get<Color>("LineSelectBackColour"); }
			set { set("LineSelectBackColour", value); }
		}
		public Color  LineSelectForeColour
		{
			get { return get<Color>("LineSelectForeColour"); }
			set { set("LineSelectForeColour", value); }
		}
		public Color  LineBackColour1
		{
			get { return get<Color>("LineBackColour1"); }
			set { set("LineBackColour1", value); }
		}
		public Color  LineBackColour2
		{
			get { return get<Color>("LineBackColour2"); }
			set { set("LineBackColour2", value); }
		}
		public Color  LineForeColour1
		{
			get { return get<Color>("LineForeColour1"); }
			set { set("LineForeColour1", value); }
		}
		public Color  LineForeColour2
		{
			get { return get<Color>("LineForeColour2"); }
			set { set("LineForeColour2", value); }
		}
		public int    FileScrollWidth
		{
			get { return get<int>("FileScrollWidth"); }
			set { set("FileScrollWidth", value); }
		}
		public Color  ScrollBarFileRangeColour
		{
			get { return get<Color>("ScrollBarFileRangeColour"); }
			set { set("ScrollBarFileRangeColour", value); }
		}
		public Color  ScrollBarDisplayRangeColour
		{
			get { return get<Color>("ScrollBarDisplayRangeColour"); }
			set { set("ScrollBarDisplayRangeColour", value); }
		}
		public int    RowHeight
		{
			get { return get<int>("RowHeight"); }
			set { set("RowHeight", value); }
		}
		public bool   LoadLastFile
		{
			get { return get<bool>("LoadLastFile"); }
			set { set("LoadLastFile", value); }
		}
		public string LastLoadedFile
		{
			get { return get<string>("LastLoadedFile"); }
			set { set("LastLoadedFile", value); }
		}
		public bool   OpenAtEnd
		{
			get { return get<bool>("OpenAtEnd"); }
			set { set("OpenAtEnd", value); }
		}
		public bool   FileChangesAdditive
		{
			get { return get<bool>("FileChangesAdditive"); }
			set { set("FileChangesAdditive", value); }
		}
		public bool   IgnoreBlankLines
		{
			get { return get<bool>("IgnoreBlankLines"); }
			set { set("IgnoreBlankLines", value); }
		}
		public bool   AlwaysOnTop
		{
			get { return get<bool>("AlwaysOnTop"); }
			set { set("AlwaysOnTop", value); }
		}
		public bool   ShowTOTD
		{
			get { return get<bool>("ShowTOTD"); }
			set { set("ShowTOTD", value); }
		}
		public bool   CheckForUpdates
		{
			get { return get<bool>("CheckForUpdates"); }
			set { set("CheckForUpdates", value); }
		}
		public bool   HighlightsEnabled
		{
			get { return get<bool>("HighlightsEnabled"); }
			set { set("HighlightsEnabled", value); }
		}
		public bool   FiltersEnabled
		{
			get { return get<bool>("FiltersEnabled"); }
			set { set("FiltersEnabled", value); }
		}
		public bool   TransformsEnabled
		{
			get { return get<bool>("TransformsEnabled"); }
			set { set("TransformsEnabled", value); }
		}
		public bool   ActionsEnabled
		{
			get { return get<bool>("ActionsEnabled"); }
			set { set("ActionsEnabled", value); }
		}
		public bool   WatchEnabled
		{
			get { return get<bool>("WatchEnabled"); }
			set { set("WatchEnabled", value); }
		}
		public int    FileBufSize
		{
			get { return get<int>("FileBufSize"); }
			set { set("FileBufSize", value); }
		}
		public int    MaxLineLength
		{
			get { return get<int>("MaxLineLength"); }
			set { set("MaxLineLength", value); }
		}
		public int    LineCacheCount
		{
			get { return get<int>("LineCacheCount"); }
			set { set("LineCacheCount", value); }
		}
		public string HighlightPatterns
		{
			get { return get<string>("HighlightPatterns"); }
			set { set("HighlightPatterns", value); }
		}
		public string FilterPatterns
		{
			get { return get<string>("FilterPatterns"); }
			set { set("FilterPatterns", value); }
		}
		public string TransformPatterns
		{
			get { return get<string>("TransformPatterns"); }
			set { set("TransformPatterns", value); }
		}
		public string ActionPatterns
		{
			get { return get<string>("ActionPatterns"); }
			set { set("ActionPatterns", value); }
		}
		public string HighlightPatternSets
		{
			get { return get<string>("HighlightPatternSets"); }
			set { set("HighlightPatternSets", value); }
		}
		public string FilterPatternSets
		{
			get { return get<string>("FilterPatternSets"); }
			set { set("FilterPatternSets", value); }
		}
		public string TransformPatternSets
		{
			get { return get<string>("TransformPatternSets"); }
			set { set("TransformPatternSets", value); }
		}
		public string ActionPatternSets
		{
			get { return get<string>("ActionPatternSets"); }
			set { set("ActionPatternSets", value); }
		}
		public string RowDelimiter
		{
			get { return get<string>("RowDelimiter"); }
			set { set("RowDelimiter", value); }
		}
		public string ColDelimiter
		{
			get { return get<string>("ColDelimiter"); }
			set { set("ColDelimiter", value); }
		}
		public int    ColumnCount
		{
			get { return get<int>("ColumnCount"); }
			set { set("ColumnCount", value); }
		}
		public string Encoding
		{
			get { return get<string>("Encoding"); }
			set { set("Encoding", value); }
		}
		public string[] FindHistory
		{
			get { return get<string[]>("FindHistory"); }
			set { set("FindHistory", value); }
		}
		public string[] OutputFilepathHistory
		{
			get { return get<string[]>("OutputFilepathHistory"); }
			set { set("OutputFilepathHistory", value); }
		}
		public LaunchApp[] LogProgramOutputHistory
		{
			get { return get<LaunchApp[]>("LogProgramOutputHistory"); }
			set { set("LogProgramOutputHistory", value); }
		}
		public NetConn[] NetworkConnectionHistory
		{
			get { return get<NetConn[]>("NetworkConnectionHistory"); }
			set { set("NetworkConnectionHistory", value); }
		}
		public SerialConn[] SerialConnectionHistory
		{
			get { return get<SerialConn[]>("SerialConnectionHistory"); }
			set { set("SerialConnectionHistory", value); }
		}
		public PipeConn[] PipeConnectionHistory
		{
			get { return get<PipeConn[]>("PipeConnectionHistory"); }
			set { set("PipeConnectionHistory", value); }
		}

		// Default construct settings
		public Settings()
		{
			RecentFiles                     = "";
			Font                            = new Font("Microsoft Sans Serif", 8.25f, GraphicsUnit.Point);
			RestoreScreenLoc                = true;
			ScreenPosition                  = new Point(50, 50);
			WindowSize                      = new Size(640, 480);
			AlternateLineColours            = true;
			LineSelectBackColour            = Color.DarkGreen;
			LineSelectForeColour            = Color.White;
			LineBackColour1                 = Color.WhiteSmoke;
			LineBackColour2                 = Color.White;
			LineForeColour1                 = Color.Black;
			LineForeColour2                 = Color.Black;
			FileScrollWidth                 = Constants.FileScrollWidthDefault;
			ScrollBarFileRangeColour        = Color.FromArgb(128, Color.White);
			ScrollBarDisplayRangeColour     = Color.FromArgb(128, Color.SteelBlue);
			RowHeight                       = Constants.RowHeightDefault;
			LoadLastFile                    = false;
			LastLoadedFile                  = "";
			OpenAtEnd                       = true;
			FileChangesAdditive             = true;
			IgnoreBlankLines                = false;
			AlwaysOnTop                     = false;
			ShowTOTD                        = true;
			CheckForUpdates                 = true;
			HighlightsEnabled               = true;
			FiltersEnabled                  = true;
			TransformsEnabled               = true;
			ActionsEnabled                  = true;
			WatchEnabled                    = false;
			FileBufSize                     = Constants.FileBufSizeDefault;
			MaxLineLength                   = Constants.MaxLineLengthDefault;
			LineCacheCount                  = Constants.LineCacheCountDefault;
			HighlightPatterns               = "<root/>";
			FilterPatterns                  = "<root/>";
			TransformPatterns               = "<root/>";
			ActionPatterns                  = "<root/>";
			HighlightPatternSets            = "<root/>";
			FilterPatternSets               = "<root/>";
			TransformPatternSets            = "<root/>";
			ActionPatternSets               = "<root/>";
			RowDelimiter                    = ""; // stored in humanised form
			ColDelimiter                    = ""; // stored in humanised form
			ColumnCount                     = 1;
			Encoding                        = "";
			FindHistory                     = new string[0];
			OutputFilepathHistory           = new string[0];
			LogProgramOutputHistory         = new LaunchApp[0];
			NetworkConnectionHistory        = new NetConn[0];
			SerialConnectionHistory         = new SerialConn[0];
			PipeConnectionHistory           = new PipeConn[0];
		}
		public Settings(string filepath)
		:base(filepath)
		{}
		
		/// <summary>Perform validation on the loaded settings</summary>
		public override void Validate()
		{
			// If restoring the screen location, ensure it's onscreen
			if (RestoreScreenLoc)
			{
				Size sz = WindowSize;
				Point pt = ScreenPosition;
				bool valid =
					sz.Width  < SystemInformation.VirtualScreen.Width &&
					sz.Height < SystemInformation.VirtualScreen.Height &&
					pt.X >= SystemInformation.VirtualScreen.Left && pt.X < SystemInformation.VirtualScreen.Right - 20 &&
					pt.Y >= SystemInformation.VirtualScreen.Top  && pt.Y < SystemInformation.VirtualScreen.Bottom - 20;
				if (!valid) RestoreScreenLoc = false;
			}

			// File scroll width must be within range
			int file_scroll_width = FileScrollWidth;
			if (file_scroll_width < Constants.FileScrollMinWidth || file_scroll_width > Constants.FileScrollMaxWidth)
				FileScrollWidth = Constants.FileScrollWidthDefault;

			// Row height within range
			int row_height = RowHeight;
			if (row_height < Constants.RowHeightMinHeight || row_height > Constants.RowHeightMaxHeight)
				RowHeight = Constants.RowHeightDefault;

			// File buffer size
			int file_buf_size = FileBufSize;
			if (file_buf_size < Constants.FileBufSizeMin || file_buf_size > Constants.FileBufSizeMax)
				FileBufSize = Constants.FileBufSizeDefault;
			
			// Max line length
			int max_line_length = MaxLineLength;
			if (max_line_length < Constants.MaxLineLengthMin || max_line_length > Constants.MaxLineLengthMax)
				MaxLineLength = Constants.MaxLineLengthDefault;

			// Line cache count
			int line_cache_count = LineCacheCount;
			if (line_cache_count < Constants.LineCacheCountMin || line_cache_count > Constants.LineCacheCountMax)
				LineCacheCount = Constants.LineCacheCountDefault;

			int column_count = ColumnCount;
			if (column_count < Constants.ColumnCountMin || column_count > Constants.ColumnCountMax)
				ColumnCount = Constants.ColumnCountDefault;
		}
		
		/// <summary>Types the serialiser needs to know about</summary>
		protected override IEnumerable<Type> KnownTypes
		{
			get
			{
				return new[]
				{
					typeof(string[]),
					typeof(StandardStreams),
					typeof(LaunchApp),
					typeof(LaunchApp[]),
					typeof(NetConn),
					typeof(NetConn[]),
					typeof(SerialConn),
					typeof(SerialConn[]),
					typeof(PipeConn),
					typeof(PipeConn[])
				};
			}
		}
	}
}
