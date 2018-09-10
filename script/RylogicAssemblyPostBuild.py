#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
import Rylogic as Tools
import DeployLib
import UserVars

# The post-build step for Rylogic Assemblies
def PostBuild(assembly:str, projdir:str, targetdir:str, platform:str, config:str, deps:[str]):

	# Assert up-to-date tools
	Tools.AssertVersion(1);

	# Ensure the target directory exists
	os.makedirs(targetdir, exist_ok=True);

	# Sign the assembly
	#signtool = UserVars.winsdk + "\\bin\\signtool.exe"
	#Tools.Exec([signtool, "sign", "/fd", "SHA256" ])

	# Set this to false to disable running tests on compiling
	RunTests = True

	# The build outputs
	dll = os.path.join(targetdir, f"{assembly}.dll")
	exe = os.path.join(targetdir, f"{assembly}.exe")
	target = dll if os.path.exists(dll) else exe

	# Run unit tests
	if RunTests:
		if not os.path.exists(target):
			print(f"{assembly} assembly not found.   **** Unit tests skipped ****")
		else:
			# Use PowerShell to run the unit-tests
			if target == dll:
				command = (
					"& {\n" +
					f"Set-Location {targetdir};\n" +
					"\n".join(f"[Reflection.Assembly]::LoadFile('{os.path.join(targetdir,dep)}')|Out-Null;\n" for dep in deps) +
					f"[Reflection.Assembly]::LoadFile('{target}')|Out-Null;\n" +
					f"Exit [{assembly}.Program]::Main();\n" +
					"}")

				powershell = UserVars.powershell64 if platform == "x64" else UserVars.powershell32
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

	return
