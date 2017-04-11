#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os, sys, imp, re
sys.path.append(re.sub(r"(\w:[\\/]).*", r"\1script", __file__))
import Rylogic as Tools
import HtmlExpand
import UserVars

# Expands '*.htm' files in a directory and copies them to an output directory
def BuildDocs(srcdir:str, dstdir:str):

	print("Building documentation... "+srcdir+" -> "+dstdir)
	for filepath in Tools.EnumFiles(srcdir):

		# If the file is an '.htm' file, but not an 'include.htm' file
		if re.match(r".*(?<!include)\.htm$", filepath, flags=re.IGNORECASE):

			dir,fname = os.path.split(filepath)
			ftitle,extn = os.path.splitext(fname)
			outfilepath = dstdir + "\\" + ftitle + ".html"

			# Expand the templates in the doc file
			HtmlExpand.ExpandHtmlFile(filepath, outfilepath)


	#proj    = UserVars.root + r"\projects\RyLogViewer"
	#docsdir = proj + r"\docs"
	#resdir  = proj + r"\Resources"
	#
	##Process all non-include html template files in each directory
	#ExportDirectory(docsdir)
	#ExportDirectory(resdir)
	return
