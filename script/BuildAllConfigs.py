#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Use:
#  BuildAllConfigs $(ProjectPath) [Rebuild|Clean]

import sys, os, shutil, re
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\..\script")
import Rylogic as Tools
import UserVars

print(
	"*************************************************************************\n"
	"Build All Configs\n"
	"Copyright Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

proj = sys.argv[1]
target = sys.argv[2] if len(sys.argv) > 2 else ""
# e.g: "\"folder\proj_name:Rebuild\""

configs = [
	"debug",
	"release",
	#"dev_debug",
	#"dev_release",
	]
platforms = [
	"x86",
	"x64",
	#"win32",
	]

try:
	procs = []

	parallel = True
	same_window = False

	#Invoke MSBuild
	for platform in platforms:
		for config in configs:
			args = [UserVars.msbuild, proj, "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"]
			if target != "": args = args + ["/t:"+target]
			if parallel:
				procs.extend([Tools.Spawn(args, same_window=same_window)])
			else:
				print("\n *** " + platform + " - " + config + " ***\n")
				Tools.Exec(args)

	errors = False
	for proc in procs:
		proc.wait()
		if proc.returncode != 0:
			errors = True
	
	if errors:
		Tools.OnError("Errors occurred")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
