#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"  Elevate Deploy\n"
		"    Copyright (C) Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Build
	sln = UserVars.rylogic_sln
	projects = [ # e.g: "\"folder\proj_name:Rebuild\""
		"elevate"
		]
	platforms = [
		"x86",
		"x64"
		]
	configs = [
		"debug",
		"release"
		]
	Tools.MSBuild(sln, projects, platforms, configs, parallel=True, same_window=True)
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
