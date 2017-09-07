#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# This script is set up to build projects in the RyLogViewer.sln file
# If you get errors, check that you can get this solution to build
# If projects have moved/changed/etc they'll need sorting in this solution

import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars
import BuildInstaller

def Deploy():
	print(
		"*************************************************************************\n"
		"RyLogViewer Deploy\n"
		"Copyright (C) Rylogic Limited 2017\n"
		"*************************************************************************")

	# Check tools paths are valid
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex, UserVars.msbuild, UserVars.ziptool, UserVars.vs_dir])

	# Paths
	config = "Release"
	srcdir = UserVars.root + "\\projects\\RyLogViewer"
	dstdir = UserVars.root + "\\bin\\RyLogViewer"
	targetdir = srcdir + "\\bin\\" + config
	objdir = srcdir + "\\obj\\" + config
	sln = srcdir + "\\RyLogViewer.sln"

	# Publish to WWW
	publish = input("Publish to web site?") == "y"

	# Ensure output directories exist and are empty
	print("\nCleaning deploy directory: " + dstdir)
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Check versions
	version = Tools.Extract(srcdir + "\\Version.cs", "AssemblyVersion\(\"(.*)\"\)").group(1)
	vers_history = Tools.Extract(srcdir + "\\docs\\inc\\version_history.include.htm", r"<span class='version_label'>Version (.*?)</span>").group(1)
	if vers_history != version:
		raise Exception("Version History has not been updated")

	# Build the project
	print("\nBuilding...")
	Tools.MSBuild(sln, [], ["Any CPU"], [config], False, True)

	# Copy build products to the deploy dir
	print("\nCopying files to " + dstdir + "...")
	Tools.Copy(targetdir + "\\RyLogViewer.exe"            , dstdir + "\\")
	Tools.Copy(targetdir + "\\RyLogViewer.exe.config"     , dstdir + "\\")
	Tools.Copy(targetdir + "\\RyLogic.dll"                , dstdir + "\\")
	Tools.Copy(targetdir + "\\RyLogViewer.Extensions.dll" , dstdir + "\\")
	Tools.Copy(targetdir + "\\docs"                       , dstdir + "\\docs\\")
	Tools.Copy(targetdir + "\\plugins"                    , dstdir + "\\plugins\\")
	Tools.Copy(targetdir + "\\examples"                   , dstdir + "\\examples\\")

	# Build the installer
	print("\nBuilding installer...")
	msi = BuildInstaller.Build(
		"RyLogViewer",
		version,
		srcdir + "\\installer\\installer.wxs",
		srcdir,
		targetdir,
		dstdir + "\\..",
		[
			["docs"    ,"INSTALLFOLDER"],
			["plugins" ,"INSTALLFOLDER"],
			["examples","INSTALLFOLDER"],
		])
	print(msi + " created.")


	# Publish to WWW
	if publish:
		print("\nPublishing to web site...")
		Tools.Copy(msi, UserVars.wwwroot + "\\rylogviewer\\")

	return

# Run as standalone script
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)
