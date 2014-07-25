#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Cex Deploy\n"
		"Copyright (c) Rylogic Limited 2014\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	# Use the everything sln so that dependent projects get built as well
	sln = UserVars.proj_dir + r"\everything.sln"
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
