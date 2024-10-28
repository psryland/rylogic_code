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
from typing import List
from enum import Enum
import Rylogic as Tools
import BuildInstaller
import BuildDocs
import BuildInstaller
import UserVars

restored = []

# Available projects that can be built
class EProjects(Enum):
	Sqlite3 = "Sqlite3"
	Scintilla = "Scintilla"
	Audio = "Audio"
	View3d = "View3d"
	P3d = "P3d"
	Rylogic = "Rylogic"
	RylogicCore = "Rylogic.Core"
	RylogicCoreWindows = "Rylogic.Core.Windows"
	RylogicScintilla = "Rylogic.Scintilla"
	RylogicView3d = "Rylogic.View3d"
	RylogicGuiWinForms = "Rylogic.Gui.WinForms"
	RylogicGuiWPF = "Rylogic.Gui.WPF"
	Csex = "Csex"
	LDraw = "LDraw"
	RyLogViewer = "RyLogViewer"
	CoinFlip = "CoinFlip"
	SolarHotWater = "SolarHotWater"
	TimeTracker = "TimeTracker"
	RylogicTextAligner = "Rylogic.TextAligner"
	Fishomatic = "Fishomatic"
	AllNative = "AllNative"
	AllManaged = "AllManaged"
	All = "All"

	# Print all available projects
	@staticmethod
	def ListProjects():
		for elem in filter(lambda x: not x.startswith('_') and x != 'ListProjects', vars(EProjects)):
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
def CleanObj(dir:str, platforms:List[str]=None, configs:List[str]=None):
	if not platforms and not configs:
		CleanDir(dir)
	else:
		platforms = platforms if platforms else ["x64", "x86"]
		configs = configs if configs else ["Release", "Debug"]
		for p in platforms:
			for c in configs:
				d = Tools.Path(dir, p, c, check_exists=False)
				if not os.path.exists(d): continue
				CleanDir(d)
	return

# Clean the 'bin' and 'obj' directory of a dot net project
def CleanDotNet(proj_dir:str, platforms:List[str], configs:List[str]):
	if not platforms and not configs:
		CleanDir(Tools.Path(proj_dir, "obj", check_exists=False))
		CleanDir(Tools.Path(proj_dir, "bin", check_exists=False))
	else:
		CleanDir(Tools.Path(proj_dir, "obj", check_exists=False))
		CleanDir(Tools.Path(proj_dir, "bin", check_exists=False))
		#todo
	return

# Invoke MSBuild on the given solution or project file
def MSBuild(name:str, sln:str, projects:List[str], platforms:List[str], configs:List[str], props:str=None):
	print(f"\nBuilding {name}")
	Tools.SetupVcEnvironment()
	if Tools.MSBuild(sln, projects, platforms, configs, props): return
	raise RuntimeError(f"Building {name} failed")

# Deploy lib and/or dll files to the '/pr/lib' folder
def DeployLib(target_name:str, obj_dir:str, platforms:List[str], configs:List[str]):
	# Notes:
	#  - Watch out for pdb files overwriting projects with the same name.
	#  - The MSBuild system creates the '$(TargetName).pdb' file even if the project file sets the pdb name to something else.
	#  - Debugging will only load '$(TargetName).pdb' so trying to use '$(TargetName)$(TargetExt).pdb' doesn't work (sadly).
	#  - The only option is to use separate names for lib and dll projects :(
	#  - Use <project>-static.lib.
	#  - Don't make $(TargetName) == $(ProjectName), just set the name explicitly.
	for p in platforms:
		for c in configs:
			target_dir = Tools.Path(obj_dir, p, c)
			target_files = [
				Tools.Path(target_dir, f"{target_name}.lib", check_exists=False),
				Tools.Path(target_dir, f"{target_name}.dll", check_exists=False),
				Tools.Path(target_dir, f"{target_name}.imp", check_exists=False),
				Tools.Path(target_dir, f"{target_name}.pdb", check_exists=False),
			]

			# Get the destination directory: /pr/lib/p/c/target_name.extn
			dst_dir = Tools.Path(UserVars.root, "lib", p, c, check_exists=False)
			os.makedirs(dst_dir, exist_ok=True)

			# Copy the target files to the destination directories
			for filepath in filter(os.path.exists, target_files):
				_,file = os.path.split(filepath)
				Tools.Copy(filepath, Tools.Path(dst_dir, file, check_exists=False))
	return

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

# Native binary (base)
class Native(Common):
	def __init__(self, proj_name:str, workspace:str, platforms:List[str], configs:List[str]):
		Common.__init__(self, workspace)
		self.proj_name = proj_name
		#self.proj_dir = Tools.Path(workspace, "projects", self.proj_name)
		self.platforms = platforms if platforms else ["x64", "x86"]
		self.configs = configs if configs else ["Release", "Debug"]
		self.rylogic_sln = Tools.Path(workspace, "build/rylogic.sln")
		self.obj_dir = Tools.Path(workspace, "obj", UserVars.platform_toolset, check_exists=False)
		os.makedirs(self.obj_dir, exist_ok=True)
		return

	def Clean(self):
		obj_dir = Tools.Path(self.obj_dir, self.proj_name, check_exists=False)
		CleanObj(obj_dir, self.platforms, self.configs)
		return

# Native SDK dll (base)
class NativeSDKDll(Native):
	def __init__(self, proj_name:str, workspace:str, platforms:List[str], configs:List[str]):
		Native.__init__(self, proj_name, workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "sdk", self.proj_name)
		self.obj_dir = Tools.Path(self.proj_dir, "obj", UserVars.platform_toolset, check_exists=False)
		os.makedirs(self.obj_dir, exist_ok=True)
		return

	def Build(self):
		MSBuild(self.proj_name, self.rylogic_sln, [f"SDK\\{self.proj_name}"], self.platforms, self.configs)
		return

# Sqlite3 native dll
class Sqlite3(NativeSDKDll):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		NativeSDKDll.__init__(self, "sqlite3", workspace, platforms, configs)
		return

# Scintilla native dll
class Scintilla(NativeSDKDll):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		NativeSDKDll.__init__(self, "scintilla", workspace, platforms, configs)
		return

# Audio native dll
class Audio(Native):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Native.__init__(self, "audio", workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, f"projects\\rylogic\\{self.proj_name}")
		return

	def Clean(self):
		CleanObj(Tools.Path(self.obj_dir, "audio", check_exists=False), self.platforms, self.configs)
		CleanObj(Tools.Path(self.obj_dir, "audio.dll", check_exists=False), self.platforms, self.configs)
		return

	def Build(self):
		MSBuild("audio", self.rylogic_sln, ["Rylogic\\audio"], self.platforms, self.configs)
		MSBuild("audio.dll", self.rylogic_sln, ["Rylogic\\audio.dll"], self.platforms, self.configs)
		return

	def Deploy(self):
		DeployLib(self.proj_name, Tools.Path(self.obj_dir, "audio"), self.platforms, self.configs)
		DeployLib(self.proj_name, Tools.Path(self.obj_dir, "audio.dll"), self.platforms, self.configs)
		return

# View3d native dll
class View3d(Native):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Native.__init__(self, "view3d", workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, f"projects\\rylogic\\{self.proj_name}")
		return

	def Clean(self):
		CleanObj(Tools.Path(self.obj_dir, "view3d", check_exists=False), self.platforms, self.configs)
		CleanObj(Tools.Path(self.obj_dir, "view3d.dll", check_exists=False), self.platforms, self.configs)
		return

	def Build(self):
		MSBuild("View3d", self.rylogic_sln, ["Rylogic\\view3d"], self.platforms, self.configs)
		MSBuild("View3d.dll", self.rylogic_sln, ["Rylogic\\view3d.dll"], self.platforms, self.configs)
		return

	def Deploy(self):
		DeployLib(self.proj_name, Tools.Path(self.obj_dir, "view3d"), self.platforms, self.configs)
		DeployLib(self.proj_name, Tools.Path(self.obj_dir, "view3d.dll"), self.platforms, self.configs)
		return

# P3d
class P3d(Native):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Native.__init__(self, "p3d", workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects/tools", self.proj_name)
		return

	def Build(self):
		MSBuild(self.proj_name, self.rylogic_sln, [f"Tools\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		if not "Release" in self.configs or not "x64" in self.platforms: return
		target = Tools.Path(self.proj_dir, "obj/x64/Release/p3d.exe")
		deploy_dir = Tools.Path(self.workspace, "bin/P3d", check_exists=False)
		CleanDir(deploy_dir)
		Tools.Copy(target, Tools.Path(deploy_dir,""))
		return

# .NET assembly (base)
class Managed(Common):
	def __init__(self, proj_name:str, frameworks:List[str], workspace:str, platforms:List[str], configs:List[str]):
		Common.__init__(self, workspace)
		self.proj_name = proj_name
		self.frameworks = frameworks
		self.platforms = platforms if platforms else ["Any CPU"]
		self.configs = configs if configs else ["Release", "Debug"]
		#self.proj_dir = Tools.Path(workspace, "projects", self.proj_name)
		self.rylogic_sln = Tools.Path(workspace, "build/rylogic.sln")
		self.requires_signing = True
		return

	def Clean(self):
		CleanDotNet(self.proj_dir, self.platforms, self.configs)
		return

# Rylogic .NET assembly (base)
class RylogicAssembly(Managed):
	def __init__(self, proj_name:str, frameworks:List[str], workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, proj_name, frameworks, workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\rylogic", self.proj_name)
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Rylogic\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		proj = Tools.Path(self.proj_dir, f"{self.proj_name}.csproj")
		output_dir = Tools.Path(self.proj_dir, "bin", "Release")
		if self.requires_signing:
			for fw in self.frameworks:
				SignAssembly(Tools.Path(output_dir, fw, f"{self.proj_name}.dll"))
			self.nupkg = Tools.NugetPackage(proj)
		return

	def Publish(self):
		if not self.nupkg or not os.path.exists(self.nupkg): raise RuntimeError("Call Deploy before calling Publish")
		Tools.NugetPublish(self.nupkg)
		return

# Rylogic.Core .NET assembly
class RylogicCore(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.Core", ["netstandard2.0"], workspace, platforms, configs)
		return

# Rylogic.Core.Windows .NET assembly
class RylogicCoreWindows(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.Core.Windows", ["net472", "net6.0-windows"], workspace, platforms, configs)
		return

# Rylogic.Net .NET assembly
class RylogicNet(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.Net", ["netstandard2.0"], workspace, platforms, configs)
		return

# Rylogic.Scintilla .NET assembly
class RylogicScintilla(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.Scintilla", ["net472", "net6.0-windows"], workspace, platforms, configs)
		return

# Rylogic.View3d .NET assembly
class RylogicView3d(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.View3d", ["net472", "net6.0-windows"], workspace, platforms, configs)
		return

# Rylogic.Gui.WinForms .NET assembly
class RylogicGuiWinForms(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.Gui.WinForms", ["net472"], workspace, platforms, configs)
		return

# Rylogic.Gui.WPF .NET assembly
class RylogicGuiWPF(RylogicAssembly):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		RylogicAssembly.__init__(self, "Rylogic.Gui.WPF", ["net472", "net6.0-windows"], workspace, platforms, configs)
		return

# Csex
class Csex(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "Csex", ["net6.0-windows"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\tools", self.proj_name)
		self.requires_signing = False
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild("Csex", self.rylogic_sln, ["Tools\\Csex"], self.platforms, self.configs)
		return

	def Deploy(self):
		version = Tools.Extract(Tools.Path(self.proj_dir, "Csex.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying Csex Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin", "Csex", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(self.proj_dir, "bin", "Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "Csex.exe"                  ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Csex.dll"                  ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Csex.runtimeconfig.json"   ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"          ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"  ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"       ), self.bin_dir)
		return

# LDraw
class LDraw(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "LDraw", ["net6.0-windows"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects/apps/LDraw", self.proj_name)
		self.platforms = ["x64"]
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\LDraw\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		# Check versions
		version = Tools.Extract(Tools.Path(self.proj_dir, "LDraw.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying LDraw Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin/LDraw", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(self.proj_dir, "bin/Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "LDraw.exe"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "LDraw.dll"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "LDraw.runtimeconfig.json"  ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"          ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"  ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"       ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.View3d.dll"        ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "ICSharpCode.AvalonEdit.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "lib"                       ), Tools.Path(self.bin_dir, "lib", check_exists=False))

		# Build the installer
		print("Building installer...\n")
		self.installer_wxs = Tools.Path(self.proj_dir, "installer", "installer.wxs")
		self.msi = BuildInstaller.Build("LDraw", version, self.installer_wxs, self.proj_dir, target_dir,
			Tools.Path(self.bin_dir, ".."),
			[
				["binaries", "INSTALLFOLDER", ".", False,
					r".*\.dll",
					r"LDraw.runtimeconfig.json"],
				["lib_files", "lib", "lib", True],
			])
		print(f"{self.msi} created.\n")
		return
	
	def Publish(self):
		if not hasattr(self, "msi") or not os.path.exists(self.msi): raise RuntimeError("Call Deploy before Publish")
		print("\nPublishing to web site...")
		Tools.Copy(self.msi, Tools.Path(UserVars.wwwroot, "files/ldraw", check_exists=False))
		return

# RylogViewer
class RyLogViewer(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "RyLogViewer", ["net6.0-windows"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\apps", self.proj_name)
		self.platforms = ["x64"]
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\RyLogViewer\\{self.proj_name}"], self.platforms, self.configs)
		return

	def Deploy(self):
		# Check versions
		version = Tools.Extract(Tools.Path(self.proj_dir, "RyLogViewer.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying RyLogViewer Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin", "RyLogViewer", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(self.proj_dir, "bin", "Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "RyLogViewer.UI.exe"               ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "RyLogViewer.UI.dll"               ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "RyLogViewer.UI.runtimeconfig.json"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"         ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"              ), self.bin_dir)

		# Build the installer
		#print("Building installer...\n")
		#self.installer_wxs = Tools.Path(self.proj_dir, "installer", "installer.wxs")
		#self.msi = BuildInstaller.Build("LDraw", version, self.installer_wxs, self.proj_dir, target_dir,
		#	Tools.Path(self.bin_dir, ".."),
		#	[
		#		["binaries", "INSTALLFOLDER", ".", False,
		#			r"LDraw\..*\.dll",
		#			r"Rylogic\..*\.dll",
		#			r"ICSharpCode.AvalonEdit.dll",
		#		],
		#		["lib_files", "lib", "lib", True],
		#	])
		#print(f"{self.msi} created.\n")
		return
	
	def Publish(self):
		if not hasattr(self, "msi") or not os.path.exists(self.msi): raise RuntimeError("Call Deploy before Publish")
		print("\nPublishing to web site...")
		Tools.Copy(self.msi, Tools.Path(UserVars.wwwroot, "files/rylogviewer", check_exists=False))
		return

# CoinFlip
class CoinFlip(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "CoinFlip", ["net6.0-windows"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\apps", self.proj_name)
		self.platforms = ["x64"]
		return

	def Clean(self):
		CleanDotNet(Tools.Path(self.proj_dir, "CoinFlip.Model", check_exists=False), self.platforms, self.configs)
		CleanDotNet(Tools.Path(self.proj_dir, "CoinFlip.UI", check_exists=False), self.platforms, self.configs)
		CleanDotNet(Tools.Path(self.proj_dir, "ExchApi.Common", check_exists=False), self.platforms, self.configs)
		CleanDotNet(Tools.Path(self.proj_dir, "ExchApi.Binance", check_exists=False), self.platforms, self.configs)
		CleanDotNet(Tools.Path(self.proj_dir, "Bot.Arbitrage", check_exists=False), self.platforms, self.configs)

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\CoinFlip\\ExchApi.Common"], self.platforms, self.configs)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\CoinFlip\\ExchApi.Binance"], self.platforms, self.configs)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\CoinFlip\\CoinFlip.Model"], self.platforms, self.configs)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\CoinFlip\\CoinFlip.UI"], self.platforms, self.configs)
		return

	def Deploy(self):
		# Check versions
		version = Tools.Extract(Tools.Path(self.proj_dir, "CoinFlip.UI\\CoinFlip.UI.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying CoinFlip Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin\\CoinFlip", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(self.proj_dir, "CoinFlip.UI\\bin\\Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "CoinFlip.UI.exe"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "CoinFlip.UI.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "CoinFlip.UI.runtimeconfig.json"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "CoinFlip.Model.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "ExchApi.Common.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "ExchApi.Binance.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Net.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.View3d.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "System.Data.SQLite.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Dapper.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Newtonsoft.Json.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "lib"), Tools.Path(self.bin_dir, "lib", check_exists=False))

		# Build the installer
		if False:
			print("Building installer...\n")
			self.installer_wxs = Tools.Path(self.proj_dir, "installer", "installer.wxs")
			self.msi = BuildInstaller.Build("LDraw", version, self.installer_wxs, self.proj_dir, target_dir,
				Tools.Path(self.bin_dir, ".."),
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

# Solar Hot Water
class SolarHotWater(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "SolarHotWater", ["net6.0-windows"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\apps", self.proj_name)
		self.platforms = ["x64"]
		return

	def Clean(self):
		CleanDotNet(Tools.Path(self.proj_dir, "SolarHotWater.Common", check_exists=False), self.platforms, self.configs)
		CleanDotNet(Tools.Path(self.proj_dir, "FroniusMonitor.Service", check_exists=False), self.platforms, self.configs)
		CleanDotNet(Tools.Path(self.proj_dir, "SolarHotWater.UI", check_exists=False), self.platforms, self.configs)

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\SolarHotWater\\SolarHotWater.Common"], self.platforms, self.configs)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\SolarHotWater\\FroniusMonitor.Service"], self.platforms, self.configs)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\SolarHotWater\\SolarHotWater.UI"], self.platforms, self.configs)
		return

	def Deploy(self):
		self.DeployService()
		self.DeployUI()
		return

	def DeployService(self):
		proj_dir = Tools.Path(self.proj_dir, "FroniusMonitor.Service")

		# Check versions
		version = Tools.Extract(Tools.Path(proj_dir, "FroniusMonitor.Service.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying FroniusMonitor.Service Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin", "SolarHotWater", "Fronius", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(proj_dir, "bin/Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "FroniusMonitor.Service.exe"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "FroniusMonitor.Service.dll"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "SolarHotWater.Common.dll"                   ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"                           ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"                   ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "System.Data.SQLite.dll"                     ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "System.ServiceProcess.ServiceController.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Newtonsoft.Json.dll"                        ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Dapper.dll"                                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Microsoft.*.dll"                            ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "FroniusMonitor.Service.deps.json"           ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "FroniusMonitor.Service.runtimeconfig.json"  ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "appsettings.json"                           ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "runtimes"                                   ), Tools.Path(self.bin_dir, "runtimes", check_exists=False))

	def DeployUI(self):
		proj_dir = Tools.Path(self.proj_dir, "SolarHotWater.UI")

		# Check versions
		version = Tools.Extract(Tools.Path(proj_dir, "SolarHotWater.UI.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying SolarHotWater.UI Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin", "SolarHotWater", "UI", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(proj_dir, "bin/Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "SolarHotWater.UI.exe"                       ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "SolarHotWater.UI.dll"                       ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"                           ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"                   ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"                        ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Net.dll"                            ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.View3d.dll"                         ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "System.Data.SQLite.dll"                     ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Microsoft.CodeAnalysis.dll"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Microsoft.CodeAnalysis.CSharp.dll"          ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Microsoft.CodeAnalysis.CSharp.Scripting.dll"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Microsoft.CodeAnalysis.Scripting.dll"       ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "SolarHotWater.UI.deps.json"                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "SolarHotWater.UI.runtimeconfig.json"        ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Dapper.dll"                                 ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Newtonsoft.Json.dll"                        ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "runtimes"                                   ), Tools.Path(self.bin_dir, "runtimes", check_exists=False))
		Tools.Copy(Tools.Path(target_dir, "lib"                                        ), Tools.Path(self.bin_dir, "lib", check_exists=False))

		# Build the installer
		#print("Building installer...\n")
		#self.installer_wxs = Tools.Path(self.proj_dir, "installer", "installer.wxs")
		#self.msi = BuildInstaller.Build("LDraw", version, self.installer_wxs, self.proj_dir, target_dir,
		#	Tools.Path(self.bin_dir, ".."),
		#	[
		#		["binaries", "INSTALLFOLDER", ".", False,
		#			r"LDraw\..*\.dll",
		#			r"Rylogic\..*\.dll",
		#			r"ICSharpCode.AvalonEdit.dll",
		#		],
		#		["lib_files", "lib", "lib", True],
		#	])
		#print(f"{self.msi} created.\n")
		return
	
	def Publish(self):
	#	if not hasattr(self, "msi") or not os.path.exists(self.msi): raise RuntimeError("Call Deploy before Publish")
	#	print("\nPublishing to web site...")
	#	Tools.Copy(self.msi, Tools.Path(UserVars.wwwroot, "files/ldraw", check_exists=False))
		return

# Time Tracker
class TimeTracker(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "TimeTracker", ["net6.0-windows"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\apps", self.proj_name)
		self.platforms = ["x64"]
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild("TimeTracker", self.rylogic_sln, ["Apps\\TimeTracker\\TimeTracker"], self.platforms, self.configs)
		return

	def Deploy(self):
		version = Tools.Extract(Tools.Path(self.proj_dir, "TimeTracker.csproj"), r"<Version>(.*)</Version>").group(1)
		print(f"Deploying TimeTracker Version: {version}\n")

		# Ensure output directories exist and are empty
		self.bin_dir = Tools.Path(UserVars.root, "bin", "TimeTracker", check_exists=False)
		CleanDir(self.bin_dir)

		# Copy build products to the output directory
		print(f"Copying files to {self.bin_dir}...\n")
		target_dir = Tools.Path(self.proj_dir, "bin", "Release", self.frameworks[0])
		Tools.Copy(Tools.Path(target_dir, "TimeTracker.exe"               ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "TimeTracker.dll"               ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "TimeTracker.runtimeconfig.json"), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.dll"              ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Core.Windows.dll"      ), self.bin_dir)
		Tools.Copy(Tools.Path(target_dir, "Rylogic.Gui.WPF.dll"           ), self.bin_dir)
		return

# Fishomatic
class Fishomatic(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "Fishomatic", ["netcoreapp3.1"], workspace, platforms, configs)
		self.platforms = ["x64"]
		self.proj_dir = Tools.Path(workspace, "projects\\apps", self.proj_name)
		return

	def Clean(self):
		CleanDotNet(Tools.Path(self.proj_dir, "Fishomatic", check_exists=False), self.platforms, self.configs)

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		MSBuild(self.proj_name, self.rylogic_sln, [f"Apps\\Ideas\\Fishomatic"], self.platforms, self.configs)
		return

	def Deploy(self):
		return

	def Publish(self):
		return

# Rylogic.TextAligner
class RylogicTextAligner(Managed):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Managed.__init__(self, "Rylogic.TextAligner", ["net472"], workspace, platforms, configs)
		self.proj_dir = Tools.Path(workspace, "projects\\apps", self.proj_name)
		self.vsix_ids = ["DF402917-6013-40CA-A4C6-E1640DA86B90", "26C3C30A-6050-4CBF-860E-6C5590AF95EF"]
		self.signing_algos = ["sha1", "sha256"]
		self.targets = ['2019', '2022']
		return

	def Build(self):
		DotNetRestore(self.rylogic_sln)
		for target in self.targets:
			MSBuild(f"{self.proj_name}.{target}", self.rylogic_sln, [f"Apps\\{self.proj_name}\\{self.proj_name}.{target}"], self.platforms, self.configs)
		return

	def Deploy(self):

		self.deployed = []
		for target, vsix_id, algo in zip(self.targets, self.vsix_ids, self.signing_algos):
			
			proj_dir = Tools.Path(self.proj_dir, target)
			manifest  = Tools.Path(proj_dir, "source.extension.vsixmanifest")

			# Check the manifest version
			version_regex = f'Id="{vsix_id}" Version="(?P<version>.*?)"'
			match_version = Tools.Extract(manifest, version_regex)
			if not match_version: raise RuntimeError(f"Failed to extract version number from:\r\n {manifest}")
			version = match_version.group("version")
			print(f"Deploying {self.proj_name} ({target}) Version: {version}\n")

			# Check version is greater than the last released version (update this after a release)
			min_released_version = "1.10.0"
			if version <= min_released_version:
				raise RuntimeError("Version number needs bumping")

			# Check the assembly info
			assinfo = Tools.Path(proj_dir, "RylogicTextAlignerPackage.cs")
			ass_version = Tools.Extract(assinfo, r"AssemblyVersion\(\"(?P<vers>.*?)\"\)")
			ass_filevers = Tools.Extract(assinfo, r"AssemblyFileVersion\(\"(?P<vers>.*?)\"\)")
			ipr_version = Tools.Extract(assinfo, r"InstalledProductRegistration\(\".*?\", \".*?\", \"(?P<vers>.*?)\"")
			if not ass_version or ass_version.group("vers") != version:
				raise RuntimeError(f"AssemblyVersion has not been updated to {version} (in {assinfo})")
			if not ass_filevers or ass_filevers.group("vers") != version:
				raise RuntimeError(f"AssemblyFileVersion has not been updated to {version} (in {assinfo})")
			if not ipr_version or ipr_version.group("vers") != version:
				raise RuntimeError(f"InstalledProductRegistration has not been updated to {version} (in {assinfo})")

			# Ensure the ouptut directory exists
			bin_dir = Tools.Path(UserVars.root, "bin", self.proj_name, check_exists=False)
			bin_path = Tools.Path(bin_dir, f"{self.proj_name}.{target}.v{version}.vsix", check_exists=False)

			# Copy build products to the output directory
			print(f"Copying files to: {bin_dir}")
			target_path = Tools.Path(proj_dir, f"bin\\Release\\{self.proj_name}.vsix")
			Tools.Copy(target_path, bin_path)

			# Sign the vsix
			Tools.SignVsix(bin_path, algo)
			self.deployed.append(bin_path)

		return

	def Publish(self):
		# Uploading to marketplace.visualstudio.com is a manual step... you don't want to bugger it up.
		return

# Rylogic .NET assemblies
class Rylogic(Group):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
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
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
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
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Group.__init__(self, workspace)
		self.items = [
			Rylogic      (workspace, platforms, configs),
			Csex         (workspace, platforms, configs),
			LDraw        (workspace, platforms, configs),
			SolarHotWater(workspace, platforms, configs),
			TimeTracker  (workspace, platforms, configs),
		]
		return

# Build all Software projects
class All(Group):
	def __init__(self, workspace:str, platforms:List[str], configs:List[str]):
		Group.__init__(self, workspace)
		self.items = [
			Sqlite3      (workspace, platforms, configs),
			Scintilla    (workspace, platforms, configs),
			Audio        (workspace, platforms, configs),
			View3d       (workspace, platforms, configs),
			P3d          (workspace, platforms, configs),
			Rylogic      (workspace, platforms, configs),
			Csex         (workspace, platforms, configs),
			LDraw        (workspace, platforms, configs),
			RyLogViewer  (workspace, platforms, configs),
			Fishomatic   (workspace, platforms, configs),
			SolarHotWater(workspace, platforms, configs),
			TimeTracker  (workspace, platforms, configs),
		]
		return

# Main
def Main(args:List[str]):

	# Set defaults for command line options
	# Get the current workspace directory from the path of this file
	workspace = Tools.Path(os.path.dirname(__file__), "..")
	projects = []
	platforms = []
	configs = []
	clean = False
	build = True
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
		elif arg == "-nobuild":
			i = i + 1
			build = False
		elif arg == "-rebuild":
			i = i + 1
			clean = True
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
				UserVars.code_sign_cert_pw = args[i]
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

	# Find the builder class name for the given project name
	def FindProject(proj_name:str):
		for x in EProjects.__members__:
			if proj_name != EProjects[x].value: continue
			return x
		return None
		
	# Normalise parameters
	for i in range(0, len(platforms)):
		if platforms[i].lower() == "x64": platforms[i] = "x64"
		if platforms[i].lower() == "x86": platforms[i] = "x86"
		if platforms[i].lower() == "any cpu" or platforms[i].lower() == "anycpu": platforms[i] = "Any CPU"
	for i in range(0, len(configs)):
		if configs[i].lower() == "release": configs[i] = "Release"
		if configs[i].lower() == "debug": configs[i] = "Debug"
	for p in projects:
		if FindProject(p) != None: continue
		raise RuntimeError(f"'{p}' is not a valid project name")
	if len(projects) == 0: projects = ["All"]

	if not clean and not build and not deploy and not publish: build = True
	if publish: deploy = True

	# Build/Clean/Deploy each given project
	for project in projects:
		name = FindProject(project)
		builder = eval(name)(workspace, platforms, configs)

		# Prompt for the cert password if signing is needed
		if (deploy or publish) and hasattr(builder, "requires_signing") and builder.requires_signing and not UserVars.code_sign_cert_pw:
			Tools.PromptCertPassword()

		# Clean if '-clean' is used
		if clean:
			print("")
			builder.Clean()

		# If no project name is given build them all
		if build:
			print("")
			builder.Build()

		# Deploy the named project(s)
		if deploy:
			print("")
			builder.Deploy()

		# Publish the named project(s)
		if publish:
			print("")
			builder.Publish()

	print(f"\nComplete: {workspace}")
	return

# Entry Point
if __name__ == "__main__":
	argv_before = sys.argv

	# Examples:
	#sys.argv=['build.py']
	#sys.argv=['build.py', '-projects', 'Audio', '-deploy']
	#sys.argv=['build.py', '-project', 'Rylogic.Core', '-platforms', 'x64', 'x86', '-configs', 'release', 'debug']
	#sys.argv=['build.py', '-project', 'Rylogic.Core', 'Rylogic.Core.Windows', '-deploy']
	#sys.argv=['build.py', '-projects', 'Rylogic.TextAligner', '-build', '-deploy']
	#sys.argv=['build.py', '-projects', 'LDraw', '-configs', 'Release', '-deploy']
	#sys.argv=['build.py', '-projects', 'Rylogic', '-deploy']
	#sys.argv=['build.py', '-projects', 'Scintilla', '-clean']
	#sys.argv=['build.py', '-projects', 'LDraw', '-deploy']
	#sys.argv=['build.py', '-projects', 'Csex', '-deploy']
	#sys.argv=['build.py', '-projects', 'P3d', '-deploy']
	#print(f"Command Line: {str(sys.argv)}")
	
	if argv_before != sys.argv: print("WARNING: Command line arguments have been modified for testing purposes")
	Main(sys.argv)
