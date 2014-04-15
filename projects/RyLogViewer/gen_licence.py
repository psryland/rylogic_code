#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
import Rylogic as Tools
import UserVars

Tools.CheckVersion(1)

print(
	"=============================\n"
	"RyLogViewer Licence Generator\n"
	"=============================\n")

pk = ".\src\licence\private_key.xml"
Tools.Exec([UserVars.csex, "-gencode", "-pk", pk])
Tools.OnSuccess()
