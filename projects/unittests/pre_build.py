#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Generate a header file that includes all
# headers in the pr library
# Use:
#   pre_build.py $(ProjectDir)..\unittests

import sys, os
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
import Rylogic as Tools
import UserVars

Tools.CheckVersion(1)

dir = sys.argv[1]
outfile = dir + "\\unittests.h"
tmpfile = outfile + ".tmp"

srcdirs = [
	"\\sdk\\pr\\pr"
	]

exclude = [
	"pr/geometry/mesh_tools.h",
	"pr/gui/",
	"pr/image/",
	"pr/linedrawer/customobjectdata.h",
	"pr/linedrawer/ldr_helper_c.h",
	"pr/linedrawer/ldr_ode.h",
	"pr/macros/on_exit.h",
	"pr/maths/pr_to_ode.h",
	"pr/physics/",
	"pr/sound/",
	"pr/storage/xfile",
	"pr/terrain/",
	]

try:
	# generate a file that includes all headers
	with open(tmpfile, mode='w') as outf:
		outf.write(
			"#ifndef PR_UNITTESTS_UNITTESTS_H\n"
			"#define PR_UNITTESTS_UNITTESTS_H\n"
			"//\n"
			"// This is a generated file\n"
			"//\n"
			)
		for sd in srcdirs:
			for file in Tools.EnumFiles(sd):
				file = file.lower().replace("\\","/")
				if os.path.splitext(file)[1] != ".h": continue
				if any([True for excl in exclude if file.find(excl) != -1]): continue;
				outf.write("#include \""+file[file.rfind("pr/"):]+"\"\n")
				
		outf.write(
			"\n"
			"#endif\n"
			)

	# swap the tmp file with the file if difference
	if Tools.DiffContent(outfile, tmpfile):
		Tools.Copy(tmpfile, outfile)

	# delete the tmp file
	os.remove(tmpfile)

except Exception as ex:
	Tools.OnException(ex)