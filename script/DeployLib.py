#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

# Post Build Event for exporting library files to a directory
# Use:
#   DeployLib.py $(TargetPath) $(PlatformName) $(ConfigurationName)
# 
# Note: 
#  pdb files are associated with the file name at the time they are build so it is
#  not possible to rename the lib and pdb. 
import sys, os
import Rylogic, Build

# Entry point
if __name__ == "__main__":
	try:
		#sys.argv = ["", "P:\\pr\\obj\\v143\\audio.dll\\x64\\Debug\\audio.dll", "x64", "Debug"]
		target_path = sys.argv[1] if len(sys.argv) > 1 else input("Target Path? ")
		platform   = sys.argv[2] if len(sys.argv) > 2 else input("Platform (x86,x64)? ")
		config     = sys.argv[3] if len(sys.argv) > 3 else input("Configuration (debug,release)? ")
		if platform.lower() == "win32": platform = "x86"

		target_dir, target_file = os.path.split(target_path)
		target_name, _=  os.path.splitext(target_file)
		obj_dir = Rylogic.Path(target_dir, "..\\..")

		Build.DeployLib(target_name, obj_dir, [platform], [config])

	except Exception as ex:
		Rylogic.OnException(ex)
