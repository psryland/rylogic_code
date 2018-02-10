#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import shutil, subprocess

subprocess.check_call(["py", ".\\deploy.py"], cwd="..\\..\\rylogic", shell=True)
subprocess.check_call(["npm", "run", "build"], cwd=".\\", shell=True)

