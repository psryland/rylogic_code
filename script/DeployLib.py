#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

# Post Build Event for exporting library files to a directory
# Use:
#   DeployLib $(TargetPath) $(PlatformTarget) $(Configuration) [altdstdir, ...]
# 
# This will copy mylib.lib (or dll) to "<dstdir>\platform\config\mylib.lib
# and optionally the pdb as well if it exists.
# 'altdstdir' is an optional array of alternate directories to copy the binaries to.
# Note: 
#  pdb files are associated with the file name at the time they are build so it is
#  not possible to rename the lib and pdb. 
import sys, os
import Rylogic as Tools
import UserVars

# Deploy a single binary to the \pr\lib folder
def DeployLib(targetpath:str, platform:str, config:str, altdstdir=[]):

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	if platform.lower() == "win32": platform = "x86"
	targetpath  = os.path.abspath(targetpath) # don't change the filename case
	platform    = platform.lower()
	config      = config.lower()
	srcdir,file = os.path.split(targetpath)
	fname,extn  = os.path.splitext(file)

	# Get the destination directories
	dstdirs = (
		[os.path.join(UserVars.root, "lib", platform, config)] +
		[os.path.join(d.rstrip("/\\"), platform, config) for d in altdstdir])

	# Copy the library file to the lib folder.
	for dir in dstdirs:
		# Trim .lib or .dll from the file title
		outname = fname[:-4] if fname.lower().endswith(".lib") or fname.lower().endswith(".dll") else fname
		Tools.Copy(targetpath, os.path.join(dir, outname + extn))

		# If the lib is a dll, look for an import library and copy that too, if it exists
		if extn == ".dll" and os.path.exists(os.path.join(srcdir, fname + ".imp")):
			Tools.Copy(os.path.join(srcdir, fname + ".imp"), os.path.join(dir, outname + ".imp"))

# Run as standalone script
if __name__ == "__main__":
	try:
		#sys.argv = ["", "P:\\pr\\obj\\v142\\audio.dll\\x64\\Debug\\audio.dll.dll", "x64", "Debug"]
		targetpath = sys.argv[1] if len(sys.argv) > 1 else input("TargetPath? ")
		platform   = sys.argv[2] if len(sys.argv) > 2 else input("Platform (x86,x64)? ")
		config     = sys.argv[3] if len(sys.argv) > 3 else input("Configuration (debug,release)? ")
		altdstdir  = sys.argv[4:] if len(sys.argv) > 4 else []

		DeployLib(targetpath, platform, config, altdstdir)

	except Exception as ex:
		Tools.OnException(ex)
