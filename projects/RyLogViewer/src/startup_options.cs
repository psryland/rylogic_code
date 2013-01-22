using System;
using System.IO;
using System.Windows.Forms;

namespace RyLogViewer
{
	public class StartupOptions
	{
		/// <summary>The filepath to a file given on the command line</summary>
		public string FileToLoad { get; private set; }
		
		/// <summary>True if we should run the app in portable mode</summary>
		public bool PortableMode { get; private set; }
		
		/// <summary>A location that the app should read settings from and write to</summary>
		public string AppDataDir { get; private set; }
		
		/// <summary>The filepath to the settings file to use</summary>
		public string SettingsPath { get; set; }
		
		/// <summary>Settings for an export from the command line. Null if not an export</summary>
		public string ExportPath { get; private set; }
		
		/// <summary>The row delimiter to use in the output file of an export (humanised)</summary>
		public string RowDelim { get; private set; }
		
		/// <summary>The column delimiter to use in the output file of an export (humanised)</summary>
		public string ColDelim { get; private set; }
		
		/// <summary>The highlight set file to load</summary>
		public string HighlightSetPath { get; private set; }
		
		/// <summary>The filter set file to load</summary>
		public string FilterSetPath { get; private set; }
		
		/// <summary>The transform set file to load</summary>
		public string TransformSetPath { get; private set; }
		
		/// <summary>True if the app should run without displaying any UI</summary>
		public bool NoGUI { get; private set; }

		/// <summary>True if the command line help options should be displayed</summary>
		public bool ShowHelp { get; private set; }

		/// <summary>Load and parse the startup options</summary>
		public StartupOptions(string[] args)
		{
			var exe_dir = Path.GetDirectoryName(Application.ExecutablePath) ?? @".\";
			if (!Directory.Exists(exe_dir))
				throw new ArgumentException("Cannot determine the current executable directory");
			
			// Get the app data directory
			var app_dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			app_dir = Path.Combine(app_dir, Application.CompanyName);
			app_dir = Path.Combine(app_dir, Application.ProductName);
			
			// Determine whether to run the app in portable mode
			PortableMode = File.Exists(Path.Combine(exe_dir, "portable"));
			
			// Check the command line options
			for (int i = 0, iend = args.Length; i != iend; ++i)
			{
				string arg = args[i].ToLowerInvariant();
				
				if (arg[0] != '-')
				{
					if (FileToLoad != null) throw new ArgumentException("File to load already given");
					FileToLoad = arg;
					continue;
				}
				
				// Helper for comparing option strings
				Func<string, bool> IsOption = opt => string.CompareOrdinal(arg, 0, opt, 0, opt.Length) == 0;
				
				// (order these by longest option first)
				if      (IsOption(CmdLineOption.RDelim       )) { RowDelim = arg.Substring(CmdLineOption.RDelim.Length); }
				else if (IsOption(CmdLineOption.CDelim       )) { ColDelim = arg.Substring(CmdLineOption.CDelim.Length); }
				else if (IsOption(CmdLineOption.NoGUI        )) { NoGUI = true; }
				else if (IsOption(CmdLineOption.HighlightSet )) { HighlightSetPath = arg.Substring(CmdLineOption.HighlightSet.Length); }
				else if (IsOption(CmdLineOption.FilterSet    )) { FilterSetPath = arg.Substring(CmdLineOption.FilterSet.Length); }
				else if (IsOption(CmdLineOption.TransformSet )) { TransformSetPath = arg.Substring(CmdLineOption.TransformSet.Length); }
				else if (IsOption(CmdLineOption.Portable     )) { PortableMode = true; }
				else if (IsOption(CmdLineOption.SettingsPath )) { SettingsPath = arg.Substring(CmdLineOption.SettingsPath.Length); }
				else if (IsOption(CmdLineOption.Export       )) { ExportPath = arg.Substring(CmdLineOption.Export.Length); }
				else if (IsOption(CmdLineOption.ShowHelp     )) { ShowHelp = true; }
				else throw new ArgumentException("Unknown command line option '"+arg+"'.");
			}
			
			// Set the AppDataDir based on whether we're running in portable mode or not
			AppDataDir = Path.GetFullPath(PortableMode ? exe_dir : app_dir);
			
			// If we're in portable mode, check that we have write access to the local directory
			if (PortableMode)
			{
				if (!Directory.Exists(AppDataDir) || (new DirectoryInfo(AppDataDir).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					throw new IOException("Unable to run in portable mode as the directory ('"+AppDataDir+"') is readonly.");
			}
			// If not in portable mode, check the AppData directory exists (or can be created)
			else
			{
				if (!Directory.Exists(AppDataDir))
					Directory.CreateDirectory(AppDataDir);
			}
			
			// If the export option is given, a 'FileToLoad' must also be given
			if (ExportPath != null && FileToLoad == null)
				throw new ArgumentException("A file to export must be given if the '-e' option is used");
			
			// If a settings path has not been given, use the defaults
			if (SettingsPath == null)
				SettingsPath = Path.Combine(AppDataDir, "settings.xml");
		}
	}
}
