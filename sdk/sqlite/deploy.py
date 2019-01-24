#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

try:

	print(
		"*************************************************************************\n"
		"Sqlite3 Deploy\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	sln = os.path.join(UserVars.root, "sdk", "sqlite", "sqlite3.sln")
	Tools.MSBuild(sln, ["sqlite3"], ["x86","x64"], ["debug","release"], parallel=True, same_window=True)
	Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
