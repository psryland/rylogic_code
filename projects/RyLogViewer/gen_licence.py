#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os
sys.path.append(os.path.realpath(os.path.dirname(__file__) + "\\..\\..\\script"))
import Rylogic as Tools
import UserVars

Tools.AssertVersion(1)
Tools.AssertPathsExist([UserVars.csex])

print(
	"=============================\n"
	"RyLogViewer Licence Generator\n"
	"=============================\n")

pk = ".\src\licence\private_key.xml"
Tools.Exec([UserVars.csex, "-gencode", "-pk", pk])
Tools.OnSuccess()
