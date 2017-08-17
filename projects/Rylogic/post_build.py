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

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\Rylogic"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\Rylogic\\bin\\Debug"
	platform  = sys.argv[3]              if len(sys.argv) > 3 else "AnyCPU"
	config    = sys.argv[4]              if len(sys.argv) > 4 else "Debug"
	platform = platform if platform != "AnyCPU" else "x86"

	# Ensure directories exist
	os.makedirs(targetdir, exist_ok=True);

	# Sign the assembly
	#signtool = UserVars.winsdk + "\\bin\\signtool.exe"
	#Tools.Exec([signtool, "sign", "/fd", "SHA256" ])

	# Set this to false to disable running tests on compiling
	RunTests = True

	dll = targetdir + "\\Rylogic.dll"
	exe = targetdir + "\\Rylogic.exe"

	# Run unit tests
	if RunTests:
		target = dll if os.path.exists(dll) else exe
		if os.path.exists(target):
			# Use the power shell to run the unit tests
			powershell = UserVars.powershell64 if platform == "x64" else UserVars.powershell32
			res,outp = Tools.Run([powershell, "-noninteractive", "-noprofile", "-sta", "-nologo", "-command", "[Reflection.Assembly]::LoadFile('"+target+"')|Out-Null;exit [pr.Program]::Main();"])
			outp = re.sub(r"Attempting to perform the InitializeDefaultDrives operation on the 'FileSystem' provider failed.\n(.*)", r"\1", outp)
			print(outp)
			if not res:
				raise Exception("   **** Unit tests failed ****   ")
		else:
			print("Rylogic assembly not found.   **** Unit tests skipped ****")

	# Copy to the lib directory
	if os.path.exists(dll):
		DeployLib.DeployLib(dll, "AnyCPU", config)

except Exception as ex:
	Tools.OnException(ex)