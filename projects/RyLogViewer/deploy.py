#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, subprocess, shutil

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
RylogicEnv.CheckVersion(1)

confirm = input(
	"Is there an 'Upgrade Path' in the setup project and have you changed the Product GUID and version?\n"
	"This is needed so that the new installer will replace the existing installation if there.\n"
	"(y/n):")
if confirm != "y":
	RylogicEnv.OnError()

config = input("Configuration (debug, release):")

srcdir = RylogicEnv.qdrive + "\\projects\\rylogviewer"
dstdir = RylogicEnv.qdrive + "\\bin"
symdir = RylogicEnv.qdrive + "\\local\\symbols"
proj   = srcdir + "\\RylogViewer.sln"
dst    = dstdir + "\\rylogviewer"
sym    = symdir + "\\rylogviewer"
bindir = srcdir + "\\bin\\" + config

print(
	"*************************************************************************\n"
	"RylogViewer Deploy\n"
	"Copyright © Rylogic Limited 2013\n"
	"\n"
	"         Source: " + bindir + "\n"
	"    Destination: " + dst + "\n"
	"  Configuration: " + config + "\n"
	"*************************************************************************")
input()

try:
	#Invoke MSBuild
	print("Building the exe...")
	RylogicEnv.Run(RylogicEnv.msbuild,'"'+proj+'" /p:Configuration='+config+' /t:Build')

	#Ensure directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	RylogicEnv.Copy(bindir + "\\rylogviewer.exe", dst + "\\rylogviewer.exe")
	RylogicEnv.Copy(bindir + "\\rylogviewer.pdb", sym + "\\rylogviewer.pdb")
	RylogicEnv.Copy(bindir + "\\pr.dll"         , dst + "\\pr.dll"  )
	RylogicEnv.Copy(bindir + "\\pr.pdb"         , sym + "\\pr.pdb"  )

	#Create a zip of the dstdir
	print("Creating zip file..." + dstdir + r".zip")
	if os.path.exists(dstdir + r".zip"): os.unlink(dstdir + r".zip")
	RylogicEnv.Run(RylogicEnv.zip,'a "'+dstdir+'.zip" "'+dstdir+'"')

	#Copy the installer to the web site
	if os.path.exists(srcdir + r"\setup\setup\express\singleimage\diskimages\disk1\setup.exe"):
		print("Copying RylogViewerSetup.exe to www...")
		RylogicEnv.Copy(srcdir + r"\setup\setup\express\singleimage\diskimages\disk1\setup.exe", RylogicEnv.wwwroot + r"\data\RylogViewerSetup.exe")

	RylogicEnv.OnSuccess()

except Exception as ex:
	print("Error: " + ex)
	RylogicEnv.OnError()
