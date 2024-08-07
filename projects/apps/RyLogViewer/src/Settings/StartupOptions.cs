﻿using System;
using System.IO;
using Rylogic.Common;
using Rylogic.Utility;

namespace RyLogViewer
{
	public class StartupOptions
	{
		public StartupOptions()
			:this(Array.Empty<string>())
		{ }
		public StartupOptions(string[] args)
		{
			var exe_dir = Util.ResolveAppPath();
			if (!Path_.DirExists(exe_dir))
				throw new ArgumentException("Cannot determine the current executable directory");

			// Check the command line options
			for (int i = 0, iend = args.Length; i != iend; ++i)
			{
				var arg = args[i].ToLowerInvariant();

				// No option character implies the file to load
				if (arg[0] != '-' && arg[0] != '/')
				{
					if (FileToLoad != null) throw new ArgumentException("Command line should specify a single file path only. If the file path contains white space, remember to use quotes. e.g. RyLogViewer \"my file.txt\"");
					FileToLoad = arg;
					continue;
				}

				// Helper for comparing option strings
				bool IsOption(string opt) => string.CompareOrdinal(arg, 0, opt, 0, opt.Length) == 0;

				// (order these by longest option first)
				if      (IsOption(CmdLineOption.RDelim))       { RowDelim = arg.Substring(CmdLineOption.RDelim.Length); }
				else if (IsOption(CmdLineOption.CDelim))       { ColDelim = arg.Substring(CmdLineOption.CDelim.Length); }
				else if (IsOption(CmdLineOption.NoGUI))        { NoGUI = true; }
				else if (IsOption(CmdLineOption.Silent))       { Silent = true; }
				else if (IsOption(CmdLineOption.PatternSet))   { PatternSetFilepath = arg.Substring(CmdLineOption.PatternSet.Length); }
				else if (IsOption(CmdLineOption.SettingsPath)) { SettingsPath = arg.Substring(CmdLineOption.SettingsPath.Length); }
				else if (IsOption(CmdLineOption.LogFilePath))  { LogFilePath = arg.Substring(CmdLineOption.LogFilePath.Length); }
				else if (IsOption(CmdLineOption.Export))       { ExportPath = arg.Substring(CmdLineOption.Export.Length); }
				else if (IsOption(CmdLineOption.Portable))     { PortableMode = true; }
				else if (IsOption(CmdLineOption.ShowHelp))     { ShowHelp = true; }
				else if (IsOption(CmdLineOption.ShowHelp2))    { ShowHelp = true; }
				else throw new ArgumentException("Unknown command line option '" + arg + "'.");
			}

			// Determine whether to run the app in portable mode
			PortableMode |= Path_.FileExists(Path_.CombinePath(exe_dir, "portable"));

			// Set the UserDataDir based on whether we're running in portable mode or not
			UserDataDir = PortableMode
				? Util.ResolveAppPath()
				: Util.ResolveUserDocumentsPath(Util.AppCompany, Util.AppProductName);

			// If we're in portable mode, check that we have write access to the local directory
			if (PortableMode)
			{
				if (!Path_.DirExists(UserDataDir) || (new DirectoryInfo(UserDataDir).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					throw new IOException("Unable to run in portable mode as the directory ('" + UserDataDir + "') is readonly.");
			}
			// If not in portable mode, check the UserDataDir directory exists (or can be created)
			else
			{
				if (!Path_.DirExists(UserDataDir))
					Directory.CreateDirectory(UserDataDir);
			}

			// If the export option is given, a 'FileToLoad' must also be given
			if (ExportPath != null && FileToLoad == null)
				throw new ArgumentException("A file to export must be given if the '-e' option is used");

			// If a settings path has not been given, use the defaults
			if (SettingsPath == null)
				SettingsPath = Path.Combine(UserDataDir, "settings2.xml");

			// Set the licence file path
			LicenceFilepath = PortableMode
				? Path.Combine(exe_dir, "licence.xml")
				: Path.Combine(UserDataDir, "licence.xml");

			// If no licence file exists, create the free one
			if (!Path_.FileExists(LicenceFilepath))
				new Licence().WriteLicenceFile(LicenceFilepath);
		}

		/// <summary>The filepath to a file given on the command line</summary>
		public string? FileToLoad { get; }

		/// <summary>True if we should run the app in portable mode</summary>
		public bool PortableMode { get; }

		/// <summary>A location that the app should read settings from and write to</summary>
		public string UserDataDir { get; }

		/// <summary>The filepath to the settings file to use</summary>
		public string SettingsPath { get; set; }

		/// <summary>The location of the licence file</summary>
		public string LicenceFilepath { get; set; }

		/// <summary>The file path to write log data do</summary>
		public string? LogFilePath { get; set; }

		/// <summary>Settings for an export from the command line. Null if not an export</summary>
		public string? ExportPath { get; }

		/// <summary>The row delimiter to use in the output file of an export (humanised)</summary>
		public string RowDelim { get; } = "\n";

		/// <summary>The column delimiter to use in the output file of an export (humanised)</summary>
		public string ColDelim { get; } = ",";

		/// <summary>The pattern set file to load</summary>
		public string? PatternSetFilepath { get; }

		/// <summary>True if the app should run without displaying any UI</summary>
		public bool NoGUI { get; }

		/// <summary>True if the app shouldn't write anything to stdout</summary>
		public bool Silent { get; }

		/// <summary>True if the command line help options should be displayed</summary>
		public bool ShowHelp { get; }

		private class CmdLineOption
		{
			// Order by longest option first
			public const string RDelim = "-rdelim";
			public const string CDelim = "-cdelim";
			public const string Silent = "-silent";
			public const string NoGUI = "-nogui";
			public const string PatternSet = "-ps";
			public const string SettingsPath = "-s";
			public const string LogFilePath = "-l";
			public const string Export = "-e";
			public const string Portable = "-p";
			public const string ShowHelp = "-h";
			public const string ShowHelp2 = "/?";
		}
	}
}
