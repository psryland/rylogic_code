#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Execute unit tests
# Use:
#   run_tests $(TargetPath)
import sys, os
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	Tools.CheckVersion(1)

	tests_exe = sys.argv[1]
	if os.path.exists(tests_exe):
		Tools.Exec([tests_exe, "runtests"])
	else:
		print("Unit tests not run")

except Exception as ex:
	Tools.OnException(ex,False)