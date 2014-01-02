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

srcdir = UserVars.root + "\\sdk\\pr\\prcs"
proj   = srcdir + "\\Rylogic.csproj"
config = "both" #input("Configuration (debug, release, both(default))? ")
if config == "": config = "both"

input(
	" Deploy Settings:\n"
	"         Source: " + srcdir + "\n"
	"  Configuration: " + config + "\n"
	"Press enter to continue")

try:
	#Invoke MSBuild
	print("Building...")

	if config == "debug" or config == "both":
		Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration=Debug"])

	if config == "release" or config == "both":
		Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration=Release"])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
