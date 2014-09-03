#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\script")) # include path relative to this file
import RylogicEnv as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"  Whatever Deploy\n"
		"    Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

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
	print("Building the exe...")
	Tools.Exec([UserVars.msbuild, UserVars.msbuild_props, sln_or_proj, "/t:MyProject", "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"])

	#Ensure directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	Tools.Copy(bindir + r"\csex.exe", dst + r"\csex.exe")
	Tools.Copy(bindir + r"\csex.pdb", sym + r"\csex.pdb")
	Tools.Copy(bindir + r"\pr.dll"  , dst + r"\pr.dll"  )
	Tools.Copy(bindir + r"\pr.pdb"  , sym + r"\pr.pdb"  )

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
