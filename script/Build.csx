#! "net9.0"
// Build script
#r "System.IO"
#r "System.Text.Json"
#r "nuget: Rylogic.Core, 1.0.4"
#load "UserVars.csx"
#load "Tools.csx"
#nullable enable

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Console = Internal.Console;

Main(Environment.GetCommandLineArgs().Skip(1).ToList());

// Build script main function
void Main(IList<string> args)
{
	Console.WriteLine(string.Join(" ", args));
}

// Base class for all builders
public class Common
{
	protected string m_workspace;
	protected bool m_requires_signing;

	public Common(string workspace)
	{
		m_workspace = workspace;
		m_requires_signing = false;
	}
	public virtual void Clean() { }
	public virtual void Build() { }
	public virtual void Deploy() { }
	public virtual void Publish() { }

	// Ensure the directory 'dir' exists and is empty
	public static void CleanDir(string dir)
	{
		//Log.Message(f"Cleaning deploy directory: {dir}")
		Directory.Delete(dir, true);
		Directory.CreateDirectory(dir);
	}
}


// Base class for .NET projects
public class Managed : Common
{
	protected string m_proj_name;
	protected string m_proj_dir;
	protected string m_rylogic_sln;
	protected IList<string> m_frameworks;
	protected IList<string> m_platforms;
	protected IList<string> m_configs;

	public Managed(string proj_name, IList<string> frameworks, string workspace, IList<string>? platforms, IList<string>? configs)
		: base(workspace)
	{
		m_proj_name = proj_name;
		m_proj_dir = UserVars.Path([workspace, "projects", m_proj_name]);
		m_rylogic_sln = UserVars.Path([workspace, "rylogic.sln"]);
		m_frameworks = frameworks;
		m_platforms = platforms ?? ["Any CPU"];
		m_configs = configs ?? ["Release", "Debug"];
		m_requires_signing = true;
	}
	public override void Clean()
	{
		CleanDotNet(m_proj_dir, m_platforms, m_configs);
	}

	// Clean the 'bin' and 'obj' directory of a dot net project
	public static void CleanDotNet(string proj_dir, IList<string>? platforms = null, IList<string>? configs = null)
	{
		CleanDir(UserVars.Path([proj_dir, "obj"], check_exists: false));
		CleanDir(UserVars.Path([proj_dir, "bin"], check_exists: false));
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

		//Log.Message(f"Nuget restore: {sln_or_proj}")
		Tools.Run([UserVars.MSBuild, sln_or_proj, "/t:restore", "/verbosity:minimal", "/nologo"]);
		//Tools.Exec([UserVars.dotnet, "restore", sln_or_proj, "--verbosity", "quiet"])
		//Tools.Exec([UserVars.nuget, "restore", sln_or_proj, "-Verbosity", "quiet"])
		m_restored.Add(sln_or_proj);
	}
	private static List<string> m_restored = [];
}

// LDraw
class LDraw : Managed
{
	public LDraw(string workspace, IList<string>? platforms = null, IList<string>? configs = null)
		:base("LDraw", ["net9.0-windows"], workspace, platforms, configs)
	{
		m_proj_dir = UserVars.Path([workspace, "projects/apps", m_proj_name]);
		m_platforms = ["x64"];
	}
	public override void Build()
	{
		DotNetRestore(m_rylogic_sln);
		Tools.MSBuild(m_rylogic_sln, projects: [$"Apps\\{m_proj_name}"], platforms: m_platforms, configs: m_configs);
	}

#if false
	def Deploy(self):
		// Check versions
		version = Tools.Extract(Tools.Path(m_proj_dir, "LDraw.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying LDraw Version: {version}\n")

		// Ensure output directories exist and are empty
		m_bin_dir = Tools.Path(UserVars.root, "bin/LDraw", check_exists=False)
		CleanDir(m_bin_dir)

		// Copy build products to the output directory
		print(f"Copying files to {m_bin_dir}...\n")
		target_dir = Tools.Path(m_proj_dir, "bin/Release", m_frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "LDraw.exe"                 ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "LDraw.dll"                 ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "LDraw.runtimeconfig.json"  ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"          ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"  ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"       ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.View3d.dll"        ), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "ICSharpCode.AvalonEdit.dll"), m_bin_dir)
		Tools.Copy(Tools.Path(target_dir, "lib"                       ), Tools.Path(m_bin_dir, "lib", check_exists=False))

		// Build the installer
		print("Building installer...\n")
		m_installer_wxs = Tools.Path(m_proj_dir, "installer", "installer.wxs")
		m_msi = BuildInstaller.Build("LDraw", version, m_installer_wxs, m_proj_dir, target_dir,
			Tools.Path(m_bin_dir, ".."),
			[
				["binaries", "INSTALLFOLDER", ".", False,
					r".*\.dll",
					r"LDraw.runtimeconfig.json"],
				["lib_files", "lib", "lib", True],
			])
		print(f"{m_msi} created.\n")
		return
	
	def Publish(self):
		if not hasattr(self, "msi") or not os.path.exists(m_msi): raise RuntimeError("Call Deploy before Publish")
		print("\nPublishing to web site...")
		Tools.Copy(m_msi, Tools.Path(UserVars.wwwroot, "files/ldraw", check_exists=False))
		return
		#endif
}