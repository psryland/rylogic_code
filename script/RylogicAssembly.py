#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
import Rylogic as Tools
import UserVars

# The post-build step for Rylogic Assemblies
def PostBuild(assembly:str, proj_dir:str, target_dir:str, framework:str, platform:str, config:str, deps:[str], run_tests:bool=True):

	# Assert up-to-date tools
	Tools.AssertVersion(1)

	# Ensure the target directory exists
	os.makedirs(target_dir, exist_ok=True)

	# Set this to false to disable running tests on compiling
	RunTests = run_tests
	#RunTests = False

	# The build outputs
	dll = os.path.join(target_dir, f"{assembly}.dll")
	exe = os.path.join(target_dir, f"{assembly}.exe")
	target = dll if os.path.exists(dll) else exe

	# Run unit tests
	if RunTests:
		if not os.path.exists(target):
			print(f"{assembly} assembly not found.   **** Unit tests skipped ****")
		else:
			
			if target == dll:
				# Run using dotnet - Doesn't work because libraries can't have entry points
				#res,outp = Tools.Run([UserVars.dotnet, target])

				# Map 'deps' to full paths.
				# Dependent dlls should be next to the test dll, thanks to the build system
				deps = [os.path.join(target_dir, p) for p in deps]
				
				# Run using cross-platform PowerShell
				command = (
					"& {\n" +
					f"    Set-Location {target_dir};\n" +
					#f""   .join(f"    Add-Type -AssemblyName '{dep}';\n" for dep in deps) +
					f"    Add-Type -AssemblyName '{target}';\n" +
					#f""   .join(f"    [Reflection.Assembly]::LoadFile('{dep}')|Out-Null;\n" for dep in deps) + 
					#f"    [Reflection.Assembly]::LoadFile('{target}')|Out-Null;\n" + 
					f"    $result = [{assembly}.Program]::Main();\n" +
					f"    Exit $result;\n" +
					"}")
				#print(command)

				res,outp = Tools.Run([UserVars.pwsh, "-NonInteractive", "-NoProfile", "-NoLogo", "-Command", command])
				print(outp)
				if not res:
					raise Exception("   **** Unit tests failed ****   ")

			elif target == exe:

				# Run the exe directly
				res,outp = Tools.Run([target])
				print(outp)
				if not res:
					raise Exception("   **** Unit tests failed ****   ")

	# Don't copy to the lib folder, that is the deploy step
	# Rylogic assemblies shouldn't be copied anyway, as we don't know which framework
	# to copy, i.e. netstandard2.0, netcoreapp3.1, ?
	return

# Entry point
if __name__ == "__main__":
	pass
