#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Execute unit tests
# Use:
#   run_tests $(TargetPath)
import sys, os, re, ctypes
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	# Set this to false to disable running tests on compiling
	RunTests = True

	test_dll_path = os.path.abspath(sys.argv[1])
	if RunTests and os.path.exists(test_dll_path):

		# Load the MS unit test dll
		mstests_dll_path = UserVars.vs_dir + "\\Common7\\IDE\\CommonExtensions\\Microsoft\\TestWindow\\Extensions\\Cpp\\Microsoft.VisualStudio.TestTools.CppUnitTestFramework.x64.dll"
		mstests = ctypes.windll.LoadLibrary(mstests_dll_path)
		
		# Load the unit tests
		tests_dll = ctypes.windll.LoadLibrary(test_dll_path)

		# Run the unit tests
		tests_dll.RunAllTests(0)
	else:
		print("   **** Unit tests not run ****   ")

except Exception as ex:
	print(" TESTS FAILED : " + str(ex))
