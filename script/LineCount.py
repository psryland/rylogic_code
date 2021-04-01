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
		#"P:\\projects",
		#"P:\\include",
		#"S:\\software\\PC\\RexConfig",
		"S:\\software\\shared",
		"S:\\software\\STM32",
		"S:\\software\\ARM7\\products\\battery_pack_v2",
		#"S:\\software\\SDK\\pr",
		#"S:\\software\\SDK\\pr\\projects\\Rylogic",
		]

	excludes = [
		"\\STM32 Library",
		"\\obj",
		"\\Debug",
		"\\Release",
		"\\_ReSharper",
		"\\.metadata",
		"\\.vscode",
		"\\.vs",
		"\\directshow.net",
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
	
	semi_count = 0
	line_count = 0
	for dir in dirs:
		for fpath in Tools.EnumFiles(dir):
			if any([True for excl in excludes if fpath.find(excl) != -1]):
				continue
			fname,extn = os.path.splitext(fpath)
			if extn in extns:
				try:
					with open(fpath) as f:
						for i,line in enumerate(f):
							semi_count += line.count(';')
							line_count += 1

					print(str(i) + " - " + fpath)
				except Exception as ex:
					print(str(ex))
					print(fpath)
					print(i)

	print("\n")
	print("Total Line Count: " + str(line_count))
	print("Total Semi Colon Count: " + str(semi_count))
	Tools.OnSuccess()
	
except Exception as ex:
	Tools.OnException(ex)
