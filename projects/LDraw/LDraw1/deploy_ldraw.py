﻿#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, re
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import BuildInstaller
import UserVars

def Deploy():
	print("""
		*************************************************************************
		 LDraw Deploy
		 Copyright (c) Rylogic 2002
		*************************************************************************
		""")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex])

	srcdir = os.path.join(UserVars.root, "projects", "LDraw")
	dstdir = os.path.join(UserVars.root, "bin", "LDraw")

	# Publish to WWW
	publish = input("Publish to web site? ") == "y"

	# Ensure output directories exist and are empty
	print(f"\nCleaning deploy directory: {dstdir}")
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Check versions
	version = Tools.Extract(os.path.join(srcdir, "Properties", "AssemblyInfo.cs"), "AssemblyVersion\\(\"(.*)\"\\)").group(1)

	# Build the project
	print("\nBuilding...")
	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")
	Tools.MSBuild(sln, ["Rylogic\\scintilla", "Rylogic\\sqlite3", "Rylogic\\renderer11", "Rylogic\\view3d"], ["x86","x64"], ["release"], False, True)
	Tools.MSBuild(sln, ["LDraw\\LDraw"], ["Any CPU"], ["release"], False, True)

	# Copy build products to the output directory
	print(f"\nCopying files to {dstdir}...")
	targetdir = os.path.join(srcdir, "bin", "Release")
	Tools.Copy(os.path.join(targetdir, "LDraw.exe"),                os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "LDraw.API.dll"),            os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Core.dll"),         os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Core.Windows.dll"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Gui.WinForms.dll"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Scintilla.dll"),    os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.View3d.dll"),       os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Google.Protobuf.dll"),      os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Grpc.Core.dll"),            os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Grpc.Core.Api.dll"),        os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "grpc_csharp_ext.x64.dll"),  os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "grpc_csharp_ext.x86.dll"),  os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "lib"),                      os.path.join(dstdir, "lib"))

	# Build the installer
	print("\nBuilding installer...")
	installer_wxs = os.path.join(srcdir, "installer", "installer.wxs")
	msi = BuildInstaller.Build("LDraw", version, installer_wxs, srcdir, targetdir,
		os.path.join(dstdir, ".."),
		[
			["binaries", "INSTALLFOLDER", ".", False,
				r"LDraw\..*\.dll",
				r"Rylogic\..*\.dll",
				"Google.Protobuf.dll",
				"Grpc.Core.dll",
				"grpc_csharp_ext.x64.dll",
				"grpc_csharp_ext.x86.dll",
			],
			["lib_files", "lib", "lib", True],
		])
	print(msi + " created.")

	# Publish to WWW
	if publish:
		print("\nPublishing to web site...")
		Tools.Copy(msi, os.path.join(UserVars.wwwroot, "files", "ldraw", ""))

	return

#Run as standalone script
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)