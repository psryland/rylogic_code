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

if __name__ == "__main__":
	try:
		Tools.AssertVersion(1)
		
		# Set this to false to disable running tests on compiling
		RunTests = True

		if RunTests:
			target = (sys.argv[1] if len(sys.argv) > 1 else input("assembly? ")).lower()

			# Use the power shell to run the unit tests
			Tools.Exec(["powershell", "-noninteractive", "-noprofile", "-sta", "-nologo", "-command", "[Reflection.Assembly]::LoadFile('"+target+"')|Out-Null;exit [pr.Program]::Main();"])
		else:
			print("   **** Unit tests not run ****   ")

	except Exception as ex:
		print("   **** Unit tests failed ****   ")
