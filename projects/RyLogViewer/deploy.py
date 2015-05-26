#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# This script is setup to build projects in the RyLogViewer.sln file
# If you get errors, check that you can get this solution to build
# If projects have moved/changed/etc they'll need sorting in this solution

import sys, os, shutil, re
sys.path.append(re.sub(r"(\w:[\\/]).*", r"\1script", __file__))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"RylogViewer Deploy\n"
		"Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex, UserVars.msbuild, UserVars.ziptool, UserVars.devenv])

	wwwroot = r"Z:\www\rylogic.co.nz"
	installer_name = "RyLogViewerSetup.exe"
	dstdir = UserVars.root + "\\bin"
	symdir = UserVars.root + "\\local\\symbols"
	sln    = UserVars.root + "\\projects\\RylogViewer\\RylogViewer.sln"
	dst    = dstdir + "\\rylogviewer"
	sym    = symdir + "\\rylogviewer"
	config = "release"
	#config = input("Configuration (debug, release(default))? ")
	#if config == "": config = "release"

	srcdir     = UserVars.root + "\\projects\\rylogviewer"
	bindir     = srcdir + "\\bin\\" + config
	objdir     = srcdir + "\\obj\\" + config
	exampledir = srcdir + "\\plugins\\exampleplugin"

	#Build the docs
	def ExportDirectory(dir):
		for file in os.listdir(dir):
			filepath = dir + "\\" + file
			if re.match(r".*(?<!include)\.htm$",filepath, flags=re.IGNORECASE):
				print(filepath)
				outfile = re.sub(r"\.htm", r".html", filepath)
				Tools.Exec([UserVars.csex, "-expand_template", "-f", filepath, "-o", outfile])
	ExportDirectory(srcdir + r"\docs")
	ExportDirectory(srcdir + r"\Resources")

	#Invoke MSBuild
	print("Building...")
	projects = [
		"RyLogViewer",
		"RyLogViewer.Extensions",
		"RyLogViewer.ExamplePlugin",
		]
	platforms = [
		"Any CPU",
		]
	configs = [
		"release",
		"debug",
		]
	Tools.MSBuild(sln, projects, platforms, configs, False, False)

	#Ensure output directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	Tools.Copy(bindir + "\\rylogviewer.exe" , dst + "\\rylogviewer.exe")
	Tools.Copy(bindir + "\\rylogviewer.pdb" , sym + "\\rylogviewer.pdb")
	Tools.Copy(bindir + "\\rylogic.dll"     , dst + "\\rylogic.dll"  )
	Tools.Copy(bindir + "\\rylogic.pdb"     , sym + "\\rylogic.pdb"  )
	Tools.Copy(bindir + "\\docs"            , dst + "\\docs")
	Tools.Copy(bindir + "\\examples"        , dst + "\\examples")
	Tools.Copy(bindir + "\\plugins"         , dst + "\\plugins")

	#Create a zip of the dstdir
	dstzip = dst + ".zip"
	print("Creating zip file..." + dstzip)
	if os.path.exists(dstzip): os.unlink(dstzip)
	Tools.Exec([UserVars.ziptool, "a", dstzip, dst])

	#Build the installer
	Tools.Exec([UserVars.devenv, srcdir+"\\installer\\installer.vdproj", "/Build"])

	#Copy the installer to the web site
	installer = objdir + r"\setup.exe"
	if os.path.exists(installer):
		print("Copying RylogViewerSetup.exe to www...")
		Tools.Copy(installer, dstdir + "\\" + RyLogViewerSetup.exe)
		Tools.Copy(installer, wwwroot + "\\data\\" + installer_name)
	else:
		print("No installer produced")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
