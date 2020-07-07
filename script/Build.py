#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Notes:
#  - This is a "living" document. I.e. I'm too lazy to add all the projects now.
#    Just add them on demand.
#  - If you get an error saying 'UserVars not found'. Run py .\script\Setup.py
#    to generate a suitable UserVars.py file for the current system and working
#    directory location
#  - Deploy means copy the Release products to a staging area or location on the
#    local PC.
#  - Publish means copy installers/zips/etc to locations for other people to
#    download and use.

import sys, os, subprocess, datetime, re, shutil
from importlib import machinery as Import
import Rylogic as Tools
import RylogicAssembly as RA
import BuildInstaller
import BuildDocs
import BuildInstaller
import UserVars

restored = []

# Available projects that can be built
class EProjects():
	Sqlite3 = "Sqlite3"
	Scintilla = "Scintilla"
	View3d = "View3d"
	Audio = "Audio"
	P3d = "P3d"
	Rylogic = "Rylogic"
	RylogicCore = "Rylogic.Core"
	RylogicCoreWindows = "Rylogic.Core.Windows"
	RylogicScintilla = "Rylogic.Scintilla"
	RylogicView3d = "Rylogic.View3d"
	RylogicGuiWinForms = "Rylogic.Gui.WinForms"
	RylogicGuiWPF = "Rylogic.Gui.WPF"
	CSex = "CSex"
	LDraw = "LDraw"
	AllNative = "AllNative"
	AllManaged = "AllManaged"
	All = "All"

	# Print all available projects
	@staticmethod
	def ListProjects():
		for elem in filter(lambda x: not x.startswith('__'), vars(EProjects)):
			print(elem)
		return

# Check that the given path exists
def CheckPath(path:str):
	if path and not os.path.exists(path): raise FileNotFoundError(f"Path {path} does not exist")
	return path

# Ensure the directory 'dir' exists and is empty
def CleanDir(dir:str):
	print(f"Cleaning deploy directory: {dir}")
	Tools.ShellDelete(dir)
	os.makedirs(dir)
	return

# Ensure the directory 'dir' exists and is empty
def CleanObj(dir:str, platforms:[str]=None, configs:[str]=None):
	if not platforms and not configs:
		CleanDir(dir)
	else:
		platforms = platforms if platforms else ["x64", "x86"]
		configs = configs if configs else ["Release", "Debug"]
		for p in platforms:
			for c in configs:
				d = os.path.join(dir, p, c)
				if not os.path.exists(d): continue
				CleanDir(d)
	return

# Clean the 'bin' and 'obj' directory of a dot net project
def CleanDotNet(proj_dir:str, platforms:[str], configs:[str]):
	if not platforms and not configs:
		CleanDir(os.path.join(proj_dir, "obj"))
		CleanDir(os.path.join(proj_dir, "bin"))
	else:
		CleanDir(os.path.join(proj_dir, "obj"))
		CleanDir(os.path.join(proj_dir, "bin"))
		#todo
	return

# Invoke MSBuild on the given solution or project file
def MSBuild(name:str, sln:str, projects:[str], platforms:[str], configs:[str], props:str=None):
	print(f"\nBuilding {name}")
	Tools.SetupVcEnvironment()
	if Tools.MSBuild(sln, projects, platforms, configs, props): return
	raise RuntimeError(f"Building {name} failed")

# Restore nuget packages
def DotNetRestore(sln_or_proj:str):
	if sln_or_proj in restored: return
	Tools.SetupVcEnvironment()
	print(f"Nuget restore: {sln_or_proj}")
	Tools.Exec([UserVars.msbuild, sln_or_proj, "/t:restore", "/verbosity:minimal", "/nologo"])
	#Tools.Exec([UserVars.dotnet, "restore", sln_or_proj, "--verbosity", "quiet"])
	#Tools.Exec([UserVars.nuget, "restore", sln_or_proj, "-Verbosity", "quiet"])
	restored.append(sln_or_proj)
	return

# Sign an assembly
def SignAssembly(target):
	if not os.path.exists(target): return
	Tools.SignAssembly(target)
	return

# Base for all builders
class Common():
	def __init__(self, workspace:str):
		self.workspace = workspace
		self.requires_signing = False
		return

	def Clean(self):
		return

	def Build(self):
		return

	def Deploy(self):
		return

	def Publish(self):
		return

# Native binary (base)
class Native(Common):
	def __init__(self, proj_name:str, workspace:str, platforms:[str], configs:[str]):
		Common.__init__(self, workspace)
		self.proj_name = proj_name
		self.proj_dir = os.path.join(workspace, "projects", self.proj_name)
		self.rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
		self.platforms = platforms if platforms else ["x64", "x86"]
		self.configs = configs if configs else ["Release", "Debug"]
		self.obj_dir = os.path.join(workspace, "obj", UserVars.platform_toolset)
		return

	def Clean(self):
		CleanObj(self.obj_dir, self.platforms, self.configs)
		return

# Native SDK dll (base)
class NativeSDKDll(Native):
	def __init__(self, proj_name:str, workspace:str, platforms:[str], configs:[str]):
		Native.__init__(self, proj_name, workspace, platforms, configs)
		self.proj_dir = os.path.join(workspace, "sdk", self.proj_name)
		return

	def Build(self):
		MSBuild(self.proj_name, self.rylogic_sln, [f"SDK\\{self.proj_name}"], self.platforms, self.configs)
		return

# Sqlite3 native dll
class Sqlite3(NativeSDKDll):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		NativeSDKDll.__init__(self, "sqlite3", workspace, platforms, configs)
		return

# Scintilla native dll
class Scintilla(NativeSDKDll):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		NativeSDKDll.__init__(self, "scintilla", workspace, platforms, configs)
		return

# Audio native dll
class Audio(Native):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Native.__init__(self, "audio", workspace, platforms, configs)
		return

	def Clean(self):
		CleanObj(os.path.join(self.obj_dir, "audio"), self.platforms, self.configs)
		CleanObj(os.path.join(self.obj_dir, "audio.dll"), self.platforms, self.configs)
		return

	def Build(self):
		MSBuild("Audio", self.rylogic_sln, ["Rylogic\\audio"], self.platforms, self.configs)
		MSBuild("Audio.dll", self.rylogic_sln, ["Rylogic\\audio.dll"], self.platforms, self.configs)
		return

# View3d native dll
class View3d(Native):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Native.__init__(self, "audio", workspace, platforms, configs)
		return

	def Clean(self):
		CleanObj(os.path.join(self.obj_dir, "view3d"), self.platforms, self.configs)
		CleanObj(os.path.join(self.obj_dir, "view3d.dll"), self.platforms, self.configs)
		return

	def Build(self):
		MSBuild("View3d", self.rylogic_sln, ["Rylogic\\view3d"], self.platforms, self.configs)
		MSBuild("View3d.dll", self.rylogic_sln, ["Rylogic\\view3d.dll"], self.platforms, self.configs)
		return

# P3d
class P3d(Native):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Native.__init__(self, "p3d", workspace, platforms, configs)
		return

	def Build(self):
		MSBuild(self.proj_name, self.rylogic_sln, [f"Tools\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		if not "Release" in self.configs or not "x64" in self.platforms: return
		deploy_dir = os.path.join(self.workspace, "bin", "P3d")
		target = os.path.join(self.obj_dir, self.proj_name, "x64", "Release", "p3d.exe")
		CleanDir(deploy_dir)
		Tools.Copy(target, os.path.join(deploy_dir,""))
		return

# .NET assembly (base)
class Managed(Common):
	def __init__(self, proj_name:str, frameworks:[str], workspace:str, platforms:[str], configs:[str]):
		Common.__init__(self, workspace)
		self.proj_name = proj_name
		self.frameworks = frameworks
		self.platforms = platforms if platforms else ["Any CPU"]
		self.configs = configs if configs else ["Release", "Debug"]
		self.proj_dir = os.path.join(workspace, "projects", self.proj_name)
		self.rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
		self.requires_signing = True
		return

	def Clean(self):
		CleanDotNet(self.proj_dir, self.platforms, self.configs)
		return

# Rylogic .NET assembly (base)
class RylogicAssembly(Managed):
	def __init__(self, proj_name:str, frameworks:[str], workspace:str, platforms:[str], configs:[str]):
		Managed.__init__(self, proj_name, frameworks, workspace, platforms, configs)
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Rylogic\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		proj = os.path.join(self.proj_dir, f"{self.proj_name}.csproj")
		output_dir = os.path.join(self.proj_dir, "bin", "Release")
		for fw in self.frameworks:
			SignAssembly(os.path.join(output_dir, fw, f"{self.proj_name}.dll"))
		self.nupkg = Tools.NugetPackage(proj)
		return

	def Publish(self):
		if not self.nupkg or not os.path.exists(self.nupkg): raise RuntimeError("Call Deploy before calling Publish")
		Tools.NugetPublish(self.nupkg)
		return

# Rylogic.Core .NET assembly
class RylogicCore(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.Core", ["netstandard2.0", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Rylogic.Core.Windows .NET assembly
class RylogicCoreWindows(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.Core.Windows", ["net472", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Rylogic.Net .NET assembly
class RylogicNet(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.Net", ["net472", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Rylogic.Scintilla .NET assembly
class RylogicScintilla(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.Scintilla", ["net472", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Rylogic.View3d .NET assembly
class RylogicView3d(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.View3d", ["net472", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Rylogic.Gui.WinForms .NET assembly
class RylogicGuiWinForms(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.Gui.WinForms", ["net472", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Rylogic.Gui.WPF .NET assembly
class RylogicGuiWPF(RylogicAssembly):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		RylogicAssembly.__init__(self, "Rylogic.Gui.WPF", ["net472", "netcoreapp3.1"], workspace, platforms, configs)
		return

# Csex
class Csex(Managed):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Managed.__init__(self, "CSex", ["net472"], workspace, platforms, configs)
		self.requires_signing = False
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild("CSex", self.rylogic_sln, ["Tools\\Csex"], self.platforms, self.configs)
		return

# LDraw
class LDraw(Managed):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Managed.__init__(self, "LDraw", ["net472"], workspace, platforms, configs)
		self.platforms = ["x64"]
		self.proj_dir = os.path.join(workspace, "projects", "LDraw", self.proj_name)
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"LDraw\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		# Check versions
		version = Tools.Extract(os.path.join(self.proj_dir, "LDraw.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying LDraw Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = os.path.join(UserVars.root, "bin", "LDraw")
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = os.path.join(self.proj_dir, "bin", "Release", self.frameworks[0])
		Tools.Copy(os.path.join(target_dir, "LDraw.exe"                 ), os.path.join(self.bin_dir, ""))
		Tools.Copy(os.path.join(target_dir, "Rylogic.Core.dll"          ), os.path.join(self.bin_dir, ""))
		Tools.Copy(os.path.join(target_dir, "Rylogic.Core.Windows.dll"  ), os.path.join(self.bin_dir, ""))
		Tools.Copy(os.path.join(target_dir, "Rylogic.Gui.WPF.dll"       ), os.path.join(self.bin_dir, ""))
		Tools.Copy(os.path.join(target_dir, "Rylogic.View3d.dll"        ), os.path.join(self.bin_dir, ""))
		Tools.Copy(os.path.join(target_dir, "ICSharpCode.AvalonEdit.dll"), os.path.join(self.bin_dir, ""))
		Tools.Copy(os.path.join(target_dir, "lib"                       ), os.path.join(self.bin_dir, "lib"))

		# Build the installer
		print("Building installer...\n")
		self.installer_wxs = os.path.join(self.proj_dir, "installer", "installer.wxs")
		self.msi = BuildInstaller.Build("LDraw", version, self.installer_wxs, self.proj_dir, target_dir,
			os.path.join(self.bin_dir, ".."),
			[
				["binaries", "INSTALLFOLDER", ".", False,
					r"LDraw\..*\.dll",
					r"Rylogic\..*\.dll",
					r"ICSharpCode.AvalonEdit.dll",
				],
				["lib_files", "lib", "lib", True],
			])
		print(f"{self.msi} created.\n")
		return
	
	def Publish(self):
		if not hasattr(self, "msi") or not os.path.exists(self.msi): raise RuntimeError("Call Deploy before Publish")
		print("\nPublishing to web site...")
		Tools.Copy(self.msi, os.path.join(UserVars.wwwroot, "files", "ldraw", ""))
		return

# Rylogic.TextAligner
class RylogicTextAligner(Managed):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Managed.__init__(self, "Rylogic.TextAligner", ["net472"], workspace, platforms, configs)
		self.manifest  = os.path.join(self.proj_dir, "source.extension.vsixmanifest")
		self.vsix_name = "Rylogic.TextAligner.vsix"
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"VSExtensions\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):

		# Check the manifest version
		self.vsix_id = "DF402917-6013-40CA-A4C6-E1640DA86B90"
		version_regex = f'Id="{self.vsix_id}" Version="(?P<version>.*?)"'
		match_version = Tools.Extract(self.manifest, version_regex)
		if not match_version: raise RuntimeError("Failed to extract version number from:\r\n " + self.manifest)
		self.version = match_version.group("version")
		print(f"Deploying {self.proj_name} Version: {self.version}\n")

		# Check version is greater than the last released version (update this after a release)
		min_released_version = "1.09.0"
		if self.version <= min_released_version:
			raise RuntimeError("Version number needs bumping")

		# Check the assembly info
		assinfo = os.path.join(self.proj_dir, "Properties", "AssemblyInfo.cs")
		ass_version = Tools.Extract(assinfo, r"AssemblyVersion\(\"(?P<vers>.*?)\"\)")
		ass_filevers = Tools.Extract(assinfo, r"AssemblyFileVersion\(\"(?P<vers>.*?)\"\)")
		if not ass_version or ass_version.group("vers") != self.version:
			raise RuntimeError(f"AssemblyVersion has not been updated to {version}")
		if not ass_filevers or ass_filevers.group("vers") != self.version:
			raise RuntimeError(f"AssemblyFileVersion has not been updated to {version}")

		# Ensure the ouptut directory exists
		self.bin_dir = os.path.join(UserVars.root, "bin")
		self.bin_path = Tools.ChgExtn(os.path.join(self.bin_dir, self.vsix_name), f".v{self.version}.vsix")

		# Copy build products to the output directory
		print(f"Copying files to: {self.bin_dir}")
		target_path = os.path.join(self.proj_dir, "bin", "Release", self.vsix_name)
		Tools.Copy(target_path, self.bin_path)

		# Sign the vsix
		Tools.SignVsix(self.bin_path, "sha1")
		return

	def Publish(self):
		if not hasattr(self, "bin_path") or not os.path.exists(self.bin_path): raise RuntimeError("Call Deploy before Publish")
		print("\nPublishing...")

		# Copy to www
		# Can't download vsix file, so zip first
		zip_path = Tools.ZipFile(self.bin_path)
		print("\nPublishing to web site...")
		Tools.Copy(zip_path, os.path.join(UserVars.wwwroot, "files", "rylogic", ""))

		# Ask to install
		print("\nInstall locally...")
		install = input(f"Install {self.bin_path} (y/n)? ")
		if install == 'y':
			try:
				print("Uninstalling previous versions...")
				vsix_installer = CheckPath(os.path.join(UserVars.vs_dir, "Common7", "IDE", "VSIXInstaller.exe"))
				Tools.Exec(["cmd", "/C", vsix_installer, "/a", f"/u:{self.vsix_id}"])
			except Exception as ex:
				print(f"Uninstall failed: {str(ex)}")
			try:
				print("Installing latest version...")
				Tools.Exec(["cmd", "/C", self.bin_path])
			except Exception as ex:
				print(f"Install failed: {str(ex)}")

		# Uploading to marketplace.visualstudio.com is a manual
		# step... you don't want to bugger it up.
		return

# Groups of projects
class Group(Common):
	def __init__(self, workspace:str):
		Common.__init__(self, workspace)
		self.items = []
		return

	def Clean(self):
		for item in self.items: item.Clean()
		return

	def Build(self):
		for item in self.items: item.Build()
		return

	def Deploy(self):
		for item in self.items: item.Deploy()
		return

	def Publish(self):
		for item in self.items: item.Publish()
		return

# Rylogic .NET assemblies
class Rylogic(Group):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Group.__init__(self, workspace)
		self.requires_signing = True
		self.items = [
			RylogicCore       (workspace, platforms, configs),
			RylogicCoreWindows(workspace, platforms, configs),
			RylogicNet        (workspace, platforms, configs),
			RylogicScintilla  (workspace, platforms, configs),
			RylogicView3d     (workspace, platforms, configs),
			RylogicGuiWinForms(workspace, platforms, configs),
			RylogicGuiWPF     (workspace, platforms, configs),
		]
		return

# Build the native projects
class AllNative(Group):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Group.__init__(self, workspace)
		self.items = [
			Sqlite3  (workspace, platforms, configs),
			Scintilla(workspace, platforms, configs),
			View3d   (workspace, platforms, configs),
			Audio    (workspace, platforms, configs),
			P3d      (workspace, platforms, configs),
		]
		return

# Build the managed projects
class AllManaged(Group):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Group.__init__(self, workspace)
		self.items = [
			Rylogic  (workspace, platforms, configs),
			Csex     (workspace, platforms, configs),
			LDraw    (workspace, platforms, configs),
		]
		return

# Build all Software projects
class All(Group):
	def __init__(self, workspace:str, platforms:[str], configs:[str]):
		Group.__init__(self, workspace)
		self.items = [
			Sqlite3  (workspace, platforms, configs),
			Scintilla(workspace, platforms, configs),
			Audio    (workspace, platforms, configs),
			View3d   (workspace, platforms, configs),
			P3d      (workspace, platforms, configs),
			Rylogic  (workspace, platforms, configs),
			Csex     (workspace, platforms, configs),
			LDraw    (workspace, platforms, configs),
		]
		return

# Main
def Main(args:[str]):

	# Set defaults for command line options
	# Get the current workspace directory from the path of this file
	workspace = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
	projects = []
	platforms = []
	configs = []
	clean = False
	build = False
	deploy = False
	publish = False

	# Parse command line
	i = 1
	while i < len(args):
		arg = args[i].lower()
		if False:
			pass
		elif arg == "-clean":
			i = i + 1
			clean = True
		elif arg == "-build":
			i = i + 1
			build = True
		elif arg == "-deploy":
			i = i + 1
			deploy = True
		elif arg == "-publish":
			i = i + 1
			publish = True
		elif arg == "-cert_pw":
			i = i + 1
			if i == len(args) or args[i].startswith('-'):
				raise RuntimeError("Rylogic code signing certificate password expected")
			else:
				UserVars.rylogic_cert_pw = args[i]
				i = i + 1
		elif arg == "-workspace":
			i = i + 1
			if i == len(args) or args[i].startswith('-'):
				raise RuntimeError("Workspace argument missing")
			else:
				workspace = args[i]
				i = i + 1
		elif arg == "-project" or arg == "-projects":
			i = i + 1
			if i == len(args) or args[i].startswith('-'):
				EProjects.ListProjects()
				return
			while i != len(args) and not args[i].startswith('-'):
				projects.append(args[i])
				i = i + 1
		elif arg == "-platform" or arg == "-platforms":
			i = i + 1
			if i == len(args) or args[i].startswith('-'):
				raise RuntimeError("Platform argument missing")
			while i != len(args) and not args[i].startswith('-'):
				platforms.append(args[i])
				i = i + 1
		elif arg == "-config" or arg == "-configs":
			i = i + 1
			if i == len(args) or args[i].startswith('-'):
				raise RuntimeError("Config argument missing")
			while i != len(args) and not args[i].startswith('-'):
				configs.append(args[i])
				i = i + 1
		else:
			raise RuntimeError(f"Unknown command line argument: {args[i]}")

	# Normalise parameters
	for i in range(0, len(platforms)):
		if platforms[i].lower() == "x64": platforms[i] = "x64"
		if platforms[i].lower() == "x86": platforms[i] = "x86"
		if platforms[i].lower() == "any cpu" or platforms[i].lower() == "anycpu": platforms[i] = "Any CPU"
	for i in range(0, len(configs)):
		if configs[i].lower() == "release": configs[i] = "Release"
		if configs[i].lower() == "debug": configs[i] = "Debug"
	if not projects: projects = ["All"]
	if not clean and not build and not deploy and not publish: build = True
	if publish: deploy = True

	# Build/Clean/Deploy each given project
	for project in projects:
		name = project.replace('.','')
		builder = eval(name)(workspace, platforms, configs)

		# Prompt for the cert password if signing is needed
		if (deploy or publish) and hasattr(builder, "requires_signing") and builder.requires_signing and not UserVars.rylogic_cert_pw:
			Tools.PromptCertPassword()

		# Clean if '-clean' is used
		if clean:
			builder.Clean()

		# If no project name is given build them all
		if build:
			builder.Build()

		# Deploy the named project(s)
		if deploy:
			builder.Deploy()

		# Publish the named project(s)
		if publish:
			builder.Publish()

	print(f"\nComplete: {workspace}")
	return

# Entry Point
if __name__ == "__main__":
	try:
		# Examples:
		#sys.argv=['build.py', '-project', 'Rylogic.Core', '-platforms', 'x64', 'x86', '-configs', 'release', 'debug']
		#sys.argv=['build.py', '-project', 'Rylogic.Core', 'Rylogic.Core.Windows', '-deploy']
		#sys.argv=['build.py', '-projects', 'Rylogic.TextAligner', '-build', '-deploy', '-publish']
		#sys.argv=['build.py', '-projects', 'LDraw', '-configs', 'Release', '-deploy']
		#sys.argv=['build.py', '-projects', 'Rylogic', '-clean', '-build', '-deploy']
		#print(f"Command Line: {str(sys.argv)}")
		Main(sys.argv)

	except Exception as ex:
		print(f"ERROR: {str(ex)}")
		sys.exit(-1)

