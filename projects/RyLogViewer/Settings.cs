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
		public bool   IncludeBlankLines
		{
			get { return get<bool>("IncludeBlankLines"); }
			set { set("IncludeBlankLines", value); }
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
		public bool   TailEnabled
		{
			get { return get<bool>("TailEnabled"); }
			set { set("TailEnabled", value); }
		}
		public int    LineCount
		{
			get { return get<int>("LineCount"); }
			set { set("LineCount", value); }
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
		public string Encoding
		{
			get { return get<string>("Encoding"); }
			set { set("Encoding", value); }
		}

		public Settings(ELoadOptions opts = ELoadOptions.Normal)
		{
			if (opts == ELoadOptions.Normal)
			{
				try { Reload(); return; }
				catch (Exception ex) { Debug.WriteLine(ex); }
			}
			
			RecentFiles          = "";
			Font                 = new Font("Microsoft Sans Serif", 8.25f, GraphicsUnit.Point);
			RestoreScreenLoc     = true;
			ScreenPosition       = new Point(50, 50);
			WindowSize           = new Size(640, 480);
			MenuPosition         = Point.Empty;
			ToolsPosition        = new Point(0, 30);
			StatusPosition       = Point.Empty;
			AlternateLineColours = true;
			LineSelectBackColour = Color.DarkGreen;
			LineSelectForeColour = Color.Lime;
			LineBackColour1      = Color.WhiteSmoke;
			LineBackColour2      = Color.FromArgb(192, 255, 192);
			LineForeColour1      = Color.Black;
			LineForeColour2      = Color.Black;
			FileScrollWidth      = 28;
			RowHeight            = 18;
			LoadLastFile         = false;
			LastLoadedFile       = "";
			OpenAtEnd            = true;
			IncludeBlankLines    = true;
			AlwaysOnTop          = false;
			ShowTOTD             = true;
			TailEnabled          = false;
			LineCount            = 1000;
			HighlightPatterns    = "<root/>";
			FilterPatterns       = "<root/>";
			HighlightPatternSets = "<root/>";
			FilterPatternSets    = "<root/>";
			RowDelimiter         = "";
			ColDelimiter         = "";
			Encoding             = "";
		}
	}
}
