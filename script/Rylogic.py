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
def AssertVersion(version):
	if version > UserVars.version:
		raise ValueError("UserVars.version is incorrect. Check your UserVars.py file")

# Validate the machine name is correct for the current machine
def AssertMachineName(machine):
	if UserVars.machine.lower() != socket.gethostname().lower():
		raise ValueError("Machine name does not correct for this PC. Check your UserVars.py file")

# Validate an array of paths for existance.
# Intended for use at the start of a script to validate UserVars.py
def AssertPathsExist(paths):
	missing = [];
	for path in paths:
		if not os.path.exists(path):
			missing.append(path)
	if (len(missing) != 0):
		msg = "Missing paths detected. Check your UserVars.py file\n"
		for p in missing: msg += p + "\n"
		raise FileExistsError(msg)

# Convert a relative or full filepath that might be wrapped in quotes into a full file path
def NormaliseFilepath(filepath):
	filepath = filepath.replace('"','')
	filepath = filepath if os.path.isabs(filepath) else os.path.abspath(filepath)
	return filepath

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
def Copy(src, dst, only_if_modified=True, show_unchanged=False, ignore_non_existing=False):
	
	# Check that source exists
	if not os.path.exists(src):
		if ignore_non_existing:
			return
		else:
			raise FileNotFoundError("ERROR: "+src+" does not exist")
		
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
	if not only_if_modified or DiffContent(src,dst):
		dstdir = os.path.dirname(dst)
		if not os.path.exists(dstdir): os.makedirs(dstdir)
		print(src + " --> " + dst)
		shutil.copy2(src, dst)
		
	elif show_unchanged:
		print(src + " --> unchanged")

# Return the line number of a given byte offset into a file
def LineNumber(fpath, ofs):
	lineno = 0
	with open(fpath, encoding="utf-8") as f:
		for line in f:
			ofs -= len(line)
			if ofs < 0: break
			lineno += 1
	return lineno

# Returns a VS style file link string: "full\filepath.ext(line): "
# One of 'lineno' or 'ofs' must be non-null. 'ofs' is the byte offset into the file
def VSLink(file,lineno=None,ofs=None):
	if lineno is None:
		if ofs is None: raise Exception("One of 'lineno' or 'ofs' must be given")
		try: lineno = LineNumber(file,ofs)
		except: lineno = 0
	return "\n" + file + "(" + str(lineno + 1) + "): "

# Executes a program and returns it's stdout/stderr as a string
def Run(args, expected_return_code=0,show_arguments=False):
	try:
		if show_arguments: print(args)
		outp = subprocess.check_output(args, universal_newlines=True, stderr=subprocess.STDOUT)
		return True,outp
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return True,e.output
		return False,e.output

# Executes a program echoing its output to stdout
def Exec(args, expected_return_code=0, working_dir=".\\", show_arguments=False):
	try:
		if show_arguments: print(args)
		subprocess.check_call(args, cwd=working_dir)
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return
		raise

# Call another script. Remember, you can import and call directly.. probably preferable to this
def Call(script, args, expected_return_code=0,show_arguments=False):
	Exec([sys.executable, script] + args, expected_return_code=expected_return_code, show_arguments=show_arguments)

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
	with open(filepath, encoding="utf-8") as f:
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
	with open(filepath, encoding="utf-8") as f:
		for line in f:
			m = pat.search(line)
			if m: matches = matches + [m]
	return matches

# Template expander
# 'template_filepath' is the template file
# 'output_filepath' is the output file to create
# 'regex_pattern' is a regular expression pattern for the template fields
# 'subst_func' is the callback function that does the text substitution
# To support include files, use a fancy regex pattern and return the entire
# contents of the included file as the result of 'subst_func'
# Usage:
#   def Subst(match):
#       print(match.group()[1:-1])
#       return match.group()[1:-1]
#   Expand(r"template.txt", r"template_output.txt", r"\[([-_\w]+)\]", Subst)
#   Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
def Expand(template_filepath, output_filepath, regex_pattern, subst_func):
	pat = re.compile(regex_pattern)
	with open(template_filepath) as f:
		buf = f.read(-1)
		s = 0
		while True:
			m = pat.search(buf, s)
			if not m: break
			subst = subst_func(m)
			buf = buf[:m.start()] + subst + buf[m.end():]
			s = m.start()
	with open(output_filepath, mode='w') as f:
		f.write(buf)

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
		AssertPathsExist([UserVars.elevate])
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
# e.g.
#   sln = "C:\path\mysolution.sln"
#	projects = [
#		"project_name",
#		"\"folder\proj_name:Rebuild\""
#		]
#	platforms = [
#		"x64",
#		"x86"
#		]
#	configs = [
#		"release",
#		"debug",
#		]
#	Tools.MSBuild(sln, projects, platforms, configs, True, True)
def MSBuild(sln, projects, platforms, configs, parallel=False, same_window=True):
	projs = ";".join(projects)
	procs = []
	AssertPathsExist([UserVars.msbuild])
	for platform in platforms:
		for config in configs:
			args = [UserVars.msbuild, UserVars.msbuild_props, sln, "/t:"+projs, "/p:Configuration="+config+";Platform="+platform, "/m", "/verbosity:minimal", "/nologo"]
			if parallel:
				procs.extend([Spawn(args, same_window=same_window)])
			else:
				print(" *** " + platform + " - " + config + " ***")
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
	AssertPathsExist([UserVars.root])
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

# Create a zip of a directory
def ZipDirectory(zip_path, root_dir):
	zipf = zipfile.ZipFile(zip_path, 'w')
	for root, dirs, files in os.walk(root_dir):
		for file in files:
			filepath = os.path.join(root, file)
			arcpath  = os.path.relpath(filepath, root_dir)
			zipf.write(filepath, arcpath)
	zipf.close()
	
# End