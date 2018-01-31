#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(PlatformName) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import DeployLib
import UserVars

try:
	Tools.AssertVersion(1);

	assembly = "Rylogic.Core"
	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\"+assembly
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\"+assembly+"\\bin\\Debug\\netstandard2.0"
	platform  = sys.argv[3]              if len(sys.argv) > 3 else "AnyCPU"
	config    = sys.argv[4]              if len(sys.argv) > 4 else "Debug"
	#platform = platform if platform != "AnyCPU" else "x86"

	# Ensure directories exist
	os.makedirs(targetdir, exist_ok=True);

	# Sign the assembly
	#signtool = UserVars.winsdk + "\\bin\\signtool.exe"
	#Tools.Exec([signtool, "sign", "/fd", "SHA256" ])

	# Set this to false to disable running tests on compiling
	RunTests = True

	dll = targetdir + "\\"+assembly+".dll"
	exe = targetdir + "\\"+assembly+".exe"
	target = dll if os.path.exists(dll) else exe

	# Run unit tests
	if RunTests:
		if not os.path.exists(target):
			print(assembly + " assembly not found.   **** Unit tests skipped ****")
		else:
			if target == dll:
				# Use PowerShell to run the unit tests
				# Not using 'dotnet' because we want to run the tests when built as a class library
				powershell = UserVars.powershell64 if platform == "x64" else UserVars.powershell32
				command = (
					"& {\n"+
					"Set-Location "+targetdir+";\n"+
					"[Reflection.Assembly]::LoadFile('"+target+"')|Out-Null;\n"+
					"Exit ["+assembly+".Program]::Main();\n"+
					"}")
				res,outp = Tools.Run([powershell, "-NonInteractive", "-NoProfile", "-STA", "-NoLogo", "-Command", command])
				print(outp)
				if not res:
					raise Exception("   **** Unit tests failed ****   ")
			elif target == exe:
				# Run the exe directly
				res,outp = Tools.Run([target])
				print(outp)
				if not res:
					raise Exception("   **** Unit tests failed ****   ")

	# Copy to the lib directory
	if os.path.exists(dll):
		DeployLib.DeployLib(dll, "AnyCPU", config)

except Exception as ex:
	Tools.OnException(ex)