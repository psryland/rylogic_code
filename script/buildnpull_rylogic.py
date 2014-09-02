#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil, re
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	# build rylogic.dll
	Tools.Exec([sys.executable, UserVars.root + r"\projects\Rylogic\deploy.py", "nowait"])

	# pull
	Tools.Exec([sys.executable, r"R:\software\SDK\pr\lib\_pull_pr_libs.py", "nowait"])

	config = "debug"
	Tools.Copy("R:\\software\\SDK\\pr\\lib\\anycpu\\" + config + "\\rylogic.dll", "R:\\software\\SDK\\pr\\lib\\anycpu\\");
	Tools.Copy("R:\\software\\SDK\\pr\\lib\\anycpu\\" + config + "\\rylogic.pdb", "R:\\software\\SDK\\pr\\lib\\anycpu\\");

	Tools.OnSuccess();

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
