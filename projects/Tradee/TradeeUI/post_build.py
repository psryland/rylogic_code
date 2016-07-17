#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# post_build.py $(TargetDir) $(PlatformTarget) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.:[\\/]).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	appname   = "Tradee"
	projdir   = sys.argv[1].rstrip('\\') if len(sys.argv) > 1 else UserVars.root + "\\projects\\Tradee\\TradeeUI"
	targetdir = sys.argv[2].rstrip('\\') if len(sys.argv) > 2 else UserVars.root + "\\projects\\Tradee\\TradeeUI\\bin\\Debug"
	config    = sys.argv[3]              if len(sys.argv) > 3 else "Debug"

	# Ensure the 'lib' directory exists
	x64libs = targetdir+"\\lib\\x64\\"
	x86libs = targetdir+"\\lib\\x86\\"
	if not os.path.exists(targetdir): os.makedirs(targetdir)
	if not os.path.exists(x64libs): os.makedirs(x64libs)
	if not os.path.exists(x86libs): os.makedirs(x86libs)

	# Copy the support dlls
	# These are the native dlls that visual studio doesn't automatically handle
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\view3d.dll", targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\view3d.dll", targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\scintilla.dll", targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\scintilla.dll", targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\sdk\\sqlite\\lib\\x86\\"+config+"\\sqlite3.dll", targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\sdk\\sqlite\\lib\\x64\\"+config+"\\sqlite3.dll", targetdir+"\\lib\\x64\\", only_if_modified=True)

except Exception as ex:
	Tools.OnException(ex)