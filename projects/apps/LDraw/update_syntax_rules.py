#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, re
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic as Tools
import UserVars

project_dir = os.path.dirname(__file__)
rylogic_root = Tools.Path(project_dir, "../../..")
ldraw_h = Tools.Path(rylogic_root, "include/pr/view3d-12/ldraw/ldraw.h")

# Update the LdrSyntaxRules.xshd file
def UpdateSyntaxRules():
	rules_filepath = Tools.Path(project_dir, "res", "LdrSyntaxRules.xshd")
	UpdateELdrObjects(rules_filepath)
	UpdateEKeywords(rules_filepath)
	return

# Update the ELdrObject keywords
def UpdateELdrObjects(rules_filepath:str):
	# Read the ELdrObject enum values
	pat = r"#define PR_ENUM_LDRAW_OBJECTS\(x\)\\(.*?)ELdrObject"
	section = Tools.Extract(ldraw_h, pat, regex_flags=re.S, by_line=False).group(1)

	# Generate the ldraw object keywords
	ldraw_objects = ""
	for line in section.splitlines():
		m = re.match(r"\s*x\((.*?)\s*,.*", line)
		if not m: continue
		kw = m.group(1)
		if kw == "Unknown": continue
		ldraw_objects += f"\t\t\t<Word>*{kw}</Word>\n"
	
	tag_beg = "\t\t\t<!-- ##AUTO-GENERATED## ELdrObject Begin -->\n"
	tag_end = "\t\t\t<!-- ##AUTO-GENERATED## ELdrObject End -->\n"
	Tools.UpdateTaggedSection(rules_filepath, tag_beg, tag_end, ldraw_objects)
	return

# Update the EKeyword keywords
def UpdateEKeywords(rules_filepath:str):
	# Read the EKeyword enum values
	pat = r"#define PR_ENUM_LDRAW_KEYWORDS\(x\)\\(.*?)EKeyword"
	section = Tools.Extract(ldraw_h, pat, regex_flags=re.S, by_line=False).group(1)

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
	# Read the EKeyword enum values
	pat = r"#define PR_ENUM_LDRAW_KEYWORDS\(x\)\\(.*?)EKeyword"
	section = Tools.Extract(ldraw_h, pat, regex_flags=re.S, by_line=False).group(1)

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

#Run as standalone script
if __name__ == "__main__":
	try:
		UpdateSyntaxRules()
	except Exception as ex:
		Tools.OnException(ex)
