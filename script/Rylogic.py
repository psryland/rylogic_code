#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, time, shutil, glob, subprocess, threading, re, enum, socket, zipfile, ctypes, hashlib, urllib.request
import xml.etree.ElementTree as xml
import xml.dom.minidom as minidom
from typing import Callable
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
def OnError(msg = "\nFailed\n", enter_to_close=True, pause_time_seconds=0, sys_exit=False):
	print(msg)
	try:
		if enter_to_close: input("Press enter to close")
		else: time.sleep(pause_time_seconds)
	except:
		pass
	if sys_exit:
		sys.exit(-1)

# Terminate on exception
def OnException(ex,enter_to_close=True, pause_time_seconds=0):
	OnError("ERROR: " + str(ex), enter_to_close=enter_to_close, pause_time_seconds=pause_time_seconds)
	return

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
def ShellDelete(path, wait_time_ms = 100):
	if os.path.exists(path):
		shutil.rmtree(path, ignore_errors=True)
		time.sleep(wait_time_ms * 0.001) # Give the OS time to do it
		if os.path.exists(path):
			raise Exception("Failed to delete '"+path+"'. Check for locked files")

# Change the file extension on 'path'. 'extn' should include the dot
def ChgExtn(filepath:str, extn:str):
	dir_fname,_ = os.path.splitext(filepath)
	return dir_fname + extn

# Enumerate recursively through a directory
# 'filter' can be a regex: e.g. EnumFiles(dir, filter=r".*(?<!include)\.htm$", flags=re.IGNORECASE):
def EnumFiles(root, filter:str=None, flags=0):
	for dirname, _, filenames in os.walk(root):
		# Return the files
		# We could remove entries from 'dirnames' to
		# prevent recursion into those folders...
		for filename in filenames:
			if filter and not re.match(filter, filename, flags): continue
			yield os.path.join(dirname, filename)

# Enumerate recursively through a directory
def EnumDirs(root, filter:str=None, flags=0):
	for dirname, dirnames, _ in os.walk(root):
		# Return the directories
		for dir in dirnames:
			if filter and not re.match(filter, dir, flags): continue
			yield os.path.join(dirname, dir)
			
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
def Copy(src, dst, only_if_modified=True, show_unchanged=False, ignore_non_existing=False, quiet=False, filter:str=None, filter_flags=0, follow_symlinks=True):

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
	elif not ignore_non_existing:
		raise FileNotFoundError("ERROR: "+src+" does not exist")

	# If the 'src' represents multiple files, 'dst' must be a directory
	if src_is_dir or len(lst) > 1:
		# if 'dst' doesn't exist, assume it's a directory
		if not os.path.exists(dst):
			dst_is_dir = True
		# or if it does exist, check that it is actually a directory
		elif not dst_is_dir:
			raise FileNotFoundError("ERROR: "+dst+" is not a valid directory")

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
			Copy(s, d+"\\", only_if_modified, show_unchanged, ignore_non_existing, quiet, filter, filter_flags, follow_symlinks)

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
			if not quiet: print(s + " --> " + d)
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
# Returns (result,output)
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
		proc = subprocess.Popen(args,creationflags=cf,startupinfo=si)
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
		AssertPathsExist([UserVars.elevate])
		args = [UserVars.elevate, sys.executable] + sys.argv + ["elevated"]
		if show_arguments: print(args)
		subprocess.check_call(args, cwd=working_dir)
	except Exception as ex:
		print("Run as Admin failed\n" + str(ex))
	sys.exit(0)

# Touch a file to change it's timestamp
def TouchFile(fname, times=None):
	with open(fname, mode='a'):
		os.utime(fname, times=times)

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
	if sys.platform != "win32": return
	if 'VISUALSTUDIOVERSION' in os.environ: return
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
		m = re.fullmatch("(.+)=(.*)", line)
		if m: os.environ[m[1]] = m[2]

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
def MSBuild(sln_or_proj_file:str, projects:[str], platforms:[str], configs:[str], parallel=False, same_window=True, msbuild_props=[]):
	
	# Handle None's
	projects  = projects if projects else []
	platforms = platforms if platforms else []
	configs   = configs if configs else []

	# Build the arguments list
	args = [UserVars.msbuild] + msbuild_props + [sln_or_proj_file, "/m", "/verbosity:minimal", "/nologo"]

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
			Exec(args)
		else:
			for platform in platforms:
				for config in configs:
					args_ = args + [f"/p:Configuration={config};Platform={platform}"]
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
def DotNet(command:str, sln_or_proj_file:str, projects:[str], platforms:[str], configs:[str], parallel=False, same_window=True):

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

# Create a Nuget package from the given project file
# Expects a file called 'package.nuspec' in the same directory as 'proj'
# The resulting package will be in UserVars.root\lib\packages
def NugetPackage(proj:str, publish:bool):

	# No nuspec = no publish
	nuspec = os.path.join(os.path.split(proj)[0], "package.nuspec")
	if not os.path.exists(nuspec):
		return

	# Read the version numbers from the spec file and project file
	vers0 = Extract(nuspec, r"<version>(?P<vers>.*)</version>").group("vers")
	vers1 = Extract(proj, r"<Version>(?P<vers>.*)</Version>").group("vers")
	vers2 = Extract(proj, r"<FileVersion>(?P<vers>.*)</FileVersion>").group("vers")

	# Compare it to the versions in the project file
	if vers0 != vers1 or vers0 != vers2:
		raise Exception(f"Version number mismatch between project file ({proj}) and nuspec file ({nuspec})")

	# Build the Nuget package directly in the lib\packages folder
	Exec([UserVars.nuget, "pack", nuspec, "-OutputDirectory", os.path.join(UserVars.root, "lib", "packages")])
	
	# Publish the package
	if publish:
		package_name = Extract(nuspec, r"<id>(?P<id>.*)</id>").group("id")
		package_path = os.path.join(UserVars.root, "lib", "packages", f"{package_name}.{vers0}.nupkg")

		# Sign the package
		if False: # my cert's out of date..
			cert = os.path.join(UserVars.root, "projects", "Rylogic.pfx")
			time_server = "http://timestamp.digicert.com"
			Exec([UserVars.nuget, "sign", package_path, "-CertificatePath", cert, "-Timestamper", time_server])
		
		# Push the package to nuget.org
		Exec([UserVars.nuget, "push", package_path, UserVars.nuget_api_key, "-source", "https://api.nuget.org/v3/index.json"])
	return

# Create a zip of a directory
def ZipDirectory(zip_path, root_dir):
	zipf = zipfile.ZipFile(zip_path, 'w')
	for root,_,files in os.walk(root_dir):
		for file in files:
			filepath = os.path.join(root, file)
			arcpath  = os.path.relpath(filepath, root_dir)
			zipf.write(filepath, arcpath)
	zipf.close()

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

# Testing
if __name__ == "__main__":
	try:
		pass
	except KeyboardInterrupt:
		pass
	except Exception as ex:
		print(str(ex))
