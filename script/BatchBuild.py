#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\..\script")
import Rylogic as Tools
import UserVars

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
#	"linedrawer",
#	"physics",
#	"unittests",
	"view3d",
#	"sol",
	"cex",
#	"fwd",
#	"TextFormatter",
#	"prautoexp",
	"Rylogic",
#	"Rylogic.VSExtension",
#	"Csex_vs2012",
#	"RylogViewer",
	"TestCS"
	]
configs = [
	"debug",
	"release"
	]
platforms = [
	"x86",
	"x64"
	]

try:
	#Invoke MSBuild
	projs = ";".join(projects)
	for platform in platforms:
		for config in configs:
			print("\n *** " + platform + " - " + config + " ***\n")
			Tools.Exec([UserVars.msbuild, sln, "/t:"+projs, "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
