﻿#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
import Rylogic as Tools
import UserVars

print(
	"*************************************************************************\n"
	"  Rylogic.VSExtension Deploy\n"
	"    Copyright Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

dstdir = UserVars.pr_root + "\\bin"
srcdir = UserVars.pr_root + "\\projects\\Rylogic.VSExtension"
manifest = srcdir + "\\source.extension.vsixmanifest"
proj   = srcdir + "\\Rylogic.VSExtension.sln"
config = "release"
bindir = srcdir + "\\bin\\" + config

vers_patn = r'Id="1d697591-233a-4a5b-bf85-2fccc769dfe3" Version="(?P<vers>.*?)"'
match_version = Tools.Extract(manifest, vers_patn)
if match_version == None: Tools.OnError("ERROR: failed to extract version number from:\r\n " + manifest)
version = input("Current version is: " + match_version.group("vers") + "\r\nEnter version number (default is unchanged): ")
if version == "": version = match_version.group("vers")
vers_sub  = r'Id="1d697591-233a-4a5b-bf85-2fccc769dfe3" Version="'+version+'"'
min_released_version = "0.99"
if version <= min_released_version: Tools.OnError("ERROR: Can't use that version number, it's less than what's already been released");

input(
	"Deploy Settings:\n"
	"         Source: " + srcdir + "\n"
	"    Destination: " + dstdir + "\n"
	"  Configuration: " + config + "\n"
	"        Version: " + version + "\n"
	"Press enter to continue")

try:
	# Update the manifest file
	print("Updating version...")
	Tools.UpdateFile(manifest, vers_patn, vers_sub)

	#Invoke MSBuild
	print("Building...")
	Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration="+config, "/p:Platform=Any CPU"])

	#Copy build products to dst
	print("Copying files to " + dstdir)
	Tools.Copy(bindir + "\\Rylogic.VSExtension.vsix", dstdir)

	#Ask to install
	dstfile = dstdir + "\\Rylogic.VSExtension.vsix"
	install = input("Install " + dstfile + " (y/n)? ")
	if install == 'y':
		Tools.Exec(["cmd", "/C", dstfile])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))