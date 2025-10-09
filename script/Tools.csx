#! "net9.0"
// This is a port of 'Rylogic.py' to C# script
#r "System.Text.Json"
#r "nuget: Rylogic.Core, 1.0.4"
#r "System.Diagnostics.Process"
#load "UserVars.csx"
#nullable enable

using System;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Console = Internal.Console;

public class Tools
{
	//Executes a program and returns it's stdout/stderr as a string
	// 'checked' will raise an exception if the program returns a non-zero exit code
	// 'return_output' will return the output of the program as a string. If false, the output is printed to stdout
	// 'expected_return_code' is the expected return code. If the program returns a different code, an exception is raised
	// Returns (result,output)
	public static (bool Result, string Output) Run(IList<string> args, bool throw_on_error = true, bool return_output = true, int expected_return_code = 0, bool show_arguments = false, string? cwd = null, bool shell = false, Encoding? encoding = null)
	{
		if (args.Count == 0)
			throw new ArgumentException("args must contain at least one element (the program to run)");
		if (show_arguments)
			Console.WriteLine(string.Join(" ", args));

		try
		{
			var filename = args[0];
			var arguments = string.Join(" ", args.Skip(shell ? 0 : 1).Select(x => x.Contains(" ") ? $"\"{x}\"" : x));
			var psi = new ProcessStartInfo
			{
				FileName = filename,
				Arguments = arguments,
				RedirectStandardOutput = return_output,
				RedirectStandardError = return_output,
				UseShellExecute = shell,
				CreateNoWindow = true,
				WorkingDirectory = cwd ?? string.Empty,
				StandardOutputEncoding = encoding ?? Encoding.UTF8,
				StandardErrorEncoding = encoding ?? Encoding.UTF8,
			};

			using var proc = new Process { StartInfo = psi };
			proc.Start();

			var output = return_output ? proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd() : "";
			proc.WaitForExit();

			var success = proc.ExitCode == expected_return_code;
			if (!success && throw_on_error)
				throw new Exception($"Process returned {proc.ExitCode}: {output}");

			return (success, output);
		}
		catch (Exception ex)
		{
			if (throw_on_error) throw;
			return (false, ex.Message);
		}
	}

	// Run a program in a separate console window
	// Returns the process for the caller to call wait() on,
	//  e.g.
	//    proc = Spawn(["cmd", "/C" ,"echo Hello"])
	//    proc.wait()
	public static Process Spawn(IList<string> args, int expected_return_code = 0, bool same_window = false, bool show_window = true, bool show_arguments = false)
	{
		if (args.Count == 0)
			throw new ArgumentException("args must contain at least one element (the program to run)");
		if (show_arguments)
			Console.WriteLine(string.Join(" ", args));

		var filename = args[0];
		var arguments = string.Join(" ", args.Skip(1).Select(x => x.Contains(" ") ? $"\"{x}\"" : x));
		var psi = new ProcessStartInfo
		{
			FileName = filename,
			Arguments = arguments,
			UseShellExecute = !same_window, // Required to open a new console
			CreateNoWindow = !show_window,
			WindowStyle = show_window ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
		};
		
		return Process.Start(psi) ?? throw new Exception("Failed to start process");
	}

	// Find the visual studio batch file for setting up a dev environment
	public static void SetupVcEnvironment()
	{
		// Already set up
		if (Environment.GetEnvironmentVariable("VISUALSTUDIOVERSION") is not null)
			return;

		// Not a windows environment
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return;

		// VS environment not set up in user vars
		//if not UserVars.vs_envvars: 
		//	print("VS Environment not set in UserVars")
		//	return

		//print("Initializing VS Environment", flush=True)

		// Find 'vswhere.exe' from the VS installer
		var vswhere = Path.Join(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "Microsoft Visual Studio", "Installer", "vswhere.exe");
		if (!Path.Exists(vswhere))
			throw new Exception($"vswhere.exe not found at: {vswhere}");

		// Get the VS install path
		var (_, vs_path) = Run([vswhere, "-latest", "-property", "installationPath"]);
		vs_path = vs_path.TrimEnd();

		// Get the VcVars batch file
		var vsvars_path = Path.Join(vs_path, "VC", "Auxiliary", "Build", "vcvars64.bat");

		// Run the vcvars batch file, then dump the state of the environment variables
		var (_, env) = Run(["cmd", "/C", vsvars_path, "&", "set"]);

		// Add/Replace the environment variables in 'os.environ'
		foreach (var line in env.Split("\n", StringSplitOptions.RemoveEmptyEntries))
		{
			var m = Regex.Match(line, "^(.+?)=(.*)$");
			if (!m.Success)
				continue;

			var key = m.Captures[1].Value;
			var value = m.Captures[2].Value;
			Environment.SetEnvironmentVariable(key, value);
		}

		// print("Environment:\n")
		// for k in os.environ: print(f"{k} = {os.environ[k]}")
	}

	// Invoke MSBuild on a solution or project file.
	// Solution file usage:
	//   sln_or_proj_file = "C:\path\mysolution.sln"
	//	projects = ["project_name","\"folder\proj_name:Rebuild\""]
	//	platforms = ["x64","x86","Any CPU"]
	//	configs = ["release","debug"]
	//	Tools.MSBuild(sln_or_proj_file, projects, platforms, configs, True, True)
	// Project file usage:
	//   sln_or_proj_file = "C:\path\myproject.csproj"
	//	projects = []
	//	platforms = ["x64","x86","AnyCPU"]
	//	configs = ["release","debug"]
	//	Tools.MSBuild(sln_or_proj_file, projects, platforms, configs, True, True)
	public static void MSBuild(string sln_or_proj_file, IList<string>? projects = null, IList<string>? platforms = null, IList<string>? configs = null, bool parallel = false, bool same_window = true, IList<string>? additional_args = null)
	{
		//Log.Message(f"\nBuilding {name}")
		SetupVcEnvironment();

		if (UserVars.MSBuild is null)
			throw new Exception("MSBuild path has not been set in UserVars");

		// Handle default options
		projects ??= [];
		platforms ??= [];
		configs ??= [];
		additional_args ??= [];

		// Build the arguments list
		List<string> args = [UserVars.MSBuild, sln_or_proj_file, "/m", "/verbosity:minimal", "/nologo"];
		args.AddRange(additional_args);

		// Set the targets to build
		// Targets should be the names as shown in the solution explorer (i.e. Folder\Project.Name)
		if (projects.Count != 0)
		{
			// Replace '.' in the project name with '_'
			projects = [.. projects.Select(x => x.Replace(".", "_"))];
			args.Add($"/t:{string.Join(';', projects)}");
		}

		// Set the platform/config
		List<Process> procs = [];
		bool errors = false;
		try
		{
			if (platforms.Count == 0 && configs.Count == 0)
			{
				Run(args);
			}
			else
			{
				foreach (var platform in platforms)
				{
					foreach (var config in configs)
					{
						var args_ = args;
						args_.Add($"/p:Configuration={config};Platform={platform}");

						if (parallel)
						{
							procs.Add(Spawn(args_, same_window: same_window));
						}
						else
						{
							//Log.Message($"{platform}|{config}:");
							Run(args_);
						}
					}
				}
			}
		}
		// Wait for all processes to finish, and check for error return codes
		finally
		{
			foreach (var proc in procs)
			{
				proc.WaitForExit();
				errors |= proc.ExitCode != 0;
			}
		}

		if (errors)
			throw new Exception("Build Failed");
	}
}