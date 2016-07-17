#!/usr/bin/env python3
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
	Tools.AssertPathsExist([UserVars.root, UserVars.csex, UserVars.msbuild, UserVars.ziptool, UserVars.vs_dir])

	wwwroot = r"Z:\www\rylogic.co.nz"
	installer_name = "RyLogViewerSetup.exe"

	srcdir = UserVars.root + "\\projects\\RylogViewer"
	dstdir = UserVars.root + "\\bin"
	symdir = UserVars.root + "\\local\\symbols"

	dst    = dstdir + "\\rylogviewer"
	sym    = symdir + "\\rylogviewer"
	config = "release"
	#config = input("Configuration (debug, release(default))? ")
	#if config == "": config = "release"

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
		#"RyLogViewer.Extensions",
		#"RyLogViewer.ExamplePlugin",
		]
	configs = [
		"release",
		"debug",
		]
	platforms = [
		"Any CPU",
		]
	if not Tools.MSBuild(srcdir + "\\RyLogViewer.sln", projects, platforms, configs, False, True):
		Tools.OnError("Errors occurred")

	#Ensure output directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	target_dir = srcdir + "\\bin\\" + config
	Tools.Copy(target_dir + "\\RylogViewer.exe"            , dst + "\\")
	Tools.Copy(target_dir + "\\Rylogic.dll"                , dst + "\\")
	Tools.Copy(target_dir + "\\RylogViewer.Extensions.dll" , dst + "\\")
	Tools.Copy(target_dir + "\\docs"                       , dst + "\\")
	Tools.Copy(target_dir + "\\examples"                   , dst + "\\")
	Tools.Copy(target_dir + "\\plugins"                    , dst + "\\")

	#Create a zip of the destination directory
	dstzip = dst + ".zip"
	print("Creating zip file..." + dstzip)
	if os.path.exists(dstzip): os.unlink(dstzip)
	Tools.Exec([UserVars.ziptool, "a", dstzip, dst])

	#Build the installer
	Tools.Exec([UserVars.vs_dir + "\\Common7\\ide\\devenv.exe", srcdir+"\\installer\\installer.vdproj", "/Build"])

	#Copy the installer to the web site
	installer = srcdir + "\\obj\\" + config + r"\setup.exe"
	if os.path.exists(installer):
		print("Copying RylogViewerSetup.exe to www...")
		Tools.Copy(installer, dstdir + "\\" + RyLogViewerSetup.exe)
		Tools.Copy(installer, wwwroot + "\\data\\" + installer_name)
	else:
		print("No installer produced")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
