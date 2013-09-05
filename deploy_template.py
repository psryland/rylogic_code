#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, subprocess, shutil

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
RylogicEnv.CheckVersion(1)

#srcdir = RylogicEnv.qdrive + r"\projects\Csex"
#dstdir = RylogicEnv.qdrive + r"\bin"
#symdir = RylogicEnv.qdrive + r"\local\symbols"
#proj   = srcdir + r"\Csex_vs2012.csproj"
#config = "release"
#dst    = dstdir + r"\csex"
#sym    = symdir + r"\csex"
#bindir = srcdir + r"\bin\" + config

print(
	"*************************************************************************\n"
	"  Whatever Deploy\n"
	"    Copyright © Rylogic Limited 2013\n"
	"\n"
	"         Source: " + bindir + "\n"
	"    Destination: " + dst + "\n"
	"  Configuration: " + config + "\n"
	"*************************************************************************")
input()

try:
	#Invoke MSBuild
	print("Building the exe...")
	RylogicEnv.Run(RylogicEnv.msbuild,'"'+proj+'" /p:Configuration='+config+';Platform=AnyCPU')

	#Ensure directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	RylogicEnv.Copy(bindir + r"\csex.exe", dst + r"\csex.exe")
	RylogicEnv.Copy(bindir + r"\csex.pdb", sym + r"\csex.pdb")
	RylogicEnv.Copy(bindir + r"\pr.dll"  , dst + r"\pr.dll"  )
	RylogicEnv.Copy(bindir + r"\pr.pdb"  , sym + r"\pr.pdb"  )

	RylogicEnv.OnSuccess()

except Exception as ex:
	print("Error: " + ex)
	RylogicEnv.OnError()

