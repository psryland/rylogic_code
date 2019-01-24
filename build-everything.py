#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# The purpose of this script is to build the entire Rylogic library
# using a single click (based on MSBuild)

import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "script")))
import Rylogic as Tools
import UserVars
#import BuildInstaller

def BuildEverything():
	'''
	Compile the entire Rylogic repo using MSBuild
	'''

	# Assert UserVars
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([
		UserVars.root,
		UserVars.winsdk,
		UserVars.textedit,
		os.path.join(UserVars.winsdk_bin, "x64", "fxc.exe")
		])
	Tools.AssertLatestWinSDK()
	
	# Build the Rylogic solution
	Tools.MSBuild(UserVars.rylogic_sln, [], ["x64", "x86"], ["Release"])

	return

if __name__ == "__main__":
	try:
		print(" *** Rylogic Repo - Build Everything *** \n")
		BuildEverything()
		
	except Exception as ex:
		print(str(ex))
		sys.exit(-1)