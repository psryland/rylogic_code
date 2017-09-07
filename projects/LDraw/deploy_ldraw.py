#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#

import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars
import BuildInstaller

def Deploy():
	print(
		"*************************************************************************\n"
		" LDraw Deploy\n"
		"  Copyright Rylogic Limited 2002\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex])

	srcdir = UserVars.root + "\\projects\\LDraw"
	dstdir = UserVars.root + "\\bin\\ldraw"
	targetdir = srcdir + "\\bin\\Release"
	sln = UserVars.root + "\\build\\Rylogic.sln"

	# Publish to WWW
	publish = input("Publish to web site?") == "y"

	# Ensure output directories exist and are empty
	print("\nCleaning deploy directory: " + dstdir)
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Check versions
	version = Tools.Extract(srcdir + "\\Properties\AssemblyInfo.cs", "AssemblyVersion\(\"(.*)\"\)").group(1)

	# Build the project
	print("\nBuilding...")
	Tools.MSBuild(sln, ["scintilla", "renderer11", "view3d"], ["x86","x64"], ["release"], False, True)
	Tools.MSBuild(sln, ["LDraw"], ["Any CPU"], ["release"], False, True)

	# Copy build products to the output directory
	print("\nCopying files to " + dstdir + "...")
	Tools.Copy(targetdir + "\\LDraw.exe"   , dstdir + "\\")
	Tools.Copy(targetdir + "\\Rylogic.dll" , dstdir + "\\")
	Tools.Copy(targetdir + "\\lib"         , dstdir + "\\lib")

	# Build the installer
	print("\nBuilding installer...")
	msi = BuildInstaller.Build(
		"LDraw",
		version,
		srcdir + "\\installer\\installer.wxs",
		srcdir,
		targetdir,
		dstdir + "\\..",
		[
			["lib","INSTALLFOLDER"],
		])
	print(msi + " created.")

	# Publish to WWW
	if publish:
		print("\nPublishing to web site...")
		Tools.Copy(msi, UserVars.wwwroot + "\\ldraw\\")

	return

#Run as standalone script
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)
