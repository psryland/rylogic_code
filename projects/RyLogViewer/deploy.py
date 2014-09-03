#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"RylogViewer Deploy\n"
		"Copyright Rylogic Limited 2013\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	installer_name = "RyLogViewerSetup.exe"
	srcdir = UserVars.root + "\\projects\\rylogviewer"
	dstdir = UserVars.root + "\\bin"
	symdir = UserVars.root + "\\local\\symbols"
	sln    = srcdir + "\\RylogViewer.sln"
	dst    = dstdir + "\\rylogviewer"
	sym    = symdir + "\\rylogviewer"
	config = input("Configuration (debug, release(default))? ")
	if config == "": config = "release"

	bindir     = srcdir + "\\bin\\" + config
	objdir     = srcdir + "\\obj\\" + config
	extndll    = srcdir + "\\extensionapi\\bin\\"+config+"\\RyLogViewerExtensions.dll"
	exampledir = srcdir + "\\plugins\\exampleplugin"

	input(
		" Deploy Settings:\n"
		"         Source: " + bindir + "\n"
		"    Destination: " + dst + "\n"
		"  Configuration: " + config + "\n"
		"Press enter to continue")

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
	projects = [
		"RyLogViewer",
		"RyLogViewerExtensions",
		"ExamplePlugin",
		]
	projs = ";".join(projects)
	platform = "Any CPU"
	print("Building the exe...")
	Tools.Exec([UserVars.msbuild, UserVars.msbuild_props, sln, "/t:"+projs, "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"])

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
	Tools.Copy(extndll                      , dst + "\\plugins")
	Tools.Copy(

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
		Tools.Copy(installer, UserVars.wwwroot + "\\data\\" + installer_name)
	else:
		print("No installer produced")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
