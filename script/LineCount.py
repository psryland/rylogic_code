#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Copyright Rylogic Ltd 2012
#
# Build shaders using fxc.exe
# Use:
#  BuildShader.py $(Fullpath) [pp] [obj] [debug] [trace]
#
# Expected input is an hlsl file.
# The file is scanned for: PR_RDR_SHADER_VS, PR_RDR_SHADER_PS, etc
# For each symbol found a compiled shader as header data is generated
# in the output directory.
#
# Add 'pp' to the command line for preprocessed output
# Add 'obj' to the command line for a 'compiled shader object' file
#  that can be used with the runtime shader support in the renderer.
#
import sys, os, tempfile
import Rylogic as Tools
import UserVars

try:
	Tools.CheckVersion(1)

	dirs = [
		r"P:\sdk\pr",
		r"P:\projects",
		]

	excludes = [
		r"\obj",
		r"\Debug",
		r"\Release",
		r"\_ReSharper",
		r"\.metadata",
		r"\directshow.net",
		]
		
	extns = [
		".c",
		".cpp",
		".h",
		".hpp",
		".hlsl",
		".hlsli",
		".cs",
		]
	
	line_count = 0;
	for dir in dirs:
		for fpath in Tools.EnumFiles(dir):
			if any([True for excl in excludes if fpath.find(excl) != -1]):
				continue;
			fname,extn = os.path.splitext(fpath)
			if extn in extns:
				try:
					with open(fpath) as f:
						for i,l in enumerate(f): pass
					print(str(i+1) + " - " + fpath)
					line_count += i + 1
				except Exception as ex:
					print(str(ex))
					print(fpath)
					print(i)

	print("Total Line Count: " + str(line_count))
	Tools.OnSuccess()
	
except Exception as ex:
	Tools.OnException(ex)
