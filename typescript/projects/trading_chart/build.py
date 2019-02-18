#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import sys, os, shutil, subprocess
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import UserVars

root  = os.path.join(UserVars.root, "typescript")
proj  = os.path.join(root, "projects", "trading_chart")
built = os.path.join(proj, "built")
dist  = os.path.join(proj, "dist")

# Install if not installed yet
if not os.path.exists(proj + "\\node_modules"):
	subprocess.check_call(["npm", "install"], cwd=proj, shell=True)

# Clean
if os.path.exists(dist):
	shutil.rmtree(dist)

# Build 'trading_chart'
os.chdir(proj)
subprocess.check_call(["npm", "run", "build-all"], cwd=proj, shell=True)

# Deploy
shutil.copy2(os.path.join(dist ,"trading_chart.bundle.min.js") , os.path.join(root, "lib"))
shutil.copy2(os.path.join(dist ,"trading_chart.bundle.js"    ) , os.path.join(root, "lib"))
