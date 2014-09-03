#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil, re
import Rylogic as Tools
import UserVars

try:
	# build view3d.dll
	Tools.Exec([sys.executable, r"P:\projects\view3d\deploy.py", "nowait"])
	
	# pull
	Tools.Exec([sys.executable, r"R:\software\SDK\pr\lib\_pull_pr_libs.py", "nowait"])
	
	Tools.OnSuccess();
	
except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
