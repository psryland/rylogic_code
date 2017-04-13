#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

def GenerateLicence():
	print(
		"=============================\n"
		"RyLogViewer Licence Generator\n"
		"=============================\n")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.csex])

	pk = ".\src\licence\private_key.xml"
	Tools.Exec([UserVars.csex, "-gencode", "-pk", pk])

# Run as standalone script
if __name__ == "__main__":
	try:
		GenerateLicence()
		Tools.OnSuccess()

	except Exception as ex:
		Tools.OnException(ex)
