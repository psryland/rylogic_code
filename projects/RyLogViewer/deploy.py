#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append("../../script")
import Rylogic as Tools
import UserVars

print(
	"*************************************************************************\n"
	"RylogViewer Deploy\n"
	"Copyright © Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

srcdir = UserVars.pr_root + "\\projects\\rylogviewer"
dstdir = UserVars.pr_root + "\\bin"
symdir = UserVars.pr_root + "\\local\\symbols"
proj   = srcdir + "\\RylogViewer.sln"
dst    = dstdir + "\\rylogviewer"
sym    = symdir + "\\rylogviewer"
config = input("Configuration (debug, release(default))? ")
if config == "": config = "release"
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
	print("Building the exe...")
	Tools.Exec([UserVars.msbuild, proj, "/t:RyLogViewer:Rebuild", "/p:Configuration="+config])

	#Ensure directories exist and are empty
	if os.path.exists(dst): shutil.rmtree(dst)
	if os.path.exists(sym): shutil.rmtree(sym)
	os.makedirs(dst)
	os.makedirs(sym)

	#Copy build products to dst
	print("Copying files to " + dst)
	Tools.Copy(bindir + "\\rylogviewer.exe", dst + "\\rylogviewer.exe")
	Tools.Copy(bindir + "\\rylogviewer.pdb", sym + "\\rylogviewer.pdb")
	Tools.Copy(bindir + "\\pr.dll"         , dst + "\\pr.dll"  )
	Tools.Copy(bindir + "\\pr.pdb"         , sym + "\\pr.pdb"  )
	Tools.Copy(bindir + "\\docs"           , dst + "\\docs")
	Tools.Copy(bindir + "\\examples"       , dst + "\\examples")
	Tools.Copy(bindir + "\\plugins"        , dst + "\\plugins")

	#Create a zip of the dstdir
	dstzip = dst + ".zip"
	print("Creating zip file..." + dstzip)
	if os.path.exists(dstzip): os.unlink(dstzip)
	Tools.Exec([UserVars.ziptool, "a", dstzip, dst])

	#Copy the installer to the web site
	installer = srcdir + r"\setup\setup\express\singleimage\diskimages\disk1\setup.exe"
	if os.path.exists(installer):
		print("Copying RylogViewerSetup.exe to www...")
		Tools.Copy(installer, UserVars.wwwroot + r"\data\RylogViewerSetup.exe")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
