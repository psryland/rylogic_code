#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# post_build.py $(TargetDir) $(PlatformTarget) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"(.:[\\/]).*", r"\1script", os.path.abspath(__file__))) # add the \script path
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	appname = "LineDrawer"
	targetdir = sys.argv[1] if len(sys.argv) > 1 else "P:\\obj\\v120\\linedrawer\\x64\\Debug\\"
	platform  = sys.argv[2] if len(sys.argv) > 2 else "x64"
	config    = sys.argv[3] if len(sys.argv) > 3 else "release"
	if platform.lower() == "win32": platform = "x86"

	# Copy dependencies to targetdir
	Tools.Copy(UserVars.root+"\\obj\\v120\\scintilla\\"+platform+"\\"+config+"\\scintilla.dll", targetdir)

except Exception as ex:
	Tools.OnException(ex)
