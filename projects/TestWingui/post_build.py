#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Post build step
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(PlatformTarget) $(ConfigurationName)
import sys, os, string
sys.path.append(r"P:\script")
import Rylogic as Tools
import UserVars

try:
	projdir   = sys.argv[1] if len(sys.argv) > 1 else input("projdir? ")
	targetdir = sys.argv[2] if len(sys.argv) > 2 else input("targetdir? ")
	platform  = sys.argv[3] if len(sys.argv) > 3 else input("platform? ")
	config    = sys.argv[4] if len(sys.argv) > 4 else input("config? ")
	#projdir   = "C:\\Users\\Paul\\Documents\\Visual Studio 2013\\Projects\\Win32Project1\\"
	#targetdir = "C:\\Users\\Paul\\Documents\\Visual Studio 2013\\Projects\\Win32Project1\\Debug\\"
	#platform  = "x86"
	#config    = "Debug"

	# Copy the support dlls for both platforms
	Tools.Copy(UserVars.root+"\\lib\\"+platform+"\\"+config+"\\view3d.dll", targetdir, only_if_modified=True)
	Tools.Copy(UserVars.root+"\\lib\\"+platform+"\\"+config+"\\scintilla.dll", targetdir, only_if_modified=True)
	
except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))