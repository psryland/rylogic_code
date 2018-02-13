#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import shutil, subprocess
root = "P:\\pr\\javascript"

subprocess.check_call(["npm", "run", "build-all"], cwd=root+"\\rylogic", shell=True)
shutil.copy2(root+"\\rylogic\\dist\\rylogic.js", root+"\\lib\\")
shutil.copy2(root+"\\rylogic\\dist\\rylogic.min.js", root+"\\lib\\")

subprocess.check_call(["npm", "run", "build-all"], cwd=root+"\\projects\\trading_chart", shell=True)

