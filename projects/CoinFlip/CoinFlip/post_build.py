#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(PlatformName) $(ConfigurationName)
import sys, os, re, string
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\Trading\\CoinFlip\\CoinFlip"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\Trading\\CoinFlip\\CoinFlip\\bin\\Debug"
	platform  = sys.argv[3]              if len(sys.argv) > 3 else "AnyCPU"
	config    = sys.argv[4]              if len(sys.argv) > 4 else "Debug"

	# Ensure directories exist
	os.makedirs(targetdir, exist_ok=True);

	# Copy the support dlls for both platforms
	# These are the native dlls that visual studio doesn't automatically handle
	Tools.Copy(os.path.join(UserVars.root, "lib", "x86", config, "view3d.dll")   , targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(os.path.join(UserVars.root, "lib", "x64", config, "view3d.dll")   , targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(os.path.join(UserVars.root, "lib", "x86", config, "scintilla.dll"), targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(os.path.join(UserVars.root, "lib", "x64", config, "scintilla.dll"), targetdir+"\\lib\\x64\\", only_if_modified=True)
	Tools.Copy(os.path.join(UserVars.root, "lib", "x86", config, "sqlite3.dll")  , targetdir+"\\lib\\x86\\", only_if_modified=True)
	Tools.Copy(os.path.join(UserVars.root, "lib", "x64", config, "sqlite3.dll")  , targetdir+"\\lib\\x64\\", only_if_modified=True)

except Exception as ex:
	Tools.OnException(ex)