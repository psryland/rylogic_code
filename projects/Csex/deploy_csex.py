#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

def Deploy():
	print("""
		*************************************************************************
		  Csex Deploy
		    Copyright (c) Rylogic 2013
		*************************************************************************
		""")

	dstdir = os.path.join(UserVars.root, "bin", "Csex")
	srcdir = os.path.join(UserVars.root, "projects", "Csex")

	# Ensure output directories exist and are empty
	print(f"\nCleaning deploy directory: {dstdir}")
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Build
	print("\nBuilding...")
	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")
	Tools.MSBuild(sln, ["Tools\\Csex"], ["Any CPU"], ["Release"], False, True)

	# Copy build products to the output directory
	print(f"\nCopying files to {dstdir}...")
	targetdir = os.path.join(srcdir, "bin", "Release")
	Tools.Copy(os.path.join(targetdir, "Csex.exe"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Core.dll"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Core.Windows.dll"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.View3d.dll"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Scintilla.dll"), os.path.join(dstdir, ""))
	Tools.Copy(os.path.join(targetdir, "Rylogic.Gui.WinForms.dll"), os.path.join(dstdir, ""))
	return

# Entry Point
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)

