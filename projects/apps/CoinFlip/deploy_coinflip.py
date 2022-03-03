#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# This script is set up to build projects in the RyLogViewer.sln file
# If you get errors, check that you can get this solution to build
# If projects have moved/changed/etc they'll need sorting in this solution

import sys, os, shutil, re
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars
import BuildInstaller

def Deploy():
	print(
		"*************************************************************************\n"
		"CoinFlip Deploy\n"
		"Copyright (c) Rylogic 2017\n"
		"*************************************************************************")

	# Check tools paths are valid
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex, UserVars.msbuild, UserVars.ziptool, UserVars.vs_dir])

	# Paths
	config = "Release"
	srcdir = UserVars.root + "\\projects\\CoinFlip"
	dstdir = UserVars.root + "\\bin\\CoinFlip"
	targetdir = srcdir + "\\CoinFlip\\bin\\" + config
	objdir = srcdir + "\\CoinFlip\\obj\\" + config
	sln = srcdir + "\\CoinFlip.sln"

	# Ensure output directories exist and are empty
	print("\nCleaning deploy directory: " + dstdir)
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Build the project
	print("\nBuilding...")
	Tools.MSBuild(sln, ["scintilla", "sqlite3", "renderer11", "view3d"], ["x86","x64"], [config], False, True)
	Tools.MSBuild(sln, [], ["Any CPU"], [config], False, True)

	# Copy build products to the deploy dir
	print("\nCopying files to " + dstdir + "...")
	Tools.Copy(targetdir + "\\CoinFlip.exe"        , dstdir + "\\")
	Tools.Copy(targetdir + "\\CoinFlip.exe.config" , dstdir + "\\")
	Tools.Copy(targetdir + "\\*.dll"               , dstdir + "\\")
	Tools.Copy(targetdir + "\\lib"                 , dstdir + "\\lib\\")
	Tools.Copy(targetdir + "\\bots"                , dstdir + "\\bots\\")

	# Build the installer
	#print("\nBuilding installer...")
	#msi = BuildInstaller.Build(
	#	"RyLogViewer",
	#	version,
	#	srcdir + "\\installer\\installer.wxs",
	#	srcdir,
	#	targetdir,
	#	dstdir + "\\..",
	#	[
	#		["docs"    ,"INSTALLFOLDER"],
	#		["plugins" ,"INSTALLFOLDER"],
	#		["examples","INSTALLFOLDER"],
	#	])

	return

# Run as standalone script
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)
