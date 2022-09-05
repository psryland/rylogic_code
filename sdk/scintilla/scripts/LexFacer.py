#!/usr/bin/env python3
# LexFacer.py - regenerate the SciLexer.h files from the Scintilla.iface interface
# definition file.
# Implemented 2000 by Neil Hodgson neilh@scintilla.org
# Requires Python 3.6 or later

import os, pathlib, sys
import Face
import FileGenerator

def printLexHFile(f):
	out = []
	out.append("#pragma region SCLEX Constants")
	for name in f.order:
		v = f.features[name]
		if v["FeatureType"] in ["val"]:
			if "SCE_" in name or "SCLEX_" in name:
				out.append("constexpr int " + name + " = " + v["Value"] + ";")
	out.append("#pragma endregion")

	out.append("#pragma region SCLEX Enumerations") 
	for name in f.order: 
		v = f.features[name] 
		if v["FeatureType"] != "enu":
			continue
		prefix = v["Value"] 
		out.append("enum class E" + name) 
		out.append("{") 
		for member_name in list(filter(lambda x: x.startswith(prefix), f.order)): 
			field = member_name[len(prefix):] 
			field = field[0:1].upper() + field[1:].lower() 
			field = field if field[0].isalpha() else "_" + field 
			out.append("\t" + field + " = " + member_name + ",") 
		out.append("};") 
	out.append("#pragma endregion") 
	return out

def printLexCSFile(f):
	out = []
	out.append("\t\t#region SCLEX Constants")
	for name in f.order:
		v = f.features[name]
		if v["FeatureType"] in ["val"]:
			if "SCE_" in name or "SCLEX_" in name:
				out.append("\t\tpublic const int " + name + " = " + v["Value"] + ";")
	out.append("\t\t#endregion")

	out.append("\t\t#region SCLEX Enumerations")
	for name in f.order:
		v = f.features[name]
		if v["FeatureType"] != "enu":
			continue
		prefix = v["Value"]
		out.append("\t\tpublic enum E" + name)
		out.append("\t\t{")
		for member_name in list(filter(lambda x: x.startswith(prefix), f.order)):
			field = member_name[len(prefix):]
			field = field[0:1].upper() + field[1:].lower()
			field = field if field[0].isalpha() else "_" + field
			out.append("\t\t\t" + field + " = " + member_name + ",")
		out.append("\t\t}")
	out.append("\t\t#endregion")
	return out

def RegenerateAll(root, _showMaxID):
	f = Face.Face()
	f.ReadFromFile(root / "include/LexicalStyles.iface")
	FileGenerator.Regenerate(root / "include/SciLexer.h", "/* ", printLexHFile(f))
	FileGenerator.Regenerate(root / "include/SciLexer.cs", "/* ", printLexCSFile(f))

if __name__ == "__main__":
	RegenerateAll(pathlib.Path(__file__).resolve().parent.parent, True)
