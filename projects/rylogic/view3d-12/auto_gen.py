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

# Hash a string to a constant value
def HashI(s: str):
	_FNV_offset_basis32 = 2166136261
	_FNV_prime32 = 16777619
	h = _FNV_offset_basis32
	for c in s.lower(): h = 0xFFFFFFFF & ((h ^ ord(c)) * _FNV_prime32)
	return h

# Convert from a line of text to a C++ string literal
def TransformToCppString(line: str) -> str:
	line = line.replace('\\', '\\\\')
	line = line.replace('"', '\\"')
	line = line.replace('\n', '\\n')
	return f"\"{line}\"\n"

# Convert from C++ code macro to C# literal
def TransformEnumLineToCSharp(line: str) -> str:
	m = re.match(r"\s*x\(\s*(.*?)\s*,\s*=\s*HashI\(\s*(.*?)\s*\)\).*", line.strip())
	return f"{m[1]} = unchecked((int){HashI(m[2])}),\n" if m else line.strip()

# Convert from C++ code macro to python
def TransformEnumLineToPython(line: str) -> str:
	m = re.match(r"\s*x\(\s*(.*?)\s*,\s*=\s*HashI\(\s*(.*?)\s*\)\).*", line.strip())
	return f"{m[1]} = {HashI(m[2])}\n" if m else line.strip()

# Convert from C++ code macro to C++
def TransformEnumLineTo010Template(line: str) -> str:
	m = re.match(r"\s*x\(\s*(.*?)\s*,\s*=\s*HashI\(\s*(.*?)\s*\)\).*", line.strip())
	return f"{m[1]} = {HashI(m[2])},\n" if m else line.strip()

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

	# Update the keywords in 'LDraw.cs'
	Tools.ReplaceSection(
		root_dir / "include/pr/view3d-12/ldraw/ldraw.h",
		root_dir / "projects/rylogic/Rylogic.Gfx/src/LDraw/LDraw.cs",
		"#define PR_ENUM_LDRAW_KEYWORDS(x)",
		"PR_DEFINE_ENUM2_BASE(EKeyword, PR_ENUM_LDRAW_KEYWORDS, int)",
		"// AUTO-GENERATED-KEYWORDS-BEGIN",
		"// AUTO-GENERATED-KEYWORDS-END",
		TransformEnumLineToCSharp)

	# Update the keywords in 'ldraw_helper.py'
	Tools.ReplaceSection(
		root_dir / "include/pr/view3d-12/ldraw/ldraw.h",
		root_dir / "projects/rylogic/py-rylogic/rylogic/ldraw/ldraw.py",
		"#define PR_ENUM_LDRAW_KEYWORDS(x)",
		"PR_DEFINE_ENUM2_BASE(EKeyword, PR_ENUM_LDRAW_KEYWORDS, int)",
		"# AUTO-GENERATED-KEYWORDS-BEGIN",
		"# AUTO-GENERATED-KEYWORDS-END",
		TransformEnumLineToPython)

	# Update the commands in 'ldraw_helper.py'
	Tools.ReplaceSection(
		root_dir / "include/pr/view3d-12/ldraw/ldraw_commands.h",
		root_dir / "projects/rylogic/py-rylogic/rylogic/ldraw/ldraw.py",
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
		TransformEnumLineTo010Template)

AutoGen()