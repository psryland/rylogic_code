import sys, os, time, shutil, subprocess, re
import UserVars

# Terminate the script indicating success
def OnSuccess():
	print("\n   Success\n\n")
	time.sleep(5)
	sys.exit(0)

# Terminate the script indicating an error
def OnError(msg):
	print(msg + "\n\n   Failed\n\n")
	input("Press enter to close")
	sys.exit(-1)

# Check that the UserVars file is the correct version
def CheckVersion(check_version):
	if check_version > UserVars.version:
		OnError("User variables are out of date, please update")

# Compare the timestamps of two files and return true if they are different
def Diff(src,dst):
	sfound = os.path.exists(src)
	dfound = os.path.exists(dst)
	return not sfound or not dfound or os.stat(src).st_mtime != os.stat(dst).st_mtime

# Compare the content of two files and return true if they are different, ignoring file timestamps
def DiffContent(src,dst):
	sfound = os.path.exists(src)
	dfound = os.path.exists(dst)
	if not sfound or not dfound or os.path.getsize(src) != os.path.getsize(dst):
		return True
	with open(src) as s:
		with open(dst) as d:
			return s.read() != d.read()

# Copy 'src' to 'dst' optionally if 'src' is newer than 'dst'
def Copy(src, dst, only_if_modified=True, show_unchanged=False):
	# If the 'src' is a directory, copy each file to 'dst' (which must also be a directory)
	if os.path.isdir(src):
		# if 'dst' doesn't exist, create it as a directory or if it does
		# exist, check that it is actually a directory
		if not os.path.exists(dst): os.makedirs(dst)
		elif not os.path.isdir(dst): OnError("ERROR: "+dst+" is not a valid directory")
		# Copy each file in 'src' to 'dst'
		for file in os.listdir(src):
			Copy(src + "\\" + file, dst + "\\" + file, only_if_modified)
		return
	
	# If 'dst' is a directory, use the same filename from 'src'
	if os.path.isdir(dst):
		srcdir,srcfile = os.path.split(src)
		dst = dst.rstrip("/\\") + "\\" + srcfile
	
	# Copy the file to 'dst'
	if not only_if_modified or Diff(src,dst):
		dstdir = os.path.dirname(dst)
		if not os.path.exists(dstdir): os.makedirs(dstdir)
		print(src + " --> " + dst)
		shutil.copy2(src, dst)
		
	elif show_unchanged:
		print(src + " --> unchanged")

# Executes a program and returns it's stdout/stderr as a string
def Run(args, expected_return_code=0,show_arguments=False):
	try:
		if show_arguments: print(args)
		return subprocess.check_output(args, universal_newlines=True, stderr=subprocess.STDOUT)
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return e.output
		OnError("ERROR: " + str(e.output))
	except Exception as e:
		OnError("ERROR: " + str(e))

# Executes a program echoing its output to stdout
def Exec(args, expected_return_code=0, working_dir=".\\"):
	try:
		#print(args)
		subprocess.check_call(args, cwd=working_dir)
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return
		OnError("ERROR: " + str(e.output))
	except Exception as e:
		OnError("ERROR: " + str(e))

# Extract data from a text file using a regex
# Returns the regex match object or null
def Extract(filepath, regex):
	with open(filepath) as f:
		pat = re.compile(regex)
		for line in f:
			m = pat.search(line)
			if m: return m
		return None