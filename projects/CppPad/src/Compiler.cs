using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using pr.common;
using pr.util;

namespace CppPad
{
	public interface Compiler :IDisposable
	{
		/// <summary>Compiler specific build</summary>
		Process CreateBuildProcess(IEnumerable<string> files, string obj_dir);

		///// <summary>Called when the compile process completes</summary>
		//private void HandleCompileFinished(object sender, EventArgs e)
		//{
		//	// Note: this is called in a worker thread context

		//	var proc = (Process)sender;
		//	proc.OnBuildComplete();

		//	//	# If the /c switch is given, then no exe is produced
		//	//	if "/c" in args:
		//	//		return ""
		//	//
		//	//	# Return the name of the executable
		//	//	exe = outdir + "\\" + fname + ".exe"

		//}
	}

	/// <summary>The MSVC compiler</summary>
	public class CompilerMSVC :Compiler
	{
		public CompilerMSVC(MSVCCompilerSettings settings)
		{
			Settings = settings;

			// Set up the environment variables for the compiler
			foreach (var ev in settings.EnvironmentVars)
				Environment.SetEnvironmentVariable(ev.Key, ev.Value);
		}
		public virtual void Dispose()
		{}

		/// <summary>Application settings</summary>
		public MSVCCompilerSettings Settings { get; private set; }

		/// <summary>Build using MSVC</summary>
		public Process CreateBuildProcess(IEnumerable<string> files, string obj_dir)
		{
			// Get the compiler path
			var cl = Settings.CompilerExePath;

			// Generate the command line arguments
			var args = new StringBuilder();

			// Add compiler switches
			foreach (var sw in Settings.Switches)
				args.Append($" {sw}");

			// Add defines
			foreach (var def in Settings.Defines)
				args.Append($" /D{def}");

			// Add include paths
			foreach (var inc in Settings.IncludePaths)
				args.Append($" /I\"{inc}\"");

			// Add library paths
			foreach (var lib in Settings.LibraryPaths)
				args.Append($" /L\"{lib}\"");

			// Add more switches extracted from the source file
			//args += ExtractSwitches(src_file)

			// Set the output file
			//args.Append($" /Fo\"{obj_dir}\"");

			// Set the executable
			//args += ["/Fe" + outdir + "\\"]

			// Add the files to be compiled
			foreach (var file in files)
				args.Append($" \"{file}\"");

			// Create the compile process
			var info = new ProcessStartInfo
			{
				UseShellExecute        = false,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				CreateNoWindow         = true,
				FileName               = cl,
				Arguments              = args.ToString(),
				WorkingDirectory       = Path_.Canonicalise(obj_dir),
			};
			return new Process { StartInfo = info };
		}
	}
}
