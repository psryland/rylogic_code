using System;
using System.Windows.Media;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace RyLogViewer.Options
{
	public class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			General = new General();
			LogData = new LogData();
			Format = new Format();

			RecentFiles = string.Empty;
			RecentPatternSets = string.Empty;
			//Font = new Font("Consolas", 8.25f, GraphicsUnit.Point);
			PatternSetDirectory = Util.ResolveUserDocumentsPath(Util.AppCompany, Util.AppProductName);
			ExportFilepath = null;
			FileScrollWidth = Constants.FileScrollWidthDefault;
			ScrollBarFileRangeColour = Colors.White.Modify(a:0x80);
			ScrollBarCachedRangeColour = Colors.LightBlue.Modify(a:0x40);
			ScrollBarDisplayRangeColour = Colors.SteelBlue.Modify(a:0x80);
			BookmarkColour = Colors.Violet;
			TabSizeInSpaces = 4;
			AlwaysOnTop = false;
			FirstRun = true;
			CheckForUpdatesServer = "http://www.rylogic.co.nz/";
			UseWebProxy = false;
			WebProxyHost = string.Empty;
			WebProxyPort = Constants.PortNumberWebProxyDefault;
			QuickFilterEnabled = false;
			HighlightsEnabled = true;
			FiltersEnabled = false;
			TransformsEnabled = false;
			ActionsEnabled = false;
			TailEnabled = true;
			WatchEnabled = true;
			OutputFilepathHistory = new string[0];
			//Patterns = PatternSet.Default();
			//LogProgramOutputHistory = new LaunchApp[0];
			//NetworkConnectionHistory = new NetConn[0];
			//SerialConnectionHistory = new SerialConn[0];
			//PipeConnectionHistory = new PipeConn[0];
			//AndroidLogcat = new AndroidLogcat();

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			: base(filepath)
		{
			AutoSaveOnChanges = true;
		}
		public Settings(Settings rhs)
			: base(rhs)
		{
			AutoSaveOnChanges = true;
		}
		public override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>General settings</summary>
		public General General
		{
			get { return get<General>(nameof(General)); }
			set
			{
				Guard.ArgNotNull(value, $"Setting '{nameof(General)}' cannot be null");
				set(nameof(General), value);
			}
		}

		/// <summary>LogData settings</summary>
		public LogData LogData
		{
			get { return get<LogData>(nameof(LogData)); }
			set
			{
				Guard.ArgNotNull(value, $"Setting '{nameof(LogData)}' cannot be null");
				set(nameof(LogData), value);
			}
		}

		/// <summary>Format/Appearance settings</summary>
		public Format Format
		{
			get { return get<Format>(nameof(Format)); }
			set
			{
				Guard.ArgNotNull(value, $"Setting '{nameof(Format)}' cannot be null");
				set(nameof(Format), value);
			}
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

		///// <summary>Log view font</summary>
		//public Font Font
		//{
		//	get { return get<Font>(nameof(Font)); }
		//	set { set(nameof(Font), value); }
		//}

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
		public int TabSizeInSpaces
		{
			get { return get<int>(nameof(TabSizeInSpaces)); }
			set { set(nameof(TabSizeInSpaces), value); }
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

		/// <summary>The output filepath for streamed data sources</summary>
		public string[] OutputFilepathHistory
		{
			get { return get<string[]>(nameof(OutputFilepathHistory)); }
			set { set(nameof(OutputFilepathHistory), value); }
		}

		///// <summary>Patterns</summary>
		//public PatternSet Patterns
		//{
		//	get { return get<PatternSet>(nameof(Patterns)); }
		//	set { set(nameof(Patterns), value); }
		//}

		///// <summary></summary>
		//public LaunchApp[] LogProgramOutputHistory
		//{
		//	get { return get<LaunchApp[]>(nameof(LogProgramOutputHistory)); }
		//	set { set(nameof(LogProgramOutputHistory), value); }
		//}

		///// <summary></summary>
		//public NetConn[] NetworkConnectionHistory
		//{
		//	get { return get<NetConn[]>(nameof(NetworkConnectionHistory)); }
		//	set { set(nameof(NetworkConnectionHistory), value); }
		//}

		///// <summary></summary>
		//public SerialConn[] SerialConnectionHistory
		//{
		//	get { return get<SerialConn[]>(nameof(SerialConnectionHistory)); }
		//	set { set(nameof(SerialConnectionHistory), value); }
		//}

		///// <summary></summary>
		//public PipeConn[] PipeConnectionHistory
		//{
		//	get { return get<PipeConn[]>(nameof(PipeConnectionHistory)); }
		//	set { set(nameof(PipeConnectionHistory), value); }
		//}

		///// <summary></summary>
		//public AndroidLogcat AndroidLogcat
		//{
		//	get { return get<AndroidLogcat>(nameof(AndroidLogcat)); }
		//	set { set(nameof(AndroidLogcat), value); }
		//}

		/// <summary>Perform validation on the loaded settings</summary>
		public override Exception Validate()
		{
			General.Validate();
			LogData.Validate();
			Format.Validate();

			// File scroll width must be within range
			int file_scroll_width = FileScrollWidth;
			if (file_scroll_width < Constants.FileScrollMinWidth || file_scroll_width > Constants.FileScrollMaxWidth)
				FileScrollWidth = Constants.FileScrollWidthDefault;

			// Network connection settings
			//todo foreach (var c in NetworkConnectionHistory)
			//todo {
			//todo 	if (c.ProtocolType != ProtocolType.Tcp && c.ProtocolType != ProtocolType.Udp)
			//todo 		c.ProtocolType = ProtocolType.Tcp;
			//todo }
			return null;
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		protected override void UpgradeCore(XElement settings, string from_version)
		{
			for (; from_version != Version;)
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

			string Upgrade_vXX_to_vYY(XElement s)
			{
				// Modify the contents of 'settings'
				// Do not use any types that might change over time

				// Done, return the version
				return "vY.Y";
			}
		}
	}
}
