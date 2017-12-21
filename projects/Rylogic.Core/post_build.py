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

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\Rylogic.Core"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\Rylogic.Core\\bin\\Debug\\netstandard2.0"
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

	dll = targetdir + "\\Rylogic.Core.dll"
	exe = targetdir + "\\Rylogic.Core.exe"

	# Run unit tests
	if RunTests:
		target = dll if os.path.exists(dll) else exe
		if os.path.exists(target):
			# Use PowerShell to run the unit tests
			powershell = UserVars.powershell64 if platform == "x64" else UserVars.powershell32
			res,outp = Tools.Run([powershell, "-noninteractive", "-noprofile", "-sta", "-nologo", "-command", "dotnet", target])
			print(outp)
			if not res:
				raise Exception("   **** Unit tests failed ****   ")
		else:
			print("Rylogic.Core assembly not found.   **** Unit tests skipped ****")

	# Copy to the lib directory
	if os.path.exists(dll):
		DeployLib.DeployLib(dll, "AnyCPU", config)

except Exception as ex:
	Tools.OnException(ex)