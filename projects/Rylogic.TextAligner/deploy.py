#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"  Rylogic.TextAligner Deploy\n"
		"    Copyright (c) Rylogic 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.msbuild])

	dstdir = UserVars.root + "\\bin"
	srcdir = UserVars.root + "\\projects\\Rylogic.TextAligner"
	manifest = srcdir + "\\source.extension.vsixmanifest"
	bindir = srcdir + "\\bin\\Release"
	extn_name = "Rylogic.TextAligner.vsix"

	vers_patn = r'Id="DF402917-6013-40CA-A4C6-E1640DA86B90" Version="(?P<vers>.*?)"'
	match_version = Tools.Extract(manifest, vers_patn)
	if match_version == None: Tools.OnError("ERROR: failed to extract version number from:\r\n " + manifest)
	version = input("Current version is: " + match_version.group("vers") + "\r\nEnter version number (default is unchanged): ")
	if version == "": version = match_version.group("vers")
	vers_sub  = r'Id="DF402917-6013-40CA-A4C6-E1640DA86B90" Version="'+version+'"'
	min_released_version = "1.07"
	if version <= min_released_version: Tools.OnError("ERROR: Can't use that version number, it's less than what's already been released");

	input(
		"Deploy Settings:\n"
		"         Source: " + srcdir + "\n"
		"    Destination: " + dstdir + "\n"
		"        Version: " + version + "\n"
		"Press enter to continue")

	# Update the manifest file
	print("Updating version...")
	Tools.UpdateFileByLine(manifest, vers_patn, vers_sub)

	#Invoke MSBuild
	print("Building...")
	Tools.MSBuild(UserVars.rylogic_sln, ["VSExtensions\\Rylogic.TextAligner"], ["Any CPU"], ["Release"])

	#Copy build products to dst
	print("Copying files to " + dstdir)
	Tools.Copy(bindir + "\\" + extn_name, dstdir)

	#Ask to install
	dstfile = dstdir + "\\" + extn_name
	install = input("Install " + dstfile + " (y/n)? ")
	if install == 'y':
		try:
			print("Uninstalling previous versions...")
			Tools.Exec(["cmd", "/C", UserVars.vs_dir + "\\Common7\\IDE\\VSIXInstaller.exe", "/q", "/a", "/u:"+extn_name])
		except: pass
		print("Installing...")
		Tools.Exec(["cmd", "/C", dstfile])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
