#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(re.sub(r"(.:[\\/]).*", r"\1script", os.path.abspath(__file__))) # add the \script path
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"LineDrawer Deploy\n"
		"Copyright © Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Use the everything sln so that dependent projects get built as well
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"linedrawer",
		]
	configs = [
		"release",
		"debug",
		]
	platforms = [
		"x64",
		"x86"
		]

	# Build the project
	if not Tools.MSBuild(UserVars.root + "\\build\\Rylogic.sln", projects, platforms, configs, parallel=True, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	files = [
		"linedrawer.exe",
		"scintilla.dll",
		]
	Tools.DeployToBin("linedrawer", files, platforms, "release", CopyForArch=False)

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
