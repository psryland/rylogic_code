#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
from pr import UserVars

print(
	"*************************************************************************\n"
	"RylogViewer Deploy\n"
	"Copyright © Rylogic Limited 2013\n"
	"*************************************************************************")

RylogicEnv.CheckVersion(1)

srcdir = UserVars.pr_root + "\\projects\\rylogviewer"
dstdir = UserVars.pr_root + "\\bin"
symdir = UserVars.pr_root + "\\local\\symbols"
proj   = srcdir + "\\RylogViewer.sln"
dst    = dstdir + "\\rylogviewer"
sym    = symdir + "\\rylogviewer"
config = input("Configuration (debug, release)? ")
bindir = srcdir + "\\bin\\" + config

input(
	" Deploy Settings:\n"
	"         Source: " + bindir + "\n"
	"    Destination: " + dst + "\n"
	"  Configuration: " + config + "\n"
	"Press enter to continue")

try:
	confirm = input(
		"Is there an 'Upgrade Path' in the setup project and have you changed the Product GUID and version?\n"
		"This is needed so that the new installer will replace the existing installation if there.\n"
		"(y/n):")
	if confirm != "y":
		RylogicEnv.OnError()

	#Invoke MSBuild
	print("Building the exe...")
	RylogicEnv.Exec([UserVars.msbuild, proj, "/p:Configuration=" + config, "/t:Build"])

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
	dstzip = dstdir + ".zip"
	print("Creating zip file..." + dstzip)
	if os.path.exists(dstzip): os.unlink(dstzip)
	RylogicEnv.Exec([UserVars.ziptool, "a", dstzip, dstdir])

	#Copy the installer to the web site
	installer = srcdir + r"\setup\setup\express\singleimage\diskimages\disk1\setup.exe"
	if os.path.exists(installer):
		print("Copying RylogViewerSetup.exe to www...")
		RylogicEnv.Copy(installer, UserVars.wwwroot + r"\data\RylogViewerSetup.exe")

	RylogicEnv.OnSuccess()

except Exception as ex:
	RylogicEnv.OnError("Error: " + str(ex))
