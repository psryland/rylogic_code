import os, sys, time, shutil, subprocess
from pr import UserVars

def CheckVersion(check_version):
	if check_version > UserVars.version:
		OnError("RylogicEnv.py is out of date")

def OnError(msg):
	print(msg + "\n\n   Failed\n\n")
	input("Press enter to close")
	sys.exit(1)

def OnSuccess():
	print("\n   Success\n\n")
	time.sleep(5)
	sys.exit(0)

# Compares the timestamps of two files and returns true if they are different
def Diff(src,dst):
	sfound = os.path.exists(src)
	dfound = os.path.exists(dst)
	return not sfound or not dfound or os.stat(src).st_mtime != os.stat(dst).st_mtime

# Compares the content of two files and returns true if they are different.
# Ignores file timestamps
def DiffContent(src,dst):
	sfound = os.path.exists(src)
	dfound = os.path.exists(dst)
	if not sfound or not dfound or os.path.getsize(src) != os.path.getsize(dst):
		return True
	with open(src) as s:
		with open(dst) as d:
			return s.read() != d.read()

# Copy 'src' to 'dst' optionally if 'src' is newer than 'dst'
def Copy(src, dst, only_if_modified=True):
	if only_if_modified and not Diff(src,dst):
		print(src + " --> unchanged")
	else:
		print(src + " --> " + dst)
		dirname = os.path.dirname(dst)
		if not os.path.exists(dirname): os.makedirs(dirname)
		shutil.copy2(src, dst)

# Executes a program and returns it's stdout/stderr as a string
def Run(args, expected_return_code=0):
	try:
		return subprocess.check_output(args, universal_newlines=True, stderr=subprocess.STDOUT)
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return e.output
		OnError("ERROR: " + e.output)
	except Exception as e:
		OnError("ERROR: " + str(e))

# Executes a program echoing its output to stdout
def Exec(args, expected_return_code=0):
	try:
		subprocess.check_call(args)
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return
		OnError("ERROR: " + e.output)
	except Exception as e:
		OnError("ERROR: " + str(e))

