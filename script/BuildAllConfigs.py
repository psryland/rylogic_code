#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Use:
#  BuildAllConfigs $(TargetName)

import sys, os, shutil, re
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Build All Configs\n"
		"Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	sln = UserVars.root + "\\build\\Rylogic.sln"

	# e.g: "\"folder\proj_name:Rebuild\""
	proj = sys.argv[1] if len(sys.argv) > 1 else input("project? ")
	projects = [proj]

	platforms = [
		"x86",
		"x64",
		#"win32",
		]

	configs = [
		"debug",
		"release",
		#"dev_debug",
		#"dev_release",
		]

	#Invoke MSBuild
	if not Tools.MSBuild(sln, projects, platforms, configs, parallel=True, same_window=True):
		Tools.OnError("Errors occurred")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
