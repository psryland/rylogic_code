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
		"Copyright (C) Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Use the everything sln so that dependent projects get built as well
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"linedrawer",
		]
	configs = [
		"release",
		]
	platforms = [
		"x64",
		]

	# Build the project
	if not Tools.MSBuild(UserVars.root + "\\build\\Rylogic.sln", projects, platforms, configs, parallel=True, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	objdir = UserVars.root + "\\obj\\" + UserVars.platform_toolset + "\\linedrawer"
	outdir = UserVars.root + "\\bin"
	for p in platforms:
		Tools.Copy(objdir+"\\"+p+"\\release\\LineDrawer.exe", outdir+"\\linedrawer\\"+p+"\\")
		Tools.Copy(objdir+"\\"+p+"\\release\\scintilla.dll" , outdir+"\\linedrawer\\"+p+"\\")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)