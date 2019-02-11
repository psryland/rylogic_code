#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
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
	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")
	projects = ["Tools\\cex"]
	platforms = ["x64","x86"]
	configs = ["release","debug"]

	# Build the project
	if not Tools.MSBuild(sln, projects, platforms, configs, parallel=False, same_window=True):
		Tools.OnError("Errors occurred")

	# Deploy
	objdir = os.path.join(UserVars.root, "obj", UserVars.platform_toolset, "cex")
	outdir = os.path.join(UserVars.root, "bin")
	for p in platforms:
		Tools.Copy(os.path.join(objdir, p, "release", "Cex.exe"), os.path.join(outdir, "cex", p) + "\\")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
