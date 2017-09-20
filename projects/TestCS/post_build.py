#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars
import BuildDocs

try:
	Tools.AssertVersion(1);

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\TestCS"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\TestCS\\bin\\Debug"
	config    = sys.argv[3]              if len(sys.argv) > 3 else "Debug"

	# Ensure directories exist
	os.makedirs(targetdir, exist_ok=True);
	os.makedirs(targetdir+"\\lib\\x64", exist_ok=True)
	os.makedirs(targetdir+"\\lib\\x86", exist_ok=True)

	# Copy native dlls
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\view3d.dll"   , targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\view3d.dll"   , targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\audio.dll"    , targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\audio.dll"    , targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\scintilla.dll", targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\scintilla.dll", targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\sqlite3.dll"  , targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\sqlite3.dll"  , targetdir+"\\lib\\x64\\", only_if_modified=True)

except Exception as ex:
	Tools.OnException(ex)