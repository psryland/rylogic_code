#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\..\script")
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Batch Build\n"
		"Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	sln = UserVars.root + "\\projects\\vs2012\\everything.sln"
	# e.g: "\"folder\proj_name:Rebuild\""
	projects = [
		"renderer11",
	#	"renderer11:Rebuild",
		"linedrawer",
	#	"physics",
	#	"unittests",
		"view3d",
	#	"view3d:Rebuild",
	#	"sol",
	#	"cex",
	#	"fwd",
	#	"TextFormatter",
	#	"prautoexp",
	#	"Rylogic",
	#	"Rylogic.VSExtension",
	#	"Csex_vs2012",
	#	"RylogViewer",
	#	"TestCS"
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

	parallel = True
	same_window = False

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
