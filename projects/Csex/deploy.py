#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
import Rylogic as Tools
import UserVars

print(
	"*************************************************************************\n"
	"  Csex Deploy\n"
	"    Copyright (C) Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

dstdir = UserVars.root + "\\bin"
srcdir = UserVars.root + "\\projects\\Csex"
symdir = UserVars.root + "\\local\\symbols"
proj   = srcdir + "\\Csex_vs2012.csproj"
config = "release"
dst    = dstdir + "\\csex"
sym    = symdir + "\\csex"
bindir = srcdir + "\\bin\\" + config

input(
	"Deploy Settings:\n"
	"    Destination: " + dst + "\n"
	"  Configuration: " + config + "\n"
	"Press enter to continue")

try:
	#Invoke MSBuild
	print("Building the exe...")
	Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration="+config+";Platform=AnyCPU"])

	#Ensure directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	Tools.Copy(bindir + r"\csex.exe"    , dst)
	Tools.Copy(bindir + r"\rylogic.dll" , dst)
	
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
