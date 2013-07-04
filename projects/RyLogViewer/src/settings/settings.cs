using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.util;

namespace RyLogViewer
{
	/// <summary>RyLog Viewer settings</summary>
	public sealed class Settings :SettingsBase<Settings>
	{
		public string LicenceHolder
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.LicenceHolder)); }
			set { set(Reflect<Settings>.MemberName(x => x.LicenceHolder), value); }
		}
		public string Company
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.Company)); }
			set { set(Reflect<Settings>.MemberName(x => x.Company), value); }
		}
		public string RecentFiles
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.RecentFiles)); }
			set { set(Reflect<Settings>.MemberName(x => x.RecentFiles), value); }
		}
		public Font   Font
		{
			get { return get<Font>(Reflect<Settings>.MemberName(x => x.Font)); }
			set { set(Reflect<Settings>.MemberName(x => x.Font), value); }
		}
		public bool   RestoreScreenLoc
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.RestoreScreenLoc)); }
			set { set(Reflect<Settings>.MemberName(x => x.RestoreScreenLoc), value); }
		}
		public Point  ScreenPosition
		{
			get { return get<Point>(Reflect<Settings>.MemberName(x => x.ScreenPosition)); }
			set { set(Reflect<Settings>.MemberName(x => x.ScreenPosition), value); }
		}
		public Size   WindowSize
		{
			get { return get<Size>(Reflect<Settings>.MemberName(x => x.WindowSize)); }
			set { set(Reflect<Settings>.MemberName(x => x.WindowSize), value); }
		}
		public bool   AlternateLineColours
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.AlternateLineColours)); }
			set { set(Reflect<Settings>.MemberName(x => x.AlternateLineColours), value); }
		}
		public Color  LineSelectBackColour
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.LineSelectBackColour)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineSelectBackColour), value); }
		}
		public Color  LineSelectForeColour
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.LineSelectForeColour)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineSelectForeColour), value); }
		}
		public Color  LineBackColour1
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.LineBackColour1)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineBackColour1), value); }
		}
		public Color  LineBackColour2
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.LineBackColour2)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineBackColour2), value); }
		}
		public Color  LineForeColour1
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.LineForeColour1)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineForeColour1), value); }
		}
		public Color  LineForeColour2
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.LineForeColour2)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineForeColour2), value); }
		}
		public int    FileScrollWidth
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.FileScrollWidth)); }
			set { set(Reflect<Settings>.MemberName(x => x.FileScrollWidth), value); }
		}
		public Color  ScrollBarFileRangeColour
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.ScrollBarFileRangeColour)); }
			set { set(Reflect<Settings>.MemberName(x => x.ScrollBarFileRangeColour), value); }
		}
		public Color  ScrollBarCachedRangeColour
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.ScrollBarCachedRangeColour)); }
			set { set(Reflect<Settings>.MemberName(x => x.ScrollBarCachedRangeColour), value); }
		}
		public Color  ScrollBarDisplayRangeColour
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.ScrollBarDisplayRangeColour)); }
			set { set(Reflect<Settings>.MemberName(x => x.ScrollBarDisplayRangeColour), value); }
		}
		public Color  BookmarkColour
		{
			get { return get<Color>(Reflect<Settings>.MemberName(x => x.BookmarkColour)); }
			set { set(Reflect<Settings>.MemberName(x => x.BookmarkColour), value); }
		}
		public int    RowHeight
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.RowHeight)); }
			set { set(Reflect<Settings>.MemberName(x => x.RowHeight), value); }
		}
		public bool   LoadLastFile
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.LoadLastFile)); }
			set { set(Reflect<Settings>.MemberName(x => x.LoadLastFile), value); }
		}
		public string LastLoadedFile
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.LastLoadedFile)); }
			set { set(Reflect<Settings>.MemberName(x => x.LastLoadedFile), value); }
		}
		public bool   OpenAtEnd
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.OpenAtEnd)); }
			set { set(Reflect<Settings>.MemberName(x => x.OpenAtEnd), value); }
		}
		public bool   FileChangesAdditive
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.FileChangesAdditive)); }
			set { set(Reflect<Settings>.MemberName(x => x.FileChangesAdditive), value); }
		}
		public bool   IgnoreBlankLines
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.IgnoreBlankLines)); }
			set { set(Reflect<Settings>.MemberName(x => x.IgnoreBlankLines), value); }
		}
		public bool   AlwaysOnTop
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.AlwaysOnTop)); }
			set { set(Reflect<Settings>.MemberName(x => x.AlwaysOnTop), value); }
		}
		public bool   ShowTOTD
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.ShowTOTD)); }
			set { set(Reflect<Settings>.MemberName(x => x.ShowTOTD), value); }
		}
		public bool   CheckForUpdates
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.CheckForUpdates)); }
			set { set(Reflect<Settings>.MemberName(x => x.CheckForUpdates), value); }
		}
		public string CheckForUpdatesServer
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.CheckForUpdatesServer)); }
			set { set(Reflect<Settings>.MemberName(x => x.CheckForUpdatesServer), value); }
		}
		public bool   UseWebProxy
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.UseWebProxy)); }
			set { set(Reflect<Settings>.MemberName(x => x.UseWebProxy), value); }
		}
		public string WebProxyHost
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.WebProxyHost)); }
			set { set(Reflect<Settings>.MemberName(x => x.WebProxyHost), value); }
		}
		public int    WebProxyPort
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.WebProxyPort)); }
			set { set(Reflect<Settings>.MemberName(x => x.WebProxyPort), value); }
		}
		public bool   QuickFilterEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.QuickFilterEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.QuickFilterEnabled), value); }
		}
		public bool   HighlightsEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.HighlightsEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.HighlightsEnabled), value); }
		}
		public bool   FiltersEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.FiltersEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.FiltersEnabled), value); }
		}
		public bool   TransformsEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.TransformsEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.TransformsEnabled), value); }
		}
		public bool   ActionsEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.ActionsEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.ActionsEnabled), value); }
		}
		public bool   TailEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.TailEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.TailEnabled), value); }
		}
		public bool   WatchEnabled
		{
			get { return get<bool>(Reflect<Settings>.MemberName(x => x.WatchEnabled)); }
			set { set(Reflect<Settings>.MemberName(x => x.WatchEnabled), value); }
		}
		public int    FileBufSize
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.FileBufSize)); }
			set { set(Reflect<Settings>.MemberName(x => x.FileBufSize), value); }
		}
		public int    MaxLineLength
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.MaxLineLength)); }
			set { set(Reflect<Settings>.MemberName(x => x.MaxLineLength), value); }
		}
		public int    LineCacheCount
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.LineCacheCount)); }
			set { set(Reflect<Settings>.MemberName(x => x.LineCacheCount), value); }
		}
		public string HighlightPatterns
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.HighlightPatterns)); }
			set { set(Reflect<Settings>.MemberName(x => x.HighlightPatterns), value); }
		}
		public string FilterPatterns
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.FilterPatterns)); }
			set { set(Reflect<Settings>.MemberName(x => x.FilterPatterns), value); }
		}
		public string TransformPatterns
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.TransformPatterns)); }
			set { set(Reflect<Settings>.MemberName(x => x.TransformPatterns), value); }
		}
		public string ActionPatterns
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.ActionPatterns)); }
			set { set(Reflect<Settings>.MemberName(x => x.ActionPatterns), value); }
		}
		public string HighlightPatternSets
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.HighlightPatternSets)); }
			set { set(Reflect<Settings>.MemberName(x => x.HighlightPatternSets), value); }
		}
		public string FilterPatternSets
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.FilterPatternSets)); }
			set { set(Reflect<Settings>.MemberName(x => x.FilterPatternSets), value); }
		}
		public string TransformPatternSets
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.TransformPatternSets)); }
			set { set(Reflect<Settings>.MemberName(x => x.TransformPatternSets), value); }
		}
		public string ActionPatternSets
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.ActionPatternSets)); }
			set { set(Reflect<Settings>.MemberName(x => x.ActionPatternSets), value); }
		}
		public string RowDelimiter
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.RowDelimiter)); }
			set { set(Reflect<Settings>.MemberName(x => x.RowDelimiter), value); }
		}
		public string ColDelimiter
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.ColDelimiter)); }
			set { set(Reflect<Settings>.MemberName(x => x.ColDelimiter), value); }
		}
		public int    ColumnCount
		{
			get { return get<int>(Reflect<Settings>.MemberName(x => x.ColumnCount)); }
			set { set(Reflect<Settings>.MemberName(x => x.ColumnCount), value); }
		}
		public string Encoding
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.Encoding)); }
			set { set(Reflect<Settings>.MemberName(x => x.Encoding), value); }
		}
		public string[] OutputFilepathHistory
		{
			get { return get<string[]>(Reflect<Settings>.MemberName(x => x.OutputFilepathHistory)); }
			set { set(Reflect<Settings>.MemberName(x => x.OutputFilepathHistory), value); }
		}
		public LaunchApp[] LogProgramOutputHistory
		{
			get { return get<LaunchApp[]>(Reflect<Settings>.MemberName(x => x.LogProgramOutputHistory)); }
			set { set(Reflect<Settings>.MemberName(x => x.LogProgramOutputHistory), value); }
		}
		public NetConn[] NetworkConnectionHistory
		{
			get { return get<NetConn[]>(Reflect<Settings>.MemberName(x => x.NetworkConnectionHistory)); }
			set { set(Reflect<Settings>.MemberName(x => x.NetworkConnectionHistory), value); }
		}
		public SerialConn[] SerialConnectionHistory
		{
			get { return get<SerialConn[]>(Reflect<Settings>.MemberName(x => x.SerialConnectionHistory)); }
			set { set(Reflect<Settings>.MemberName(x => x.SerialConnectionHistory), value); }
		}
		public PipeConn[] PipeConnectionHistory
		{
			get { return get<PipeConn[]>(Reflect<Settings>.MemberName(x => x.PipeConnectionHistory)); }
			set { set(Reflect<Settings>.MemberName(x => x.PipeConnectionHistory), value); }
		}
		public AndroidLogcat AndroidLogcat
		{
			get { return get<AndroidLogcat>(Reflect<Settings>.MemberName(x => x.AndroidLogcat)); }
			set { set(Reflect<Settings>.MemberName(x => x.AndroidLogcat), value); }
		}
		public string LogFilePath
		{
			get { return get<string>(Reflect<Settings>.MemberName(x => x.LogFilePath)); }
			set { set(Reflect<Settings>.MemberName(x => x.LogFilePath), value); }
		}

		/// <summary>The settings version, used to detect when 'Upgrade' is needed</summary>
		protected override string Version { get { return "v1.1"; } }

		// Default construct settings
		public Settings()
		{
			LicenceHolder                   = Constants.EvalLicence;
			Company                         = string.Empty;
			RecentFiles                     = string.Empty;
			Font                            = new Font("Consolas", 8.25f, GraphicsUnit.Point);
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
			ScrollBarFileRangeColour        = Color.FromArgb(0x80, Color.White);
			ScrollBarCachedRangeColour      = Color.FromArgb(0x40, Color.LightBlue);
			ScrollBarDisplayRangeColour     = Color.FromArgb(0x80, Color.SteelBlue);
			BookmarkColour                  = Color.Violet;
			RowHeight                       = Constants.RowHeightDefault;
			LoadLastFile                    = false;
			LastLoadedFile                  = string.Empty;
			OpenAtEnd                       = true;
			FileChangesAdditive             = true;
			IgnoreBlankLines                = false;
			AlwaysOnTop                     = false;
			ShowTOTD                        = true;
			CheckForUpdates                 = true;
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
			HighlightPatterns               = "<root/>";
			FilterPatterns                  = "<root/>";
			TransformPatterns               = "<root/>";
			ActionPatterns                  = "<root/>";
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
			LogFilePath                     = string.Empty;
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
			
			// Network connection settings
			foreach (var c in NetworkConnectionHistory)
			{
				if (c.ProtocolType != ProtocolType.Tcp && c.ProtocolType != ProtocolType.Udp)
					c.ProtocolType = ProtocolType.Tcp;
			}
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
					typeof(PipeConn[]),
					typeof(AndroidLogcat),
					typeof(AndroidLogcat.FilterSpec)
				};
			}
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		public override void Upgrade(string from_version)
		{
			switch (from_version)
			{
			default:
				base.Upgrade(from_version);
				break;
			case "v1.0":
				Upgrade_v10_to_v11();
				break;
			}
		}

		// ReSharper disable PossibleNullReferenceException

		/// <summary>Upgrade from v1.0 to v1.1</summary>
		private void Upgrade_v10_to_v11()
		{
			// Changed:
			//  Added 'whole line' to patterns

			// Helper function for converting XElement
			Action<XElement> Convert = pat =>
				{
					pat.Add(new XElement(XmlTag.WholeLine, "false"));
				};

			// Settings patterns
			{
				var doc = XDocument.Parse(HighlightPatterns);
				foreach (var p in doc.Root.Elements(XmlTag.Highlight)) Convert(p);
				HighlightPatterns = doc.ToString(SaveOptions.None);
			}
			{
				var doc = XDocument.Parse(FilterPatterns);
				foreach (var p in doc.Root.Elements(XmlTag.Filter)) Convert(p);
				FilterPatterns = doc.ToString(SaveOptions.None);
			}
			{
				var doc = XDocument.Parse(TransformPatterns);
				foreach (var p in doc.Root.Elements(XmlTag.Transform)) Convert(p);
				TransformPatterns = doc.ToString(SaveOptions.None);
			}
			{
				var doc = XDocument.Parse(ActionPatterns);
				foreach (var p in doc.Root.Elements(XmlTag.ClkAction)) Convert(p);
				ActionPatterns = doc.ToString(SaveOptions.None);
			}

			// Done, set the version
			set(VersionKey, "v1.1");
		}

		// ReSharper restore PossibleNullReferenceException
	}
}
