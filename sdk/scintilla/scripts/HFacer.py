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
	for name in f.order:
		v = f.features[name]
		if v["FeatureType"] in ["val"]:
			if "SCE_" in name or "SCLEX_" in name:
				out.append("#define " + name + " " + v["Value"])
	return out

def printLexCSFile(f):
	out = []
	for name in f.order:
		v = f.features[name]
		if v["FeatureType"] in ["val"]:
			if "SCE_" in name or "SCLEX_" in name:
				out.append("\t\tpublic const int " + name + " = " + v["Value"] + ";")
	return out

def printHFile(f):
	out = []
	previousCategory = ""
	anyProvisional = False
	for name in f.order:
		v = f.features[name]
		if v["Category"] != "Deprecated":
			if v["Category"] == "Provisional" and previousCategory != "Provisional":
				out.append("#ifndef SCI_DISABLE_PROVISIONAL")
				anyProvisional = True
			previousCategory = v["Category"]
			if v["FeatureType"] in ["fun", "get", "set"]:
				featureDefineName = "SCI_" + name.upper()
				out.append("#define " + featureDefineName + " " + v["Value"])
			elif v["FeatureType"] in ["evt"]:
				featureDefineName = "SCN_" + name.upper()
				out.append("#define " + featureDefineName + " " + v["Value"])
			elif v["FeatureType"] in ["val"]:
				if not ("SCE_" in name or "SCLEX_" in name):
					out.append("#define " + name + " " + v["Value"])
	if anyProvisional:
		out.append("#endif")
	return out

def printCSFile(f):
	out = []
	previousCategory = ""
	anyProvisional = False
	for name in f.order:
		v = f.features[name]
		if v["Category"] != "Deprecated":
			if v["Category"] == "Provisional" and previousCategory != "Provisional":
				out.append("#ifndef SCI_DISABLE_PROVISIONAL")
				anyProvisional = True
			previousCategory = v["Category"]
			if v["FeatureType"] in ["fun", "get", "set"]:
				featureDefineName = "SCI_" + name.upper()
				out.append("\t\tpublic const int " + featureDefineName + " = unchecked((int)" + v["Value"] + ");")
			elif v["FeatureType"] in ["evt"]:
				featureDefineName = "SCN_" + name.upper()
				out.append("\t\tpublic const int " + featureDefineName + " = unchecked((int)" + v["Value"] + ");")
			elif v["FeatureType"] in ["val"]:
				if not ("SCE_" in name or "SCLEX_" in name):
					out.append("\t\tpublic const int " + name + " = unchecked((int)" + v["Value"] + ");")
	if anyProvisional:
		out.append("#endif")
	return out

def RegenerateAll(root, showMaxID):
	f = Face.Face()
	f.ReadFromFile(os.path.abspath(os.path.join(root, "include", "Scintilla.iface")))
	Regenerate(os.path.abspath(os.path.join(root, "include", "Scintilla.h" )), "/* ", printHFile(f))
	Regenerate(os.path.abspath(os.path.join(root, "include", "SciLexer.h"  )), "/* ", printLexHFile(f))
	Regenerate(os.path.abspath(os.path.join(root, "include", "Scintilla.cs")), "/* ", printCSFile(f))
	Regenerate(os.path.abspath(os.path.join(root, "include", "SciLexer.cs" )), "/* ", printLexCSFile(f))
	if showMaxID:
		valueSet = set(int(x) for x in f.values if int(x) < 3000)
		maximumID = max(valueSet)
		print("Maximum ID is %d" % maximumID)
		#~ valuesUnused = sorted(x for x in range(2001,maximumID) if x not in valueSet)
		#~ print("\nUnused values")
		#~ for v in valuesUnused:
			#~ print(v)

if __name__ == "__main__":
	root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
	RegenerateAll(root, False)
