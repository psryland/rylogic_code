using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using pr.common;

namespace RyLogViewer
{
	/// <summary>RyLog Viewer settings</summary>
	public sealed class Settings :SettingsBase
	{
		// Defaults
		public static readonly Settings Default = new Settings(ELoadOptions.Defaults);
		protected override SettingsBase DefaultData { get { return Default; } }
		
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
		public Point  MenuPosition
		{
			get { return get<Point>("MenuPosition"); }
			set { set("MenuPosition", value); }
		}
		public Point  ToolsPosition
		{
			get { return get<Point>("ToolsPosition"); }
			set { set("ToolsPosition", value); }
		}
		public Point  StatusPosition
		{
			get { return get<Point>("StatusPosition"); }
			set { set("StatusPosition", value); }
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
		public bool   TailEnabled
		{
			get { return get<bool>("TailEnabled"); }
			set { set("TailEnabled", value); }
		}
		public int    FileBufSize
		{
			get { return get<int>("FileBufSize"); }
			set { set("FileBufSize", value); }
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

		public Settings(ELoadOptions opts = ELoadOptions.Normal)
		{
			RecentFiles                     = "";
			Font                            = new Font("Microsoft Sans Serif", 8.25f, GraphicsUnit.Point);
			RestoreScreenLoc                = true;
			ScreenPosition                  = new Point(50, 50);
			WindowSize                      = new Size(640, 480);
			MenuPosition                    = Point.Empty;
			ToolsPosition                   = new Point(0, 30);
			StatusPosition                  = Point.Empty;
			AlternateLineColours            = true;
			LineSelectBackColour            = Color.DarkGreen;
			LineSelectForeColour            = Color.Lime;
			LineBackColour1                 = Color.WhiteSmoke;
			LineBackColour2                 = Color.FromArgb(192, 255, 192);
			LineForeColour1                 = Color.Black;
			LineForeColour2                 = Color.Black;
			FileScrollWidth                 = 20;
			ScrollBarFileRangeColour        = Color.FromArgb(128, Color.White);
			ScrollBarDisplayRangeColour     = Color.FromArgb(128, Color.SteelBlue);
			RowHeight                       = 18;
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
			TailEnabled                     = false;
			FileBufSize                     = 10 * 1024 * 1024;
			LineCacheCount                  = 10000;
			HighlightPatterns               = "<root/>";
			FilterPatterns                  = "<root/>";
			HighlightPatternSets            = "<root/>";
			FilterPatternSets               = "<root/>";
			RowDelimiter                    = "";
			ColDelimiter                    = "";
			ColumnCount                     = 1;
			Encoding                        = "";
			FindHistory                     = new string[0];
			OutputFilepathHistory           = new string[0];
			LogProgramOutputHistory         = new LaunchApp[0];
			NetworkConnectionHistory        = new NetConn[0];
			SerialConnectionHistory         = new SerialConn[0];
			PipeConnectionHistory           = new PipeConn[0];
			
			// Load all the default values first, then if the load options are 'normal'
			// load from file. This ensures options missing in the file exist with default values
			if (opts == ELoadOptions.Normal)
			{
				try { Reload(); }
				catch (Exception ex) { Debug.WriteLine(ex); }
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
					typeof(PipeConn[])
				};
			}
		}
	}
}
