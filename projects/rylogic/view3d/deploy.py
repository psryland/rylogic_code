#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"View3d Deploy\n"
		"Copyright (c) Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Check for optional parameters
	nowait = True if "nowait" in [arg.lower() for arg in sys.argv] else False
	trace  = True if "trace"  in [arg.lower() for arg in sys.argv] else False

	sln = os.path.join(UserVars.root, "build", "rylogic.sln")
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"view3d",
		]
	platforms = [
		"x86",
		"x64"
		]
	configs = [
		"debug",
		"release"
		]

	if not Tools.MSBuild(sln, projects, platforms, configs, parallel = True, same_window = True):
		Tools.OnError("Errors occurred")

	Tools.OnSuccess(pause_time_seconds = (0 if nowait else 1))

except Exception as ex:
	Tools.OnException(ex)
