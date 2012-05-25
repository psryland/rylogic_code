using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace pr
{
	[Serializable]
	public class Settings
	{
		public string    recent_files    = "";
		public bool      auto_reload     = true;
		public Rectangle window_position = new Rectangle(
				Screen.PrimaryScreen.WorkingArea.X + Screen.PrimaryScreen.WorkingArea.Width  * 1/6,
				Screen.PrimaryScreen.WorkingArea.Y + Screen.PrimaryScreen.WorkingArea.Height * 1/8,
				Screen.PrimaryScreen.WorkingArea.Width  * 2/3,
				Screen.PrimaryScreen.WorkingArea.Height * 3/4);

		public static Rectangle WindowPosition  {get {return This.window_position     ;} set {This.window_position     = value;}}
		public static string    RecentFiles     {get {return This.recent_files        ;} set {This.recent_files        = value;}}
		public static bool      AutoReload      {get {return This.auto_reload         ;} set {This.auto_reload         = value;}}

		// Singleton access
		private class SingletonCreator { internal static readonly Settings m_instance = Load(); }
		public static Settings This { get { return SingletonCreator.m_instance; } }

		// Events
		public delegate void OnSaveHandler();
		public delegate void OnSaveErrorHandler(Exception ex);
		public static event OnSaveHandler OnSave;
		public static event OnSaveErrorHandler OnSaveError;
		
		// Return the filepath of where the app settings are saved
		public static string FilePath
		{
			get { return Path.GetFullPath(Application.StartupPath + @"\settings.xml"); }
		}

		// Load an instance of the application settings
		public static Settings Load()
		{
			try
			{
				using (StreamReader sr = new StreamReader(FilePath))
					return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(sr);
			}
			catch (Exception) { return new Settings(); } // Create default settings
		}

		// Save the application settings
		public static void Save()
		{
			try
			{
				using (StreamWriter sw = new StreamWriter(FilePath))
					new XmlSerializer(typeof(Settings)).Serialize(sw, This);
				if (OnSave != null) OnSave(); // Notify of settings saved
			}
			catch (Exception ex) { if (OnSaveError != null) OnSaveError(ex); } // Notify of the save error
		}
	}
}
