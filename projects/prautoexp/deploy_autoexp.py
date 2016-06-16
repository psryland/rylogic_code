#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(re.sub(r"(.:[\\/]).*", r"\1script", os.path.abspath(__file__))) # add the \script path
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"pr::AutoExp Deploy\n"
		"Copyright (C) Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Use the everything sln so that dependent projects get built as well
	sln = UserVars.root + "\\build\\Rylogic.sln"
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"prautoexp",
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
	if not Tools.MSBuild(sln, projects, platforms, configs, parallel=True, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	objdir = UserVars.root + "\\obj\\" + UserVars.platform_toolset + "\\prautoexp"
	outdir = UserVars.root + "\\lib"
	for p in platforms:
		for c in configs:
			Tools.Copy(objdir+"\\"+p+"\\"+c+"\\prautoexp.dll", outdir+"\\"+p+"\\"+c+"\\")
			Tools.Copy(objdir+"\\"+p+"\\"+c+"\\prautoexp.lib", outdir+"\\"+p+"\\"+c+"\\")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
