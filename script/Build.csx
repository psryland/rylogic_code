#! "net9.0"
// Build script
#r "System.IO"
#r "System.Text.Json"
#r "nuget: Rylogic.Core, 1.0.4"
#load "UserVars.csx"
#load "Tools.csx"
#load "BuildInstaller.csx"
#load "Nuget.csx"
#nullable enable

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Rylogic.Extn;
using Console = Internal.Console;

// Available projects that can be built
public enum EProjects
{
	Sqlite3,            // = "Sqlite3";
	Scintilla,          // = "Scintilla";
	Audio,              // = "Audio";
	View3d,             // = "View3d";
	P3d,                // = "P3d";
	Rylogic,            // = "Rylogic";
	RylogicCore,        // = "Rylogic.Core";
	RylogicDB,          // = "Rylogic.DB";
	RylogicDirectShow,  // = "Rylogic.DirectShow";
	RylogicGfx,         // = "Rylogic.Gfx";
	RylogicGuiWPF,      // = "Rylogic.Gui.WPF";
	RylogicNet,         // = "Rylogic.Net";
	RylogicScintilla,   // = "Rylogic.Scintilla";
	RylogicWindows,     // = "Rylogic.Windows";
	Csex,               // = "Csex";
	LDraw,              // = "LDraw";
	RyLogViewer,        // = "RyLogViewer";
	RylogicTextAligner, // = "Rylogic.TextAligner";
	AllNative,          // = "AllNative";
	AllManaged,         // = "AllManaged";
	All,                // = "All";
}

// Base class for all builders
public abstract class Common
{
	public string Workspace;
	public bool RequiresSigning;
	public Common(string workspace)
	{
		Workspace = workspace;
		RequiresSigning = false;
	}
	public virtual void Clean() { }
	public virtual void Build() { }
	public virtual void Deploy() { }
	public virtual void Publish() { }
}

// Groups of projects
public abstract class Group : Common
{
	public List<Common> Items = [];

	public Group(string workspace)
		: base(workspace)
	{ }
	public override void Clean()
	{
		foreach (var item in Items)
			item.Clean();
	}
	public override void Build()
	{
		foreach (var item in Items)
			item.Build();
	}
	public override void Deploy()
	{
		foreach (var item in Items)
			item.Deploy();
	}
	public override void Publish()
	{
		foreach (var item in Items)
			item.Publish();
	}
}

// Base class for .NET projects
public abstract class Managed : Common
{
	public string ProjName;
	public string ProjDir;
	public string RylogicSln;
	public IList<string> Frameworks;
	public IList<string> Platforms;
	public IList<string> Configs;

	public Managed(string proj_name, string proj_dir, IList<string> frameworks, string workspace, IList<string>? platforms, IList<string>? configs)
		:base(workspace)
	{
		ProjName = proj_name;
		ProjDir = Tools.Path([workspace, proj_dir]);
		RylogicSln = Tools.Path([workspace, "rylogic.sln"]);
		Frameworks = frameworks;
		Platforms = platforms ?? ["Any CPU"];
		Configs = configs ?? ["Release", "Debug"];
		RequiresSigning = true;
	}
	public override void Clean()
	{
		CleanDotNet(ProjDir, Platforms, Configs);
	}

	// Clean the 'bin' and 'obj' directory of a dot net project
	public static void CleanDotNet(string proj_dir, IList<string>? platforms = null, IList<string>? configs = null)
	{
		Tools.CleanDir(Tools.Path([proj_dir, "obj"], check_exists: false));
		Tools.CleanDir(Tools.Path([proj_dir, "bin"], check_exists: false));
		if (platforms is not null && configs is not null)
		{
			// todo - only clean specific directories
		}
	}

	// Restore nuget packages
	public static void DotNetRestore(string sln_or_proj)
	{
		if (m_restored.Contains(sln_or_proj))
			return;

		Tools.SetupVcEnvironment();

		Console.WriteLine($"Nuget restore: {sln_or_proj}");
		Tools.Run([UserVars.MSBuild, sln_or_proj, "/t:restore", "/verbosity:minimal", "/nologo"]);
		//Tools.Run([UserVars.dotnet, "restore", sln_or_proj, "--verbosity", "quiet"])
		//Tools.Run([UserVars.nuget, "restore", sln_or_proj, "-Verbosity", "quiet"])
		m_restored.Add(sln_or_proj);
	}
	private static List<string> m_restored = [];
}

// Rylogic .NET assembly (base)
public abstract class RylogicAssembly : Managed
{
	public string? Nupkg = null;

	public RylogicAssembly(string proj_name, List<string> frameworks, string workspace, List<string>? platforms, List<string>? configs)
		:base(proj_name, Tools.Path([workspace, "projects\\rylogic", proj_name]), frameworks, workspace, platforms, configs)
	{
	}
	public override void Build()
	{
		DotNetRestore(RylogicSln);
		Tools.MSBuild(RylogicSln, [$"Rylogic\\{ProjName}"], Platforms, Configs);
	}
	public override void Deploy()
	{
		Nupkg = Nuget.Package(Tools.Path([ProjDir, $"{ProjName}.csproj"]), NugetFiles());
	}
	public override void Publish()
	{
		if (Nupkg is null || !Path.Exists(Nupkg))
			throw new Exception("Call Deploy before calling Publish");

		Nuget.Publish(Nupkg);
	}
	protected abstract IEnumerable<Nuget.File> NugetFiles();
}

// Rylogic.Core .NET assembly
public class RylogicCore : RylogicAssembly
{
	public RylogicCore(string workspace, List<string>? platforms = null, List<string>? configs = null)
		:base("Rylogic.Core", ["net9.0", "net9.0-windows", "net481"], workspace, platforms, configs)
	{}
	protected override IEnumerable<Nuget.File> NugetFiles()
	{
		// Rylogic.Core isn't depended on windows
		foreach (var fw in (string[])["net9.0", "net481"])
			yield return new Nuget.File(Tools.Path([ProjDir, $"bin\\Release\\{fw}\\Rylogic.Core.dll"]), $"lib\\{fw}\\", true);
	}
}

// LDraw
class LDraw : Managed
{
	public string DeployDir = string.Empty;
	public string? MsiPath = null;

	public LDraw(string workspace, List<string>? platforms = null, List<string>? configs = null)
		:base("LDraw", "/projects/apps/LDraw", ["net9.0-windows"], workspace, ["x64"], configs)
	{
		DeployDir = Tools.Path([UserVars.Root, "bin/LDraw"], check_exists: false);
	}

	public override void Build()
	{
		DotNetRestore(RylogicSln);
		Tools.MSBuild(RylogicSln, projects: [$"Apps\\LDraw\\{ProjName}"], platforms: Platforms, configs: Configs);
	}

	public override void Deploy()
	{
		// Check versions
		var proj_file = Tools.Path([ProjDir, "LDraw.csproj"]);
		var version = Tools.Extract(proj_file, new Regex("<Version>(.*)</Version>")).Groups[1].Value;
		Console.WriteLine($"Deploying LDraw Version: {version}\n");

		// Ensure output directories exist and are empty
		Tools.CleanDir(DeployDir);

		var file_list = (List<string>)[
			"LDraw.exe",
			"dxcompiler.dll",
			"dxil.dll",
			"ICSharpCode.AvalonEdit.dll",
			"LDraw.dll",
			"Rylogic.Core.dll",
			"Rylogic.Gfx.dll",
			"Rylogic.Gui.WPF.dll",
			"Rylogic.Windows.dll",
			"LDraw.runtimeconfig.json",
		];
		var dir_list = (List<string>)[
			"lib",
		];

		// Copy build products to the output directory
		Console.WriteLine($"Deploying files to '{DeployDir}\\':\n");
		var target_dir = Tools.Path([ProjDir, "bin/Release", Frameworks[0]]);
		foreach (var file in file_list)
			Tools.Copy(Tools.Path([target_dir, file]), DeployDir, full_paths: false, indent: "    ");
		foreach (var dir in dir_list)
			Tools.Copy(Tools.Path([target_dir, dir]), Tools.Path([DeployDir, dir], check_exists: false), full_paths: false, indent: "    ");

		// Build the installer
		Console.WriteLine("Building installer...\n");
		var installer_wxs = Tools.Path([ProjDir, "installer", "installer.wxs"]);
		MsiPath = BuildInstaller.Build("LDraw", version, installer_wxs, ProjDir, target_dir, Tools.Path([DeployDir, ".."]),
			[
				new HarvestPath("binaries", "INSTALLFOLDER", ".", false, [new Regex(@".*\.dll"), new Regex(@"LDraw\.runtimeconfig\.json")]),
				new HarvestPath("lib_files", "lib", "lib", true),
			]);
		Console.WriteLine($"{MsiPath} created.\n");
	}

	public override void Publish()
	{
		//if (MsiPath is null)
		//	Deploy();

		//Console.WriteLine("\nPublishing to web site...");
		//Tools.Copy(MsiPath, Tools.Path([UserVars.WWWRoot, "files/ldraw", check_exists=False))
	}
}

// Build script main function
void Main(IList<string> args)
{
	// Set defaults for command line options
	string workspace = UserVars.Root;
	List<EProjects> projects = [];
	List<string>? platforms = null;
	List<string>? configs = null;
	bool clean = false;
	bool build = false;
	bool deploy = false;
	bool publish = false;

	bool IsDataArg(int i) => i != args.Count && args[i].StartsWith('-') == false;

	// Parse command line
	for (int i = 0; i != args.Count;)
	{
		var arg = args[i++].ToLowerInvariant();
		switch (arg)
		{
			case "-clean":
				{
					clean = true;
					break;
				}
			case "-build":
				{
					build = true;
					break;
				}
			case "-nobuild":
				{
					build = false;
					break;
				}
			case "-rebuild":
				{
					clean = true;
					build = true;
					break;
				}
			case "-deploy":
				{
					deploy = true;
					break;
				}
			case "-publish":
				{
					publish = true;
					break;
				}
			case "-cert_pw":
				{
					if (!IsDataArg(i)) throw new Exception("Rylogic code signing certificate password expected");
					UserVars.CodeSignCert_Pw = args[i++];
					break;
				}
			case "-workspace":
				{
					if (!IsDataArg(i)) throw new Exception("Workspace argument missing");
					workspace = args[i++];
					break;
				}
			case "-project":
			case "-projects":
				{
					if (!IsDataArg(i))
					{
						Console.WriteLine(string.Join("\n", Enum<EProjects>.Values));
						return;
					}
					for (; IsDataArg(i);)
					{
						projects.Add(Enum<EProjects>.Parse(args[i++].Replace(".","")));
					}
					break;
				}
			case "-platform":
			case "-platforms":
				{
					if (!IsDataArg(i)) throw new Exception("Platform argument missing");
					platforms ??= [];
					for (; IsDataArg(i);)
					{
						var platform = args[i++];
						if (platform.ToLowerInvariant() == "x64") platform = "x64";
						if (platform.ToLowerInvariant() == "x86") platform = "x86";
						if (platform.ToLowerInvariant() == "any cpu") platform = "Any CPU";
						if (platform.ToLowerInvariant() == "anycpu") platform = "Any CPU";
						platforms.Add(platform);
					}
					break;
				}
			case "-config":
			case "-configs":
				{
					if (!IsDataArg(i)) throw new Exception("Config argument missing");
					configs ??= [];
					for (; IsDataArg(i); )
					{
						var config = args[i++];
						if (config.ToLowerInvariant() == "release") config = "Release";
						if (config.ToLowerInvariant() == "debug") config = "Debug";
						configs.Add(config);
					}
					break;
				}
			default:
				{
					throw new Exception($"Unknown command line argument: {args[i-1]}");
				}
		}
	}

	// Normalise parameters
	if (projects.Count == 0) projects.Add(EProjects.All);
	build |= !clean && !build && !deploy && !publish;
	deploy |= publish;

	// Build/Clean/Deploy each given project
	foreach (var project in projects)
	{
		var builder_type = Type.GetType($"Submission#0+{project}");
		if (builder_type == null)
			throw new Exception($"Builder type '{project}' not found");

		var builder = (Common?)Activator.CreateInstance(builder_type, workspace, platforms, configs)
			?? throw new Exception($"Failed to create the builder type foe {project}");

		// Prompt for the cert password if signing is needed
		if ((deploy || publish) && builder.RequiresSigning)
			UserVars.CodeSignCert_Pw = UserVars.CodeSignCert_Pw; // Prompt if needed

		// Clean if '-clean' is used
		if (clean)
			builder.Clean();

		// If no project name is given build them all
		if (build)
			builder.Build();

		// Deploy the named project(s)
		if (deploy)
			builder.Deploy();

		// Publish the named project(s)
		if (publish)
			builder.Publish();
	}

	Console.WriteLine($"\nComplete: {workspace}");
	return;
}


// Testing
List<string> args =
	//Environment.GetCommandLineArgs().Skip(2).ToList();
	//["-project", "LDraw", "-deploy"];
	["-project", "Rylogic.Core", "-build", "-deploy"];

if (!args.SequenceEqual(Environment.GetCommandLineArgs().Skip(2)))
	Console.WriteLine("WARNING: Command line overridden for testing");

Main(args);
