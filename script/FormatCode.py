#!/usr/bin/env python
# -*- coding: utf-8 -*- 

#Tidy a code file
#Use:
#  FormatCode.py $(FilePath) [$(OutputFilePath)] [show]

import sys, os, subprocess
import Rylogic as Tools
import UserVars

Tools.CheckVersion(1)

filepath = sys.argv[1].lower() if len(sys.argv) > 1 else ""
outpath  = sys.argv[2].lower() if len(sys.argv) > 2 else ""
show     = sys.argv[3].lower() if len(sys.argv) > 3 else ""
if filepath == "": Tools.OnError("ERROR: no input file provided")
if outpath == "": outpath = filepath

input(
	"Formatting Code:\n"
	"  Source: " + filepath + "\n"
	"  Destination: " + outpath + "\n"
	" Press enter to continue")

trace = False

try:
	Tools.Copy(filepath, outpath)
	Tools.Exec([UserVars.root+'\\bin\\textformatter.exe', '-f', outpath, '-newlines', '0', '1'], show_arguments=trace)
	Tools.Exec([UserVars.root+'\\tools\\AStyle\\astyle.exe', '--options='+UserVars.root+'\\tools\\astyle\\format_with_newlines.cfg', outpath], show_arguments=trace)
	if show == "show": Tools.Exec([UserVars.textedit, outpath])
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
