#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(PlatformName) $(ConfigurationName)
import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import RylogicAssemblyPostBuild as PostBuild
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1);

	assembly = "Rylogic.Gui.WinForms"
	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\"+assembly
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\"+assembly+"\\bin\\Debug"
	platform  = sys.argv[3]              if len(sys.argv) > 3 else "AnyCPU"
	config    = sys.argv[4]              if len(sys.argv) > 4 else "Debug"
	deps = [
		"Rylogic.Core.dll",
		"Rylogic.Core.Windows.dll",
		"Rylogic.View3d.dll",
		]

	PostBuild.PostBuild(assembly, projdir, targetdir, platform, config, deps)

except Exception as ex:
	Tools.OnException(ex)