#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
from pr import RylogicEnv
from pr import UserVars

RylogicEnv.CheckVersion(1)

print(
	"=============================\n"
	"RyLogViewer Licence Generator\n"
	"=============================\n")

pk = ".\src\licence\private_key.xml"
RylogicEnv.Exec([UserVars.csex, "-gencode", "-pk", pk])
RylogicEnv.OnSuccess()