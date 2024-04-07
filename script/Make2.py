#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Project builder
#  This script is the basis for a project build system.
#  This exists because declarative build systems like CMake, make, etc suck, are hard to debug, and have ugly syntax.
#  Python can be debugged, is flexible enough to do anything, and has well known syntax.
#
# Definitions:
#  1. A project is a collection of files that need to be compiled or processed in some way.
#  2. Building a project involves a sequence of steps, e.g., restore, pre-build, compile, link, post-build, etc.
#  3. These files are dependent on other files, and should only be processed if their dependent files have changed.
#  4. Each file can have individual requirements for processing, e.g., preprocessing, unique compiler flags, etc.
import os, subprocess, shutil
from typing import List, Tuple, Callable

# A source file with optional extra defines etc
class SrcFile():
	def __init__(self, filepath:str):
		self.filepath = filepath
		self.fdir, self.fname = os.path.split(self.filepath)
		self.ftitle, self.ext = os.path.splitext(self.fname)
		self.flags = []
		self.defines = []
		self.includes = []
		self.is_sdk = False

		# How to compile this file
		self.compile_step: Callable[[], bool] = None
		return
	
# Base class for build system tools
class Tools:
	"""Build system tools"""
	def __init__(self):
		self.compiler = None
		self.linker = None
		self.archiver = None
		return

	# Compile a C++ file
	def CompileCXX(self, src: SrcFile, builder: 'Builder') -> bool:
		return False

	# Run the C++ compiler
	def CCPLUS(self, args:List[str], verbosity:int) -> bool:
		try:
			if not self.compiler: raise RuntimeError(f"No C++ compiler path set")
			cmd = [self.compiler] + args
			if verbosity >= 3: print(' '.join(cmd))
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

# Build system
class Builder:
	"""A project build system"""
	def __init__(self, project_file: str, tools: Tools):

		# The filepath of the project file contains the subclass of this Builder class
		self.project_file = project_file

		# The tools used to build the project
		self.tools = tools

		# The platform to build the target for, e.g. x86, x64, arm, thumb, etc
		self.platform = ''

		# 'tasks' are the build tasks to be executed. Derived projects should add tasks and order them as needed.
		self.tasks = []

		# Source files
		# This array should be filled with 'SrcFile' objects or the full paths of the source files that contribute to the project
		# When filepaths are given, the compilation process will be based on the file extension
		self.src = []

		# Preprocessor defines
		# This array should be filled with any preprocessor defines. Use 'MY_DEFINE' or 'MY_DEFINE=12' form.
		self.defines = []

		# Application include paths
		# This array should be filled with the full paths to include directories specific to the project.
		self.includes = []

		# System include paths
		# This array should be filled with the full paths to include directories for system includes.
		self.sys_includes = []

		# Libraries files to link against.
		self.libraries = []

		# Search paths used to find the libraries.
		self.library_paths = []

		# System libraries to link against.
		self.sys_libraries = []

		# Search paths used to find the system libraries.
		self.sys_library_paths = []

		# Build output directory.
		# Set this to a directory that will contain the build output and temporary files. It's a directory you can delete to perform a clean rebuild
		self.outdir = None

		# Default build configuration.
		self.config = 'debug'

		# The allowed configurations for the project
		self.configurations = ['debug', 'release']

		# The file name of the binary to produce (including extension)
		# This could be: 'my_project.elf', 'whatsimacallit.exe', 'library.s', etc
		self.target_filename = None

		# Rebuild everything (e.g., don't check dependencies)
		self.rebuild = False

		# Clean the build output directory
		self.clean = False

		# Verbosity level
		self.verbosity = 1# [0-quiet,1-minimal,2-normal,3-spamtastic]

		# Errors do not stop the build
		self.stop_on_first_error = False

		# Ignore changes to the project file when checking dependencies
		self.ignore_project_file_changes = False

		return

	# Run the build process
	def Build(self, args:List[str]):
		"""Run the build process"""
		# Parse command line
		i = 1
		while i < len(args):
			arg = args[i].lower()
			consumed, num = self.Arg(arg, i, args)
			if consumed:
				i += num
			elif arg == '-clean':
				i = i + 1
				self.clean = True
			elif arg == '-rebuild':
				i = i + 1
				self.rebuild = True
			elif arg == '-platform':
				i = i + 1
				if i == len(args) or args[i].startswith('-'):
					raise RuntimeError("Expected a platform target after '-platform'")
				else:
					self.platform = args[i]
					i = i + 1
			elif arg == '-config':
				i = i + 1
				if i == len(args) or args[i].startswith('-'):
					raise RuntimeError("Expected one of 'Debug' or 'Release' after '-config'")
				else:
					config = args[i].lower()
					if config not in self.configurations: raise RuntimeError(f"Unknown configuration: {config}")
					self.config = config
					i = i + 1
			elif arg == '-verbose':
				i = i + 1
				if i == len(args) or args[i].startswith('-'):
					raise RuntimeError("Expected a level (0, 1, 2, or 3) after '-verbose'")
				else:
					self.verbosity = int(args[i])
					i = i + 1
			else:
				raise RuntimeError(f"Unknown command line argument: {args[i]}")

		# Split the target filename into name and extension
		self.target_name, self.target_extn = os.path.splitext(self.target_filename)

		# Run the project setup. This allows the derived class to populate the build steps,
		# or make any customizations based on the input arguments before the build process starts.
		# Optionally, the build steps can be configured in the project constructor.
		self.Setup()

		# Check for required variables
		if not self.project_file:
			raise RuntimeError("No project file set")
		if not self.outdir:
			raise RuntimeError("No build output directory set")
		if not self.target_filename:
			raise RuntimeError("No target name set")
		for ip in [os.path.normpath(p) for p in self.includes]:
			if os.path.exists(ip): continue
			print(f"WARNING: Include path '{ip}' does not exist")
		for ip in [os.path.normpath(p) for p in self.sys_includes]:
			if os.path.exists(ip): continue
			print(f"WARNING: System include path '{ip}' does not exist")

		# Convert all source files to 'SrcFile' objects
		for src, i in enumerate(self.src):
			if isinstance(src, SrcFile):
				if os.path.exists(src.filepath): continue
				print(f"WARNING: Source file '{src}' does not exist")
				continue
			if isinstance(src, str):
				if not os.path.exists(src):
					print(f"WARNING: Source file '{src}' does not exist")
					continue
				src = SrcFile(src)
				src.compile_step = self.DefaultCompileStep(src.ext)
				self.src[i] = src
				continue
			raise RuntimeError(f"Invalid source file type: {str(src)}")

		# Clean builds delete the output directory
		if self.clean and os.path.exists(self.outdir):
			shutil.rmtree(self.outdir)

		# Ensure the output directory exists
		os.makedirs(self.outdir, exist_ok=True)

		# Notify Build starting
		self.BuildStart()

		# Execute the build tasks
		for task in self.tasks:
			if task.Run(self): continue
			print(f"ERROR: Task '{task.name}' failed")
			return

		# Notify Build complete
		self.BuildComplete()

	# Custom command line argument handling
	def Arg(self, arg: str, i: int, args: List[str]) -> Tuple[bool, int]:
		"""Allow derived projects to handle custom command line arguments"""
		return (False, 0)
		
	# Populate members based on config
	def Setup(self):
		"""Populate members based on the configuration"""

		# Example: find the source files
		if False:
			# Any .c/.cpp/.s file under 'src/'
			for extn in ['*.c', '*.cpp', '*.s']:
				files = glob.glob(Join(proj_root, 'src', f'**/{extn}'), recursive=True)
				self.src.extend(files)

		return

	# Build start event
	def BuildStart(self):
		"""Called when the build process is starting"""

		if self.verbosity >= 1:
			print(f"Build {self.target_filename} starting...")

		# Marker file used to detect project file changes
		last_built_file = Join(self.outdir, "last_built", check_exists=False)

		# If no last built file, rebuild all
		if not os.path.exists(last_built_file):
			if self.verbosity >= 2: print("Rebuilding all since no last built file was found")
			self.rebuild = True

		# Rebuild all if the project file or this file is newer than the last built file
		elif os.stat(last_built_file).st_mtime < os.stat(self.project_file).st_mtime or \
			 os.stat(last_built_file).st_mtime < os.stat(__file__).st_mtime:
			if self.ignore_project_file_changes:
				print("WARNING: Project file has changed, but not rebuilding")
			else:
				if self.verbosity >= 2: print("Rebuilding all since project file has changed")
				self.rebuild = True

		# Create the 'last built' marker file
		with open(last_built_file, "w"): pass
		return
	
	# Called when the build process is complete
	def BuildComplete(self):
		"""Called when the build process is complete"""
		if self.verbosity >= 1: print("Build Complete")
		return

	# Check the dependency file for any files that have changed
	def Changed(self, dep_file:str):

		# Assume all changed when rebuilding
		if self.rebuild:
			return True

		# No dependency file, assume all changed
		if not os.path.exists(dep_file):
			if self.verbosity >= 2: print(f'Dependency file {dep_file} not found')
			return True

		# The timestamp of the dependency file
		timestamp0 = os.stat(dep_file).st_birthtime
		with open(dep_file, 'r') as file:
			buf = file.read()
			buf = buf.replace('\\\n', ' ')

			# The first item in the list is the output file, the next item is the source file,
			# followed by the includes. E.g. main.o: S:/path/main.c s:/path/header.h ...
			files = buf.split()[1:]
			
			for fpath in files:

				# Dependency doesn't exist, assume changed
				if not os.path.exists(fpath):
					if self.verbosity >= 2: print(f'Dependency: {fpath} no longer exists')
					return True

				# Dependency is newer, assume changed
				timestamp1 = os.stat(fpath).st_mtime
				if timestamp1 > timestamp0:
					if self.verbosity >= 2: print(f'Dependency: {fpath} is newer')
					return True

		return False

	# Default compile step for a source file
	def DefaultCompileStep(self, ext:str) -> Callable[[SrcFile, 'Builder'], bool]:
		ext = ext.lower()
		if ext == ".cpp": return self.tools.CompileCXX
		return None

# Base class for a build task
class Task:
	"""A task is a step in the project build process."""
	def __init__(self, name: str):
		self.name = name
		return
	
	def Run(self, builder: Builder):
		"""Run the build task"""
		return

# Compile source files
class CompileTask(Task):
	"""The Compile task loops over all files in the 'src[]' array and runs their compile step."""
	def __init__(self, tools: Tools):
		super().__init__("Compile")
		self.tools = tools
		return

	# Run the compile step on each source file
	def Run(self, builder: Builder):
		"""Compile the source files"""
		for src in builder.src:

			# Check the dependency file for this file to see if a compile is needed
			if not self.Changed(Join(self.outdir, f'{src.ftitle}.d', check_exists=False)):
				if self.verbosity >= 2: print(f'Compile: {src.fname} is up to date')
				continue

			# Compile the file
			if self.verbosity >= 1:
				print(f"Compiling '{src.fname}'...")

			# Run the compile step
			if not src.compile_step(src, builder):
				if not self.stop_on_first_error: continue
				raise RuntimeError("Compile errors")

		return

# Link object files
class LinkTask(Task):
	def __init__(self, tools: Tools):
		super().__init__("Link")
		self.tools = tools
		return
	
	def Run(self, builder: Builder):
		"""Link the object files"""
		return



# Build tools for MSVC
class MSVCTools(Tools):
	"""MSVC build context"""
	def __init__(self):
		super().__init__()
		self.compiler = "cl.exe"
		self.linker = "link.exe"
		self.archiver = "lib.exe"
		self.vswhere = "C:/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
		self.toolset_version = None
		self.AutoDetect()

		# Default flags to pass to cl.exe
		self.clflags = [
			'/nologo',
			'/EHsc',
		]

		# Flags for c files
		self.cflags = []

		# Flags for c++ files
		self.cxxflags = []

		# Linker flags
		self.lflags = []

		# Warning related flags
		self.warnings = [
			'/W4',
			'/WX-',
		]
		self.sdk_warnings = [
			'/W1',
		]
		return

	# Auto detect the MsVC build tools
	def AutoDetect(self) -> bool:
		"""Auto-detect MSVC build tools"""

		# Check if the compiler is in the path
		if not os.path.exists(self.vswhere):
			return False

		# Detect the Visual Studio installation path
		self.vspath = subprocess.check_output([self.vswhere, "-latest", "-property", "installationPath"], text=True).strip()
		if not self.vspath or not os.path.exists(self.vspath):
			raise RuntimeError("Visual Studio build tools not found")

		# If no toolset version is provided, try to find the latest version
		# Find the lexigraphically largest directory name in 'msvc_path'
		if not self.toolset_version:
			msvc_path = os.path.join(self.vspath, "VC/Tools/MSVC")
			self.toolset_version = max(os.listdir(msvc_path), key=lambda x: x.lower())
			if not self.toolset_version:
				raise RuntimeError("No MSVC toolset version found")

		# Set the tool paths
		tools_path = Join(self.vspath, f"VC/Tools/MSVC/{self.toolset_version}/bin/Hostx64/x64")
		self.compiler = Join(tools_path, "cl.exe")
		self.linker = os.path.join(tools_path, "link.exe")
		self.archiver = os.path.join(tools_path, "lib.exe")
		return

	# Compile a C/C++ file
	def CompileCXX(self, src: SrcFile, builder: Builder) -> bool:
		"""Compile a source file"""

		args = []

		# Flags
		args += src.flags
		args += self.flags
		args += self.cxxflags
		args += self.warnings if not src.is_sdk else self.sdk_warnings

		# Dependency file

		# Includes
		args += [f'/I{os.path.normpath(p)}' for p in builder.sys_includes]
		args += [f'/I{os.path.normpath(p)}' for p in builder.includes + src.includes]

		# Defines
		args += [f'/D{d}' for d in builder.defines + src.defines]

		# Source file
		args += ['/c', src.filepath]

		return self.CCPLUS(args, builder.verbosity)


# Build tools for gcc
class GCCTools(Tools):
	"""GCC build context"""
	def __init__(self):
		super().__init__()
		self.compiler = "gcc"
		self.linker = "gcc"
		self.archiver = "ar"

		# Flags passed to both 'cc' and 'as' for .s and .c/.cpp files
		self.flags = []

		# Flags for c files
		self.cflags = []

		# Flags for c++ files
		self.cxxflags = []

		# Linker flags
		self.lflags = []
		return


# Join paths
def Join(*args, check_exists:bool = True, normalise:bool = True, sep:str=os.path.sep):
	"""Join a list of path components into a single path. Optionally normalize and check if the path exists."""
	if not args or None in args:
		return None
		
	path = os.path.join(*args)
	
	# Normalise the path
	if normalise:
		path = path.replace('"','')
		path = os.path.abspath(path)
		path = path.replace(os.path.sep, sep)

	# Paths with wildcards don't exist
	check_exists &= any([x in path for x in ['*','?']]) == False
	if check_exists and not os.path.exists(path):
		raise FileNotFoundError(f"Path {path} does not exist")

	return path
