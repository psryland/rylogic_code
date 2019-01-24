#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Use:
#  $(SolutionDir)..\post_build_bot.py $(TargetPath) $(SolutionDir) $(ConfigurationName)
import sys, os, re, string
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	targetpath  = sys.argv[1]              if len(sys.argv) > 1 else input("target path?")
	solutiondir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else input("solution dir?")
	config      = sys.argv[3]              if len(sys.argv) > 3 else input("config?")
	dstdir      = solutiondir + "\\CoinFlip\\bin\\"+config+"\\bots"

	# Ensure directories exist
	os.makedirs(dstdir, exist_ok=True);

	# Copy the bot to 'dstdir'
	Tools.Copy(targetpath, dstdir+"\\", only_if_modified=True)

except Exception as ex:
	Tools.OnException(ex)