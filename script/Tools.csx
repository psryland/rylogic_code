#! "net9.0"
// This is a port of 'Rylogic.py' to C# script
#r "System.Text.Json"
#r "nuget: Rylogic.Core, 1.0.4"
#r "System.Diagnostics.Process"
#load "UserVars.csx"
#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Rylogic.Extn;
using Console = Internal.Console;
using IOPath = System.IO.Path;

public class Tools
{
	/// <summary>Path helper</summary>
	public static string Path(IEnumerable<string> path_parts, bool check_exists = true, bool normalise = true)
	{
		return UserVars.Path(path_parts, check_exists, normalise);
	}

	// Ensure the directory 'dir' exists and is empty
	public static void CleanDir(string dir)
	{
		Console.WriteLine($"Cleaning deploy directory: {dir}");
		Directory.Delete(dir, true);
		Directory.CreateDirectory(dir);
	}

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
				UseShellExecute = shell, // Must be false to redirect output
				CreateNoWindow = false, // Show output in current console window
				WorkingDirectory = cwd ?? string.Empty,
			};
			if (return_output)
			{
				psi.StandardOutputEncoding = encoding ?? Encoding.UTF8;
				psi.StandardErrorEncoding = encoding ?? Encoding.UTF8;
			}

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

		Console.WriteLine("Initializing VS Environment");

		// Just need this?
		// set VCToolsInstallDir=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.41.34120\
		// set PATH=%VCToolsInstallDir%bin\Hostx64\x64;%PATH%

		// Find 'vswhere.exe' from the VS installer
		var program_files = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? throw new Exception("ProgramFiles(x86) environment variable is not defined");
		var vswhere = Path([program_files, "Microsoft Visual Studio", "Installer", "vswhere.exe"]);

		// Get the VS install path
		var (_, vs_path) = Run([vswhere, "-latest", "-property", "installationPath"]);
		vs_path = vs_path.TrimEnd();

		// Get the VcVars batch file
		var vsvars_path = Path([vs_path, "VC", "Auxiliary", "Build", "vcvars64.bat"]);

		// Run the vcvars batch file, then dump the state of the environment variables
		var (_, env) = Run(["cmd", "/C", vsvars_path, "&", "set"]);

		// Add/Replace the environment variables in 'os.environ'
		foreach (var line in env.Split("\n", StringSplitOptions.RemoveEmptyEntries))
		{
			var m = Regex.Match(line, "^(.+?)=(.*)$");
			if (!m.Success)
				continue;

			var key = m.Groups[1].Value.Trim();
			var value = m.Groups[2].Value.Trim();
			Environment.SetEnvironmentVariable(key, value);
		}

		//Console.WriteLine("Environment:\n");
		//foreach (System.Collections.DictionaryEntry x in Environment.GetEnvironmentVariables())
		//	Console.WriteLine($"{x.Key} = {x.Value}");
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
	public static bool MSBuild(string sln_or_proj_file, IList<string>? projects = null, IList<string>? platforms = null, IList<string>? configs = null, bool parallel = false, bool same_window = true, IList<string>? additional_args = null)
	{
		SetupVcEnvironment();

		if (UserVars.MSBuild is null)
			throw new Exception("MSBuild path has not been set in UserVars");

		// Handle default options
		projects ??= [];
		platforms ??= [];
		configs ??= [];
		additional_args ??= [];

		// Build the arguments list
		List<string> args = [UserVars.MSBuild, sln_or_proj_file, "/m", "/verbosity:minimal", "/nologo", ..additional_args];

		// Set the targets to build
		// Targets should be the names as shown in the solution explorer (i.e. Folder\Project.Name)
		if (projects.Count != 0)
		{
			// Replace '.' in the project name with '_'
			projects = [..projects.Select(x => x.Replace(".", "_")), ];
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
						List<string> args_ = [..args, $"/p:Configuration={config};Platform={platform}"];
						if (parallel)
						{
							procs.Add(Spawn(args_, same_window: same_window));
						}
						else
						{
							Console.WriteLine($"{platform}|{config}:");
							Run(args_, return_output: false, show_arguments: false);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Build Errors: {ex.Message}");
			errors = true;
		}
		finally
		{
			// Wait for all processes to finish, and check for error return codes
			foreach (var proc in procs)
			{
				proc.WaitForExit();
				errors |= proc.ExitCode != 0;
			}
		}

		return !errors;
	}

	// Extract data from a text file using a regex
	// Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
	// Returns the regex match object for the first match or null
	// Encoding can be: ascii, utf-8, cp1250, etc
	public static Match Extract(string filepath, Regex pat, Encoding? encoding = null, bool by_line = true)
	{
		if (by_line)
		{
			foreach (var line in File.ReadAllLines(filepath, encoding: encoding ?? Encoding.UTF8))
			{
				var m = pat.Match(line);
				if (m.Success)
					return m;
			}
		}
		else
		{
			var s = File.ReadAllText(filepath);
			var m = pat.Match(s);
			if (m.Success)
				return m;
		}

		return Match.Empty;
	}

	// Copy 'src' to 'dst' optionally if 'src' is newer than 'dst'
	public static void Copy(string src, string dst, bool only_if_modified = true, bool show_unchanged = false, bool ignore_missing = false, bool quiet = false, bool full_paths = true, Regex? filter = null, bool follow_symlinks = true, string? indent = null)
	{
		bool src_is_dir = src.EndsWith("/") || src.EndsWith("\\");
		bool dst_is_dir = dst.EndsWith("/") || dst.EndsWith("\\");
		src = Path([src], check_exists: true);
		dst = Path([dst], check_exists: false);
		src_is_dir |= Directory.Exists(src);
		dst_is_dir |= Directory.Exists(dst) || src_is_dir;

		indent ??= string.Empty;

		// Find the source files/directories to copy (filenames only, not full paths)
		List<string> lst = [];
		if (src_is_dir)
		{
			var fnames = Directory.GetFileSystemEntries(src).Select(IOPath.GetFileName).Where(x => x is not null).Select(x => x!).ToList();
			lst.AddRange(fnames);
		}
		else if (File.Exists(src))
		{
			lst.Add(IOPath.GetFileName(src));
		}
		else if (src.Contains("*") || src.Contains("?"))
		{
			var dir = IOPath.GetDirectoryName(src) ?? Directory.GetCurrentDirectory();
			var pattern = IOPath.GetFileName(src);
			var fnames = Directory.GetFiles(dir, pattern).Select(IOPath.GetFileName).Where(x => x is not null).Select(x => x!).ToList();
			lst.AddRange(fnames);
		}
		else if (!ignore_missing)
		{
			throw new FileNotFoundException($"ERROR: {src} does not exist");
		}

		// If the 'src' represents multiple files, 'dst' must be a directory
		if (src_is_dir || lst.Count > 1)
		{
			if (!Directory.Exists(dst))
			{
				dst_is_dir = true;
			}
			else if (!dst_is_dir)
			{
				throw new FileNotFoundException($"ERROR: {dst} is not a valid directory");
			}
		}

		var srcdir = (src_is_dir ? src : IOPath.GetDirectoryName(src) ?? string.Empty).TrimEnd('/', '\\');
		var dstdir = (dst_is_dir ? dst : IOPath.GetDirectoryName(dst) ?? string.Empty).TrimEnd('/', '\\');

		// Ensure 'dstdir' exists
		if (!Directory.Exists(dstdir))
		{
			Directory.CreateDirectory(dstdir);

			// Copy directory attributes if needed
			if (src_is_dir && Directory.Exists(srcdir))
			{
				var srcInfo = new DirectoryInfo(srcdir);
				var dstInfo = new DirectoryInfo(dstdir);
				dstInfo.Attributes = srcInfo.Attributes;
			}

			if (!quiet)
			{
				if (full_paths)
					Console.WriteLine($"{indent}{srcdir}\\ --> {dstdir}\\");
				else
					Console.WriteLine($"{indent}{IOPath.GetFileName(srcdir)}\\ --> {IOPath.GetFileName(dstdir)}\\");
			}
				;
		}

		// Copy the source files/directories to 'dst'
		foreach (var src_item in lst)
		{
			var s = IOPath.Combine(srcdir, src_item);
			var d = dst_is_dir ? IOPath.Combine(dstdir, src_item) : dst;

			// Test for symlinks...
			if (File.Exists(s) && new FileInfo(s).Attributes.HasFlag(FileAttributes.ReparsePoint))
				throw new Exception("ERROR: This copy function doesn't consider symlinks yet. It's a todo...");

			// Call recursively for directory copies
			if (Directory.Exists(s))
			{
				Copy(s, d + "\\", only_if_modified, show_unchanged, ignore_missing, quiet, full_paths, filter, follow_symlinks, indent + indent);
			}
			else
			{
				// Copy if not excluded by the filter
				if (filter != null && !filter.IsMatch(s))
					continue;

				// Copy if modified or always based on the flag
				if (only_if_modified && !DiffContent(s, d))
				{
					if (!quiet && show_unchanged)
						Console.WriteLine($"{indent}{s} --> unchanged");

					continue;
				}

				if (!quiet)
				{
					if (full_paths)
						Console.WriteLine($"{indent}{s} --> {d}");
					else
						Console.WriteLine($"{indent}{IOPath.GetFileName(s)} --> {IOPath.GetFileName(d)}");
				}
				File.Copy(s, d, true);
			}
		}
	}

	// Compare the content of two files and return true if they are different, ignoring file timestamps
	public static bool DiffContent(string src, string dst, bool trace = false, int blocksize = 65536)
	{
		var sfound = File.Exists(src);
		var dfound = File.Exists(dst);

		if (!sfound)
		{
			if (trace) Console.WriteLine($"Content different, '{src}' not found");
			return true;
		}
		if (!dfound)
		{
			if (trace) Console.WriteLine($"Content different, '{dst}' not found");
			return true;
		}

		var src_info = new FileInfo(src);
		var dst_info = new FileInfo(dst);

		if (src_info.Length != dst_info.Length)
		{
			if (trace) Console.WriteLine($"Content different, '{src}' and '{dst}' have different sizes");
			return true;
		}

		using (var sha256 = System.Security.Cryptography.SHA256.Create())
		{
			byte[] srcHash, dstHash;
			using (var s = new FileStream(src, FileMode.Open, FileAccess.Read))
				srcHash = sha256.ComputeHash(s);
			using (var d = new FileStream(dst, FileMode.Open, FileAccess.Read))
				dstHash = sha256.ComputeHash(d);

			if (!srcHash.SequenceEqual(dstHash))
			{
				if (trace) Console.WriteLine($"Content different, '{src}' and '{dst}' have different hashes");
				return true;
			}
		}
		return false;
	}
}