#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import HtmlExpand
import UserVars

# Export all html template files in a directory
def ExportDirectory(dir):
	for file in os.listdir(dir):
		filepath = dir + "\\" + file
		if re.match(r".*(?<!include)\.htm$",filepath, flags=re.IGNORECASE):
			print(filepath)
			outfile = re.sub(r"\.htm", r".html", filepath)
			HtmlExpand.ExpandHtmlFile(filepath, outfile)

try:
	print(
		"*************************************************************************\n"
		"  RyLogViewer Documentation\n"
		"   Copyright (c) Rylogic Limited 2012\n"
		"*************************************************************************")

	Tools.CheckVersion(1)

	proj    = UserVars.root + r"\projects\RyLogViewer"
	docsdir = proj + r"\docs"
	resdir  = proj + r"\Resources"

	#Process all non-include html template files in each directory
	ExportDirectory(docsdir)
	ExportDirectory(resdir)

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError(str(ex))
