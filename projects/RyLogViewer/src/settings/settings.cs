using System.Drawing;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Utility;

namespace RyLogViewer
{
	/// <summary>RyLog Viewer settings</summary>
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			RecentFiles                 = string.Empty;
			RecentPatternSets           = string.Empty;
			Font                        = new Font("Consolas", 8.25f, GraphicsUnit.Point);
			RestoreScreenLoc            = false; // False so that first runs start in the default window position
			ScreenPosition              = new Point(100, 100);
			WindowSize                  = new Size(640, 480);
			PatternSetDirectory         = Util.ResolveUserDocumentsPath(Application.CompanyName, Application.ProductName);
			ExportFilepath              = null;
			AlternateLineColours        = true;
			LineSelectBackColour        = Color.DarkGreen;
			LineSelectForeColour        = Color.White;
			LineBackColour1             = Color.WhiteSmoke;
			LineBackColour2             = Color.White;
			LineForeColour1             = Color.Black;
			LineForeColour2             = Color.Black;
			FileScrollWidth             = Constants.FileScrollWidthDefault;
			ScrollBarFileRangeColour    = Color.FromArgb(0x80, Color.White);
			ScrollBarCachedRangeColour  = Color.FromArgb(0x40, Color.LightBlue);
			ScrollBarDisplayRangeColour = Color.FromArgb(0x80, Color.SteelBlue);
			BookmarkColour              = Color.Violet;
			RowHeight                   = Constants.RowHeightDefault;
			LoadLastFile                = false;
			LastLoadedFile              = string.Empty;
			OpenAtEnd                   = true;
			FullPathInTitle             = true;
			TabSizeInSpaces             = 4;
			FileChangesAdditive         = true;
			IgnoreBlankLines            = false;
			AlwaysOnTop                 = false;
			FirstRun                    = true;
			ShowTOTD                    = true;
			CheckForUpdates             = false;
			CheckForUpdatesServer       = "http://www.rylogic.co.nz/";
			UseWebProxy                 = false;
			WebProxyHost                = string.Empty;
			WebProxyPort                = Constants.PortNumberWebProxyDefault;
			QuickFilterEnabled          = false;
			HighlightsEnabled           = true;
			FiltersEnabled              = false;
			TransformsEnabled           = false;
			ActionsEnabled              = false;
			TailEnabled                 = true;
			WatchEnabled                = true;
			FileBufSize                 = Constants.FileBufSizeDefault;
			MaxLineLength               = Constants.MaxLineLengthDefault;
			LineCacheCount              = Constants.LineCacheCountDefault;
			Patterns                    = PatternSet.Default();
			RowDelimiter                = string.Empty; // stored in humanised form, empty means auto detect
			ColDelimiter                = string.Empty; // stored in humanised form, empty means auto detect
			ColumnCount                 = 2;
			Encoding                    = string.Empty; // empty means auto detect
			OutputFilepathHistory       = new string[0];
			LogProgramOutputHistory     = new LaunchApp[0];
			NetworkConnectionHistory    = new NetConn[0];
			SerialConnectionHistory     = new SerialConn[0];
			PipeConnectionHistory       = new PipeConn[0];
			AndroidLogcat               = new AndroidLogcat();

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}
		public Settings(Settings rhs)
			:base(rhs)
		{
			AutoSaveOnChanges = true;
		}
		public override string Version
		{
			get { return "v1.3"; }
		}

		/// <summary>The recent log files list</summary>
		public string RecentFiles
		{
			get { return get<string>(nameof(RecentFiles)); }
			set { set(nameof(RecentFiles), value); }
		}

		/// <summary>The recent pattern set files</summary>
		public string RecentPatternSets
		{
			get { return get<string>(nameof(RecentPatternSets)); }
			set { set(nameof(RecentPatternSets), value); }
		}

		/// <summary>Log view font</summary>
		public Font Font
		{
			get { return get<Font>(nameof(Font)); }
			set { set(nameof(Font), value); }
		}

		/// <summary>Restore the screen location on startup</summary>
		public bool RestoreScreenLoc
		{
			get { return get<bool>(nameof(RestoreScreenLoc)); }
			set { set(nameof(RestoreScreenLoc), value); }
		}

		/// <summary>The main window position on screen</summary>
		public Point ScreenPosition
		{
			get { return get<Point>(nameof(ScreenPosition)); }
			set { set(nameof(ScreenPosition), value); }
		}

		/// <summary>The size of the main window</summary>
		public Size WindowSize
		{
			get { return get<Size>(nameof(WindowSize)); }
			set { set(nameof(WindowSize), value); }
		}

		/// <summary>The last open/save location of a pattern set</summary>
		public string PatternSetDirectory
		{
			get { return get<string>(nameof(PatternSetDirectory)); }
			set { set(nameof(PatternSetDirectory), value); }
		}

		/// <summary>The last filepath used in the export dialog</summary>
		public string ExportFilepath
		{
			get { return get<string>(nameof(ExportFilepath)); }
			set { set(nameof(ExportFilepath), value); }
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

		/// <summary></summary>
		public int FileScrollWidth
		{
			get { return get<int>(nameof(FileScrollWidth)); }
			set { set(nameof(FileScrollWidth), value); }
		}

		/// <summary></summary>
		public Color ScrollBarFileRangeColour
		{
			get { return get<Color>(nameof(ScrollBarFileRangeColour)); }
			set { set(nameof(ScrollBarFileRangeColour), value); }
		}

		/// <summary></summary>
		public Color ScrollBarCachedRangeColour
		{
			get { return get<Color>(nameof(ScrollBarCachedRangeColour)); }
			set { set(nameof(ScrollBarCachedRangeColour), value); }
		}

		/// <summary></summary>
		public Color ScrollBarDisplayRangeColour
		{
			get { return get<Color>(nameof(ScrollBarDisplayRangeColour)); }
			set { set(nameof(ScrollBarDisplayRangeColour), value); }
		}

		/// <summary></summary>
		public Color BookmarkColour
		{
			get { return get<Color>(nameof(BookmarkColour)); }
			set { set(nameof(BookmarkColour), value); }
		}

		/// <summary></summary>
		public int RowHeight
		{
			get { return get<int>(nameof(RowHeight)); }
			set { set(nameof(RowHeight), value); }
		}

		/// <summary></summary>
		public bool LoadLastFile
		{
			get { return get<bool>(nameof(LoadLastFile)); }
			set { set(nameof(LoadLastFile), value); }
		}

		/// <summary></summary>
		public string LastLoadedFile
		{
			get { return get<string>(nameof(LastLoadedFile)); }
			set { set(nameof(LastLoadedFile), value); }
		}

		/// <summary></summary>
		public bool OpenAtEnd
		{
			get { return get<bool>(nameof(OpenAtEnd)); }
			set { set(nameof(OpenAtEnd), value); }
		}

		/// <summary></summary>
		public bool FullPathInTitle
		{
			get { return get<bool>(nameof(FullPathInTitle)); }
			set { set(nameof(FullPathInTitle), value); }
		}

		/// <summary></summary>
		public int TabSizeInSpaces
		{
			get { return get<int>(nameof(TabSizeInSpaces)); }
			set { set(nameof(TabSizeInSpaces), value); }
		}

		/// <summary></summary>
		public bool FileChangesAdditive
		{
			get { return get<bool>(nameof(FileChangesAdditive)); }
			set { set(nameof(FileChangesAdditive), value); }
		}

		/// <summary></summary>
		public bool IgnoreBlankLines
		{
			get { return get<bool>(nameof(IgnoreBlankLines)); }
			set { set(nameof(IgnoreBlankLines), value); }
		}

		/// <summary></summary>
		public bool AlwaysOnTop
		{
			get { return get<bool>(nameof(AlwaysOnTop)); }
			set { set(nameof(AlwaysOnTop), value); }
		}

		/// <summary></summary>
		public bool FirstRun
		{
			get { return get<bool>(nameof(FirstRun)); }
			set { set(nameof(FirstRun), value); }
		}

		/// <summary></summary>
		public bool ShowTOTD
		{
			get { return get<bool>(nameof(ShowTOTD)); }
			set { set(nameof(ShowTOTD), value); }
		}

		/// <summary></summary>
		public bool CheckForUpdates
		{
			get { return get<bool>(nameof(CheckForUpdates)); }
			set { set(nameof(CheckForUpdates), value); }
		}

		/// <summary></summary>
		public string CheckForUpdatesServer
		{
			get { return get<string>(nameof(CheckForUpdatesServer)); }
			set { set(nameof(CheckForUpdatesServer), value); }
		}

		/// <summary></summary>
		public bool UseWebProxy
		{
			get { return get<bool>(nameof(UseWebProxy)); }
			set { set(nameof(UseWebProxy), value); }
		}

		/// <summary></summary>
		public string WebProxyHost
		{
			get { return get<string>(nameof(WebProxyHost)); }
			set { set(nameof(WebProxyHost), value); }
		}

		/// <summary></summary>
		public int WebProxyPort
		{
			get { return get<int>(nameof(WebProxyPort)); }
			set { set(nameof(WebProxyPort), value); }
		}

		/// <summary></summary>
		public bool QuickFilterEnabled
		{
			get { return get<bool>(nameof(QuickFilterEnabled)); }
			set { set(nameof(QuickFilterEnabled), value); }
		}

		/// <summary></summary>
		public bool HighlightsEnabled
		{
			get { return get<bool>(nameof(HighlightsEnabled)); }
			set { set(nameof(HighlightsEnabled), value); }
		}

		/// <summary></summary>
		public bool FiltersEnabled
		{
			get { return get<bool>(nameof(FiltersEnabled)); }
			set { set(nameof(FiltersEnabled), value); }
		}

		/// <summary></summary>
		public bool TransformsEnabled
		{
			get { return get<bool>(nameof(TransformsEnabled)); }
			set { set(nameof(TransformsEnabled), value); }
		}

		/// <summary></summary>
		public bool ActionsEnabled
		{
			get { return get<bool>(nameof(ActionsEnabled)); }
			set { set(nameof(ActionsEnabled), value); }
		}

		/// <summary></summary>
		public bool TailEnabled
		{
			get { return get<bool>(nameof(TailEnabled)); }
			set { set(nameof(TailEnabled), value); }
		}

		/// <summary></summary>
		public bool WatchEnabled
		{
			get { return get<bool>(nameof(WatchEnabled)); }
			set { set(nameof(WatchEnabled), value); }
		}

		/// <summary></summary>
		public int FileBufSize
		{
			get { return get<int>(nameof(FileBufSize)); }
			set { set(nameof(FileBufSize), value); }
		}

		/// <summary></summary>
		public int MaxLineLength
		{
			get { return get<int>(nameof(MaxLineLength)); }
			set { set(nameof(MaxLineLength), value); }
		}

		/// <summary></summary>
		public int LineCacheCount
		{
			get { return get<int>(nameof(LineCacheCount)); }
			set { set(nameof(LineCacheCount), value); }
		}

		/// <summary></summary>
		public string RowDelimiter
		{
			get { return get<string>(nameof(RowDelimiter)); }
			set { set(nameof(RowDelimiter), value); }
		}

		/// <summary></summary>
		public string ColDelimiter
		{
			get { return get<string>(nameof(ColDelimiter)); }
			set { set(nameof(ColDelimiter), value); }
		}

		/// <summary>Columns</summary>
		public int ColumnCount
		{
			get { return get<int>(nameof(ColumnCount)); }
			set { set(nameof(ColumnCount), value); }
		}

		/// <summary>File encoding</summary>
		public string Encoding
		{
			get { return get<string>(nameof(Encoding)); }
			set { set(nameof(Encoding), value); }
		}

		/// <summary>Patterns</summary>
		public PatternSet Patterns
		{
			get { return get<PatternSet>(nameof(Patterns)); }
			set { set(nameof(Patterns), value); }
		}

		/// <summary>The output filepath for streamed data sources</summary>
		public string[] OutputFilepathHistory
		{
			get { return get<string[]>(nameof(OutputFilepathHistory)); }
			set { set(nameof(OutputFilepathHistory), value); }
		}

		/// <summary></summary>
		public LaunchApp[] LogProgramOutputHistory
		{
			get { return get<LaunchApp[]>(nameof(LogProgramOutputHistory)); }
			set { set(nameof(LogProgramOutputHistory), value); }
		}

		/// <summary></summary>
		public NetConn[] NetworkConnectionHistory
		{
			get { return get<NetConn[]>(nameof(NetworkConnectionHistory)); }
			set { set(nameof(NetworkConnectionHistory), value); }
		}

		/// <summary></summary>
		public SerialConn[] SerialConnectionHistory
		{
			get { return get<SerialConn[]>(nameof(SerialConnectionHistory)); }
			set { set(nameof(SerialConnectionHistory), value); }
		}

		/// <summary></summary>
		public PipeConn[] PipeConnectionHistory
		{
			get { return get<PipeConn[]>(nameof(PipeConnectionHistory)); }
			set { set(nameof(PipeConnectionHistory), value); }
		}

		/// <summary></summary>
		public AndroidLogcat AndroidLogcat
		{
			get { return get<AndroidLogcat>(nameof(AndroidLogcat)); }
			set { set(nameof(AndroidLogcat), value); }
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

		/// <summary>Example upgrade method</summary>
		private string Upgrade_vXX_to_vYY(XElement settings)
		{
			// Modify the contents of 'settings'
			// Do not use any types that might change over time

			// Done, return the version
			return "vY.Y";
		}
	}
}
