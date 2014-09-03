#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"LineDrawer Deploy\n"
		"Copyright © Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	# Use the everything sln so that dependent projects get built as well
	sln = UserVars.proj_dir + r"\everything.sln"
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"linedrawer",
		]
	configs = [
		"release",
		"debug",
		]
	platforms = [
		"x64",
		"x86",
		]

	# Build the project
	if not Tools.MSBuild(sln, projects, platforms, configs, parallel=False, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	files = [
		"linedrawer.exe",
		]
	Tools.DeployToBin("linedrawer", files, platforms, "release", CopyForArch=True)

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
