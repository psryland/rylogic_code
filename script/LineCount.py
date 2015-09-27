#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Copyright Rylogic Ltd 2012
#
import sys, os, tempfile
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)

	dirs = [
		#r"R:\software\PC\RexConfig",
		#r"R:\software\STM32",
		#r"R:\software\ARM7",
		r"R:\software\SDK\pr",
		#r"R:\software\SDK\pr\projects\Rylogic",
		]

	excludes = [
		r"\STM32 Library",
		r"\obj",
		r"\Debug",
		r"\Release",
		r"\_ReSharper",
		r"\.metadata",
		r"\directshow.net",
		]
		
	extns = [
		".cs",
		".c",
		".cpp",
		".h",
		".hpp",
		# ".hlsl",
		# ".hlsli",
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
