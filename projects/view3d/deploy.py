#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"View3d Deploy\n"
		"Copyright Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	sln = UserVars.root + "\\projects\\vs2012\\everything.sln"
	# e.g: "\"folder\proj_name:Rebuild\""
	projects = [
		"view3d",
		]
	configs = [
		"debug",
		"release"
		]
	platforms = [
		"x86",
		"x64"
		]

	procs = []
	parallel = False
	same_window = True

	#Invoke MSBuild
	projs = ";".join(projects)
	for platform in platforms:
		for config in configs:
			args = [UserVars.msbuild, UserVars.msbuild_props, sln, "/t:"+projs, "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"]
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
