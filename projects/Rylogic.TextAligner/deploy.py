#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
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

	dstdir    = os.path.join(UserVars.root, "bin")
	srcdir    = os.path.join(UserVars.root, "projects", "Rylogic.TextAligner")
	manifest  = os.path.join(srcdir, "source.extension.vsixmanifest")
	assinfo   = os.path.join(srcdir, "Properties", "AssemblyInfo.cs")
	vsix_name = "Rylogic.TextAligner.vsix"

	# Check the manifest version
	version_regex = r'Id="DF402917-6013-40CA-A4C6-E1640DA86B90" Version="(?P<version>.*?)"'
	match_version = Tools.Extract(manifest, version_regex)
	if not match_version:
		Tools.OnError("ERROR: failed to extract version number from:\r\n " + manifest)

	# Prompt for a new version
	version = input(f"Current version is: {match_version.group('version')}\r\nEnter version number (default is unchanged): ")
	if version == "": version = match_version.group("version")
	version_sub  = f"Id=\"DF402917-6013-40CA-A4C6-E1640DA86B90\" Version=\"{version}\""

	# Check version is greater than the last released version (update this after a release)
	min_released_version = "1.07"
	if version <= min_released_version:
		Tools.OnError("ERROR: Can't use that version number, it's less than what's already been released")

	input(
		"Deploy Settings:\n"
		"         Source: " + srcdir + "\n"
		"    Destination: " + dstdir + "\n"
		"        Version: " + version + "\n"
		"Press enter to continue")

	# Update the manifest file
	print("Updating version...")
	Tools.UpdateFileByLine(manifest, version_regex, version_sub)
	Tools.UpdateFileByLine(assinfo, "AssemblyVersion\\(\"(.*?)\"\\)", f"AssemblyVersion(\"{version}\")")
	Tools.UpdateFileByLine(assinfo, "AssemblyFileVersion\\(\"(.*?)\"\\)", f"AssemblyFileVersion(\"{version}\")")

	# Invoke MSBuild
	print("Building...")
	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")
	Tools.MSBuild(sln, ["VSExtensions\\Rylogic.TextAligner"], ["Any CPU"], ["Release"])

	# Copy build products to dst
	print(f"Copying files to: {dstdir}")
	Tools.Copy(os.path.join(srcdir, "bin", "Release", vsix_name), dstdir)

	# Sign the vsix
	do_sign = input("Sign the VSIX (default is no)? ")
	if do_sign == "y":
		pw = input("Password for 'Rylogic.pfx': ")
		# Use dotnet tool install -g OpenVsixSignTool
		# then:
		# openvsixsigntool sign --sha1 <thumb print of code signing cert> -fd sha1 P:\pr\bin\Rylogic.TextAligner.vsix
		#Tools.Exec([UserVars.vsixsigntool, "sign", "/f", os.path.join(srcdir, "..", "Rylogic.pfx"), "/sha1", "becacfb644f8e6796ca53235270df64c887ce9ff", "/p", pw, "/fd", "sha1", os.path.join(dstdir, vsix_name)])

	#Ask to install
	dstfile = os.path.join(dstdir, vsix_name)
	install = input(f"Install {dstfile} (y/n)? ")
	if install == 'y':
		try:
			print("Uninstalling previous versions...")
			Tools.Exec(["cmd", "/C", os.path.join(UserVars.vs_dir, "Common7", "IDE", "VSIXInstaller.exe"), "/q", "/a", f"/u:{vsix_name}"])
		except: pass
		print("Installing...")
		Tools.Exec(["cmd", "/C", dstfile])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
