#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, sys
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"  Elevate Deploy\n"
		"    Copyright (c) Rylogic 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	# Build
	proj = os.path.join(UserVars.root, "SDK", "rylogic", "build", "elevate.vcxproj")
	platforms = [
		"x86",
		"x64"
		]
	configs = [
		"debug",
		"release"
		]
	Tools.MSBuild(proj, [], platforms, configs, parallel=True, same_window=True)
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
