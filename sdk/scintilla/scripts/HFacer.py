#!/usr/bin/env python3
# HFacer.py - regenerate the Scintilla.h and SciLexer.h files from the Scintilla.iface interface
# definition file.
# Implemented 2000 by Neil Hodgson neilh@scintilla.org
# Requires Python 2.5 or later

import sys
import os
import Face

from FileGenerator import UpdateFile, Generate, Regenerate, UpdateLineInFile, lineEnd

def printLexHFile(f):
	out = []
	out.append("#pragma region SCLEX Constants")
	for name in f.order:
		v = f.features[name]
		if v["FeatureType"] in ["val"]:
			if "SCE_" in name or "SCLEX_" in name:
				#out.append("#define " + name + " " + v["Value"])
				out.append("static int const " + name + " = " + v["Value"] + ";")
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
	return out

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
			#out.append("#define " + featureDefineName + " " + v["Value"])
		elif v["FeatureType"] in ["evt"]:
			featureDefineName = "SCN_" + name.upper()
			out.append("constexpr int " + featureDefineName + " = " + v["Value"] + ";")
			#out.append("#define " + featureDefineName + " " + v["Value"])
		elif v["FeatureType"] in ["val"]:
			if not ("SCE_" in name or "SCLEX_" in name):
				out.append("constexpr int " + name + " = " + v["Value"] + ";")
				#out.append("#define " + name + " " + v["Value"])
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
			#field = field if not field in ["inline","char","delete","return","asm","pascal"] else field + "_"
			#Yay C macros... /s
			#field = field if field != "NULL" else "NONE"
			#field = field if field != "BLACKONWHITE" else "BLACK_ON_WHITE"
			#field = field if field != "STRICT" else "STRICT_"
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
	f.ReadFromFile(root + "include/Scintilla.iface")
	Regenerate(root + "include/Scintilla.h", "/* ", printHFile(f))
	Regenerate(root + "include/SciLexer.h", "/* ", printLexHFile(f))
	Regenerate(root + "include/Scintilla.cs", "/* ", printCSFile(f))
	Regenerate(root + "include/SciLexer.cs", "/* ", printLexCSFile(f))
	if showMaxID:
		valueSet = set(int(x) for x in f.values if int(x) < 3000)
		maximumID = max(valueSet)
		print("Maximum ID is %d" % maximumID)
		#~ valuesUnused = sorted(x for x in range(2001,maximumID) if x not in valueSet)
		#~ print("\nUnused values")
		#~ for v in valuesUnused:
			#~ print(v)

if __name__ == "__main__":
	root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..")) + "/"
	RegenerateAll(root, False)
