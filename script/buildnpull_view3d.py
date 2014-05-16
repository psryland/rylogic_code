#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil, re
sys.path.append(os.path.splitdrive(os.path.realpath(__file__))[0] + r"\script")
import Rylogic as Tools
import UserVars

try:
	# build view3d.dll
	Tools.Exec([sys.executable, r"P:\projects\view3d\deploy.py"])
	
	# pull
	Tools.Exec([sys.executable, r"R:\software\SDK\pr\lib\_pull_pr_libs.py"])
	
except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
