#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Execute unit tests
# Use:
#   run_tests $(TargetPath)
import sys, os, re
sys.path.append(re.sub(r"(.*[/\\])projects[/\\].*", r"\1script", __file__)) # add the \script path
import Rylogic as Tools
import UserVars

if __name__ == "__main__":
	try:
		Tools.AssertVersion(1)

		# Set this to false to disable running tests on compiling
		RunTests = True

		if RunTests:
			target = (sys.argv[1] if len(sys.argv) > 1 else r"P:\projects\Rylogic\bin\Debug\Rylogic.dll").lower()
			print("Unit Testing: " + target)

			# Use the power shell to run the unit tests
			res,outp = Tools.Run(["powershell", "-noninteractive", "-noprofile", "-sta", "-nologo", "-command", "[Reflection.Assembly]::LoadFile('"+target+"')|Out-Null;exit [pr.Program]::Main();"])
			outp = re.sub(r"Attempting to perform the InitializeDefaultDrives operation on the 'FileSystem' provider failed.\n(.*)", r"\1", outp)
			print(outp)
			if not res:
				raise Exception()

	except Exception as ex:
		print("   **** Unit tests failed ****   ")
