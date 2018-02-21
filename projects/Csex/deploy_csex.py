#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"  Csex Deploy\n"
		"    Copyright (c) Rylogic 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.msbuild])

	dstdir = UserVars.root + "\\bin"
	srcdir = UserVars.root + "\\projects\\Csex"
	proj   = srcdir + "\\Csex.csproj"
	config = "release"
	dst    = dstdir + "\\csex"
	bindir = srcdir + "\\bin\\" + config

	input(
		"Deploy Settings:\n"
		"    Destination: " + dst + "\n"
		"  Configuration: " + config + "\n"
		"Press enter to continue")

	#Invoke MSBuild
	print("Building the exe...")
	Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration="+config+";Platform=AnyCPU"])

	#Ensure directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	os.makedirs(dst)

	#Copy build products to dst
	print("Copying files to " + dst)
	Tools.Copy(bindir + r"\csex.exe"    , dst)
	Tools.Copy(bindir + r"\rylogic.dll" , dst)
	
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
