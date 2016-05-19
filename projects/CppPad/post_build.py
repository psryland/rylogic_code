#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# CppPad build step
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(ConfigurationName)
import sys, os, re, string
sys.path.append(re.sub(r"^(.*)\\projects\\.*", r"\1\\script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1);

	projdir   = sys.argv[1] if len(sys.argv) > 3 else UserVars.root + "\\projects\\CppPad"
	targetdir = sys.argv[2] if len(sys.argv) > 3 else UserVars.root + "\\projects\\CppPad\\bin\\Debug"
	config    = sys.argv[3] if len(sys.argv) > 3 else "Debug"

	# Ensure the 'lib' directory exists
	x64libs = targetdir+"\\lib\\x64\\"+config+"\\"
	x86libs = targetdir+"\\lib\\x86\\"+config+"\\"
	if not os.path.exists(targetdir): os.makedirs(targetdir)
	if not os.path.exists(x64libs): os.makedirs(x64libs)
	if not os.path.exists(x86libs): os.makedirs(x86libs)
	
	# Copy the support dlls
	# These are the native dlls that visual studio doesn't automatically handle
	Tools.Copy(UserVars.root + "\\lib\\x86\\"+config+"\\scintilla.dll", targetdir+"lib\\x86\\"+config+"\\")
	Tools.Copy(UserVars.root + "\\lib\\x64\\"+config+"\\scintilla.dll", targetdir+"lib\\x64\\"+config+"\\")

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))