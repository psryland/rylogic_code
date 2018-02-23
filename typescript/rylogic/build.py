#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, subprocess

root  = "P:\\pr\\typescript"
proj  = root + "\\rylogic"
built = proj + "\\built"
dist  = proj + "\\dist"

# Install if not installed yet
if not os.path.exists(proj + "\\node_modules"):
	subprocess.check_call(["npm", "install"], cwd=proj, shell=True)

# Clean 'dist'
if os.path.exists(dist):
	shutil.rmtree(dist)

# Build 'rylogic'
subprocess.check_call(["npm", "run", "build-all"], cwd=proj, shell=True)

# Deploy
shutil.copy(dist+"\\rylogic.bundle.js", root+"\\lib\\")
shutil.copy(dist+"\\rylogic.bundle.min.js", root+"\\lib\\")
