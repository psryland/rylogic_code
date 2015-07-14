#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Post Build Event for exporting binary files
# Use:
#   $(ProjectDir)..\..\script\DeployBin.py $(TargetPath) $(PlatformTarget) $(Configuration) [dstsubdir]
import sys, os
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	targetpath = sys.argv[1] if len(sys.argv) > 1 else input("TargetPath? ")
	platform   = sys.argv[2] if len(sys.argv) > 2 else input("Platform (x86,x64)? ")
	config     = sys.argv[3] if len(sys.argv) > 3 else input("Configuration (debug,release)? ")
	dstsubdir  = sys.argv[4] if len(sys.argv) > 4 else ""
	if platform.lower() == "win32": platform = "x86"

	targetpath  = targetpath.lower()
	platform    = platform.lower()
	config      = config.lower()
	dstsubdir   = dstsubdir.lower()
	srcdir,file = os.path.split(targetpath)
	fname,extn  = os.path.splitext(file)

	# Default to a subdir matching the target filename
	if dstsubdir == "":
		dstsubdir = fname

	# Set the output directory and ensure it exists
	dstdirroot = UserVars.root + r"\bin"
	dstdir = dstdirroot + "\\" + dstsubdir + "\\" + platform
	if not os.path.exists(dstdir): os.makedirs(dstdir)

	# Only publish release builds
	if config == "release":
		# Copy the binary to the bin folder
		Tools.Copy(targetpath, dstdir + "\\" + file)
		
		# If the system architecture matches this release, copy to the root dstdir
		if platform == UserVars.arch:
			Tools.Copy(dstdir + "\\" + file, dstdirroot + "\\" + file)

except Exception as ex:
	Tools.OnException(ex)
