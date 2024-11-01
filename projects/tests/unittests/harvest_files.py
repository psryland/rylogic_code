#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Generate a header file that includes all
# headers in the pr library
# Use:
#   harvest_files.py $(PlatformTarget) $(Configuration)

import sys, os, tempfile
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic, UserVars

try:
	platform = sys.argv[1] if len(sys.argv) > 1 else "any"
	config   = sys.argv[2] if len(sys.argv) > 2 else "release"
	tmpfile = Rylogic.Path(tempfile.gettempdir(), f"unittests.{platform}.{config}.h", check_exists=False)
	outfile = Rylogic.Path(os.path.dirname(__file__), "src\\unittests.h", check_exists=False)

	srcdirs = [
		UserVars.root + "\\include"
	]
	exclude = [
		"pr/geometry/mesh_tools.h",
		"pr/gui/",
		"pr/image/",
		"pr/ldraw/ldr_helper_c.h",
		"pr/ldraw/ldr_ode.h",
		"pr/macros/on_exit.h",
		"pr/maths/pr_to_ode.h",
		"pr/physics/",
		"pr/sound/",
		"pr/storage/xfile/",
		"pr/storage/zip/",
		"pr/script_old/",
		"pr/terrain/",
		"pr/collision/todo/",
		"pr/collision/builder/",
		"pr/view3d-12/",
	]

	# generate a file that includes all headers
	with open(tmpfile, mode='w') as outf:
		outf.write(
			"// This is a generated file\n"
			"#pragma once\n"
			"#include <sdkddkver.h>\n"
			"#include <winsock2.h> // Include winsock2.h before windows.h\n"
			"\n"
			"// Headers to unit test\n"
			)

		for sd in srcdirs:
			for file in Rylogic.EnumFiles(sd):
				file = file.lower().replace("\\","/")
				if os.path.splitext(file)[1] != ".h": continue
				if any([True for excl in exclude if file.find(excl) != -1]): continue
				outf.write(f"#include \"{os.path.relpath(file, sd)}\"\n")

	# swap the tmp file with the file if difference
	# This sometimes fails because 'VS intellisense' (vcpkgsrv.exe) holds a lock on the source file
	Rylogic.Copy(tmpfile, outfile, only_if_modified=True)

	# delete the tmp file
	os.remove(tmpfile)

except Exception as ex:
	Rylogic.OnException(ex)