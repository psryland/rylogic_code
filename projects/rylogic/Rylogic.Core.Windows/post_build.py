#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Use:
#  post_build.py $(TargetPath)
import sys, os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic as Tools

try:
	#print(str(sys.argv))
	sys.argv = ["", "E:/Rylogic/projects/rylogic/Rylogic.Core.Windows/bin/Debug/net8.0-windows/Rylogic.Core.Windows.dll"]
	Tools.UnitTest(sys.argv[1], ["Rylogic.Core.dll"], True)

except Exception as ex:
	Tools.OnException(ex)