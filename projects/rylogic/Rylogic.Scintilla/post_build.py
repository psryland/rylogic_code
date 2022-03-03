#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(TargetPath)
import sys, os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic as Tools

try:
	#print(str(sys.argv))
	Tools.UnitTest(sys.argv[1], ["Rylogic.Core.dll"], True)

except Exception as ex:
	Tools.OnException(ex)