import sys, os, time, shutil, subprocess, re, socket
import UserVars

# Terminate the script indicating success
def OnSuccess(pause_time_seconds=5):
	print("\n   Success\n\n")
	time.sleep(pause_time_seconds)
	sys.exit(0)

# Terminate the script indicating an error
def OnError(msg,enter_to_close=True):
	print(msg + "\n\n   Failed\n\n")
	if enter_to_close: input("Press enter to close")
	sys.exit(-1)

# Terminate on exception
def OnException(ex,enter_to_close=True):
	OnError("ERROR: " + str(ex),enter_to_close=enter_to_close)

# Check that the UserVars file is the correct version
def CheckVersion(check_version):
	if check_version > UserVars.version:
		raise ValueError("User variables are out of date, please update")
	if UserVars.machine.lower() != socket.gethostname().lower():
		raise ValueError("Machine name does not match UserVars.machine, Check you UserVars.py file")

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
		elif not os.path.isdir(dst): raise FileNotFoundError("ERROR: "+dst+" is not a valid directory")
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
		raise

# Executes a program echoing its output to stdout
def Exec(args, expected_return_code=0, working_dir=".\\", show_arguments=False):
	try:
		if show_arguments: print(args)
		subprocess.check_call(args, cwd=working_dir)
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return
		raise

# Run a program in a separate console window
# Returns the process for the caller to call wait() on,
#  e.g.
#    proc = Spawn(["cmd", "/C" ,"echo Hello"])
#    proc.wait()
def Spawn(args, expected_return_code=0, same_window=False, show_window=True, show_arguments=False):
	try:
		if show_arguments: print(args)
		si = subprocess.STARTUPINFO()
		if not show_window:
			si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
			si.wShowWindow = subprocess.SW_HIDE
		cf = subprocess.CREATE_NEW_CONSOLE
		if same_window:
			cf = 0
		proc = subprocess.Popen(args,creationflags=cf,startupinfo=si)
		return proc
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return
		raise

# Extract data from a text file using a regex
# Capture groups are defined like: (?P<name>.*)
# and accessed like: m.group("name")
# Returns the regex match object or null
def Extract(filepath, regex):
	pat = re.compile(regex)
	with open(filepath) as f:
		for line in f:
			m = pat.search(line)
			if m: return m
		return None

# Modify a file using regex
# Capture groups are defined like: (?P<name>.*)
# and accessed like: m.group("name")
def UpdateFile(filepath, regex, repl, all=False):
	pat = re.compile(regex)
	with open(filepath+".tmp", mode='w') as outf:
		with open(filepath, mode='r') as inf:
			for line in inf:
				m = pat.search(line)
				if not m: outf.write(line)
				else:
					outf.write(pat.sub(repl, line))
					if not all: break;
			for line in inf:
				outf.write(line)
	os.unlink(filepath)
	os.rename(filepath+".tmp", filepath)

# Enumerate recursively through a directory
def EnumFiles(root):
	for dirname, dirnames, filenames in os.walk(root):
		# Return the files
		for filename in filenames:
			yield os.path.join(dirname, filename)
		# We could remove entries from 'dirnames' to
		# prevent recursion into those folders...

# Tests if this script is being run with admin rights, if not restarts the script elevated
def RunAsAdmin(expected_return_code=0, working_dir=".\\", show_arguments=False):
	try:
		if "elevated" in sys.argv: return;
		subprocess.check_output(["net", "session"], stderr=subprocess.STDOUT)
		print("Admin rights available")
		return
	except Exception as ex:
		print("Running script under Administrator account...")
	
	try:
		args = [UserVars.elevate, sys.executable] + sys.argv + ["elevated"];
		if show_arguments: print(args)
		subprocess.check_call(args, cwd=working_dir)
	except Exception:
		pass
	sys.exit(0)

# Touch a file to change it's timestamp
def TouchFile(fname, times=None):
	with open(fname, mode='a') as f:
		os.utime(fname, times=times)
		