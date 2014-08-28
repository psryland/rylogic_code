using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;

namespace RyLogViewer
{
	/// <summary>RyLog Viewer settings</summary>
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			RecentFiles                     = string.Empty;
			Font                            = new Font("Consolas", 8.25f, GraphicsUnit.Point);
			RestoreScreenLoc                = false; // False so that first runs start in the default window position
			ScreenPosition                  = new Point(100, 100);
			WindowSize                      = new Size(640, 480);
			AlternateLineColours            = true;
			LineSelectBackColour            = Color.DarkGreen;
			LineSelectForeColour            = Color.White;
			LineBackColour1                 = Color.WhiteSmoke;
			LineBackColour2                 = Color.White;
			LineForeColour1                 = Color.Black;
			LineForeColour2                 = Color.Black;
			FileScrollWidth                 = Constants.FileScrollWidthDefault;
			ScrollBarFileRangeColour        = Color.FromArgb(0x80, Color.White);
			ScrollBarCachedRangeColour      = Color.FromArgb(0x40, Color.LightBlue);
			ScrollBarDisplayRangeColour     = Color.FromArgb(0x80, Color.SteelBlue);
			BookmarkColour                  = Color.Violet;
			RowHeight                       = Constants.RowHeightDefault;
			LoadLastFile                    = false;
			LastLoadedFile                  = string.Empty;
			OpenAtEnd                       = true;
			FullPathInTitle                 = true;
			TabSizeInSpaces                 = 4;
			FileChangesAdditive             = true;
			IgnoreBlankLines                = false;
			AlwaysOnTop                     = false;
			FirstRun                        = true;
			ShowTOTD                        = true;
			CheckForUpdates                 = false;
			CheckForUpdatesServer           = "http://www.rylogic.co.nz:80/";
			UseWebProxy                     = false;
			WebProxyHost                    = string.Empty;
			WebProxyPort                    = Constants.PortNumberWebProxyDefault;
			QuickFilterEnabled              = false;
			HighlightsEnabled               = true;
			FiltersEnabled                  = false;
			TransformsEnabled               = false;
			ActionsEnabled                  = false;
			TailEnabled                     = true;
			WatchEnabled                    = true;
			FileBufSize                     = Constants.FileBufSizeDefault;
			MaxLineLength                   = Constants.MaxLineLengthDefault;
			LineCacheCount                  = Constants.LineCacheCountDefault;
			HighlightPatterns               = DefaultHighlightingPatterns().ToArray();
			FilterPatterns                  = DefaultFilters().ToArray();
			TransformPatterns               = DefaultTransforms().ToArray();
			ActionPatterns                  = DefaultClickActions().ToArray();
			HighlightPatternSets            = "<root/>";
			FilterPatternSets               = "<root/>";
			TransformPatternSets            = "<root/>";
			ActionPatternSets               = "<root/>";
			RowDelimiter                    = string.Empty; // stored in humanised form, empty means auto detect
			ColDelimiter                    = string.Empty; // stored in humanised form, empty means auto detect
			ColumnCount                     = 1;
			Encoding                        = string.Empty; // empty means auto detect
			OutputFilepathHistory           = new string[0];
			LogProgramOutputHistory         = new LaunchApp[0];
			NetworkConnectionHistory        = new NetConn[0];
			SerialConnectionHistory         = new SerialConn[0];
			PipeConnectionHistory           = new PipeConn[0];
			AndroidLogcat                   = new AndroidLogcat();
		}
		public Settings(string filepath) :base(filepath) {}
		public Settings(Settings rhs) :base(rhs) {}

		/// <summary>The settings version, used to detect when 'Upgrade' is needed</summary>
		protected override string Version { get { return "v1.3"; } }

		public string RecentFiles
		{
			get { return get(x => x.RecentFiles); }
			set { set(x => x.RecentFiles, value); }
		}
		public Font   Font
		{
			get { return get(x => x.Font); }
			set { set(x => x.Font, value); }
		}
		public bool   RestoreScreenLoc
		{
			get { return get(x => x.RestoreScreenLoc); }
			set { set(x => x.RestoreScreenLoc, value); }
		}
		public Point  ScreenPosition
		{
			get { return get(x => x.ScreenPosition); }
			set { set(x => x.ScreenPosition, value); }
		}
		public Size   WindowSize
		{
			get { return get(x => x.WindowSize); }
			set { set(x => x.WindowSize, value); }
		}
		public bool   AlternateLineColours
		{
			get { return get(x => x.AlternateLineColours); }
			set { set(x => x.AlternateLineColours, value); }
		}
		public Color  LineSelectBackColour
		{
			get { return get(x => x.LineSelectBackColour); }
			set { set(x => x.LineSelectBackColour, value); }
		}
		public Color  LineSelectForeColour
		{
			get { return get(x => x.LineSelectForeColour); }
			set { set(x => x.LineSelectForeColour, value); }
		}
		public Color  LineBackColour1
		{
			get { return get(x => x.LineBackColour1); }
			set { set(x => x.LineBackColour1, value); }
		}
		public Color  LineBackColour2
		{
			get { return get(x => x.LineBackColour2); }
			set { set(x => x.LineBackColour2, value); }
		}
		public Color  LineForeColour1
		{
			get { return get(x => x.LineForeColour1); }
			set { set(x => x.LineForeColour1, value); }
		}
		public Color  LineForeColour2
		{
			get { return get(x => x.LineForeColour2); }
			set { set(x => x.LineForeColour2, value); }
		}
		public int    FileScrollWidth
		{
			get { return get(x => x.FileScrollWidth); }
			set { set(x => x.FileScrollWidth, value); }
		}
		public Color  ScrollBarFileRangeColour
		{
			get { return get(x => x.ScrollBarFileRangeColour); }
			set { set(x => x.ScrollBarFileRangeColour, value); }
		}
		public Color  ScrollBarCachedRangeColour
		{
			get { return get(x => x.ScrollBarCachedRangeColour); }
			set { set(x => x.ScrollBarCachedRangeColour, value); }
		}
		public Color  ScrollBarDisplayRangeColour
		{
			get { return get(x => x.ScrollBarDisplayRangeColour); }
			set { set(x => x.ScrollBarDisplayRangeColour, value); }
		}
		public Color  BookmarkColour
		{
			get { return get(x => x.BookmarkColour); }
			set { set(x => x.BookmarkColour, value); }
		}
		public int    RowHeight
		{
			get { return get(x => x.RowHeight); }
			set { set(x => x.RowHeight, value); }
		}
		public bool   LoadLastFile
		{
			get { return get(x => x.LoadLastFile); }
			set { set(x => x.LoadLastFile, value); }
		}
		public string LastLoadedFile
		{
			get { return get(x => x.LastLoadedFile); }
			set { set(x => x.LastLoadedFile, value); }
		}
		public bool   OpenAtEnd
		{
			get { return get(x => x.OpenAtEnd); }
			set { set(x => x.OpenAtEnd, value); }
		}
		public bool   FullPathInTitle
		{
			get { return get(x => x.FullPathInTitle); }
			set { set(x => x.FullPathInTitle, value); }
		}
		public int    TabSizeInSpaces
		{
			get { return get(x => x.TabSizeInSpaces); }
			set { set(x => x.TabSizeInSpaces, value); }
		}
		public bool   FileChangesAdditive
		{
			get { return get(x => x.FileChangesAdditive); }
			set { set(x => x.FileChangesAdditive, value); }
		}
		public bool   IgnoreBlankLines
		{
			get { return get(x => x.IgnoreBlankLines); }
			set { set(x => x.IgnoreBlankLines, value); }
		}
		public bool   AlwaysOnTop
		{
			get { return get(x => x.AlwaysOnTop); }
			set { set(x => x.AlwaysOnTop, value); }
		}
		public bool   FirstRun
		{
			get { return get(x => x.FirstRun); }
			set { set(x => x.FirstRun, value); }
		}
		public bool   ShowTOTD
		{
			get { return get(x => x.ShowTOTD); }
			set { set(x => x.ShowTOTD, value); }
		}
		public bool   CheckForUpdates
		{
			get { return get(x => x.CheckForUpdates); }
			set { set(x => x.CheckForUpdates, value); }
		}
		public string CheckForUpdatesServer
		{
			get { return get(x => x.CheckForUpdatesServer); }
			set { set(x => x.CheckForUpdatesServer, value); }
		}
		public bool   UseWebProxy
		{
			get { return get(x => x.UseWebProxy); }
			set { set(x => x.UseWebProxy, value); }
		}
		public string WebProxyHost
		{
			get { return get(x => x.WebProxyHost); }
			set { set(x => x.WebProxyHost, value); }
		}
		public int    WebProxyPort
		{
			get { return get(x => x.WebProxyPort); }
			set { set(x => x.WebProxyPort, value); }
		}
		public bool   QuickFilterEnabled
		{
			get { return get(x => x.QuickFilterEnabled); }
			set { set(x => x.QuickFilterEnabled, value); }
		}
		public bool   HighlightsEnabled
		{
			get { return get(x => x.HighlightsEnabled); }
			set { set(x => x.HighlightsEnabled, value); }
		}
		public bool   FiltersEnabled
		{
			get { return get(x => x.FiltersEnabled); }
			set { set(x => x.FiltersEnabled, value); }
		}
		public bool   TransformsEnabled
		{
			get { return get(x => x.TransformsEnabled); }
			set { set(x => x.TransformsEnabled, value); }
		}
		public bool   ActionsEnabled
		{
			get { return get(x => x.ActionsEnabled); }
			set { set(x => x.ActionsEnabled, value); }
		}
		public bool   TailEnabled
		{
			get { return get(x => x.TailEnabled); }
			set { set(x => x.TailEnabled, value); }
		}
		public bool   WatchEnabled
		{
			get { return get(x => x.WatchEnabled); }
			set { set(x => x.WatchEnabled, value); }
		}
		public int    FileBufSize
		{
			get { return get(x => x.FileBufSize); }
			set { set(x => x.FileBufSize, value); }
		}
		public int    MaxLineLength
		{
			get { return get(x => x.MaxLineLength); }
			set { set(x => x.MaxLineLength, value); }
		}
		public int    LineCacheCount
		{
			get { return get(x => x.LineCacheCount); }
			set { set(x => x.LineCacheCount, value); }
		}
		public Highlight[] HighlightPatterns
		{
			get { return get(x => x.HighlightPatterns); }
			set { set(x => x.HighlightPatterns, value); }
		}
		public Filter[] FilterPatterns
		{
			get { return get(x => x.FilterPatterns); }
			set { set(x => x.FilterPatterns, value); }
		}
		public Transform[] TransformPatterns
		{
			get { return get(x => x.TransformPatterns); }
			set { set(x => x.TransformPatterns, value); }
		}
		public ClkAction[] ActionPatterns
		{
			get { return get(x => x.ActionPatterns); }
			set { set(x => x.ActionPatterns, value); }
		}
		public string HighlightPatternSets
		{
			get { return get(x => x.HighlightPatternSets); }
			set { set(x => x.HighlightPatternSets, value); }
		}
		public string FilterPatternSets
		{
			get { return get(x => x.FilterPatternSets); }
			set { set(x => x.FilterPatternSets, value); }
		}
		public string TransformPatternSets
		{
			get { return get(x => x.TransformPatternSets); }
			set { set(x => x.TransformPatternSets, value); }
		}
		public string ActionPatternSets
		{
			get { return get(x => x.ActionPatternSets); }
			set { set(x => x.ActionPatternSets, value); }
		}
		public string RowDelimiter
		{
			get { return get(x => x.RowDelimiter); }
			set { set(x => x.RowDelimiter, value); }
		}
		public string ColDelimiter
		{
			get { return get(x => x.ColDelimiter); }
			set { set(x => x.ColDelimiter, value); }
		}
		public int    ColumnCount
		{
			get { return get(x => x.ColumnCount); }
			set { set(x => x.ColumnCount, value); }
		}
		public string Encoding
		{
			get { return get(x => x.Encoding); }
			set { set(x => x.Encoding, value); }
		}
		public string[] OutputFilepathHistory
		{
			get { return get(x => x.OutputFilepathHistory); }
			set { set(x => x.OutputFilepathHistory, value); }
		}
		public LaunchApp[] LogProgramOutputHistory
		{
			get { return get(x => x.LogProgramOutputHistory); }
			set { set(x => x.LogProgramOutputHistory, value); }
		}
		public NetConn[] NetworkConnectionHistory
		{
			get { return get(x => x.NetworkConnectionHistory); }
			set { set(x => x.NetworkConnectionHistory, value); }
		}
		public SerialConn[] SerialConnectionHistory
		{
			get { return get(x => x.SerialConnectionHistory); }
			set { set(x => x.SerialConnectionHistory, value); }
		}
		public PipeConn[] PipeConnectionHistory
		{
			get { return get(x => x.PipeConnectionHistory); }
			set { set(x => x.PipeConnectionHistory, value); }
		}
		public AndroidLogcat AndroidLogcat
		{
			get { return get(x => x.AndroidLogcat); }
			set { set(x => x.AndroidLogcat, value); }
		}

		/// <summary>Return the highlighting patterns for a default instance of the settings</summary>
		private IEnumerable<Highlight> DefaultHighlightingPatterns()
		{
			yield return new Highlight
				{
					Expr        = @"(Error:)|(E/)",
					PatnType    = EPattern.RegularExpression,
					ForeColour  = Color.FromArgb(0xff,0xff,0xff,0xff),
					BackColour  = Color.FromArgb(0xff,0x8b,0x00,0x00),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				};
			yield return new Highlight
				{
					Expr        = @"(Warn:)|(W/)",
					PatnType    = EPattern.RegularExpression,
					ForeColour  = Color.FromArgb(0xff,0x00,0x00,0x00),
					BackColour  = Color.FromArgb(0xff,0xff,0xff,0x00),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				};
			yield return new Highlight
				{
					Expr        = @"(Info:)|(I/)",
					PatnType    = EPattern.RegularExpression,
					ForeColour  = Color.FromArgb(0xff,0x00,0x00,0x00),
					BackColour  = Color.FromArgb(0xff,0xc4,0xff,0xff),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				};
			yield return new Highlight
				{
					Expr        = @"#",
					PatnType    = EPattern.Substring,
					ForeColour  = Color.FromArgb(0xff,0x00,0x00,0x00),
					BackColour  = Color.FromArgb(0xff,0xc4,0xff,0xc7),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				};
			yield return new Highlight
				{
					Expr = @"\w+\.txt",
					Active = true,
					PatnType = EPattern.RegularExpression,
					IgnoreCase = false,
					Invert = false,
					WholeLine = false,
					ForeColour = Color.FromArgb(0xFF,0x2A,0x00,0xFF),
					BackColour = Color.FromArgb(0xFF,0xB3,0xCD,0xF2),
					BinaryMatch = false,
				};
		}

		/// <summary>Return the filter patterns for a default instance of the settings</summary>
		private IEnumerable<Filter> DefaultFilters()
		{
			yield return new Filter
				{
					Expr = @"##",
					Active = true,
					PatnType = EPattern.Substring,
					IgnoreCase = false,
					Invert = false,
					WholeLine = false,
					IfMatch = EIfMatch.Reject
				};
		}

		/// <summary>Return the transforms for a default instance of the settings</summary>
		private IEnumerable<Transform> DefaultTransforms()
		{
			yield break;
		}

		/// <summary>Return the click action patterns for a default instance of the settings</summary>
		private IEnumerable<ClkAction> DefaultClickActions()
		{
			yield return new ClkAction
				{
					Expr = @"\w+\.txt",
					Active = true,
					PatnType = EPattern.RegularExpression,
					IgnoreCase = false,
					Invert = false,
					WholeLine = false,
					Executable = @"C:\windows\notepad.exe",
					Arguments = @"{FilePath}",
					WorkingDirectory = string.Empty
				};
		}

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

			// Network connection settings
			foreach (var c in NetworkConnectionHistory)
			{
				if (c.ProtocolType != ProtocolType.Tcp && c.ProtocolType != ProtocolType.Udp)
					c.ProtocolType = ProtocolType.Tcp;
			}
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		public override void Upgrade(XElement settings, string from_version)
		{
			for (;from_version != Version;)
			{
				switch (from_version)
				{
				default:
					base.Upgrade(settings, from_version);
					break;
				case "vX.X": // example
					from_version = Upgrade_vXX_to_vYY(settings);
					break;
				}
			}
		}

		// ReSharper disable PossibleNullReferenceException
		/// <summary>Example upgrade method</summary>
		private string Upgrade_vXX_to_vYY(XElement settings)
		{
			// Modify the contents of 'settings'
			// Do not use any types that might change over time

			// Done, return the version
			return "vY.Y";
		}
		// ReSharper restore PossibleNullReferenceException
	}
}
