using System;
using System.IO;
using System.Windows.Forms;

namespace RyLogViewer
{
	public class StartupOptions
	{
		/// <summary>The filepath to a file given on the commandline</summary>
		public string FileToLoad { get; private set; }
		
		/// <summary>True if we should run the app in portable mode</summary>
		public bool PortableMode { get; private set; }
		
		/// <summary>The directory that the executable is running from</summary>
		public string ExeDir { get; private set; }
		
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
		
		/// <summary>True if the app should run with displaying any UI</summary>
		public bool NoGUI { get; private set; }
		
		/// <summary>True if the command line help options should be displayed</summary>
		public bool ShowHelp { get; private set; }

		/// <summary>Load and parse the startup options</summary>
		public StartupOptions(string[] args)
		{
			ExeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".\\";
			if (ExeDir == null) throw new ArgumentException("Cannot determine the current executable directory");
			
			// Determine whether to run the app in portable mode
			PortableMode = File.Exists(Path.Combine(ExeDir, "portable"));

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
				Func<string, bool> IsOption = opt => { return string.CompareOrdinal(arg, 0, opt, 0, opt.Length) == 0; };

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
			
			// If the export option is given, a 'FileToLoad' must also be given
			if (ExportPath != null && FileToLoad == null)
				throw new ArgumentException("A file to export must be given if the '-e' option is used");
			
			// If a settings path has not been given, use the defaults
			if (SettingsPath == null)
				SettingsPath = PortableMode
					? Path.Combine(ExeDir, "settings.xml")
					: Settings.DefaultFilepath;
			
			// Check that we have write access to the settings file location
			string settings_dir = Path.GetDirectoryName(SettingsPath) ?? ".\\";
			if (Directory.Exists(settings_dir) && (new DirectoryInfo(settings_dir).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				throw new ArgumentException("Unable to run in portable mode as the local directory ('"+ExeDir+"') is readonly.");
		}
	}
}
