using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Utility;

namespace LDraw
{
	public class StartupOptions
	{
		public StartupOptions(string[] args)
		{
			FilesToLoad = new List<string>();

			var exe_dir = Util2.ResolveAppPath();
			if (!Directory.Exists(exe_dir))
				throw new ArgumentException("Cannot determine the current executable directory");

			// Check the command line options
			for (int i = 0, iend = args.Length; i != iend; ++i)
			{
				var arg = args[i].ToLowerInvariant();

				// No option character implies the file to load
				if (arg[0] != '-' && arg[0] != '/')
				{
					FilesToLoad.Add(arg);
					continue;
				}

				// Helper for comparing option strings
				bool IsOption(string opt) => string.CompareOrdinal(arg, 0, opt, 0, opt.Length) == 0;

				// (order these by longest option first)
				if      (IsOption(CmdLine.SettingsPath )) { SettingsPath = arg.Substring(CmdLine.SettingsPath.Length); }
				else if (IsOption(CmdLine.Portable     )) { PortableMode = true; }
				else if (IsOption(CmdLine.ShowHelp     )) { ShowHelp = true; }
				else if (IsOption(CmdLine.ShowHelp2    )) { ShowHelp = true; }
				else throw new ArgumentException("Unknown command line option '"+arg+"'.");
			}

			// Determine whether to run the app in portable mode
			PortableMode |= Path_.FileExists(Path.Combine(exe_dir, "portable"));

			// Set the UserDataDir based on whether we're running in portable mode or not
			UserDataDir = Path.GetFullPath(PortableMode ? exe_dir : Util.ResolveUserDocumentsPath(Application.CompanyName, Application.ProductName));

			// If we're in portable mode, check that we have write access to the local directory
			if (PortableMode)
			{
				if (!Path_.DirExists(UserDataDir) || (new DirectoryInfo(UserDataDir).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					throw new IOException("Unable to run in portable mode as the directory ('"+UserDataDir+"') is readonly.");
			}
			// If not in portable mode, check the UserDataDir directory exists (or can be created)
			else
			{
				if (!Path_.DirExists(UserDataDir))
					Directory.CreateDirectory(UserDataDir);
			}

			// If a settings path has not been given, use the defaults
			if (SettingsPath == null)
				SettingsPath = Path.Combine(UserDataDir, "settings.xml");
		}

		/// <summary>The filepath to a file given on the command line</summary>
		public List<string> FilesToLoad { get; private set; }

		/// <summary>True if we should run the app in portable mode</summary>
		public bool PortableMode { get; private set; }

		/// <summary>A location that the app should read settings from and write to</summary>
		public string UserDataDir { get; private set; }

		/// <summary>The filepath to the settings file to use</summary>
		public string SettingsPath { get; set; }

		/// <summary>True if the command line help options should be displayed</summary>
		public bool ShowHelp { get; private set; }
	}

	public static class CmdLine
	{
		// Order by longest option first
		public const string SettingsPath = "-s";
		public const string Portable     = "-p";
		public const string ShowHelp     = "-h";
		public const string ShowHelp2    = "/?";

		/// <summary>Help string</summary>
		public static string Help
		{
			get
			{
				return
					"LDraw Command Line:\n" +
					"   Syntax: LDraw.exe [options] [script_filepath]\n" +
					"\n" +
					"   Options:\n" +
					"   -s <filepath> : Load/Save application settings to the given filepath.\n" +
					"   -p : Run LDraw in portable mode.\n" +
					"   -h or /? : Show this help message\n" +
					"";
			}
		}
	}
}
