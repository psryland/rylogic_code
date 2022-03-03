#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, re
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../script")))
import Rylogic as Tools
import UserVars

# Update the ELdrObject keywords
def UpdateELdrObjects(rules_filepath:str):

	ldr_object_h = Tools.Path(project_dir, "../../../../include/pr/ldraw/ldr_object.h")
	if not os.path.exists(ldr_object_h):
		raise FileNotFoundError(f"ldr_object.h file not found at '{ldr_object_h}'")

	# Read the ELdrObject enum values
	pat = r"#define PR_ENUM_LDROBJECTS\(x\)\\(.*?)ELdrObject"
	section = Tools.Extract(ldr_object_h, pat, regex_flags=re.S, by_line=False).group(1)

	# Generate the ldr object keywords
	ldr_objects = ""
	for line in section.splitlines():
		m = re.match(r"\s*x\((.*?)\s*,.*", line)
		if not m: continue
		kw = m.group(1)
		if kw == "Unknown": continue
		ldr_objects += f"\t\t\t<Word>*{kw}</Word>\n"
	
	tag_beg = "\t\t\t<!-- ##AUTO-GENERATED## ELdrObject Begin -->\n"
	tag_end = "\t\t\t<!-- ##AUTO-GENERATED## ELdrObject End -->\n"
	Tools.UpdateTaggedSection(rules_filepath, tag_beg, tag_end, ldr_objects)
	return

# Update the EKeyword keywords
def UpdateEKeywords(rules_filepath:str):

	ldr_object_h = Tools.Path(project_dir, "../../../../include/pr/ldraw/ldr_object.h")
	if not os.path.exists(ldr_object_h):
		raise FileNotFoundError(f"ldr_object.h file not found at '{ldr_object_h}'")

	# Read the EKeyword enum values
	pat = r"#define PR_ENUM_LDRKEYWORDS\(x\)\\(.*?)EKeyword"
	section = Tools.Extract(ldr_object_h, pat, regex_flags=re.S, by_line=False).group(1)

	# Generate the ldr object keywords
	keywords = ""
	for line in section.splitlines():
		m = re.match(r"\s*x\((.*?)\s*,.*", line)
		if not m: continue
		kw = m.group(1)
		keywords += f"\t\t\t<Word>*{kw}</Word>\n"
	
	tag_beg = "\t\t\t<!-- ##AUTO-GENERATED## EKeyword Begin -->\n"
	tag_end = "\t\t\t<!-- ##AUTO-GENERATED## EKeyword End -->\n"
	Tools.UpdateTaggedSection(rules_filepath, tag_beg, tag_end, keywords)
	return

# Update the EKeyword keywords
def UpdateEPreprocessor(rules_filepath:str):

	ldr_object_h = Tools.Path(project_dir, "../../../../include/pr/ldraw/ldr_object.h")
	if not os.path.exists(ldr_object_h):
		raise FileNotFoundError(f"ldr_object.h file not found at '{ldr_object_h}'")

	# Read the EKeyword enum values
	pat = r"#define PR_ENUM_LDRKEYWORDS\(x\)\\(.*?)EKeyword"
	section = Tools.Extract(ldr_object_h, pat, regex_flags=re.S, by_line=False).group(1)

	# Generate the ldr object keywords
	keywords = ""
	for line in section.splitlines():
		m = re.match(r"\s*x\((.*?)\s*,.*", line)
		if not m: continue
		kw = m.group(1)
		keywords += f"\t\t\t<Word>*{kw}</Word>\n"
	
	tag_beg = "\t\t\t<!-- ##AUTO-GENERATED## EKeyword Begin -->\n"
	tag_end = "\t\t\t<!-- ##AUTO-GENERATED## EKeyword End -->\n"
	Tools.UpdateTaggedSection(rules_filepath, tag_beg, tag_end, keywords)
	return

# Update the LdrSyntaxRules.xshd file
def UpdateSyntaxRules(project_dir:str):

	rules_filepath = os.path.abspath(os.path.join(project_dir, "res", "LdrSyntaxRules.xshd"))
	if not os.path.exists(rules_filepath):
		raise FileNotFoundError(f"Avalon script rules file not found at '{rules_filepath}'")

	UpdateELdrObjects(rules_filepath)
	UpdateEKeywords(rules_filepath)
	return

#Run as standalone script
if __name__ == "__main__":
	try:
		project_dir = os.path.dirname(__file__)
		UpdateSyntaxRules(project_dir)

	except Exception as ex:
		Tools.OnException(ex)
