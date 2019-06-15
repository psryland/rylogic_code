#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Execute unit tests
# Use:
#   run_tests $(TargetPath)
import sys, os, re, ctypes
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)
	sys.argv = sys.argv if len(sys.argv) >= 2 else ["", "P:\\pr\\obj\\v142\\unittests\\x64\\Debug\\unittests.dll"] 

	# Set this to false to disable running tests on compiling
	RunTests = False

	test_dll_path = os.path.abspath(sys.argv[1])
	if RunTests and os.path.exists(test_dll_path):

		vstest = os.path.join(UserVars.vs_dir, "Common7", "IDE", "Extensions", "TestPlatform", "vstest.console.exe")
		Tools.Exec([vstest, test_dll_path, "--logger:console;verbosity=minimal", "--nologo"])

	else:
		print("   **** Unit tests not run ****   ")

except Exception as ex:
	print(" TESTS FAILED : " + str(ex))
