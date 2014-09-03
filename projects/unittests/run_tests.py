#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Execute unit tests
# Use:
#   run_tests $(TargetPath)
import sys, os
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

try:
	Tools.CheckVersion(1)

	# Set this to false to disable running tests on compiling
	RunTests = True

	tests_exe = sys.argv[1]
	if os.path.exists(tests_exe) and RunTests:
		Tools.Exec([tests_exe, "runtests"])
	else:
		print("   **** Unit tests not run ****   ")

except Exception as ex:
	Tools.OnException(ex,False)