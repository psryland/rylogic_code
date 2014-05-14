#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil, re
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Rylogic Class Library Deploy\n"
		"Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	# Check for optional parameters
	nowait = True if "nowait" in [arg.lower() for arg in sys.argv] else False
	trace  = True if "trace"  in [arg.lower() for arg in sys.argv] else False

	srcdir = UserVars.root + "\\sdk\\pr\\prcs"
	proj   = srcdir + "\\Rylogic.csproj"
	config = "both" #input("Configuration (debug, release, both(default))? ")
	if config == "": config = "both"

	if not nowait:
		input(
			" Deploy Settings:\n"
			"         Source: " + srcdir + "\n"
			"  Configuration: " + config + "\n"
			"Press enter to continue")

	#Invoke MSBuild
	print("Building...")

	if config == "debug" or config == "both":
		Tools.Exec([UserVars.msbuild, UserVars.msbuild_props, proj, "/t:Rebuild", "/p:Configuration=Debug"])

	if config == "release" or config == "both":
		Tools.Exec([UserVars.msbuild, UserVars.msbuild_props, proj, "/t:Rebuild", "/p:Configuration=Release"])

	Tools.OnSuccess(pause_time_seconds=1)

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
