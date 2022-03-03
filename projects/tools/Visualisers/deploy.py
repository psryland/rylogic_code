#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, re
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:
	Tools.RunAsAdmin()

	print(
		"*************************************************************************\n"
		"Rylogic Visualisers Deploy\n"
		"Copyright (c) Rylogic 2015\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.msbuild, UserVars.vs_dir])
	
	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")
	projects = [
		"Visualisers"
		]
	platforms = [
		"x86"
		]
	configs = [
		"Release"
		]
	Tools.MSBuild(sln, projects, platforms, configs)

	Tools.Copy(
		os.path.join(UserVars.root, "projects", "Visualisers", "bin", "Release", "Rylogic.Visualisers.dll"),
		os.path.join(UserVars.vs_dir, "Common7", "Packages", "Debugger", "Visualizers"))

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
