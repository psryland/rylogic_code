#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\script")
import RylogicEnv as Tools
import UserVars

print(
	"*************************************************************************\n"
	"  Whatever Deploy\n"
	"    Copyright Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

#srcdir = UserVars.root + r"\projects\Csex"
#dstdir = UserVars.root + r"\bin"
#symdir = UserVars.root + r"\local\symbols"
#proj   = srcdir + r"\Csex_vs2012.csproj"
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

try:
	#Invoke MSBuild
	print("Building the exe...")
	Tools.Exec([UserVars.msbuild, proj, "/t:MyProject:Rebuild", "/p:Configuration="+config+";Platform="+platform])

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
