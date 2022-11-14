#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Notes:
#  - This script creates the UserVars.py file.
#  - It cannot make use of scripts in 'repo/script' because the UserVars.py file may not exist yet in a clean build.

import sys, os, subprocess, re

# Set up the UserVars file for the given workspace directory
def SetupUserVars(workspace:str, name:str, ignore_missing:bool):

	# Path checking helper
	def CheckValue(value:str, what:str):
		if not value:
			if ignore_missing: (f"Could not auto-detect the value for: {what}")
			else: raise RuntimeError(f"Auto-setup failed. Could not auto-detect the value for: {what}")
		return value if value else None
	def CheckPath(path:str, what:str):
		if not path or not os.path.exists(path):
			if ignore_missing: print(f"Could not auto-detect the path for: {what}")
			else: raise RuntimeError(f"Auto-setup failed. Could not auto-detect the path for: {what}")
		return path if path and os.path.exists(path) else None

	# Auto-detect the needed variables
	local_dump = os.path.abspath(os.path.join(workspace, "..", "dump"))
	dump_dir = (
		local_dump if os.path.exists(local_dump) else
		os.environ["TMP"] if "TMP" in os.environ and os.path.exists(os.environ["TMP"]) else
		os.environ["TEMP"] if "TEMP" in os.environ and os.path.exists(os.environ["TEMP"]) else
		None)

	# Find the VS installer tool 'vswhere'
	vswhere = CheckPath(os.path.join(os.environ["ProgramFiles(x86)"], "Microsoft Visual Studio", "Installer", "vswhere.exe"), "'vswhere.exe'")

	# Find the VS installation directory
	vs_dir = subprocess.check_output([vswhere, "-latest", "-property", "installationPath"], universal_newlines=True).strip() if vswhere else None
	vs_dir = CheckPath(vs_dir, "Visual Studio installation directory")

	# Run the command line tool to set the visual studio environment variables in the environment
	vs_envvars = CheckPath(os.path.join(vs_dir, "Common7", "Tools", "VsDevCmd.bat") if vs_dir else None, "'VsDevCmd.bat'")
	if vs_envvars:
		env = subprocess.check_output(["cmd", "/C", "@echo", "off", "&", vs_envvars, "&", "@echo", "on", "&", "set"], universal_newlines=True)
		for line in env.splitlines():
			m = re.fullmatch("^(.+?)=(.*)$", line)
			if not m: continue
			os.environ[m[1]] = m[2]

	# Get the windows SDK directory from the environment variables
	winsdk_ver = CheckValue(os.environ["WindowsSDKVersion"].rstrip('\\') if "WindowsSDKVersion" in os.environ else None, "Windows SDK version")
	winsdk_dir = CheckPath(os.environ["WindowsSdkDir"].rstrip('\\') if "WindowsSdkDir" in os.environ else None, "Windows SDK directory")

	# Get the dot net framework directory from the environment
	framework_ver = CheckValue(os.environ["FrameworkVersion"] if "FrameworkVersion" in os.environ else None, "Microsoft.NET Framework version")
	framework_dir = CheckPath(os.environ["FrameworkDir"].rstrip('\\') if "FrameworkDir" in os.environ else None, "Microsoft.NET Framework directory")

	# Guess at the dotnet.exe path
	dotnet_path = CheckPath(os.path.join(os.environ["ProgramFiles"], "dotnet", "dotnet.exe"), "'dotnet.exe'")

	# Get more VS variables
	vs_vers = CheckValue(os.environ["VisualStudioVersion"] if "VisualStudioVersion" in os.environ else None, "Visual Studio version")
	vc_vers = CheckValue(os.environ["VCToolsVersion"] if "VCToolsVersion" in os.environ else None, "Visual C++ tools version")

	# Guess the Git command line tool path
	git_path0 = os.path.join(os.environ["ProgramFiles"], "Git", "bin", "git.exe")
	git_path1 = os.path.join(os.environ["LOCALAPPDATA"], "Programs", "Git", "bin", "git.exe")
	git_path = CheckPath(
		git_path0 if os.path.exists(git_path0) else
		git_path1 if os.path.exists(git_path1) else
		"", "Git command line tool")

	# Guess the powershell path
	pwsh7_path = os.path.join(os.environ["ProgramFiles"], "PowerShell", "7", "pwsh.exe")
	pwsh1_path = os.path.join(os.environ["SYSTEMROOT"], "System32", "WindowsPowerShell", "v1.0", "powershell.exe")
	pwsh_path = CheckPath(pwsh7_path if os.path.exists(pwsh7_path) else pwsh1_path, "PowerShell")

	# Pull Nuget if missing
	getnuget_path = os.path.abspath(os.path.join(workspace, "tools", "nuget", "_get_nuget.py"))
	subprocess.check_call(["python.exe", getnuget_path])
	nuget_path0 = os.path.abspath(os.path.join(workspace, "tools", "nuget", "nuget.exe"))
	nuget_path = CheckPath(
		nuget_path0 if os.path.exists(nuget_path0) else
		"", "Nuget command line tool")

	# Escape back slashes in paths
	def Escape(s:str):
		return '"' + s.replace('\\', '\\\\') + '"' if s else "None"

	# Copy the UserVars template and make substitutions for the current workspace
	def ReplaceTags(match):
		tag = match.group(1)
		if tag == "USER_NAME":             return Escape(name)
		if tag == "REPO_ROOT_DIRECTORY":   return Escape(workspace)
		if tag == "DUMP_DIRECTORY":        return Escape(dump_dir)
		if tag == "WINDOWS_SDK_VERSION":   return Escape(winsdk_ver)
		if tag == "WINDOWS_SDK_DIRECTORY": return Escape(winsdk_dir)
		if tag == "FRAMEWORK_VERSION":     return Escape(framework_ver)
		if tag == "FRAMEWORK_DIRECTORY":   return Escape(framework_dir)
		if tag == "DOTNET_PATH":           return Escape(dotnet_path)
		if tag == "VS_DIRECTORY":          return Escape(vs_dir)
		if tag == "VS_VERS":               return Escape(vs_vers)
		if tag == "VC_VERS":               return Escape(vc_vers)
		if tag == "GIT_PATH":              return Escape(git_path)
		if tag == "POWERSHELL_PATH":       return Escape(pwsh_path)
		if tag == "NUGET_PATH":            return Escape(nuget_path)
		if tag == "NUGET_API_KEY":         return "None"
		if tag == "CODE_SIGN_CERT_PATH":   return "None"
		if tag == "ANDROID_SDK_DIRECTORY": return "None"
		if tag == "JAVA_SDK_DIRECTORY":    return "None"
		if tag == "TEXTEDITOR_PATH":       return "None"
		if tag == "MERGETOOL_PATH":        return "None"
		if tag == "WWW_ROOT_DIRECTORY":    return "None"
		raise RuntimeError(f"Unknown template tag ({match.group(0)}) in UserVars.template.py")

	# Template expander (from RexBionics.py)
	def Expand(template_filepath, output_filepath, regex_pattern, subst_func):
		pat = re.compile(regex_pattern)
		with open(template_filepath) as f:
			buf = f.read(-1)
			s = 0
			while True:
				m = pat.search(buf, s)
				if not m: break
				subst = subst_func(m)
				buf = buf[:m.start()] + (subst if subst else "") + buf[m.end():]
				s = m.start()
		with open(output_filepath, mode='w') as f:
			f.write(buf)
		return

	# Expand 'UserVars.template.py' to 'UserVars.py'
	template_path = os.path.join(workspace, "script", "UserVars.template.py")
	uservars_path = os.path.join(workspace, "script", "UserVars.py")
	Expand(template_path, uservars_path, r"\"<([_\w]+)>\"", ReplaceTags)

	print(f"'{uservars_path}' generated")
	return

# Entry Point
if __name__ == "__main__":
	try:
		# Example command line:
		#sys.argv = ['', '-name', 'Boris', '-ignore_missing']

		# Parse command line
		name = 'Paul'
		ignore_missing = False
		i = 1
		while i < len(sys.argv):
			if sys.argv[i].lower() == "-name":
				name = sys.argv[i+1]
				i = i + 2
			elif sys.argv[i].lower() == "-ignore_missing":
				ignore_missing = True
				i = i + 1
			else:
				raise RuntimeError(f"Unknown command line argument: {sys.argv[i]}")
	

		# Get the current workspace directory from the path of this file
		workspace = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))

		# Setup the UserVars.py
		SetupUserVars(workspace, name, ignore_missing)

	except Exception as ex:
		print(f"ERROR: {str(ex)}")
		sys.exit(-1)
