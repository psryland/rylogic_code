#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Notes:
#  - This is a "living" document. I.e. I'm too lazy to add all the projects now.
#    Just add them on demand.
#  - If you get an error saying 'UserVars not found'. Run py .\script\Setup.py
#    to generate a suitable UserVars.py file for the current system and working
#    directory location

import sys, os, subprocess, datetime, re, shutil
from importlib import machinery as Import
import Rylogic as Tools
import BuildInstaller
import BuildDocs
import BuildInstaller
import UserVars

# Available projects that can be built
class EProjects():
	Sqlite3 = "Sqlite3"
	Scintilla = "Scintilla"
	View3d = "View3d"
	Audio = "Audio"
	Rylogic = "Rylogic"
	CSex = "CSex"
	P3D = "P3D"
	LDraw = "LDraw"
	Native = "Native"

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

# Invoke MSBuild on the given solution or project file
def MSBuild(name:str, sln:str, projects:[str], platforms:[str], configs:[str], props:str=None):
	print(f"\nBuilding {name}")
	Tools.SetupVcEnvironment()
	if Tools.MSBuild(sln, projects, platforms, configs, props): return
	raise RuntimeError(f"Building {name} failed")

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

# Build SQLite3 native dll
def BuildSqlite3(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["x64", "x86"]
	configs = configs if configs else ["Release", "Debug"]
	MSBuild("Sqlite3", rylogic_sln, ["SDK\\sqlite3"], platforms, configs)
	return
def CleanSqlite3(workspace:str, platforms:[str], configs:[str]):
	CleanObj(os.path.join(workspace, "sdk", "sqlite", "obj"), platforms, configs)
	return

# Build the Scintilla native dll
def BuildScintilla(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["x64", "x86"]
	configs = configs if configs else ["Release", "Debug"]
	MSBuild("Scintilla", rylogic_sln, ["SDK\\scintilla"], platforms, configs)
	return
def CleanScintilla(workspace:str, platforms:[str], configs:[str]):
	obj_dir = os.path.join(workspace, "sdk", "scintilla", "obj", UserVars.platform_toolset)
	CleanObj(os.path.join(obj_dir, "scintilla"), platforms, configs)
	return

# Build the Audio native dll
def BuildAudio(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["x64", "x86"]
	configs = configs if configs else ["Release", "Debug"]
	MSBuild("Audio", rylogic_sln, ["Rylogic\\audio"], platforms, configs)
	MSBuild("Audio.dll", rylogic_sln, ["Rylogic\\audio.dll"], platforms, configs)
	return
def CleanAudio(workspace:str, platforms:[str], configs:[str]):
	obj_dir = os.path.join(workspace, "obj", UserVars.platform_toolset)
	CleanObj(os.path.join(obj_dir, "audio"), platforms, configs)
	CleanObj(os.path.join(obj_dir, "audio.dll"), platforms, configs)
	return

# Build the View3d native dll
def BuildView3d(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["x64", "x86"]
	configs = configs if configs else ["Release", "Debug"]
	MSBuild("View3d", rylogic_sln, ["Rylogic\\view3d"], platforms, configs)
	MSBuild("View3d.dll", rylogic_sln, ["Rylogic\\view3d.dll"], platforms, configs)
	return
def CleanView3d(workspace:str, platforms:[str], configs:[str]):
	obj_dir = os.path.join(workspace, "obj", UserVars.platform_toolset)
	CleanObj(os.path.join(obj_dir, "view3d"), platforms, configs)
	CleanObj(os.path.join(obj_dir, "view3d.dll"), platforms, configs)
	return

# Build Rylogic .NET assemblies
def BuildRylogic(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["Any CPU"]
	configs = configs if configs else ["Release", "Debug"]

	# Build the rylogic assemblies
	projects = [
		"Rylogic\\Rylogic.Core",
		"Rylogic\\Rylogic.Core.Windows",
		"Rylogic\\Rylogic.Scintilla",
		"Rylogic\\Rylogic.View3d",
		"Rylogic\\Rylogic.Gui.WinForms",
		"Rylogic\\Rylogic.Gui.WPF"
		]

	Tools.SetupVcEnvironment()
	Tools.Exec([UserVars.nuget, "restore", rylogic_sln])
	MSBuild("Rylogic Assemblies", rylogic_sln, projects, platforms, configs)
	return
def CleanRylogic(workspace:str, platforms:[str], configs:[str]):
	CleanDotNet(os.path.join(workspace, "projects", "Rylogic.Core")        , platforms, configs)
	CleanDotNet(os.path.join(workspace, "projects", "Rylogic.Core.Windows"), platforms, configs)
	CleanDotNet(os.path.join(workspace, "projects", "Rylogic.Scintilla")   , platforms, configs)
	CleanDotNet(os.path.join(workspace, "projects", "Rylogic.View3d")      , platforms, configs)
	CleanDotNet(os.path.join(workspace, "projects", "Rylogic.Gui.WinForms"), platforms, configs)
	CleanDotNet(os.path.join(workspace, "projects", "Rylogic.Gui.WPF")     , platforms, configs)
	return

# Build CSex
def BuildCSex(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["Any CPU"]
	configs = configs if configs else ["Release", "Debug"]

	Tools.SetupVcEnvironment()
	Tools.Exec([UserVars.nuget, "restore", rylogic_sln])
	MSBuild("CSex", rylogic_sln, ["Tools\\Csex"], platforms, configs)
	return
def CleanCSex(workspace:str, platforms:[str], configs:[str]):
	CleanDotNet(os.path.join(workspace, "projects", "CSex"), platforms, configs)
	return

# Build P3D
def BuildP3D(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["x64", "x86"]
	configs = configs if configs else ["Release", "Debug"]
	MSBuild("p3d", rylogic_sln, ["Tools\\p3d"], platforms, configs)
	return
def CleanP3D(workspace:str, platforms:[str], configs:[str]):
	obj_dir = os.path.join(workspace, "obj", UserVars.platform_toolset)
	CleanObj(os.path.join(obj_dir, "p3d"), platforms, configs)
	return

# Build LDraw
def BuildLDraw(workspace:str, platforms:[str], configs:[str]):
	rylogic_sln = os.path.join(workspace, "build", "rylogic.sln")
	platforms = platforms if platforms else ["Any CPU"]
	configs = configs if configs else ["Release", "Debug"]

	# Build the rylogic assemblies
	projects = [
		"LDraw\\LDraw",
		]

	Tools.SetupVcEnvironment()
	Tools.Exec([UserVars.nuget, "restore", rylogic_sln])
	MSBuild("LDraw", rylogic_sln, projects, platforms, configs)
	return
def CleanLDraw(workspace:str, platforms:[str], configs:[str]):
	CleanDotNet(os.path.join(workspace, "projects", "LDraw", "LDraw"), platforms, configs)
	return

# Build the native projects
def BuildNative(workspace:str, platforms:[str]=None, configs:[str]=None):

	BuildSqlite3(workspace, platforms, configs)
	BuildScintilla(workspace, platforms, configs)
	BuildView3d(workspace, platforms, configs)
	BuildAudio(workspace, platforms, configs)
	BuildP3D(workspace, platforms, configs)
	return
def CleanNative(workspace:str, platforms:[str]=None, configs:[str]=None):
	CleanSqlite3(workspace, platforms, configs)
	CleanScintilla(workspace, platforms, configs)
	CleanView3d(workspace, platforms, configs)
	CleanAudio(workspace, platforms, configs)
	CleanP3D(workspace, platforms, configs)
	return

# Build all Software projects
# 'workspace' is the root of a freshly checked out repo
def BuildAll(workspace:str, platforms:[str]=None, configs:[str]=None):

	BuildSqlite3(workspace, platforms, configs)
	BuildScintilla(workspace, platforms, configs)
	BuildView3d(workspace, platforms, configs)
	BuildAudio(workspace, platforms, configs)
	BuildRylogic(workspace, platforms, configs)
	BuildCSex(workspace, platforms, configs)
	BuildP3D(workspace, platforms, configs)
	return
def CleanAll(workspace:str, platforms:[str]=None, configs:[str]=None):
	CleanSqlite3(workspace, platforms, configs)
	CleanScintilla(workspace, platforms, configs)
	CleanView3d(workspace, platforms, configs)
	CleanAudio(workspace, platforms, configs)
	CleanRylogic(workspace, platforms, configs)
	CleanCSex(workspace, platforms, configs)
	CleanP3D(workspace, platforms, configs)
	return

# Main
def Main(args:[str]):
	# Set defaults for command line options
	# Get the current workspace directory from the path of this file
	workspace = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
	project = None
	platforms = []
	configs = []
	clean = False

	# Parse command line
	i = 1
	while i < len(args):
		if args[i].lower() == "-clean":
			clean = True
			i = i + 1
		elif args[i].lower() == "-workspace":
			workspace = args[i+1]
			i = i + 2
		elif args[i].lower() == "-project" or args[i].lower() == "-projects":
			if len(args) > i+1:
				project = args[i+1]
				i = i + 2
			else:
				EProjects.ListProjects()
				return
		elif args[i].lower() == "-platform" or args[i].lower() == "-platforms":
			for a in args[i+1:]:
				if a[0] == '-': break
				else: platforms.append(a)
			i = i + 1 + len(platforms)
		elif args[i].lower() == "-config" or args[i].lower() == "-configs":
			for a in args[i+1:]:
				if a[0] == '-': break
				else: configs.append(a)
			i = i + 1 + len(configs)
		else:
			raise RuntimeError(f"Unknown command line argument: {args[i]}")

	# Normalise parameters
	for i in range(0,len(platforms)):
		if platforms[i].lower() == "x64": platforms[i] = "x64"
		if platforms[i].lower() == "x86": platforms[i] = "x86"
	for i in range(0,len(configs)):
		if configs[i].lower() == "release": configs[i] = "Release"
		if configs[i].lower() == "debug": configs[i] = "Debug"

	# Clean if '-clean' is used
	if clean:
		if not project:
			CleanAll(workspace, platforms, configs)
		else:
			raise RuntimeError(f"Unknown project name {project}")
		
		print(f"\nClean Complete")

	else:
		# If a project name argument is given, just build that project
		if not project:
			BuildAll(workspace)
		elif project == EProjects.Sqlite3:
			BuildSqlite3(workspace, platforms, configs)
		elif project == EProjects.Scintilla:
			BuildScintilla(workspace, platforms, configs)
		elif project == EProjects.View3d:
			BuildView3d(workspace, platforms, configs)
		elif project == EProjects.Audio:
			BuildAudio(workspace, platforms, configs)
		elif project == EProjects.Rylogic:
			BuildRylogic(workspace, platforms, configs)
		elif project == EProjects.CSex:
			BuildCSex(workspace, platforms, configs)
		elif project == EProjects.P3D:
			BuildP3D(workspace, platforms, configs)
		elif project == EProjects.LDraw:
			BuildLDraw(workspace, platforms, configs)
		elif project == EProjects.Native:
			BuildNative(workspace, platforms, configs)
		else:
			raise RuntimeError(f"Unknown project name {project}")

		print(f"\nBuild Complete: {workspace}")
	return

# Entry Point
if __name__ == "__main__":
	try:
		# Examples:
		#   sys.argv=['script\\build.py', '-project', 'Sqlite3', '-platforms', 'x64', 'x86', '-configs', 'release', 'debug']
		#   sys.argv=['script\\build.py', '-project', 'FootSender', '-configs','Dev_Release']
		#   print(f"Command Line: {str(sys.argv)}")
		Main(sys.argv)

	except Exception as ex:
		print(f"ERROR: {str(ex)}")
		sys.exit(-1)
