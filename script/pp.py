#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

#Preprocess a given file
#Use:
#  pp.py $(FilePath) [$(OutputFilePath)]

import sys, os, subprocess
import Rylogic as Tools
import Compile
import UserVars

# Preprocess a CPP source file
def Preprocess(src_filepath:str, out_filepath:str=None):
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])
	trace = True

	if not os.path.exists(src_filepath):
		raise RuntimeError("Source filepath does not exist")
	if not out_filepath:
		file,ext = os.path.splitext(src_filepath)
		out_filepath = f"{file}_pp{ext}"

	# Ensure environment variables for cl.exe are set up
	Tools.SetupVcEnvironment()

	# Include directories
	includes = [
		Tools.Path(UserVars.root, "include").replace("\\","/"),
		Tools.Path(UserVars.vs_dir, "VC\\Auxiliary\\VS\\UnitTest\\include").replace("\\","/"),
		#'/I"'+project_dir+'\\src\\emulation"',
		#'/I"'+project_dir+'\\..\\stm32_proxy\\products\\master_controller_v2\\source"',
		#'/I"'+project_dir+'\\..\\stm32_proxy"',
		#'/I"'+project_dir+'\\..\\stm32_proxy\\stm32 library\\fwlib\\library\\inc"',
		#'/I"'+project_dir+'\\..\\..\\stm32\\products\\master_controller_v2\\source"',
		#'/I"'+project_dir+'\\..\\..\\stm32"',
		#'/I"'+project_dir+'\\..\\..\\stm32\\stm32 library\\fwlib\\library\\inc"',
		#'/I"R:\\localrepo',

		#'/I"'+UserVars.root+'\\projects"',
		#'/I"'+UserVars.root+'\\sdk\\pr"',
		#'/I"'+UserVars.root+'\\sdk\\wtl\\v8.1\\include"',
		#'/I"'+UserVars.root+'\\sdk\\lua\\lua\\src"',
		#'/I"'+UserVars.root+'\\sdk\\lua"',
		#'/I"'+UserVars.root+'\\sdk\\sqlite"',
	 	]

	# Preprocessor defines
	defines = [
		'_DEBUG',
		'_CRT_SECURE_NO_WARNINGS',
		'NOMINMAX',
		'PR_UNITTESTS'
		#'DEF_STM32_PRODUCT=2',
		#'PACK=',
		]

	flags = [
		'/P',
		'/nologo',
		'/TP',
		#'/EP', #no line numbers
		]
	#/GS /analyze- /W3 /wd"4351" /Gy /Zc:wchar_t /ZI /Gm /Od /Ob0 /GF /WX- /Zc:forScope /RTC1 /Gd /Oy- /MTd /openmp /fp:precise /errorReport:prompt /EHsc 

	print("Preprocessing...")
	Tools.Exec(
		[Tools.Path(UserVars.vs_dir, "VC\\Tools\\MSVC", UserVars.vc_vers, "bin\\HostX64\\x64\\cl.exe")] + 
		[x for x in flags] + 
		[f'/I{x}' for x in includes] +
		[f'/D{x}' for x in defines] +
		[f'/Fi{out_filepath}', src_filepath],
		show_arguments=trace)

	#print("AStyling output...")
	#Tools.Exec([UserVars.root+'\\tools\\AStyle\\astyle.exe', '--options='+UserVars.root+'\\tools\\astyle\\format_with_newlines.cfg', outpath], show_arguments=trace)
	print("Cleaning output...")
	cex = Tools.Path(UserVars.root, "bin\\cex\\cex.exe")
	Tools.Exec([cex, "-newlines", "-f", out_filepath, '-limit', '0', '1'], show_arguments=trace)
	print("Showing output...")
	Tools.Exec([UserVars.vs_devenv, "/Edit", out_filepath], expected_return_code=0xffffffff)

	return

# Entry Point
if __name__ == "__main__":
	try:
		sys.argv = ["", "P:\\pr\\include\\pr\\common\\flags_enum.h"]

		# Read the source file and output file
		filepath = sys.argv[1] if len(sys.argv) > 1 else input("Input filepath: ")
		outpath  = sys.argv[2] if len(sys.argv) > 2 else None
		Preprocess(filepath, outpath)
		Tools.OnSuccess()
	except Exception as ex:
		Tools.OnException(ex)
