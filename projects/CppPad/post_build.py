#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# CppPad build step
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(PlatformName) $(ConfigurationName)
import sys, os, re, string
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1);

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\CppPad"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\CppPad\\bin\\Debug"
	platform  = sys.argv[3]              if len(sys.argv) > 3 else "AnyCPU"
	config    = sys.argv[4]              if len(sys.argv) > 4 else "Debug"
	platform  = platform if platform != "AnyCPU" else "x86"

	# Ensure directories exist
	os.makedirs(targetdir, exist_ok=True);

	# Copy the support dlls
	# These are the native dlls that visual studio doesn't automatically handle
	Tools.Copy(UserVars.root + "\\lib\\"+platform+"\\"+config+"\\scintilla.dll", targetdir+"\\lib\\"+platform+"\\"+config+"\\")

except Exception as ex:
	Tools.OnException(ex)