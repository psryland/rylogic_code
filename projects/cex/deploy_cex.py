#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Cex Deploy\n"
		"Copyright (c) Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Use the everything sln so that dependent projects get built as well
	sln = UserVars.root + "\\build\\Rylogic.sln"
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"cex",
		]
	platforms = [
		"x64",
		"x86",
		]
	configs = [
		"release",
		"debug",
		]

	# Build the project
	if not Tools.MSBuild(sln, projects, platforms, configs, parallel=False, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	objdir = UserVars.root + "\\obj\\" + UserVars.platform_toolset + "\\cex"
	outdir = UserVars.root + "\\bin"
	for p in platforms:
		Tools.Copy(objdir+"\\"+p+"\\release\\Cex.exe", outdir+"\\cex\\"+p+"\\")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
