#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# The purpose of this script is to build the entire Rylogic library
# using a single click (based on MSBuild)

import sys, os, shutil, re
sys.path.append(os.path.join(sys.path[0],"script"))
import Rylogic as Tools
import UserVars
#import BuildInstaller

def BuildEverything():
	'''
	Compile the entire Rylogic repo using MSBuild
	'''

	# Assert UserVars
	Tools.AssertLatestWinSDK()

	# Build the Rylogic solution
	Tools.MSBuild(UserVars.rylogic_sln, [], ["x64", "x86"], ["Release"])

	return

if __name__ == "__main__":
	try:
		print(" *** Rylogic Repo - Build Everything *** ")
		print("\n")
		BuildEverything()
		
	except Exception as ex:
		print(str(ex))
		sys.exit(-1)