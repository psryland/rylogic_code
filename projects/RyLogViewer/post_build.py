#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(ProjectDir) $(TargetDir) $(ConfigurationName)
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars
import BuildDocs

try:
	Tools.AssertVersion(1);

	projdir   = sys.argv[1].rstrip("\\") if len(sys.argv) > 1 else UserVars.root + "\\projects\\RyLogViewer"
	targetdir = sys.argv[2].rstrip("\\") if len(sys.argv) > 2 else UserVars.root + "\\projects\\RyLogViewer\\bin\\Debug"
	config    = sys.argv[3]              if len(sys.argv) > 3 else "Debug"

	docsdir     = targetdir + "\\docs"
	examplesdir = targetdir + "\\examples"
	pluginsdir  = targetdir + "\\plugins"

	# Create the directories in the target directory
	if not os.path.exists(targetdir):   os.makedirs(targetdir)
	if not os.path.exists(docsdir):     os.makedirs(docsdir)
	if not os.path.exists(examplesdir): os.makedirs(examplesdir)
	if not os.path.exists(pluginsdir):  os.makedirs(pluginsdir)

	# Build docs and copy additional files to the target directory
	Tools.Copy(projdir + "\\docs\\img"               , docsdir + "\\img\\")
	BuildDocs.BuildDocs(projdir + "\\docs\\docs"     , docsdir +"\\docs\\")
	BuildDocs.BuildDocs(projdir + "\\docs\\help.htm" , docsdir +"\\")

	Tools.Copy(projdir + "\\examples\\logfile.txt"                   , examplesdir + "\\")
	Tools.Copy(projdir + "\\examples\\code lookup.xml"               , examplesdir + "\\")
	Tools.Copy(projdir + "\\examples\\pattern set.pattern_set"       , examplesdir + "\\")
	Tools.Copy(projdir + "\\examples\\ExamplePlugin\\*.cs"           , examplesdir + "\\ExamplePlugin\\")
	Tools.Copy(projdir + "\\examples\\ExamplePlugin\\*.csproj"       , examplesdir + "\\ExamplePlugin\\")
	Tools.Copy(projdir + "\\src\\plugins\\IMainUI.cs"                , examplesdir + "\\ReferenceSource\\")
	Tools.Copy(projdir + "\\src\\plugins\\ITransformSubstitution.cs" , examplesdir + "\\ReferenceSource\\")
	Tools.Copy(projdir + "\\src\\plugins\\ILogDataSource.cs"         , examplesdir + "\\ReferenceSource\\")

	Tools.Copy(projdir + "\\plugins\\plugins.txt"                                        , pluginsdir + "\\")
	Tools.Copy(projdir + "\\examples\\ExamplePlugin\\bin\\"+config+"\\ExamplePlugin.dll" , pluginsdir + "\\")

	# Sign the binary
	#todo replace this with proper windows signing, and investigate buying a Cert
	#signtool = UserVars.winsdk + "\\bin\\signtool.exe"
	#Tools.Exec([])
	Tools.AssertPath(UserVars.csex)
	Tools.Exec([UserVars.csex, "-signfile", "-f", targetdir+"\\RyLogViewer.exe", "-pk", projdir+"\\src\\licence\\private_key.xml"])

except Exception as ex:
	Tools.OnException(ex)