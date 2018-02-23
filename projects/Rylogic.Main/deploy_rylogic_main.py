#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

# Deploy Rylogic.Main
def Deploy(config:str, nowait:bool, trace:bool):
	print(
		"*************************************************************************\n"
		"Rylogic.Main Deploy\n"
		"Copyright (c) Rylogic 2018\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.msbuild])
	Tools.AssertPathsExist([UserVars.root, UserVars.nuget])

	srcdir  = UserVars.root + "\\projects\\Rylogic.Main"
	proj    = srcdir + "\\Rylogic.Main.csproj"
	configs = [config.lower()] if not config == "both" else ["debug", "release"]
	sln     = UserVars.root + "\\build\\Rylogic.sln"

	# Build
	print("Building...")
	Tools.MSBuild(sln, ["Rylogic\\Rylogic.Main"], ["Any CPU"], configs, False, False)

	# Package
	if "release" in configs:
		Tools.NugetPackage(proj)

	return

if __name__ == "__main__":
	try:

		# Check for optional parameters
		config = "both" #input("Configuration (debug, release, both(default))? ")
		nowait = True if "nowait" in [arg.lower() for arg in sys.argv] else False
		trace  = True if "trace"  in [arg.lower() for arg in sys.argv] else False

		Deploy(config, nowait, trace);

		Tools.OnSuccess(pause_time_seconds=1)

	except Exception as ex:
		Tools.OnException(ex)
