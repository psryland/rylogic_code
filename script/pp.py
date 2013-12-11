#!/usr/bin/env python
# -*- coding: utf-8 -*- 

#Preprocess a given file
#Use:
#  pp.py $(FilePath) [$(OutputFilePath)]

import sys, os, subprocess
import Rylogic as Tools
import UserVars

Tools.CheckVersion(1)

# if 'cl' is not in the path, create a command that runs the vc_env script,
# and recursively launches this script. The reason for this is no child
# process can set env variables for a parent so we run ourself as a child.
try:
	subprocess.check_output(['cl.exe'], stderr=subprocess.STDOUT)
except Exception as ex:
	print("Loading Visual Studio Environment...")
	subprocess.call([UserVars.vc_env, '&', os.path.realpath(__file__)] + sys.argv[1:])
	sys.exit(0)

vs_version = os.environ['VisualStudioVersion']
filepath = sys.argv[1] if len(sys.argv) > 1 else ""
outpath  = sys.argv[2] if len(sys.argv) > 2 else ""
if filepath == "": Tools.OnError("ERROR: no input file provided")
if outpath == "": outpath = filepath + ".i"

input(
	"Preprocessing:\n"
	"  Source: " + filepath + "\n"
	"  Destination: " + outpath + "\n"
	"  Visual Studio Version: " + vs_version + "\n"
	" Press enter to continue")

trace = False

try:
	flags=["/P", "/nologo", "/TP"]
	#/GS /analyze- /W3 /wd"4351" /Gy /Zc:wchar_t /ZI /Gm /Od /Ob0 /GF /WX- /Zc:forScope /RTC1 /Gd /Oy- /MTd /openmp /fp:precise /errorReport:prompt /EHsc 

	includes=[
		'/I"'+UserVars.pr_root+'\\projects"',
		'/I"'+UserVars.pr_root+'\\sdk\\pr"',
		'/I"'+UserVars.pr_root+'\\sdk\\wtl\\v8.1\\include"',
		'/I"'+UserVars.pr_root+'\\sdk\\lua\\lua\\src"',
		'/I"'+UserVars.pr_root+'\\sdk\\lua"',
		'/I"'+UserVars.pr_root+'\\sdk\\sqlite"'
		]

	defines=[
		'/D_DEBUG',
		'/D_CRT_SECURE_NO_WARNINGS',
		'/DNOMINMAX',
		'/DPR_UNITTESTS'
		]

	print("Preprocessing...")
	Tools.Exec(['cl'] + flags + includes + defines + ['/Fi'+outpath, filepath], show_arguments=trace)

	print("Cleaning PP output...")
	Tools.Exec(['q:\\bin\\textformatter.exe', '-f', outpath, '-newlines', '0', '1'], show_arguments=trace)

	print("AStyling output...")
	Tools.Exec(['Q:\\tools\\AStyle\\astyle.exe', '--options=Q:\\tools\\astyle\\format_with_newlines.cfg', outpath], show_arguments=trace)

	print("Showing PP output...")
	Tools.Exec([UserVars.textedit, outpath])


	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
