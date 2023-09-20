#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Notes:
#  - This script creates the UserVars.py file.
#  - It cannot make use of scripts in 'repo/script' because the UserVars.py file may not exist yet in a clean build.
import os, sys, urllib.request, zipfile
nuget_dir = os.path.abspath(os.path.join(os.path.dirname(__file__)))
nuget_url = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

# Download nuget if not already there
def GetNuget():
	# Assume already installed
	if os.path.exists(os.path.join(nuget_dir, "nuget.exe")):
		return

	# Download WiX zip
	nuget_exe = os.path.join(nuget_dir, "nuget.exe")
	urllib.request.urlretrieve(nuget_url, nuget_exe)

	return

# Entrt point
if __name__ == "__main__":
	try:
		GetNuget()
	except Exception as ex:
		print(f"ERROR: {str(ex)}")
		sys.exit(-1)
