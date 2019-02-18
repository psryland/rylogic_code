#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, shutil, subprocess
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import UserVars

root   = os.path.join(UserVars.root, "typescript")
proj   = os.path.join(root, "rylogic")
objdir = os.path.join(proj, "obj")

# Install if not installed yet
if not os.path.exists(proj + "\\node_modules"):
	subprocess.check_call(["npm", "install"], cwd=proj, shell=True)

# Clean 'dist'
if os.path.exists(objdir):
	shutil.rmtree(objdir)

# Build 'rylogic'
subprocess.check_call(["npm", "run", "build-all"], cwd=proj, shell=True)

# Deploy
shutil.copy(objdir+"\\rylogic.bundle.js", root+"\\lib\\")
shutil.copy(objdir+"\\rylogic.bundle.min.js", root+"\\lib\\")
