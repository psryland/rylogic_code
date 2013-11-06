#!/usr/bin/env python
# -*- coding: utf-8 -*- 

# Post Build Event for exporting library files to a directory
# Use:
#   DeployLib $(TargetPath) $(Platform) $(Configuration) [dstdir]
# 
# This will copy mylib.platform.config.dll to "%dstdir%\mylib.platform.config.dll and
# optionally the pdb if it exists.
# 'dstdir' is optional, if not given it will default to pr_root+"\sdk\pr\lib"
# Note: the targetpath produced by visual studio needs to already be named in the form
#  name.platform.config.extn because the pdb filename associated with it is embedded in
#  the file, renaming the lib and pdb doesn't work because the lib still looks of the old
#  pdb name
import sys, os
import Rylogic as Tools
import UserVars

Tools.CheckVersion(1)

if len(sys.argv) > 1: targetpath = sys.argv[1]
else:                 targetpath = input("TargetPath? ")
if len(sys.argv) > 2: platform   = sys.argv[2]
else:                 platform   = input("Platform (x86,x64)? ")
if len(sys.argv) > 3: config     = sys.argv[3]
else:                 config     = input("Configuration (debug,release)? ")
if len(sys.argv) > 4: dstdir     = sys.argv[4]
else:                 dstdir     = ""

targetpath  = targetpath.lower();
platform    = platform.lower();
config      = config.lower();
dstdir      = dstdir.lower();
srcdir,file = os.path.split(targetpath)
fname,extn  = os.path.splitext(file)

if platform == "win32":
	platform = "x86"

if dstdir == "":
	dstdir = UserVars.pr_root + r"\sdk\pr\lib"

srcfname = srcdir + "\\" + fname
dstfname = dstdir + "\\" + fname

#Copy the library file to the lib folder
Tools.Copy(targetpath, dstfname + extn)

#If there's an associated pdb file copy that too
if os.path.exists(srcfname + ".pdb"):
	Tools.Copy(srcfname + ".pdb", dstfname + ".pdb")

#If the lib is a dll, look for an import library and copy that too, if it exists
if extn == ".dll":
	if os.path.exists(srcfname + ".lib"):
		Tools.Copy(srcfname + ".lib", dstfname + ".lib")

#Produce a OMF version of the lib
#wordsize = 64 if platform == "x64" else 32
#q:\tools\objconv -fOMF%wordsize% "%dstdir%\%file%.%platform%.%config%.%extn%" "%dstdir%\%file%.%platform%.%config%.omf.%extn%"
