#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import os, sys, subprocess

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
RylogicEnv.CheckVersion(1)

print(
	"=============================\n"
	"RyLogViewer Licence Generator\n"
	"=============================\n")

pk = ".\src\licence\private_key.xml"
RylogicEnv.Run(RylogicEnv.csex, '-gencode -pk "'+pk+'"')
RylogicEnv.OnSuccess()