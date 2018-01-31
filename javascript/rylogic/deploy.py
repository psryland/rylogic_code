#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import shutil, subprocess

subprocess.check_call(["npm", "run", "build-all"], cwd=".\\", shell=True)
shutil.copy2(".\\dist\\rylogic.js", "..\\lib\\")
shutil.copy2(".\\dist\\rylogic.min.js", "..\\lib\\")
