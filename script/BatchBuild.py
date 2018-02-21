#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Batch Build\n"
		"Copyright (c) Rylogic 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	sln = UserVars.root + "\\build\\Rylogic.sln"
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
	platforms = [
		"x86",
		"x64"
		]
	configs = [
		"debug",
		"release"
		]

	if not Tools.MSBuild(sln, projects, platforms,configs,parallel = True, same_window = True):
		Tools.OnError("Errors occurred")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
