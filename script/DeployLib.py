#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

# Post Build Event for exporting library files to a directory
# Use:
#   DeployLib $(TargetPath) $(Platform) $(Configuration) [dstdir]
# 
# This will copy mylib.lib (or dll) to "<dstdir>\platform\config\mylib.lib
# and optionally the pdb as well if it exists.
# 'dstdir' is optional, if not given it will default to root+"\lib"
# Note: 
#  pdb files are associated with the file name at the time they are build so it is
#  not possible to rename the lib and pdb. 
import sys, os
import Rylogic as Tools
import UserVars

# Deploy a single binary to the \PC\Lib folder
def DeployLib(targetpath:str, platform:str, config:str, dstdir:str=""):

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	dstdir = dstdir if dstdir != "" else UserVars.root + "\\lib"
	if platform.lower() == "win32": platform = "x86"

	#'targetpath  = targetpath.lower() don't change the filename case
	platform    = platform.lower()
	config      = config.lower()
	dstdir      = dstdir.lower().rstrip("/\\") + "\\" + platform + "\\" + config
	symdir      = UserVars.root + "\\local\\symbols\\" + platform + "\\" + config
	srcdir,file = os.path.split(targetpath)
	fname,extn  = os.path.splitext(file)

	srcfname = srcdir + "\\" + fname
	dstfname = dstdir + "\\" + fname

	# Copy the library file to the lib folder
	Tools.Copy(targetpath, dstfname + extn)

	# If there's an associated PDB file copy that too
	if os.path.exists(srcfname + ".pdb"):
		Tools.Copy(srcfname + ".pdb", dstfname + ".pdb")
		if not os.path.exists(symdir): os.makedirs(symdir)
		Tools.Copy(srcfname + ".pdb", symdir)

	# If the lib is a dll, look for an import library and copy that too, if it exists
	if extn == ".dll":
		if os.path.exists(srcfname + ".lib"):
			Tools.Copy(srcfname + ".lib", dstfname + ".lib")

# Run as standalone script
if __name__ == "__main__":
	try:
		targetpath = sys.argv[1] if len(sys.argv) > 1 else input("TargetPath? ")
		platform   = sys.argv[2] if len(sys.argv) > 2 else input("Platform (x86,x64)? ")
		config     = sys.argv[3] if len(sys.argv) > 3 else input("Configuration (debug,release)? ")
		dstdir     = sys.argv[4] if len(sys.argv) > 4 else ""

		DeployLib(targetpath, platform, config, dstdir)
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)
