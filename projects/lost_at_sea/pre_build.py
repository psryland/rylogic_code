#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# pre_build.py $(TargetPath) $(PlatformTarget) $(ConfigurationName)

import sys, os, shutil, re
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	target   = sys.argv[1] if len(sys.argv) > 1 else input("target?")
	platform = sys.argv[2] if len(sys.argv) > 2 else input("platform?")
	config   = sys.argv[3] if len(sys.argv) > 3 else input("config?")

	dir,file = os.path.split(target)

	Tools.Copy(UserVars.root + "\\projects\\lost_at_sea\\data\\", dir + "\\data\\")

except Exception as ex:
	Tools.OnException(ex)
