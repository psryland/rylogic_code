using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;
using pr.attrib;
using pr.extn;
using pr.gui;
using pr.maths;

namespace imager
{
	public enum ESortOrder
	{
		[String("Not Sorted")]                  None,
		[String("Alphabetical Order")]          AlphabeticalAscending,
		[String("Chronological Order")]         ChronologicalAscending,
		[String("Reverse Alphabetical Order")]  AlphabeticalDecending,
		[String("Reverse Chronological Order")] ChronologicalDecending,
		[String("Random Order")]                Random,
	}

	[Flags] public enum EMediaType
	{
		Image = 1 << 0,
		Video = 1 << 1,
		Audio = 1 << 2,
		All   = Image | Audio | Video,
	}

	public enum EZoomType
	{
		/// <summary>Large images are reduced in size to fit within the window. Small images are displayed at their native resolution</summary>
		[String("Best Fit")]
		[Description("Large images are reduced in size to fit within the window. Small images are displayed at their native resolution")]
		BestFit,
	
		/// <summary>Large images are reduced in size and small images are magnified so that they fit within the window</summary>
		[String("Fit to Window")]
		[Description("Large images are reduced in size and small images are magnified so that they fit within the window")]
		FitToWindow,
		
		/// <summary>Images are shown at their native resolution, (i.e. one pixel in the image equals one pixel on screen)</summary>
		[String("Actual Size")]
		[Description("Images are shown at their native resolution, (i.e. one pixel in the image equals one pixel on screen)")]
		ActualSize,
	}

	/// <summary>A directory containing media files to include in a media list</summary>
	public class MediaPath :ICloneable
	{
		public string Pathname            { get; set; }
		public bool   SubFolders          { get; set; }
		public object Clone()             { return MemberwiseClone(); }
		public MediaPath()                {}
		public MediaPath(string pathname) { Pathname = pathname; SubFolders = true; }
		public override string ToString() { return Pathname; }
		public static implicit operator string (MediaPath mp) { return mp.Pathname; }
	}

	// ReSharper disable RedundantDefaultFieldInitializer
	[Serializable]
	public class Settings
	{
		/// <summary>"The media file types to load. Use this option to limit Imager to display only image, audio, or video files"</summary>
		[Description("The media file types to load. Use this option to limit Imager to display only image, audio, or video files")]
		public EMediaType MediaType
		{
			get {return m_media_type;}
			set {if (value != m_media_type) {m_media_type = value; SettingsChanged.Raise(this, "MediaType");} }
		}
		private EMediaType m_media_type = EMediaType.All;

		/// <summary>"A semi-colon separated list of image file extensions to match when searching
		/// A leading '+' symbol means files of this type will be loaded, '-' means they'll be skipped"</summary>
		[Description(
			"Image file extensions to include when searching for media files\n"+
			"Right click to edit this list")]
		public string ImageExtensions
		{
			get {return m_image_extensions;}
			set {if (value != m_image_extensions) {m_image_extensions = value; SettingsChanged.Raise(this, "ImageExtensions");} }
		}
		private string m_image_extensions = "+bmp;+jpg;+jpeg;+png;+tiff";
		
		/// <summary>"A semi-colon separated list of video file extensions to match when searching
		/// A leading '+' symbol means files of this type will be loaded, '-' means they'll be skipped"</summary>
		[Description(
			"Video file extensions to include when searching for media files\n"+
			"Other formats may be supported if your system has the appropriate codec installed.\n"+
			"Right click to edit this list")]
		public string VideoExtensions
		{
			get {return m_video_extensions;}
			set {if (value != m_video_extensions) {m_video_extensions = value; SettingsChanged.Raise(this, "VideoExtensions"); } }
	}
		private string m_video_extensions = "+avi;+mpg;+mpeg;+mp4;+mod;+mov";

		/// <summary>"A semi-colon separated list of audio file extensions to match when searching
		/// A leading '+' symbol means files of this type will be loaded, '-' means they'll be skipped"</summary>
		[Description(
			"Audio file extensions to include when searching for media files\n"+
			"Other formats may be supported if your system has the appropriate codec installed.\n"+
			"Right click to edit this list")]
		public string AudioExtensions
		{
			get {return m_audio_extensions;}
			set {if (value != m_audio_extensions) {m_audio_extensions = value; SettingsChanged.Raise(this, "AudioExtensions");} }
		}
		private string m_audio_extensions = "+wav;+mp3;+raw";

		/// <summary>"Select the monitor to display the media files on"</summary>
		[Description(
			"Select the monitor to display the media files on.\n"+
			"Setting this value to 'Not Set' will cause files to be displayed on the default monitor.\n"+
			"In screen saver mode, a value of 'Not Set' will cause media files to be displayed on a\n"+
			"random monitor, changing with each file.")]
		public int PrimaryDisplay
		{
		    get { return m_primary_display; }
		    set { if (value != m_primary_display && (uint)value <= (uint)Screen.AllScreens.Length) {m_primary_display = value; SettingsChanged.Raise(this, "PrimaryDisplay");} }
		}
		private int m_primary_display = 0;

		/// <summary>"Imager window remains on top of other windows"</summary>
		[Description("Imager window remains on top of other windows")]
		public bool AlwaysOnTop
		{
			get {return m_always_on_top;}
			set {if (value != m_always_on_top) {m_always_on_top = value; SettingsChanged.Raise(this, "AlwaysOnTop");} }
		}
		private bool m_always_on_top = false;

		/// <summary>"Check for a newer version of Imager on startup"</summary>
		[Description("Check for a newer version of Imager on startup")]
		public bool StartupVersionCheck
		{
			get {return m_startup_version_check;}
			set {if (value != m_startup_version_check) {m_startup_version_check = value; SettingsChanged.Raise(this, "StartupVersionCheck");} }
		}
		private bool m_startup_version_check = true;

		/// <summary>"Allow media files to appear multiple times in the media list"</summary>
		[Description("Allow media files to appear multiple times in the media list")]
		public bool AllowDuplicates
		{
			get {return m_allow_duplicates;}
			set {if (value != m_allow_duplicates) {m_allow_duplicates = value; SettingsChanged.Raise(this, "AllowDuplicates"); } }
		}
		private bool m_allow_duplicates = false;

		/// <summary>"After loading a file, the zoom will be reset based on the Zoom Type, otherwise the zoom will be unchanged after loading a file"</summary>
		[Description(
			"After loading a file, the zoom will be reset based on the Zoom Behaviour,\n"+
			"otherwise the zoom will be unchanged after loading a file")]
		public bool ResetZoomOnLoad
		{
			get {return m_reset_zoom_on_load;}
			set {if (value != m_reset_zoom_on_load) {m_reset_zoom_on_load = value; SettingsChanged.Raise(this, "ResetZoomOnLoad"); } }
		}
		private bool m_reset_zoom_on_load = true;

		/// <summary>"The zoom mode to use when resetting the zoom</summary>
		[Description(
			"The zoom mode to use when resetting the zoom\n"+
			"Best Fit - Large images are reduced in size to fit within the window. Small images are displayed at their native resolution\n"+
			"Fit to Window - Large images are reduced in size and small images are magnified so that they fit within the window.\n"+
			"Actual Size - Images are shown at their native resolution, (i.e. one pixel in the image equals one pixel on screen)")]
		public EZoomType ZoomType
		{
			get {return m_zoom_type;}
			set {if (value != m_zoom_type) {m_zoom_type = value; SettingsChanged.Raise(this, "ZoomType"); } }
		}
		private EZoomType m_zoom_type = EZoomType.BestFit;

		/// <summary>"The rate to switch displayed files during a slide show (in seconds)</summary>
		[Description("The rate to switch displayed files during a slide show (in seconds)")]
		public int SlideShowRate
		{
			get {return m_slide_show_rate;}
			set {if (value != m_slide_show_rate) {m_slide_show_rate = Maths.Clamp(value, 1, 60); SettingsChanged.Raise(this, "SlideShowRate"); } }
		}
		private int m_slide_show_rate = 5;

		/// <summary>"The volume level for video/audio files</summary>
		[Description(
			"The volume level for video/audio files.\n"+
			"Note, this does not change the overall system volume, just audio played by this app")]
		public int Volume
		{
			get {return m_volume;}
			set {if (value != m_volume) {m_volume = Math.Max(0, Math.Min(100, value)); SettingsChanged.Raise(this, "Volume");} }
		}
		private int m_volume = 50;

		/// <summary>"Cache the results of the search for media files to prevent searching when the screen saver starts."</summary>
		[Description(
			"Cache the results of the search for media files to prevent searching when the screen saver starts.\n"+
			"This will make loading the media file list faster but changes in the search folders will not be noticed\n"+
			"until a new search is performed.")]
		public bool CacheSSMediaList
		{
			get {return m_cache_ss_media_list;}
			set {if (value != m_cache_ss_media_list) {m_cache_ss_media_list = value; SettingsChanged.Raise(this, "CacheSSMediaList"); } }
		}
		private bool m_cache_ss_media_list = true;

		/// <summary>When running as a screen saver, set this sound level for video and audio files. Range: 0->100</summary>
		[Description("When running as a screen saver, set this sound level for video and audio files")]
		public int SSVolume
		{
			get {return m_ss_volume;}
			set {if (value != m_ss_volume) {m_ss_volume = Math.Max(0, Math.Min(100, value)); SettingsChanged.Raise(this, "SSVolume");} }
		}
		private int m_ss_volume = 0;

		/// <summary>"The order of folders within the media list</summary>
		[Description(
			"The order of folders within the media list.\n"+
			"None - no ordering based on folders. If File Order is random, then files across all folders will be randomised.\n"+
			"Alphabetical - ordered based on folder name.\n"+
			"Chronological - ordered based on folder timestamp.\n"+
			"Random - folder order randomised.\n")]
		public ESortOrder FolderOrder
		{
			get {return m_folder_order;}
			set {if (value != m_folder_order) {m_folder_order = value; SettingsChanged.Raise(this, "FolderOrder"); } }
		}
		private ESortOrder m_folder_order = ESortOrder.ChronologicalAscending;

		/// <summary>The order of folders within the media list when running as a screen saver</summary>
		[Description(
			"The order of folders within the screen saver media list.\n"+
			"None - no ordering based on folders. If File Order is random, then files across all folders will be randomised.\n"+
			"Alphabetical - ordered based on folder name.\n"+
			"Chronological - ordered based on folder timestamp.\n"+
			"Random - folder order randomised.\n")]
		public ESortOrder SSFolderOrder
		{
			get {return m_ss_folder_order;}
			set {if (value != m_ss_folder_order) {m_ss_folder_order = value; SettingsChanged.Raise(this, "SSFolderOrder"); } }
		}
		private ESortOrder m_ss_folder_order = ESortOrder.None;

		/// <summary>"The order of files within folders in the media list</summary>
		[Description(
			"The order of files within folders in the media list.\n"+
			"None - no ordered applied to files.\n"+
			"Alphabetical - ordered based on file name.\n"+
			"Chronological - ordered based on file timestamp.\n"+
			"Random - file order randomised. If Folder Order is 'None' then files across all folders will be randomised.\n")]
		public ESortOrder FilesOrder
		{
			get {return m_files_order;}
			set {if (value != m_files_order) {m_files_order = value; SettingsChanged.Raise(this, "FilesOrder"); } }
		}
		private ESortOrder m_files_order = ESortOrder.ChronologicalAscending;

		/// <summary>The order of files with folders in the media list when running as a screen saver</summary>
		[Description(
			"The order of files within folders in the screen saver media list.\n"+
			"None - no ordered applied to files.\n"+
			"Alphabetical - ordered based on file name.\n"+
			"Chronological - ordered based on file timestamp.\n"+
			"Random - file order randomised. If Folder Order is 'None' then files across all folders will be randomised.\n")]
		public ESortOrder SSFilesOrder
		{
			get {return m_ss_files_order;}
			set {if (value != m_ss_files_order) {m_ss_files_order = value; SettingsChanged.Raise(this, "SSFilesOrder"); } }
		}
		private ESortOrder m_ss_files_order = ESortOrder.Random;

		/// <summary>Display the file name for the current media file</summary>
		[Description("Display the file name for the current media file")]
		public bool ShowFilenames
		{
			get {return m_show_filenames;}
			set {if (value != m_show_filenames) {m_show_filenames = value; SettingsChanged.Raise(this, "ShowFilenames"); } }
		}
		private bool m_show_filenames = true;

		/// <summary>"Options controlling render quality </summary>
		[Description("Options controlling render quality ")]
		public ViewImageControl.QualityModes RenderQuality
		{
			get {return m_render_quality;}
			set { if (value != m_render_quality) { m_render_quality = value; SettingsChanged.Raise(this, "RenderQuality");} }
		}
		private ViewImageControl.QualityModes m_render_quality = new ViewImageControl.QualityModes{CompositingQuality=CompositingQuality.HighQuality, InterpolationMode=InterpolationMode.HighQualityBicubic, PixelOffsetMode=PixelOffsetMode.Half, SmoothingMode=SmoothingMode.HighQuality};

		/// <summary>A collection of paths to search when building the media list.
		/// Replace the whole list when modifying for events to be generated</summary>
		[Description("The paths to search when building the media list")]
		public List<MediaPath> MediaPaths
		{
			get { return m_media_paths; }
			set { if (value != m_media_paths) {m_media_paths = value; SettingsChanged.Raise(this, "MediaPaths"); } }
		}
		private List<MediaPath> m_media_paths = new List<MediaPath>();

		/// <summary>A collection of paths to search when building the screen saver media list.
		/// Replace the whole list when modifying for events to be generated</summary>
		[Description("The paths to search when building the screen saver media list")]
		public List<MediaPath> SSMediaPaths
		{
			get { return m_ss_media_paths; }
			set { if (value != m_ss_media_paths) {m_ss_media_paths = value; SettingsChanged.Raise(this, "SSMediaPaths"); } }
		}
		private List<MediaPath> m_ss_media_paths = new List<MediaPath>();

		///// <summary>"A bitmask of the transitions to use.</summary>
		//[Description("A bitmask of the transitions to use.")]
		//[Browsable(false)]
		//public uint TransitionsMask
		//{
		//    get {return m_transitions_mask;}
		//    set {if (value != m_transitions_mask) {m_transitions_mask = value; SettingsChanged.Raise(this, "TransitionsMask");} }
		//}
		//private uint m_transitions_mask = Transition.AllMask;

		/// <summary>"Image files recently loaded</summary>
		[Description("Image files recently loaded")]
		[Browsable(false)]
		public string RecentFiles
		{
			get {return m_recent_files;}
			set {if (value != m_recent_files) {m_recent_files = value; SettingsChanged.Raise(this, "RecentFiles"); } }
		}
		private string m_recent_files = "";

		/// <summary>"The window position last used</summary>
		[Description("The window position last used")]
		[Browsable(false)]
		public Rectangle WindowBounds
		{
			get { return m_window_bounds; }
			set { if (value != m_window_bounds) {m_window_bounds = value; SettingsChanged.Raise(this, "WindowBounds");} }
		}
		private Rectangle m_window_bounds = Rectangle.Empty;

		/// <summary>"The location to check for updates</summary>
		[Description("The location to check for updates")]
		[Browsable(false)]
		public string UpdateURL
		{
			get {return m_updates_url;}
			set {if (value != m_updates_url) {m_updates_url = value; SettingsChanged.Raise(this, "UpdateURL"); } }
		}
		private string m_updates_url = @"http://www.rylogic.co.nz/latest_versions.xml";

		/// <summary>"The location to check for help information</summary>
		[Description("The location to check for help information")]
		[Browsable(false)]
		public string HelpURL
		{
			get {return m_help_url;}
			set {if (value != m_help_url) {m_help_url = value; SettingsChanged.Raise(this, "HelpURL"); } }
		}
		private string m_help_url = @"http://www.rylogic.co.nz/imager.php#manual";

		/// <summary>The filepath for this settings data</summary>
		private string m_path = "settings.xml";

		/// <summary>Called whenever a setting is changed</summary>
		public event Action<Settings,string> SettingsChanged;

		/// <summary>Save to xml file. Returns true if successful</summary>
		public bool Save() { return Save(m_path); }
		public bool Save(string filepath)
		{
			try { using (StreamWriter sw = new StreamWriter(filepath)) { new XmlSerializer(typeof(Settings)).Serialize(sw, this); } }
			catch { return false; }
			m_path = filepath;
			return true;
		}

		/// <summary>Load from xml file</summary>
		public static Settings Load() { return Load(Path.ChangeExtension(Application.ExecutablePath,".xml")); }
		public static Settings Load(string filepath)
		{
			Settings s;
			try { using (StreamReader sr = new StreamReader(filepath)) {s = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(sr);} }
			catch (Exception) { s = new Settings(); }
			s.m_path = filepath;
			return s;
		}

		/// <summary>Reset these settings to there defaults</summary>
		public void Reset()
		{
			string path = m_path;
			Settings defaults = new Settings();
			foreach (FieldInfo fi in typeof(Settings).GetFields(BindingFlags.Instance|BindingFlags.NonPublic))
				fi.SetValue(this, fi.GetValue(defaults));
			Save(path);
		}

		/// <summary>Display options in a property grid window</summary>
		public void ShowWindow(IWin32Window parent, string title, Size size)
		{
			Form form = new Form{Text=title ,StartPosition=FormStartPosition.CenterParent ,FormBorderStyle=FormBorderStyle.SizableToolWindow ,Size=size};
			PropertyGrid grid = new PropertyGrid{SelectedObject=this ,Dock=DockStyle.Fill ,PropertySort=PropertySort.NoSort, };
			form.Controls.Add(grid);
			form.ShowDialog(parent);
			Save(m_path);
		}
	}
	// ReSharper restore RedundantDefaultFieldInitializer
}
