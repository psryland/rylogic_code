#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Cex Deploy\n"
		"Copyright (c) Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Use the everything sln so that dependent projects get built as well
	sln = UserVars.rylogic_sln
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"cex",
		]
	platforms = [
		"x64",
		"x86",
		]
	configs = [
		"release",
		"debug",
		]

	# Build the project
	if not Tools.MSBuild(sln, projects, platforms, configs, parallel=False, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	files = [
		"cex.exe",
		]
	Tools.DeployToBin("cex", files, platforms, "release", CopyForArch=True)

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
