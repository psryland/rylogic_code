#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, re, enum, time, shutil, glob, subprocess, threading, socket, code
import zipfile, ctypes, hashlib, urllib.request, getpass
import xml.etree.ElementTree as xml
import xml.dom.minidom as minidom
from typing import Callable, List, Optional
from pathlib import Path as path
import UserVars

# Support symlink on windows
# On windows, the os.symlink function throws an error when the user doesn't have
# privileges, or when the OS is too old. From windows 10 creator's update onward
# it's possible to create symlinks without admin rights. Prior to that you needed
# to be admin. This bit of code, replaces the symlink function for windows so that
# it includes the "create without admin privilege" flag
if sys.platform == "win32":
	import ctypes
	csl = ctypes.windll.kernel32.CreateSymbolicLinkW
	csl.argtypes = (ctypes.c_wchar_p, ctypes.c_wchar_p, ctypes.c_uint32)
	csl.restype = ctypes.c_ubyte

	def CreateSymLink(src:str, dst:str, is_dir:bool):
		# SYMBOLIC_LINK_FLAG_DIRECTORY = 1
		# SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2
		flags = 2 | (1 if src != None and is_dir else 0)
		r = csl(dst, src, flags)
		if r == 0: raise ctypes.WinError()

	global os
	os.symlink = CreateSymLink

# Catch and return an exception
def Catch(func:Callable):
	try: func()
	except Exception as ex: return ex
	return None

# Terminate the script indicating success
def OnSuccess(msg = "\nSuccess\n", enter_to_close=False, pause_time_seconds=5, sys_exit=False):
	print(msg)
	try:
		if enter_to_close: input("Press enter to close")
		else: time.sleep(pause_time_seconds)
	except:
		pass
	if sys_exit:
		sys.exit(0)

# Terminate the script indicating an error
def OnError(msg = "\nFailed\n", enter_to_close=False, pause_time_seconds=0, sys_exit=False):
	print(msg)
	try:
		if enter_to_close: input("Press enter to close")
		else: time.sleep(pause_time_seconds)
	except:
		pass
	if sys_exit:
		sys.exit(-1)

# Terminate on exception
def OnException(ex,enter_to_close=False, pause_time_seconds=0):
	OnError("ERROR: " + str(ex), enter_to_close=enter_to_close, pause_time_seconds=pause_time_seconds)
	return

# Path join/check
def Path(*args, check_exists:bool = True, normalise:bool = True) -> str:
	return UserVars.Path(*args, check_exists=check_exists, normalise=normalise)

# Import a module with a given name from a file location
def Import(name:str, path:str):
	return UserVars.Import(name, path)

# Check that the UserVars file is the correct version
def AssertVersion(version):
	if version > UserVars.version:
		raise ValueError("UserVars.version is incorrect. Check your UserVars.py file")

# Validate an array of paths for existence.
# Intended for use at the start of a script to validate UserVars.py
def AssertPathsExist(paths):
	missing = []
	for path in paths:
		if not path or not os.path.exists(path):
			missing.append(path)
	if (len(missing) != 0):
		msg = "Missing paths detected:\n"
		for p in missing: msg += f"\t{p}\n" if p else "\tNone\n"
		msg += "Check your UserVars.py file\n"
		raise FileExistsError(msg)

# Validate that a single path exists. Returns the path for method chaining
def AssertPath(path:str, name:str=None):
	if path and os.path.exists(path): return path
	raise FileExistsError(f"Path {name if name else path} does not exist. Check UserVars.py")

# Check that 'winsdk_path' is the latest version
def AssertLatestWinSDK():
	for dir in os.listdir(os.path.join(UserVars.winsdk, "bin")):
		if not re.match(r"\d+\.\d+\.\d+\.\d+", dir, 0): continue
		if dir > UserVars.winsdkvers:
			raise ValueError(f"Newer windows SDK version found: {dir} (Currently: {UserVars.winsdkvers})")
	return
	
# Validate the machine name is correct for the current machine
def MachineName():
	return socket.gethostname().lower()

# Convert a relative or full filepath that might be wrapped in quotes into a full file path
def NormaliseFilepath(filepath):
	filepath = filepath.replace('"','')
	filepath = filepath if os.path.isabs(filepath) else os.path.abspath(filepath)
	return filepath

# Ask for user input with a timeout. Returns [True, input] on success, or [False, ""] on timeout
def InputWithTimeout(msg, timeout_s):
	res = [False, ""]
	e = threading.Event()
	def prompt(r):
		r[1] = input(msg)
		r[0] = True
		e.set()
	threading.Thread(target=prompt, args=[res]).start()
	e.wait(timeout_s)
	return res

# Delete a file or directory tree using the shell
def ShellDelete(path, wait_time_s:float = 0.1, max_wait_time_s:float = 1.0):
	if not os.path.exists(path): return
	t0 = time.process_time()
	while time.process_time() - t0 < max_wait_time_s:
		shutil.rmtree(path, ignore_errors=True)
		time.sleep(wait_time_s) # Give the OS time to do it
		if not os.path.exists(path): return
	raise Exception(f"Failed to delete '{path}'. Check for locked files")

# Change the file extension on 'path'. 'extn' should include the dot
def ChgExtn(filepath:str, extn:str):
	dir_fname,_ = os.path.splitext(filepath)
	return dir_fname + extn

# Enumerate recursively through a directory returning the filepaths.
# 'filter' is a regex that selects the paths to be returned.
def EnumFiles(root, filter:re=None):
	for dirname, _, filenames in os.walk(root):
		# We could remove entries from 'dirnames' to prevent recursion into those folders...
		if filter: filenames = [f for f in filenames if filter.match(f)]
		for filename in filenames:
			yield os.path.join(dirname, filename)

# Enumerate recursively through a directory returning the directory names
# 'filter' is a regex that selects the paths to be returned.
def EnumDirs(root, filter:re=None):
	for dirname, dirnames, _ in os.walk(root):
		# Remove the dir names that don't match the filter. This prevents recursion into those folders
		if filter: dirnames = [d for d in dirnames if filter.match(d)]
		for dir in dirnames:
			yield os.path.join(dirname, dir)

# Enumerate recursively through a directory returning files and directory names
# 'filter' is a regex that selects the paths to be returned.
# Returns a pair: (path, is_directory)
def EnumPaths(root, dir_filter:re=None, file_filter:re=None):
	for dirname, dirnames, filenames in os.walk(root):
		# Remove the dir names that don't match the filter. This prevents recursion into those folders
		if dir_filter: dirnames = [d for d in dirnames if dir_filter.match(d)]
		for dir in dirnames:
			yield (os.path.join(dirname, dir), True)
		if file_filter: filenames = [f for f in filenames if file_filter.match(f)]
		for filename in filenames:
			yield (os.path.join(dirname, filename), False)

# Read the contents of a file into a buffer
def ReadFile(path, mode='rb', encoding="utf-8"):
	with open(path, mode, encoding=encoding) as f:
		return f.read()

# Write the contents of a buffer into a file called 'path'
def SaveFile(path, buf, mode='wb', encoding="utf-8"):
	with open(path, mode, encoding=encoding) as f:
		f.write(buf)

# Return the hash of a file
def HashFile(filepath, hasher = None, blocksize = 65536):
	hasher = hasher if hasher else hashlib.md5()
	with open(filepath, 'rb') as file:
		buf = file.read(blocksize)
		while len(buf) > 0:
			hasher.update(buf)
			buf = file.read(blocksize)
	
	return hasher.hexdigest()

# Compare the timestamps of two files and return true if they are different
def Diff(src, dst):
	sfound = os.path.exists(src)
	dfound = os.path.exists(dst)
	return not sfound or not dfound or os.stat(src).st_mtime != os.stat(dst).st_mtime

# Compare the content of two files and return true if they are different, ignoring file timestamps
def DiffContent(src, dst, trace=False, blocksize = 65536):
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
			sd = s.read(blocksize)
			dd = d.read(blocksize)
			blk = 0
			while len(sd) > 0 or len(dd) > 0:
				if sd != dd: break
				sd = s.read(blocksize)
				dd = d.read(blocksize)
				blk += 1
			
			if len(sd) != 0 or len(dd) != 0:
				if trace:
					print("Content different, '"+src+"' and '"+dst+"' have different content ")
					for i in range(0,len(sd)):
						if sd[i] != dd[i]:
							print("diff at byte " + str(i+blk*blocksize) + ": " + str(sd[i]) + " != " + str(dd[i]))
							break
				return True

	if trace: print("'"+src+"' and '"+dst+"' are identical")
	return False

# Compare the content of two files by calculating and comparing hashes, ignoring file timestamps
def DiffHash(src,dst,trace=False):
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
	if not HashFile(src) == HashFile(dst):
		if trace: print("Content different, '"+src+"' and '"+dst+"' have different hashes")
		return True

	if trace: print("'"+src+"' and '"+dst+"' are identical")
	return False
	
# Copy 'src' to 'dst' optionally if 'src' is newer than 'dst'
def Copy(src, dst, only_if_modified=True, show_unchanged=False, ignore_missing=False, quiet=False, full_paths=True, filter:Optional[str]=None, filter_flags=0, follow_symlinks=True):

	src_is_dir = src.endswith("/") or src.endswith("\\")
	dst_is_dir = dst.endswith("/") or dst.endswith("\\")
	src = os.path.abspath(src)
	dst = os.path.abspath(dst)
	src_is_dir |= os.path.isdir(src)
	dst_is_dir |= os.path.isdir(dst) or src_is_dir

	# Find the source files/directories to copy (filenames only, not full paths)
	lst = []
	if src_is_dir:
		lst += [f for f in os.listdir(src)]
	elif os.path.exists(src):
		lst += [os.path.split(src)[1]]
	elif "*" in src or "?" in src:
		lst += [os.path.split(f)[1] for f in glob.glob(src)]
	elif not ignore_missing:
		raise FileNotFoundError(f"ERROR: {src} does not exist")

	# If the 'src' represents multiple files, 'dst' must be a directory
	if src_is_dir or len(lst) > 1:
		# if 'dst' doesn't exist, assume it's a directory
		if not os.path.exists(dst):
			dst_is_dir = True
		# or if it does exist, check that it is actually a directory
		elif not dst_is_dir:
			raise FileNotFoundError(f"ERROR: {dst} is not a valid directory")

	srcdir = (src if src_is_dir else os.path.dirname(src)).rstrip("/\\")
	dstdir = (dst if dst_is_dir else os.path.dirname(dst)).rstrip("/\\")

	# Ensure 'dstdir' exists
	if not os.path.exists(dstdir):
		os.makedirs(dstdir)
		if src_is_dir: shutil.copystat(src, dstdir)
		if not quiet: print(srcdir + " --> " + dstdir)

	# Copy the source files/directories to 'dst'
	for src_item in lst:
		s = os.path.join(srcdir, src_item)
		d = os.path.join(dstdir, src_item) if dst_is_dir else dst

		# Test for symlinks...
		# Needs an option to copy symlinks, or follow the links and copy what they point to
		if os.path.islink(s):
			raise AssertionError("ERROR: This copy function doesn't consider symlinks yet. It's a todo...")

		# Call recursively for directory copies
		if os.path.isdir(s):
			Copy(s, d+"\\", only_if_modified, show_unchanged, ignore_missing, quiet, filter, filter_flags, follow_symlinks)

		# Copy the file
		else:
			# Copy if not excluded by the filter
			if filter and not re.match(filter, s, filter_flags):
				continue

				# Copy if modified or always based on the flag
			if only_if_modified and not DiffContent(s,d):
				if not quiet and show_unchanged: print(s + " --> unchanged")
				continue

			# Copy the file
			if not quiet: print(s + " --> " + d) if full_paths else print(os.path.split(s)[1] + " --> " + os.path.split(d)[1])
			shutil.copy2(s, d, follow_symlinks=follow_symlinks)

	return

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

# Replaces GCC style "file:line:col:" substrings with VS style "file(line):"
# Handles byte arrays or strings
def Gcc2Vs(line):
	# (?:\w:[/\\])? = optional drive letter followed by '\' or '/'
	# (?:[\w\-. ]+[/\\])* = 0 or more subdirectories
	# (?:[\w\-. ]+) = file name including extension
	# :(\d+) = :line_number
	# (?::\d+) = optional :column number
	pat = r"((?:\w:[/\\])?(?:[\w\-. ]+[/\\])*(?:[\w\-. ]+)):(\d+)(?::\d+)"
	repl = r"\g<1>(\g<2>)"
	if isinstance(line, bytes):
		return re.sub(pat.encode(), repl.encode(), line)
	else:
		return re.sub(pat, repl, line)

# Executes a program and returns it's stdout/stderr as a string
# 'checked' will raise an exception if the program returns a non-zero exit code
# 'return_output' will return the output of the program as a string. If false, the output is printed to stdout
# 'expected_return_code' is the expected return code. If the program returns a different code, an exception is raised
# Returns (result,output)
def Run(args, checked=False, return_output=True, expected_return_code=0, show_arguments=False, cwd=None, shell=False, encoding='utf-8'):
	try:
		if show_arguments: print(args)
		if return_output:
			outp = subprocess.check_output(args, universal_newlines=True, stderr=subprocess.STDOUT, cwd=cwd, shell=shell, encoding=encoding)
			return True,outp
		else:
			subprocess.check_call(args, universal_newlines=True, stderr=subprocess.STDOUT, cwd=cwd, shell=shell)
			return True,""

	except subprocess.CalledProcessError as e:
		if return_output:
			if checked: raise
			return e.returncode == expected_return_code,e.output
		else:
			print(e.output)
			if checked: raise
			return e.returncode == expected_return_code,""

# Executes a program echoing its output to stdout
def Exec(args, expected_return_code=0, show_arguments=False, cwd=None, shell=False):
	print("DEPRECATED: Update to 'Run'")
	Run(args, checked=True, return_output=False, expected_return_code=expected_return_code, show_arguments=show_arguments, cwd=cwd, shell=shell)

# Call another script. Remember, you can import and call directly.. probably preferable to this
def Call(script, args=[], expected_return_code=0,show_arguments=False):
	Exec(["py.exe", script] + args, expected_return_code=expected_return_code, show_arguments=show_arguments)
	return

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
		proc = subprocess.Popen(args, creationflags=cf, startupinfo=si)
		return proc
	except subprocess.CalledProcessError as e:
		if e.returncode == expected_return_code: return
		raise

# Extract data from a text file using a regex
# Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
# Returns the regex match object for the first match or null
# Encoding can be: ascii, utf-8, cp1250, etc
def Extract(filepath, regex, encoding="utf-8", regex_flags=re.DOTALL, by_line:bool=True):
	pat = re.compile(regex, regex_flags)
	with open(filepath, mode="r", encoding=encoding) as f:
		if by_line:
			for line in f:
				m = pat.search(line)
				if m: return m
		else:
			s = f.read()
			m = pat.search(s)
			if m: return m

	return None

# Extract data from a text file using a regex
# Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
# Returns a collection of matches from within the file
# Encoding can be: ascii, utf-8, cp1250, etc
def ExtractMany(filepath, regex, encoding="utf-8"):
	pat = re.compile(regex)
	matches = []
	with open(filepath, mode="r", encoding=encoding) as f:
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
			buf = buf[:m.start()] + (subst if subst else "") + buf[m.end():]
			s = m.start()
	with open(output_filepath, mode='w') as f:
		f.write(buf)
	return

# Modify a file, line-by-line, using regex
# Capture groups are defined like: (?P<name>.*) and accessed like: m.group("name")
def UpdateFileByLine(filepath:str, regex:str, repl:str, all=False):
	pat = re.compile(regex)
	with open(filepath+".tmp", mode='w') as outf:
		with open(filepath, mode='r') as inf:
			for line in inf:
				m = pat.search(line)
				if not m: outf.write(line)
				else:
					outf.write(pat.sub(repl, line))
					if not all: break
			for line in inf:
				outf.write(line)
	os.unlink(filepath)
	os.rename(filepath+".tmp", filepath)

# Modify a whole file using regex.
def UpdateFile(filepath:str, regex:str, repl:str, regex_flags=re.DOTALL):
	pat = re.compile(regex, regex_flags)
	with open(filepath) as f: s = f.read()
	s = pat.sub(repl, s, 0)
	with open(filepath+".tmp", mode="w") as f: f.write(s)
	os.unlink(filepath)
	os.rename(filepath+".tmp", filepath)

# Replace a tagged section within a file
def UpdateTaggedSection(filepath:str, tag_beg:str, tag_end:str, repl:str):
	# Check the file exists
	if not os.path.exists(filepath):
		raise FileNotFoundError(f"Cannot update tagged section in '{filepath}'.")

	# Create the string to replace with
	section = f"{tag_beg}{repl}{tag_end}"

	# The tagged section pattern
	pat = f"{tag_beg}(.*?){tag_end}"

	# Replace the tagged section in 'filepath'
	UpdateFile(filepath, pat, section, re.S)
	return

# Replace a block of lines within a file from lines from another file
# If 'src_tag_begin' and 'src_tag_end' are None, the entire file is used
# If 'dst_tag_begin' and 'dst_tag_end' are None, the entire file is replaced
# 'transform' has a signature of: "transform(line) -> line". Result should have no trailing newline or leading whitespace
def ReplaceSection(src_file: path, dst_file: path, src_tag_begin: Optional[str], src_tag_end: Optional[str], dst_tag_begin: Optional[str], dst_tag_end: Optional[str], transform: Callable[[str], str]) -> None:

	if not os.path.exists(src_file):
		raise FileNotFoundError(f"Cannot read section in '{src_file}'.")
	if not os.path.exists(dst_file):
		raise FileNotFoundError(f"Cannot update section in '{dst_file}'.")

	embed = []
	lines = []
	indent = ""

	# Find the lines to embed between the src tags
	with open(src_file, "r") as file:
		within_tags = src_tag_begin is None and src_tag_end is None
		for line in file:
			if src_tag_begin is not None and src_tag_begin in line:
				within_tags = True
				continue
			if src_tag_end is not None and src_tag_end in line:
				within_tags = False
				break
			if within_tags:
				embed.append(line)

	# Read the dst file and embed the lines between the dst tags
	with open(dst_file, "r") as file:
		within_tags = dst_tag_begin is None and dst_tag_end is None
		for line in file:
			if dst_tag_begin is not None and dst_tag_begin in line:
				within_tags = True
				indent = line[:line.find(dst_tag_begin)]
				lines.append(line)
				lines.extend([f"{indent}{transform(l)}" for l in embed])
				continue
			if dst_tag_end is not None and dst_tag_end in line:
				within_tags = False
				lines.append(line)
				continue
			if not within_tags:
				lines.append(line)

	# Update the dst file
	with open(dst_file, "w") as file:
		for l in lines:
			file.write(l)

# Tests if this script is being run with admin rights, if not restarts the script elevated
def RunAsAdmin(expected_return_code=0, working_dir=".\\", show_arguments=False):
	try:
		if "elevated" in sys.argv: return
		subprocess.check_output(["net", "session"], stderr=subprocess.STDOUT)
		print("Admin rights available")
		return
	except Exception as ex:
		print("Running script under Administrator account...")
	
	try:
		elevate = Path(UserVars.root, "bin\\elevate\\elevate.exe")
		args = [elevate, sys.executable] + sys.argv + ["elevated"]
		if show_arguments: print(args)
		subprocess.check_call(args, cwd=working_dir)
	except Exception as ex:
		print("Run as Admin failed\n" + str(ex))
	sys.exit(0)

# Touch a file to change it's timestamp
def TouchFile(fname, times=None):
	with open(fname, mode='a'):
		os.utime(fname, times=times)

# Attempt to guess the encoding of a text file
def GuessEncoding(fpath:str):
	with open(fpath, mode='rb') as f:
		bom = f.read(3)
		if len(bom) >= 3 and bom[0] == 0xEF and bom[1] == 0xBB and bom[2] == 0xBF:
			return 'utf8'
		if len(bom) >= 2 and bom[0] == 0xFE and bom[1] == 0xFF:
			return 'utf16_be'
		if len(bom) >= 2 and bom[0] == 0xFF and bom[1] == 0xFE:
			return 'utf16'
		# If there is no BOM, assume ascii/utf8.. it might be codepage 1252 or something tho...
		return 'utf8'

# Check the time stamps of a list of filepaths.
# If any has a time stamp newer than 'touch_file' touch_file gets "touched"
def CheckDependencies(deps, touch_file):
	touch = os.path.getmtime(touch_file)
	newer = touch
	for dep in deps:
		newer = max(os.path.getmtime(dep), newer)
	if newer > touch:
		TouchFile(touch_file)
		
# Find the visual studio batch file for setting up a NMake environment
def SetupVcEnvironment():
	
	# Already set up
	if 'VISUALSTUDIOVERSION' in os.environ:
		return

	# Not a windows environment
	if sys.platform != "win32":
		return

	# VS environment not set up in user vars
	if not UserVars.vs_envvars: 
		print("VS Environment not set in UserVars")
		return

	print("Initializing VS Environment", flush=True)

	# Find 'vswhere.exe' from the VS installer
	vswhere = os.path.join(os.environ["ProgramFiles(x86)"], "Microsoft Visual Studio", "Installer", "vswhere.exe")
	if not os.path.exists(vswhere):
		raise ValueError(f"vswhere.exe not found at: {vswhere}")

	# Get the VS install path
	vs_path = subprocess.check_output([vswhere, "-latest", "-property", "installationPath"], universal_newlines=True).rstrip()

	# Get the VcVars batch file
	vsvars_path = os.path.join(vs_path, "VC", "Auxiliary", "Build", "vcvars64.bat")

	# Run the vcvars batch file, then dump the state of the environment variables
	env = subprocess.check_output(["cmd", "/C", vsvars_path, "&", "set"], universal_newlines=True)

	# Add/Replace the environment variables in 'os.environ'
	for line in env.splitlines():
		m = re.fullmatch("^(.+?)=(.*)$", line)
		if not m: continue
		os.environ[m[1]] = m[2]

	#print("Environment:\n")
	#for k in os.environ: print(f"{k} = {os.environ[k]}")
	return os.environ
	
# Invoke MSBuild on a solution or project file.
# Solution file usage:
#   sln_or_proj_file = "C:\path\mysolution.sln"
#	projects = ["project_name","\"folder\proj_name:Rebuild\""]
#	platforms = ["x64","x86","Any CPU"]
#	configs = ["release","debug"]
#	Tools.MSBuild(sln_or_proj_file, projects, platforms, configs, True, True)
# Project file usage:
#   sln_or_proj_file = "C:\path\myproject.csproj"
#	projects = []
#	platforms = ["x64","x86","AnyCPU"]
#	configs = ["release","debug"]
#	Tools.MSBuild(sln_or_proj_file, projects, platforms, configs, True, True)
def MSBuild(sln_or_proj_file:str, projects:List[str], platforms:List[str], configs:List[str], parallel=False, same_window=True, additional_args=[]):
	
	if not UserVars.msbuild:
		raise RuntimeError("MSBuild path has not been set in UserVars")
		
	# Handle None's
	projects  = projects if projects else []
	platforms = platforms if platforms else []
	configs   = configs if configs else []

	# Build the arguments list
	args = [UserVars.msbuild] + [sln_or_proj_file, "/m", "/verbosity:minimal", "/nologo"] + additional_args

	# Set the targets to build
	# Targets should be the names as shown in the solution explorer (i.e. Folder\Project.Name)
	if len(projects) != 0:
		projects_cleaned = [proj.replace('.','_') for proj in projects] # Replace '.' in the project name with '_'
		args += ["/t:" + ";".join(projects_cleaned)]

	# Set the platform/config
	procs = []
	errors = False
	try:
		if not platforms and not configs:
			Run(args)
		else:
			for platform in platforms:
				for config in configs:
					args_ = args + [f"/p:Configuration={config};Platform={platform}"]
					if parallel:
						proc = Spawn(args_, same_window=same_window)
						procs.append(proc)
					else:
						print(f"{platform}|{config}:")
						Run(args_)
	# Wait for all processes to finish, and check for error return codes
	finally:	
		for proc in procs:
			proc.wait()
			errors |= proc.returncode != 0
	
	return not errors

# Invoke DotNet on a solution or project file
# DotNet is basically a .NET Core wrapper around msbuild, nuget, and other tools
# Solution file usage:
#   sln_or_proj_file = "C:\path\mysolution.sln"
#	projects = ["project_name","\"folder\proj_name:Rebuild\""]
#	platforms = ["x64","x86","Any CPU"]
#	configs = ["release","debug"]
#	Tools.MSBuild(sln_or_proj_file, projects, platforms, configs, True, True)
# Project file usage:
#   sln_or_proj_file = "C:\path\myproject.csproj"
#	projects = []
#	platforms = ["x64","x86","AnyCPU"]
#	configs = ["release","debug"]
#	Tools.DotNet("build", sln_or_proj_file, projects, platforms, configs, True, True)
def DotNet(command:str, sln_or_proj_file:str, projects:List[str], platforms:List[str], configs:List[str], parallel=False, same_window=True):

	# Handle None's
	projects  = projects if projects else []
	platforms = platforms if platforms else []
	configs   = configs if configs else []

	# Build the arguments list
	args = [UserVars.dotnet, command, sln_or_proj_file, "--verbosity", "minimal", "/nologo", "-nowarn:NU1503"]

	# Set the targets to build
	# Targets should be the names as shown in the solution explorer (i.e. Folder\Project.Name)
	if len(projects) != 0:
		projects_cleaned = [proj.replace('.','_') for proj in projects] # Replace '.' in the project name with '_'
		args += ["-t:" + ";".join(projects_cleaned)]

	# Set the platform/config
	procs = []
	errors = False
	try:
		if not platforms and not configs:
			Exec(args)
		else:
			for platform in platforms:
				for config in configs:
					args_ = args + [f"-p:Configuration={config}", f"-p:Platform={platform}"]
					if parallel:
						proc = Spawn(args_, same_window=same_window)
						procs.append(proc)
					else:
						print(f"{platform}|{config}:")
						Exec(args_)
	# Wait for all processes to finish, and check for error return codes
	finally:	
		for proc in procs:
			proc.wait()
			errors |= proc.returncode != 0

	return not errors

# Run the units tests in a .net assembly
def UnitTest(assembly_filepath:str, deps:List[str]=[], run_tests:bool=True):

	# Set this to false to disable running tests on compiling
	RunTests = run_tests
	#RunTests = False

	# # The build outputs
	# dll = os.path.join(target_dir, f"{assembly_name}.dll")
	# exe = os.path.join(target_dir, f"{assembly_name}.exe")
	# target = dll if os.path.exists(dll) else exe
	target_dir, assembly_file = os.path.split(assembly_filepath)
	assembly_name, _ = os.path.splitext(assembly_file)

	# Run unit tests
	if RunTests:
		if not os.path.exists(assembly_filepath):
			print(f"{assembly_name} assembly not found.   **** Unit tests skipped ****")

		else:

			# Using pythonnet
			if False:
				# Set the runtime to .net6 (or whatever is set int he runtime config)
				import pythonnet, clr_loader
				rt_config = Path(target_dir, f"{assembly_name}.runtimeconfig.json", check_exists=False)
				rt = clr_loader.get_coreclr(runtime_config=rt_config) if os.path.exists(rt_config) else clr_loader.get_netfx()
				pythonnet.set_runtime(rt)
				import clr

				# Import the assembly and run Program.Main
				sys.path.append(target_dir)
				ass = clr.AddReference(assembly_name)
				sys.path.pop()

				# Run the Program.Main() function
				prog = ass.GetType(f"{assembly_name}.Program")
				meth = prog.GetMethod("Main") if prog is not None else None
				res = meth.Invoke(None, None) if meth is not None else None
				if res is not None and res != 0:
					raise Exception("Unit tests failed")
				pass

			# Using powershell
			if True:
				#sys.path.append(UserVars.dotnet_dir)
				command = ( 
					"& {\n" + 
					f"    Set-Location {target_dir};\n" +
					#f"    $env:Path = '{UserVars.dotnet_dir};' + $env:Path;\n" + 
					#f""   .join(f"    Add-Type -AssemblyName '{dep}';\n" for dep in deps) + 
					f"    Add-Type -AssemblyName '{assembly_filepath}';\n" + 
					#f""   .join(f"    [Reflection.Assembly]::LoadFile('{dep}')|Out-Null;\n" for dep in deps) +  
					#f"    [Reflection.Assembly]::LoadFile('{target}')|Out-Null;\n" +  
					f"    $result = [{assembly_name}.Program]::Main();\n" + 
					f"    Exit $result;\n" + 
					"}") 
				#print(command) 
				res,outp = Run([UserVars.pwsh, "-NonInteractive", "-NoProfile", "-NoLogo", "-Command", command]) 
				sys.path.pop()
				print(outp) 
				if not res: 
					raise Exception("   **** Unit tests failed ****   ") 
				pass

			# Using Csex
			if False:
				#csex = Path(UserVars.root, "bin\\Csex\\Csex.exe")
				#res,outp = Run([csex, "-unittest", assembly_filepath])
				#print(outp) 
				#if not res: 
				#	raise Exception("   **** Unit tests failed ****   ") 
				pass

	# Don't copy to the lib folder, that is the deploy step
	# Rylogic assemblies shouldn't be copied anyway, as we don't know which framework
	# to copy, i.e. netstandard2.0, netcoreapp3.1, ?
	return

# Prompt for the code signing certificate password
def PromptCertPassword():
	if UserVars.code_sign_cert_pw: return
	UserVars.code_sign_cert_pw = getpass.getpass(prompt="Code Signing Cert Password: ")
	return

# Sign an assembly
def SignAssembly(target:str):
	if not UserVars.code_sign_cert_pfx: return
	PromptCertPassword()
	Exec([UserVars.signtool, "sign", "/f", UserVars.code_sign_cert_pfx, "/p", UserVars.code_sign_cert_pw, "/fd", "SHA1", target])
	return

# Sign a VSIX extension package
def SignVsix(vsix_filepath:str, algo:str):
	# Use 'dotnet tool install -g OpenVsixSignTool' to install 'OpenVsixSignTool'
	# 'OpenVsixSignTool' is an open source (and better) version of 'VsixSignTool' (https://github.com/vcsjones/OpenOpcSignTool)
	# 'e1053e6fa1aeb7bd4ee453302116a129ca4112f9' is the thumbprint of the code signing certificate installed on the machine.
	#    Run 'mmc' then add 'Certificates' to the console. Find your code signing cert (Rylogic Limited, Sectigo RSA Code Signing CA),
	#    and open it. Under 'details' find 'Thumbprint'. The OpenVsixSignTool uses this hash value to find the cert in the store.
	# If the vsix supports VS versions less than 14.0, you need to use "-fd sha1" or the cert shows up as invalid. If the VSIX only
	#    supports VS versions >= 14.0, use sha256 instead.
	Exec(["openvsixsigntool", "sign", "--sha1", UserVars.code_sign_cert_thumbprint, "-fd", algo, vsix_filepath])
	return

# Create a Nuget package from the given project file
# Expects a file called 'package.nuspec' in the same directory as 'proj'
# The resulting package will be in UserVars.root\lib\packages
# The full package path is returned
def NugetPackage(proj:str):

	# Notes:
	#  - Assembly signing, packaging, and publishing are all separate steps.
	proj_dir = os.path.split(proj)[0]

	# No nuspec = no publish
	nuspec = os.path.join(proj_dir, "package.nuspec")
	if not os.path.exists(nuspec):
		raise RuntimeError(f"'nuspec' file missing. Expected '{nuspec}'")

	# Read the version numbers from the spec file and project file
	vers0 = Extract(nuspec, r"<version>(?P<vers>.*)</version>").group("vers")
	vers1 = Extract(proj, r"<Version>(?P<vers>.*)</Version>").group("vers")
	vers2 = Extract(proj, r"<FileVersion>(?P<vers>.*)</FileVersion>").group("vers")

	# Compare it to the versions in the project file
	if vers0 != vers1 or vers0 != vers2:
		raise Exception(f"Version number mismatch between project file ({proj}) and nuspec file ({nuspec})")

	# Check all the output files exist
	for m in ExtractMany(nuspec, r'<file\s+src="(?P<target>.*?)"'):
		target = Path(proj_dir, m.group("target"), check_exists=False)
		if os.path.exists(target): continue
		raise Exception(f"NuSpec target {target} does not exist")

	# Build the Nuget package directly in the lib\packages folder
	Exec([UserVars.nuget, "pack", nuspec, "-OutputDirectory", os.path.join(UserVars.root, "lib", "packages")])

	# Sign the package
	package_name = Extract(nuspec, r"<id>(?P<id>.*)</id>").group("id")
	package_path = os.path.join(UserVars.root, "lib", "packages", f"{package_name}.{vers0}.nupkg")
	PromptCertPassword()
	Exec([UserVars.nuget, "sign", package_path, "-CertificatePath", UserVars.code_sign_cert_pfx, "-CertificatePassword", UserVars.code_sign_cert_pw, "-Timestamper", "http://timestamp.digicert.com"])
	return package_path

# Push a nugget package to 'nuget.org'
def NugetPublish(package_path:str):
	Exec([UserVars.nuget, "push", package_path, UserVars.nuget_api_key, "-source", "https://api.nuget.org/v3/index.json"])
	return

# Create a zip of a single file
def ZipFile(file_path:str, zip_path:str=None):
	zip_path = zip_path if zip_path else ChgExtn(file_path, ".zip")
	arc_path = os.path.split(file_path)[1]
	zipf = zipfile.ZipFile(zip_path, 'w')
	zipf.write(file_path, arc_path)
	zipf.close()
	return zip_path

# Create a zip of a directory
def ZipDirectory2(root_dir:str, zip_path:str=None):
	zip_path = zip_path if zip_path else f"{root_dir}.zip"
	zipf = zipfile.ZipFile(zip_path, 'w')
	for root,_,files in os.walk(root_dir):
		for file in files:
			filepath = os.path.join(root, file)
			arcpath  = os.path.relpath(filepath, root_dir)
			zipf.write(filepath, arcpath)
	zipf.close()
	return zip_path

# Extract a zip file with progress feedback
def ExtractZip(filepath:str, destination:str, show_progress:bool):
	with zipfile.ZipFile(filepath, "r") as zf:
		if show_progress:

			length = sum(file.file_size for file in zf.infolist())
			size = 0
			print(f"Extracting "+str(length)+" bytes ["+(" "*50)+"]", end='', flush=True)
			for file in zf.infolist():
				size += file.file_size
				progress = int(50 * size / length)
				print(("\b"*52) + "["+("#"*progress)+(" "*(50-progress))+"]", end='', flush=True)
				zf.extract(file, path=destination)
			print()
		else:
			zf.extractall(destination)
	return

# Download a file from 'url' and save it to 'filepath'
def DownloadFile(url:str, filepath:str, show_progress:bool):
	
	# Open the file to write to
	with open(filepath, 'wb') as f:
		
		# Open the URL
		resp = urllib.request.urlopen(url)

		if show_progress:

			# See if the header includes a content length. If a length is given, give process feedback
			length = resp.getheader('content-length')
			blocksize = 65536
			size = 0
			if length:
				length = int(length)
				print("Downloading " + str(length) + " bytes ["+(" "*50)+"]", end='', flush=True)
				while True:
					buf = resp.read(blocksize)
					if not buf: break
					f.write(buf)
					size += len(buf)
					progress = int(50 * size / length)
					print(("\b"*52) + "["+("#"*progress)+(" "*(50-progress))+"]", end='', flush=True)
				print()

			# Otherwise give number of bytes downloaded so far
			else:
				progress = str(size) + " bytes"
				print("Downloaded "+progress, end='', flush=True)
				while True:
					buf = resp.read(blocksize)
					if not buf: break
					f.write(buf)
					size += len(buf)
					print("\b"*len(progress), end='')
					progress = str(size) + " bytes"
					print(progress, end='', flush=True)
				print()

		else:
			# Otherwise, download silently
			f.write(resp.read())
	return

# Write XML to a file with decent formatting.
# For some reason this is glaringly missing from the built in XML support
def WriteXml(root:xml.Element, filepath:str, formatted=True):
	with open(filepath, "w") as f:
		if formatted:
			f.write(minidom.parseString(xml.tostring(root)).toprettyxml())
		else:
			f.write(xml.tostring(root))

# Return XML as a string with decent formatting.
def FormatXml(root:xml.Element, formatted=True):
	if formatted:
		return minidom.parseString(xml.tostring(root)).toprettyxml()
	else:
		return xml.tostring(root)

# Enums for StrTxfm
class ECapitalise(enum.Enum):
	LowerCase  = -1
	DontChange = 0
	UpperCase  = +1
class ESeparate(enum.Enum):
	Remove     = -1
	DontChange = 0
	Add        = +1

# Transform a string by the given casing rules
# Capitalise values: -1 => to lower case, 0 => unchanged, +1 => to upper case
# Separation values: -1 => remove separator, 0 => unchanged, +1 => insert separator
# 'word_start' - How to capitalise the start of words
# 'word_case' - How to capitalise the whole word
def StrTxfm(string:str, word_start=ECapitalise.DontChange, word_case=ECapitalise.DontChange, word_sep=ESeparate.DontChange, sep="", delims=""):

	# Ignore empty or null strings
	if string == None or string == "":
		return string

	delims = delims if delims != None else ""
	strlen = len(string)
	sb = ""

	i = 0
	while i != strlen:

		# Skip over multiple delimiters
		while i != strlen and delims.find(string[i]) != -1:
			if word_sep == ESeparate.DontChange:
				sb += string[i]
			i += 1

		# Detect word boundaries
		boundary = (
			i == 0 or                                          # first letter in the string
			delims.find(string[i-1]) != -1 or                  # previous char is a delimiter
			(string[i-1].islower() and string[i].isupper()) or # lower to upper case letters
			(string[i-1].isalpha() and string[i].isdigit()) or # letter to digit
			(string[i-1].isdigit() and string[i].isalpha()) or # digit to letter
			(i < strlen - 1 and string[i-1].isupper() and string[i].isupper() and string[i+1].islower()))

		# Add/Remove separators at word boundaries
		if boundary:
			if i != 0 and word_sep == ESeparate.Add and sep != None:
				sb += sep

			if   word_start == ECapitalise.DontChange: sb += string[i]
			elif word_start == ECapitalise.UpperCase:  sb += string[i].upper()
			elif word_start == ECapitalise.LowerCase:  sb += string[i].lower()
			else: raise Exception("Unknown 'ECapitalise' value for 'word_start'")

		# Capitalise words
		else:
			if   word_case == ECapitalise.DontChange: sb += string[i]
			elif word_case == ECapitalise.UpperCase:  sb += string[i].upper()
			elif word_case == ECapitalise.LowerCase:  sb += string[i].lower()
			else: raise Exception("Unknown 'ECapitalise' value for 'word_case'")

		# Next character
		i += 1

	return sb

# Popup a message box
class EMsgBoxBtns(enum.IntEnum):
	Ok = 0,
	OkCancel = 1
	AbortRetryIgnore = 2
	YesNoCancel = 3
	YesNo = 4
	RetryNo = 5
	CancelTryAgainContinue = 6
def MsgBox(msg:str, title:str, btns=EMsgBoxBtns.Ok):
	ctypes.windll.user32.MessageBoxW(0, msg, title, btns
	)

# Start an interactive python console
def interact(banner=None, local=None, exitmsg=None):
	class ReloadingInteractiveConsole(code.InteractiveConsole):
		def __init__(self, local=None):
			super().__init__(local)
			#readline.parse_and_bind("tab: complete")
			self.stored_modifier_times = {}
			self.CheckModulesForReload()

		def runcode(self, code):
			self.CheckModulesForReload()
			super().runcode(code)
			self.CheckModulesForReload() # maybe new modules are loaded

		def CheckModulesForReload(self):
			for module_name, module in sys.modules.items():
				if not hasattr(module, '__file__') or module.__file__ is None:
					continue

				module_modifier_time = os.path.getmtime(module.__file__)
				if self.stored_modifier_times.get(module_name, module_modifier_time) < module_modifier_time:
					imp.reload(module)

				self.stored_modifier_times[module_name] = module_modifier_time
	
	# Create a console instance
	console = ReloadingInteractiveConsole(local)
	console.interact(banner=banner, exitmsg=exitmsg)

# Testing
if __name__ == "__main__":
	try:
		UnitTest("P:\\pr\\projects\\rylogic\\Rylogic.Core\\bin\\debug\\netstandard2.0\\Rylogic.Core.dll")
		#UnitTest("P:\\pr\\projects\\Rylogic.Core.Windows\\bin\\debug\\net6.0-windows\\Rylogic.Core.Windows.dll", ["Rylogic.Core.dll"])

		# This is how to run a function from the command line
		if len(sys.argv) > 1:
			if sys.argv[1] in globals():
				globals()[sys.argv[1]](sys.argv[2:])

		pass
	except KeyboardInterrupt:
		pass
	except Exception as ex:
		print(str(ex))
