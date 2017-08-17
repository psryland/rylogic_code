#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

try:

	print(
		"*************************************************************************\n"
		"Sqlite3 Deploy\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	sln = UserVars.root + "\\sdk\\sqlite\\sqlite3.sln"

	Tools.MSBuild(sln, ["sqlite3"], ["x86","x64"], ["debug","release"], parallel=True, same_window=True)
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
