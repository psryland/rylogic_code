#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

raise RuntimeError("Using Build.py")

def Deploy():
	print("""
		*************************************************************************
		  Cex Deploy
		    Copyright (c) Rylogic Limited 2014
		*************************************************************************
		""")

	dstdir = os.path.join(UserVars.root, "bin", "Cex")

	# Ensure output directories exist and are empty
	print(f"\nCleaning deploy directory: {dstdir}")
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Build
	print("\nBuilding...")
	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")
	Tools.MSBuild(sln, ["Tools\\cex"], ["x64"], ["Release"], False, True)

	# Copy build products to the output directory
	print(f"\nCopying files to {dstdir}...")
	targetdir = os.path.join(UserVars.root, "obj", UserVars.platform_toolset, "cex", "x64", "Release")
	Tools.Copy(os.path.join(targetdir, "cex.exe"), os.path.join(dstdir, ""))
	return

# Entry Point
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)

