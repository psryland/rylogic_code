#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
import Rylogic as Tools
import DeployLib
import UserVars

# The post-build step for Rylogic Assemblies
def PostBuild(assembly:str, projdir:str, targetdir:str, framework:str, platform:str, config:str, deps:[str], run_tests:bool=True):

	# Assert up-to-date tools
	Tools.AssertVersion(1)

	# Ensure the target directory exists
	os.makedirs(targetdir, exist_ok=True)

	# Sign the assembly
	#signtool = UserVars.winsdk + "\\bin\\signtool.exe"
	#Tools.Exec([signtool, "sign", "/fd", "SHA256" ])

	# Set this to false to disable running tests on compiling
	RunTests = run_tests
	#RunTests = False

	# The build outputs
	dll = os.path.join(targetdir, f"{assembly}.dll")
	exe = os.path.join(targetdir, f"{assembly}.exe")
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
				deps = [os.path.join(targetdir, p) for p in deps]
				
				# Run using cross-platform PowerShell
				command = (
					"& {\n" +
					f"    Set-Location {targetdir};\n" +
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

	# Copy to lib folder
	Tools.Copy(target, os.path.join(UserVars.root, "lib", platform, config, ""))
	return

# Nuget deploy a Rylogic Assembly
def Deploy(assembly:str, config:str, publish:bool):
	
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.msbuild])
	Tools.AssertPathsExist([UserVars.root, UserVars.nuget])

	sln     = UserVars.CheckPath(os.path.join(UserVars.root, "build", "Rylogic.sln"))
	srcdir  = UserVars.CheckPath(os.path.join(UserVars.root, "projects", assembly))
	proj    = UserVars.CheckPath(os.path.join(srcdir, f"{assembly}.csproj"))
	configs = [config.lower()] if not config == "both" else ["debug", "release"]

	# Build
	print(f"Building {assembly}...")
	Tools.MSBuild(sln, [f"Rylogic\\{assembly}"], ["Any CPU"], configs, False, False)

	# Package
	if "release" in configs:
		Tools.NugetPackage(proj, publish)

	print("")
	return

# Deploy all Rylogic assemblies
def DeployAll(config:str, publish:bool):
	Deploy("Rylogic.Core", config, publish)
	Deploy("Rylogic.Core.Windows", config, publish)
	Deploy("Rylogic.View3d", config, publish)
	Deploy("Rylogic.Scintilla", config, publish)
	Deploy("Rylogic.Gui.WinForms", config, publish)
	Deploy("Rylogic.Gui.WPF", config, publish)
	Deploy("Rylogic.DirectShow", config, publish)
	return

# Entry point
if __name__ == "__main__":

	publish = True if input("Publish to nuget.org? (y/n) ") == 'y' else False

	# Use RylogicAssembly.py
	if len(sys.argv) == 1:
		DeployAll("Release", publish)
