#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Use:
#  $(SolutionDir)..\post_build_bot.py $(TargetPath) $(ProjectDir) $(ConfigurationName)
import sys, os, re, string
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	target  = sys.argv[1]              if len(sys.argv) > 1 else input("target path?")
	projdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else input("project dir?")
	config  = sys.argv[3]              if len(sys.argv) > 3 else input("config?")
	dstdir  = os.path.join(projdir, "..", "CoinFlip.UI", "bin", config, "bots")

	# Ensure directories exist
	os.makedirs(dstdir, exist_ok=True)

	# Copy the bot to 'dstdir'
	Tools.Copy(target, os.path.join(dstdir, ""), only_if_modified=True)

except Exception as ex:
	Tools.OnException(ex)