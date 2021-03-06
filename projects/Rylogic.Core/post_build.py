#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(TargetFramework) $(PlatformName) $(ConfigurationName)
import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import RylogicAssembly as RA
import Rylogic as Tools
import UserVars

try:
	#print(str(sys.argv))
	Tools.AssertVersion(1)

	assembly = "Rylogic.Core"
	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else os.path.join(UserVars.root, "projects", assembly)
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else os.path.join(UserVars.root, "projects", assembly, "bin", "Debug", "netstandard2.0")
	framework = sys.argv[3]              if len(sys.argv) > 3 else "netstandard2.0"
	platform  = sys.argv[4]              if len(sys.argv) > 4 else "AnyCPU"
	config    = sys.argv[5]              if len(sys.argv) > 5 else "Debug"
	deps = []

	RA.PostBuild(assembly, projdir, targetdir, framework, platform, config, deps)

except Exception as ex:
	Tools.OnException(ex)