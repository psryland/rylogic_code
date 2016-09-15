#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Generate a header file that includes all
# headers in the pr library
# Use:
#   pre_build.py $(ProjectDir)..\unittests $(PlatformTarget) $(Configuration)

import sys, os, tempfile, re
sys.path.append(re.sub(r"^(.:[\\/]).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	dir      = sys.argv[1] if len(sys.argv) > 1 else UserVars.root + "\\projects\\unittests"
	platform = sys.argv[2] if len(sys.argv) > 2 else "any"
	config   = sys.argv[3] if len(sys.argv) > 3 else "release"
	outfile  = dir + "\\unittests.h"
	tempdir  = tempfile.gettempdir() + "\\" + platform + "\\" + config
	tmpfile  = tempdir + "\\unittests.h.tmp"
	os.makedirs(tempdir, exist_ok=True)

	srcdirs = [
		UserVars.root + "\\include"
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
		"pr/script_old/",
		"pr/terrain/",
		"pr/collision/todo/",
		"pr/collision/builder/",
		]

	# generate a file that includes all headers
	with open(tmpfile, mode='w') as outf:
		outf.write(
			"// This is a generated file\n"
			"#pragma once\n"
			"//\n"
			)
		for sd in srcdirs:
			for file in Tools.EnumFiles(sd):
				file = file.lower().replace("\\","/")
				if os.path.splitext(file)[1] != ".h": continue
				if any([True for excl in exclude if file.find(excl) != -1]): continue;
				outf.write("#include \""+os.path.relpath(file, sd)+"\"\n")

	# swap the tmp file with the file if difference
	# This sometimes fails because 'VS intellisense' (vcpkgsrv.exe) holds a lock on the source file
	Tools.Copy(tmpfile, outfile, only_if_modified=True)

	# delete the tmp file
	os.remove(tmpfile)

except Exception as ex:
	Tools.OnException(ex)