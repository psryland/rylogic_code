#!/usr/bin/env python3
# HFacer.py - regenerate the Scintilla.h and SciLexer.h files from the Scintilla.iface interface
# definition file.
# Implemented 2000 by Neil Hodgson neilh@scintilla.org
# Requires Python 2.5 or later

import os, pathlib, sys
import Face
import FileGenerator

def printHFile(f):
	out = []

	out.append("#pragma region SCI Constants")
	for name in f.order:
		v = f.features[name]
		if v["Category"] == "Deprecated":
			continue
		elif v["FeatureType"] in ["fun", "get", "set"]:
			featureDefineName = "SCI_" + name.upper()
			out.append("constexpr int " + featureDefineName + " = " + v["Value"] + ";")
		elif v["FeatureType"] in ["evt"]:
			featureDefineName = "SCN_" + name.upper()
			out.append("constexpr int " + featureDefineName + " = " + v["Value"] + ";")
		elif v["FeatureType"] in ["val"]:
			if not ("SCE_" in name or "SCLEX_" in name):
				out.append("constexpr int " + name + " = " + v["Value"] + ";")
	out.append("#pragma endregion")

	out.append("#pragma region SCI Enumerations")
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

def printCSFile(f):
	out = []

	out.append("\t\t#region SCI Constants")
	for name in f.order:
		v = f.features[name]
		if v["Category"] == "Deprecated":
			continue
		elif v["FeatureType"] in ["fun", "get", "set"]:
			featureDefineName = "SCI_" + name.upper()
			out.append("\t\tpublic const int " + featureDefineName + " = unchecked((int)" + v["Value"] + ");")
		elif v["FeatureType"] in ["evt"]:
			featureDefineName = "SCN_" + name.upper()
			out.append("\t\tpublic const int " + featureDefineName + " = unchecked((int)" + v["Value"] + ");")
		elif v["FeatureType"] in ["val"]:
			if not ("SCE_" in name or "SCLEX_" in name):
				out.append("\t\tpublic const int " + name + " = unchecked((int)" + v["Value"] + ");")
	out.append("\t\t#endregion")

	out.append("\t\t#region SCI Enumerations")
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

def RegenerateAll(root, showMaxID):
	f = Face.Face()
	f.ReadFromFile(root / "include/Scintilla.iface")
	FileGenerator.Regenerate(root / "include/Scintilla.h", "/* ", printHFile(f))
	FileGenerator.Regenerate(root / "include/Scintilla.cs", "/* ", printCSFile(f))
	if showMaxID:
		valueSet = set(int(x) for x in f.values if int(x) < 3000)
		maximumID = max(valueSet)
		print("Maximum ID is %d" % maximumID)

if __name__ == "__main__":
	RegenerateAll(pathlib.Path(__file__).resolve().parent.parent, False)
