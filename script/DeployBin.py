#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Post Build Event for exporting binary files
# Use:
#   $(ProjectDir)..\..\script\DeployBin.py $(TargetPath) $(PlatformTarget) $(Configuration) [dstsubdir]
import sys, os
import Rylogic as Tools
import UserVars

# Deploy a single binary to the bin folder
def DeployBin(targetpath:str, platform:str, config:str, dstsubdir:str):

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	if platform.lower() == "win32": platform = "x86"
	targetpath  = os.path.abspath(targetpath) # don't change the filename case
	srcdir,file = os.path.split(targetpath)
	fname,extn  = os.path.splitext(file)
	dstsubdir   = dstsubdir.lower() if dstsubdir != "" else fname
	platform    = platform.lower()
	config      = config.lower()
	dstdir      = UserVars.root + "\\bin\\" + dstsubdir + "\\" + platform

	# Set the output directory and ensure it exists
	os.makedirs(dstdir, exist_ok=True)

	# Only publish release builds
	if config == "release":
		# Copy the binary to the bin folder
		Tools.Copy(targetpath, dstdir + "\\" + file)
	
		# If the system architecture matches this release, copy to the root 'dstdir'
		if platform == UserVars.arch:
			Tools.Copy(dstdir + "\\" + file, UserVars.root + "\\bin\\" + file)

# Run as standalone script
if __name__ == "__main__":
	try:
		targetpath = sys.argv[1] if len(sys.argv) > 1 else input("TargetPath? ")
		platform   = sys.argv[2] if len(sys.argv) > 2 else input("Platform (x86,x64)? ")
		config     = sys.argv[3] if len(sys.argv) > 3 else input("Configuration (debug,release)? ")
		dstsubdir  = sys.argv[4] if len(sys.argv) > 4 else ""

		DeployBin(targetpath, platform, config, dstsubdir)

	except Exception as ex:
		Tools.OnException(ex)
