#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# This script is set up to build projects in the RyLogViewer.sln file
# If you get errors, check that you can get this solution to build
# If projects have moved/changed/etc they'll need sorting in this solution

import sys, os, shutil, re
sys.path.append(re.sub(r"(\w:[\\/]).*", r"\1script", __file__))
import Rylogic as Tools
import UserVars

def Deploy():
	print(
		"*************************************************************************\n"
		"RylogViewer Deploy\n"
		"Copyright (C) Rylogic Limited 2013\n"
		"*************************************************************************")

	# Check tools paths are valid
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.csex, UserVars.msbuild, UserVars.ziptool, UserVars.vs_dir])

	# Paths
	srcdir = UserVars.root + "\\projects\\RylogViewer"
	dstdir = UserVars.root + "\\bin"



	#sln    = slndir + "\\RyLogViewer.sln"
	#dst    = dstdir + "\\rylogviewer"
	#sym    = symdir + "\\rylogviewer"
	#config = "release"
	#config = input("Configuration (debug, release(default))? ")
	#if config == "": config = "release"

	#wwwroot = r"Z:\www\rylogic.co.nz"
	#installer_name = "RyLogViewerSetup.exe"

	# Get the version number
	version = Tools.Extract(srcdir + "\\Properties\\AssemblyVersion.cs", "AssemblyVersion\(\"(.*)\"\)").group(1)

	# Ensure output directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	os.makedirs(dst)

	# Build the html docs
	def ExportDocumentation(dir):
		for file in os.listdir(dir):
			filepath = dir + "\\" + file
			if re.match(r".*(?<!include)\.htm$",filepath, flags=re.IGNORECASE):
				print(filepath)
				outfile = re.sub(r"\.htm", r".html", filepath)
				Tools.Exec([UserVars.csex, "-expand_template", "-f", filepath, "-o", outfile])
	
		return
	ExportDocumentation(srcdir + "\\docs")
	ExportDocumentation(srcdir + "\\Resources")

	#Invoke MSBuild
	print("Building...")
	platforms = ["Any CPU"]
	projects = ["RyLogViewer", "RyLogViewer.Extensions", "RyLogViewer.ExamplePlugin"]
	configs = ["release","debug"]
	Tools.MSBuild(sln, projects, platforms, configs, False, True)

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

	return


# Run as standalone script
if __name__ == "__main__":
	try:
		Deploy()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)
