#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os, sys, imp, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import HtmlExpand
import UserVars

# Expands '*.htm' files in a directory and copies them to an output directory
# 'src' can be a filepath or a directory. 'dst' must be a directory
def BuildDocs(src:str, dst:str):

	src = os.path.abspath(src)
	dst = os.path.abspath(dst)

	# Export a directory containing 'htm' files
	if os.path.isdir(src):

		# If the file is an '.htm' file, but not an 'include.htm' file
		for filepath in Tools.EnumFiles(src, filter=r".*(?<!include)\.htm$", flags=re.IGNORECASE):

			# Maintain the relative directory structure in 'dstdir'
			relpath = os.path.relpath(filepath, src)
			dir,fname = os.path.split(relpath)
			outdir = os.path.join(dst, dir)

			# Expand the 'htm' file
			HtmlExpand.ExpandHtmlFile(filepath, outdir)

	else:

		# Expand the 'htm' file
		HtmlExpand.ExpandHtmlFile(src, dst)

	return
