#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
import Rylogic as Tools
import UserVars

print(
	"*************************************************************************\n"
	"  Rylogic.VSExtension Deploy\n"
	"    Copyright Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

dstdir = UserVars.pr_root + "\\bin"
srcdir = UserVars.pr_root + "\\projects\\Rylogic.VSExtension"
proj   = srcdir + "\\Rylogic.VSExtension.sln"
config = "release"
bindir = srcdir + "\\bin\\" + config

input("Remembered to increase the value of the number in the Version field in the manifest?")

input(
	"Deploy Settings:\n"
	"         Source: " + srcdir + "\n"
	"    Destination: " + dstdir + "\n"
	"  Configuration: " + config + "\n"
	"Press enter to continue")

try:
	#Invoke MSBuild
	print("Building...")
	Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration="+config, "/p:Platform=Any CPU"])

	#Copy build products to dst
	print("Copying files to " + dstdir)
	Tools.Copy(bindir + "\\Rylogic.VSExtension.vsix", dstdir)
	
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
