#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\..\script")
import Rylogic as Tools
import UserVars

print(
	"*************************************************************************\n"
	"Rylogic Class Library Deploy\n"
	"Copyright Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

srcdir = UserVars.pr_root + "\\sdk\\pr\\prcs"
proj   = srcdir + "\\Rylogic.csproj"
config = input("Configuration (debug, release(default))? ")
if config == "": config = "release"
bindir = srcdir + "\\bin\\" + config

input(
	" Deploy Settings:\n"
	"         Source: " + bindir + "\n"
	#"    Destination: " + dst + "\n"
	"  Configuration: " + config + "\n"
	"Press enter to continue")

try:
	#Invoke MSBuild
	print("Building...")
	Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration="+config])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
