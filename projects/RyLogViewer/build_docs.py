#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, imp, re, subprocess

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
RylogicEnv.CheckVersion(1)

proj    = RylogicEnv.qdrive + r"\projects\RyLogViewer"
docsdir = proj + r"\docs"
resdir  = proj + r"\Resources"

print(
	"*************************************************************************\n"
	"  RyLogViewer Documentation\n"
	"   Copyright © Rylogic Limited 2012\n"
	"\n"
	"   Directory: " + docsdir + "\n"
	"*************************************************************************")
input("Press enter to continue")

def ExportDirectory(dir):
	for file in os.listdir(dir):
		filepath = dir + "\\" + file
		if re.match(r".*(?<!include)\.htm$",filepath, flags=re.IGNORECASE):
			print(filepath)
			outfile = re.sub(r"\.htm", r".html", filepath)
			RylogicEnv.Run(RylogicEnv.csex, '-expand_template -f "'+filepath+'" -o "'+outfile+'"')

try:
	#Process all non-include html template files in each directory
	ExportDirectory(docsdir)
	ExportDirectory(resdir)

	RylogicEnv.OnSuccess()

except Exception as ex:
	print("Error: " + ex)
	RylogicEnv.OnError()
