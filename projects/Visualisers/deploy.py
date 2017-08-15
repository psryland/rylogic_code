#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.RunAsAdmin()

	print(
		"*************************************************************************\n"
		"Rylogic Visualisers Deploy\n"
		"Copyright Rylogic Limited 2015\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.msbuild, UserVars.vs_dir])
	
	sln = UserVars.root + "\\build\\Rylogic.sln"
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

	Tools.Copy(UserVars.root + "\\projects\\Visualisers\\bin\\Release\\Rylogic.Visualisers.dll", UserVars.vs_dir + "\\Common7\\Packages\\Debugger\\Visualizers")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
