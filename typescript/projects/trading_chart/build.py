#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, subprocess

root  = "P:\\pr\\typescript"
proj  = root + "\\projects\\trading_chart"
built = proj + "\\built"
dist  = proj + "\\dist"

# Clean
if os.path.exists(dist):
	shutil.rmtree(dist)

# Build 'trading_chart'
os.chdir(proj)
subprocess.check_call(["npm", "run", "build-all"], cwd=proj, shell=True)

# Deploy
shutil.copy2(dist+"\\trading_chart.bundle.js", root+"\\lib\\")
shutil.copy2(dist+"\\trading_chart.bundle.min.js", root+"\\lib\\")
