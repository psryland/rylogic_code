#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Execute unit tests
# Use:
#   run_tests $(TargetPath)
import sys, os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic, UserVars

# Set this to false to disable running tests on compiling
RunTests = True
#RunTests = False

try:
	sys.argv = sys.argv if len(sys.argv) >= 2 else ["", "P:\\pr\\obj\\v142\\unittests\\x64\\Debug\\unittests.dll"] 

	target_path = Rylogic.Path(sys.argv[1])
	target_extn = os.path.splitext(target_path)[1].lower()

	# If the target is an exe, just run it
	if RunTests and target_extn == ".exe":
		Rylogic.Exec([target_path])

	elif RunTests and target_extn == ".dll":
		vstest = Rylogic.Path(UserVars.vs_dir, "Common7\\IDE\\Extensions\\TestPlatform\\vstest.console.exe")
		Rylogic.Exec([vstest, target_path, "--logger:console;verbosity=minimal", "--nologo"])

	else:
		print("   **** Unit tests not run ****   ")

except Exception as ex:
	print(" TESTS FAILED : " + str(ex))
