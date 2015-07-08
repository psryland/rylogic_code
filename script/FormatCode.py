#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

#Tidy a code file
#Use:
#  FormatCode.py $(FilePath) [$(OutputFilePath)] [show]

import sys, os, subprocess
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.textedit])

	filepath = (sys.argv[1] if len(sys.argv) > 1 else input("filepath? ")).lower()
	outpath  = (sys.argv[2] if len(sys.argv) > 2 else "").lower()
	show     = (sys.argv[3] if len(sys.argv) > 3 else "").lower()
	if filepath == "": Tools.OnError("ERROR: no input file provided")
	if outpath == "": outpath = filepath

	input(
		"Formatting Code:\n"
		"  Source: " + filepath + "\n"
		"  Destination: " + outpath + "\n"
		" Press enter to continue")

	trace = False


	Tools.Copy(filepath, outpath)
	Tools.Exec([UserVars.root+'\\bin\\textformatter.exe', '-f', outpath, '-newlines', '0', '1'], show_arguments=trace)
	Tools.Exec([UserVars.root+'\\tools\\AStyle\\astyle.exe', '--options='+UserVars.root+'\\tools\\astyle\\format_with_newlines.cfg', outpath], show_arguments=trace)
	if show == "show": Tools.Exec([UserVars.textedit, outpath])
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
