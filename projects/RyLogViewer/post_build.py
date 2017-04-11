#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"(\w:[\\/]).*", r"\1script", __file__))
import Rylogic as Tools
import UserVars
import BuildDocs

try:
	Tools.CheckVersion(1);

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else Tools.root + "\\projects\\RyLogViewer"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else Tools.root + "\\projects\\RyLogViewer\\bin\\Debug"
	config    = sys.argv[3]              if len(sys.argv) > 3 else "Debug"

	docsdir     = targetdir + "\\docs"
	examplesdir = targetdir + "\\examples"
	pluginsdir  = targetdir + "\\plugins"

	# Create the directories in the target directory
	if not os.path.exists(targetdir): os.makedirs(targetdir)
	if not os.path.exists(docsdir): os.makedirs(docsdir)
	if not os.path.exists(examplesdir): os.makedirs(examplesdir)
	if not os.path.exists(pluginsdir): os.makedirs(pluginsdir)

	# Build docs
	BuildDocs.BuildDocs(projdir + "\\documentation", docsdir)

	# Copy additional files to the target directory
	Tools.Copy(projdir + 
	# Sign the binary
	#$(ProjectDir)..\..\bin\csex\csex.exe -signfile -f "$(TargetPath)" -pk "$(ProjectDir)src\licence\private_key.xml"

except Exception as ex:
	Tools.OnException(ex)