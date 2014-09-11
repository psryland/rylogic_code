#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Use:
#  BuildAllConfigs $(SolutionDirectory)$(SolutionFileName) $(TargetName)

import sys, os, shutil, re
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Build All Configs\n"
		"Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)

	sln  = sys.argv[1] if len(sys.argv) > 1 else input("solution? ")
	proj = sys.argv[2] if len(sys.argv) > 2 else input("project? ")

	# e.g: "\"folder\proj_name:Rebuild\""
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
