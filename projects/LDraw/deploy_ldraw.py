#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#

import sys, os, shutil, re
sys.path.append(re.sub(r"(\w:[\\/]).*", r"\1script", __file__))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		" LDraw Deploy\n"
		"  Copyright Rylogic Limited 2002\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex, UserVars.msbuild, UserVars.vs_dir])

	srcdir = UserVars.root + "\\projects\\LDraw"
	dstdir = UserVars.root + "\\bin\\ldraw"

	# Ensure output directories exist and are empty
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Build the project
	print("Building...")
	projects = [
		"renderer11",
		"scintilla",
		"view3d",
		"LDraw",
		]
	configs = [
		"release",
		]
	platforms = [
		"x86",
		"x64",
		]
	if not Tools.MSBuild(UserVars.root + "\\build\\Rylogic.sln", projects, platforms, configs, False, True):
		Tools.OnError("Errors occurred")

	# Copy build products to the output directory
	print("Copying files to " + dstdir)
	target_dir = srcdir + "\\bin\\Release"
	Tools.Copy(target_dir + "\\LDraw.exe"   , dstdir + "\\")
	Tools.Copy(target_dir + "\\Rylogic.dll" , dstdir + "\\")
	Tools.Copy(target_dir + "\\lib"         , dstdir + "\\lib")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
