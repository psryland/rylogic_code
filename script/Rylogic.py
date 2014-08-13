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
def DiffContent(src,dst,trace=False):
	sfound = os.path.exists(src)
	dfound = os.path.exists(dst)
	if not sfound:
		if trace: print("Content different, '"+src+"' not found")
		return True
	if not dfound:
		if trace: print("Content different, '"+dst+"' not found")
		return True
	if os.path.getsize(src) != os.path.getsize(dst):
		if trace: print("Content different, '"+src+"' and '"+dst+"' have different sizes")
		return True
	with open(src,'rb') as s:
		with open(dst,'rb') as d:
			sd = s.read()
			dd = d.read()
			if sd != dd:
				if trace:
					print("Content different, '"+src+"' and '"+dst+"' have different content")
					print(len(sd))
					for i in range(0,len(sd)):
						if sd[i] != dd[i]:
							print("diff at byte " + str(i) + ": " + str(sd[i]) + " != " + str(dd[i]))
							break
				return True

	if trace: print("'"+src+"' and '"+dst+"' are identical")
	return False

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
		outp = subprocess.check_output(args, universal_newlines=True, stderr=subprocess.STDOUT)
		return True,outp
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return True,e.output
		return False,e.output
		#raise

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
# Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
# Returns the regex match object for the first match or null
def Extract(filepath, regex):
	pat = re.compile(regex)
	with open(filepath) as f:
		for line in f:
			m = pat.search(line)
			if m: return m
	return None

# Extract data from a text file using a regex
# Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
# Returns a collection of matches from within the file
def ExtractMany(filepath, regex):
	pat = re.compile(regex)
	matches = []
	with open(filepath) as f:
		for line in f:
			m = pat.search(line)
			if m: matches = matches + [m]
	return matches
		
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
		# We could remove entries from 'dirnames' to
		# prevent recursion into those folders...
		for filename in filenames:
			yield os.path.join(dirname, filename)

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
		print("Run as Admin failed")
	sys.exit(0)

# Touch a file to change it's timestamp
def TouchFile(fname, times=None):
	with open(fname, mode='a') as f:
		os.utime(fname, times=times)

# Invoke MSBuild
def MSBuild(sln, projects, platforms, configs, parallel=False, same_window=True):
	projs = ";".join(projects)
	procs = []
	for platform in platforms:
		for config in configs:
			args = [UserVars.msbuild, UserVars.msbuild_props, sln, "/t:"+projs, "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"]
			if parallel:
				procs.extend([Tools.Spawn(args, same_window=same_window)])
			else:
				print("\n *** " + platform + " - " + config + " ***\n")
				Exec(args)
	
	# Wait for all processes to finish, and check for error return codes
	errors = False
	for proc in procs:
		proc.wait()
		errors |= proc.returncode != 0
	
	print("\n")
	return not errors

# Deploy a bin tool
def DeployToBin(appname, files, platforms, config, CopyForArch=False):
	print("Deploying...")
	for platform in platforms:
		srcdir = UserVars.root + "\\obj\\v120\\" + appname + "\\" + platform + "\\" + config
		dstdir = UserVars.root + "\\bin\\" + appname + "\\" + platform
		for file in files:
			Copy(srcdir+"\\"+file, dstdir+"\\"+file)
	
	# If this is a single file tool, copy the version for this platform to \bin
	if CopyForArch:
		srcdir = UserVars.root + "\\bin\\" + appname + "\\" + UserVars.arch
		for file in files:
			Copy(srcdir+"\\"+file, UserVars.root+"\\bin\\"+file)

# End