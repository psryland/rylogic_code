#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, re, shutil
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import RylogicEnv as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"  Whatever Deploy\n"
		"    Copyright (c) Rylogic 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	#srcdir = UserVars.root + r"\projects\Csex"
	#dstdir = UserVars.root + r"\bin"
	#symdir = UserVars.root + r"\local\symbols"
	#sln_or_proj = srcdir + r"\Csex_vs2012.csproj"
	#config = input("Configuration (debug, release)? ")
	#dst    = dstdir + r"\csex"
	#sym    = symdir + r"\csex"
	#bindir = srcdir + r"\bin\" + config

	input(
		"Deploy settings:\n"
		"         Source: " + bindir + "\n"
		"    Destination: " + dst + "\n"
		"  Configuration: " + config + "\n"
		"Press enter to continue")

	#Invoke MSBuild
	# To build projects within a solution use this format:
	#  /t:"solution_folder\project_name:Rebuild";"solution_folder\project_name2:Clean";"project_name3"
	#  separate projects with ';'
	#  the ':Rebuild' is optional after the project name
	#  projects with dots in the name should have the dots replaced with underscores
	print("Building...")
	Tools.MSBuild(sln, ["proj1", "proj2",], ["x86","x64"], ["release"], False, True)
	Tools.MSBuild(sln, ["proj3"], ["Any CPU"], ["release"], False, True)
	Tools.Exec([UserVars.msbuild, sln_or_proj, "/t:MyProject", "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"])

	#Ensure directories exist and are empty
	Tools.ShellDelete(dst)
	Tools.ShellDelete(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	Tools.Copy(bindir + r"\csex.exe", dst + r"\csex.exe")
	Tools.Copy(bindir + r"\csex.pdb", sym + r"\csex.pdb")
	Tools.Copy(bindir + r"\Rylogic.dll"  , dst + r"\Rylogic.dll"  )
	Tools.Copy(bindir + r"\Rylogic.pdb"  , sym + r"\Rylogic.pdb"  )

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
