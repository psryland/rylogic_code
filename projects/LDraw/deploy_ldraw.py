#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#

import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		" LDraw Deploy\n"
		"  Copyright Rylogic Limited 2002\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex])

	srcdir = UserVars.root + "\\projects\\LDraw"
	dstdir = UserVars.root + "\\bin\\ldraw"
	sln    = UserVars.root + "\\build\\Rylogic.sln"

	# Ensure output directories exist and are empty
	Tools.ShellDelete(dstdir)
	os.makedirs(dstdir)

	# Build the project
	print("Building...")
	Tools.MSBuild(sln, ["scintilla", "renderer11", "view3d"], ["x86","x64"], ["release"], False, True)
	Tools.MSBuild(sln, ["LDraw"], ["Any CPU"], ["release"], False, True)

	# Copy build products to the output directory
	print("Copying files to " + dstdir)
	target_dir = srcdir + "\\bin\\Release"
	Tools.Copy(target_dir + "\\LDraw.exe"   , dstdir + "\\")
	Tools.Copy(target_dir + "\\Rylogic.dll" , dstdir + "\\")
	Tools.Copy(target_dir + "\\lib"         , dstdir + "\\lib")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
