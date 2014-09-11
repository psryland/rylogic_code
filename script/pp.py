#!/usr/bin/env python
# -*- coding: utf-8 -*- 

#Preprocess a given file
#Use:
#  pp.py $(FilePath) [$(OutputFilePath)]

import sys, os, subprocess
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

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
	if outpath == "":
		file,ext = os.path.splitext(filepath)
		outpath = file + "_pp" + ext

	input(
		"Preprocessing:\n"
		"  Source: " + filepath + "\n"
		"  Destination: " + outpath + "\n"
		"  Visual Studio Version: " + vs_version + "\n"
		" Press enter to continue")

	trace = False

	flags=[
		'/P',
		'/nologo',
		'/TP',
		#'/EP', #no line numbers
		]
	#/GS /analyze- /W3 /wd"4351" /Gy /Zc:wchar_t /ZI /Gm /Od /Ob0 /GF /WX- /Zc:forScope /RTC1 /Gd /Oy- /MTd /openmp /fp:precise /errorReport:prompt /EHsc 

	project_dir = r'R:\software\PC\RexSerialConnection'
	includes=[
		'/I"'+project_dir+'"',
		'/I"'+project_dir+'\\src\\emulation"',
		'/I"'+project_dir+'\\..\\stm32_proxy\\products\\master_controller_v2\\source"',
		'/I"'+project_dir+'\\..\\stm32_proxy"',
		'/I"'+project_dir+'\\..\\stm32_proxy\\stm32 library\\fwlib\\library\\inc"',
		'/I"'+project_dir+'\\..\\..\\stm32\\products\\master_controller_v2\\source"',
		'/I"'+project_dir+'\\..\\..\\stm32"',
		'/I"'+project_dir+'\\..\\..\\stm32\\stm32 library\\fwlib\\library\\inc"',
		'/I"R:\\localrepo',

		'/I"'+UserVars.root+'\\projects"',
		'/I"'+UserVars.root+'\\sdk\\pr"',
		'/I"'+UserVars.root+'\\sdk\\wtl\\v8.1\\include"',
		'/I"'+UserVars.root+'\\sdk\\lua\\lua\\src"',
		'/I"'+UserVars.root+'\\sdk\\lua"',
		'/I"'+UserVars.root+'\\sdk\\sqlite"',
		]

	defines=[
		'/D_DEBUG',
		'/D_CRT_SECURE_NO_WARNINGS',
		'/DNOMINMAX',
		'/DPR_UNITTESTS'
	#	'/DDEF_STM32_PRODUCT=2',
	#	'/DPACK=',
		]

	print("Preprocessing...")
	Tools.Exec(['cl'] + flags + includes + defines + ['/Fi'+outpath, filepath], show_arguments=trace)

	print("AStyling output...")
	Tools.Exec([UserVars.root+'\\tools\\AStyle\\astyle.exe', '--options='+UserVars.root+'\\tools\\astyle\\format_with_newlines.cfg', outpath], show_arguments=trace)

	print("Cleaning output...")
	Tools.Exec([UserVars.root+'\\bin\\textformatter.exe', '-f', outpath, '-newlines', '0', '1'], show_arguments=trace)

	print("Showing output...")
	Tools.Exec([UserVars.devenv, "/Edit", outpath], expected_return_code=0xffffffff)
	#Tools.Exec([UserVars.textedit, outpath])

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
