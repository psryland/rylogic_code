#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Update auto generated files
# args: auto_gen.py $(RylogicRoot)

import sys, re
from pathlib import Path

root_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("E:/Rylogic/Code")
if root_dir is None or not root_dir.exists():
	raise RuntimeError("Invalid path to Rylogic root directory")

sys.path.append(str(root_dir / "script"))
import Rylogic as Tools

# Convert from a line of text to a C++ string literal
def TransformToCppString(line: str) -> str:
	line = line.replace('\\', '\\\\')
	line = line.replace('"', '\\"')
	line = line.replace('\n', '\\n')
	return f"\"{line}\"\n"

# Convert from C++ code macro to python
def TransformEnumLineToPython(line: str) -> str:
	return re.sub(r"\s*x\(\s*(.*?)\s*,\s*=\s*HashI\(\s*(.*?)\s*\)\).*", r"\1 = HashI(\2)\n", line.strip())

# Convert from C++ code macro to C++
def TransformEnumLineToCpp(line: str) -> str:
	return re.sub(r"\s*x\(\s*(.*?)\s*,\s*=\s*HashI\(\s*(.*?)\s*\)\).*", r"\1 = HashI(\2),\n", line.strip())

# Do the auto gen
def AutoGen():
	# Embed 'ldraw_demo_scene.ldr' into 'ldraw_demo_scene.cpp'
	Tools.ReplaceSection(
		root_dir / "projects/rylogic/view3d-12/src/ldraw/ldraw_demo_scene.ldr",
		root_dir / "projects/rylogic/view3d-12/src/ldraw/ldraw_demo_scene.cpp",
		None,
		None,
		"// AUTO-GENERATED-DEMOSCENE-BEGIN",
		"// AUTO-GENERATED-DEMOSCENE-END",
		TransformToCppString)

	# Update the keywords in 'ldraw_helper.py'
	Tools.ReplaceSection(
		root_dir / "include/pr/view3d-12/ldraw/ldraw.h",
		root_dir / "include/pr/view3d-12/ldraw/ldraw_helper.py",
		"#define PR_ENUM_LDRAW_KEYWORDS(x)",
		"PR_DEFINE_ENUM2_BASE(EKeyword, PR_ENUM_LDRAW_KEYWORDS, int)",
		"# AUTO-GENERATED-KEYWORDS-BEGIN",
		"# AUTO-GENERATED-KEYWORDS-END",
		TransformEnumLineToPython)

	# Update the commands in 'ldraw_helper.py'
	Tools.ReplaceSection(
		root_dir / "include/pr/view3d-12/ldraw/ldraw_commands.h",
		root_dir / "include/pr/view3d-12/ldraw/ldraw_helper.py",
		"#define PR_ENUM_LDRAW_COMMANDS(x)",
		"PR_DEFINE_ENUM2_BASE(ECommandId, PR_ENUM_LDRAW_COMMANDS, int)",
		"# AUTO-GENERATED-COMMANDS-BEGIN",
		"# AUTO-GENERATED-COMMANDS-END",
		TransformEnumLineToPython)
	
	# Update keywords in LDRTemplate.bt
	Tools.ReplaceSection(
		root_dir / "include/pr/view3d-12/ldraw/ldraw.h",
		root_dir / "miscellaneous/010 templates/LDRTemplate.bt",
		"#define PR_ENUM_LDRAW_KEYWORDS(x)",
		"PR_DEFINE_ENUM2_BASE(EKeyword, PR_ENUM_LDRAW_KEYWORDS, int)",
		"// AUTO-GENERATED-KEYWORDS-BEGIN",
		"// AUTO-GENERATED-KEYWORDS-END",
		TransformEnumLineToCpp)

AutoGen()