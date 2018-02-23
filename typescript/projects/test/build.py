import sys, os, shutil, subprocess

root = "P:\\pr\\typescript"
proj = root+"\\projects\\test"
dist = proj+"\\dist"

# Build 'rylogic'
subprocess.check_call(["py", root+"\\rylogic\\build.py"])

# Build 'test'
if os.path.exists(dist): shutil.rmtree(dist)
subprocess.check_call(["npm", "run", "build"], cwd=proj, shell=True)

