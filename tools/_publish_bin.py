#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Post Build Event for exporting binary files
# Use:
#   _publish_bin $(TargetPath) $(PlatformTarget) $(ConfigurationName) [dstsubdir]
import sys, os

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
from pr import UserVars
RylogicEnv.CheckVersion(1)

if len(sys.argv) > 1: targetpath = sys.argv[1]
else:                 targetpath = input("TargetPath? ")
if len(sys.argv) > 2: platform   = sys.argv[2]
else:                 platform   = input("Platform (x86,x64)? ")
if len(sys.argv) > 3: config     = sys.argv[3]
else:                 config     = input("Configuration (debug,release)? ")
if len(sys.argv) > 4: dstsubdir  = sys.argv[4]
else:                 dstsubdir  = ""

targetpath  = targetpath.lower();
platform    = platform.lower();
config      = config.lower();
dstsubdir   = dstsubdir.lower();
srcdir,file = os.path.split(targetpath)
fname,extn  = os.path.splitext(file)

if platform == "win32":
	platform = "x86"

# Default to a subdir matching the target filename
if dstsubdir == "":
	dstsubdir = fname

# Set the output directory and ensure it exists
dstdirroot = UserVars.pr_root + r"\bin"
dstdir = dstdirroot + "\\" + dstsubdir + "\\" + platform
if not os.path.exists(dstdir): os.makedirs(dstdir)

# Only publish release builds
if config == "release":
	# Copy the binary to the bin folder
	RylogicEnv.Copy(targetpath, dstdir + "\\" + file)

	# If the system architecture matches this release, copy to the root dstdir
	if platform == UserVars.arch:
		RylogicEnv.Copy(dstdir + "\\" + file, dstdirroot + "\\" + file)

