#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
RylogicEnv.CheckVersion(1)

dstdir = RylogicEnv.qdrive + "\\bin"
srcdir = RylogicEnv.qdrive + "\\projects\\Csex"
symdir = RylogicEnv.qdrive + "\\local\\symbols"
proj   = srcdir + "\\Csex_vs2012.csproj"
config = "release"
dst    = dstdir + "\\csex"
sym    = symdir + "\\csex"
bindir = srcdir + "\\bin\\" + config

print(
	"*************************************************************************\n"
	"  Csex Deploy\n"
	"    Copyright © Rylogic Limited 2013\n"
	"\n"
	"    Destination: " + dst + "\n"
	"  Configuration: " + config + "\n"
	"*************************************************************************")
input()

try:
	#Invoke MSBuild
	print("Building the exe...")
	if subprocess.call('"'+RylogicEnv.msbuild+'" "'+proj+'" /p:Configuration='+config+';Platform=AnyCPU') != 0:
		RylogicEnv.OnError()

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

