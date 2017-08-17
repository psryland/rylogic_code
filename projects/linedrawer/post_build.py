#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# post_build.py $(TargetDir) $(PlatformTarget) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	appname = "LineDrawer"
	targetdir = sys.argv[1].rstrip('\\') if len(sys.argv) > 1 else "P:\\obj\\v140\\linedrawer\\x64\\Debug"
	platform  = sys.argv[2]              if len(sys.argv) > 2 else "x64"
	config    = sys.argv[3]              if len(sys.argv) > 3 else "Debug"
	if platform.lower() == "win32": platform = "x86"

	# Copy dependencies to 'targetdir'
	Tools.Copy(UserVars.root + "\\lib\\"+platform+"\\"+config+"\\scintilla.dll", targetdir+"\\", only_if_modified=True)

except Exception as ex:
	Tools.OnException(ex)
