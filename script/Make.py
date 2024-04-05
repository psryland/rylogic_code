#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Project builder
#  This script is the base for a project building script.
#  This is because declarative build systems like CMake, make, etc suck, are hard to debug, and have ugly syntax.
#  Python can be debugged, is flexible enough to do anything, and has well known syntax.
#  In your project,
#   - Create a class that subclasses 'Builder'
#   - Populate the members 'cc', 'ccplus', 'ld', etc as needed
#   - Populate the arrays 'src', 'defines', 'includes', 'libraries', etc as needed
#   - Execute the build process by creating an instance of your class and calling 'Build'
#     e.g.
#        builder = MyProject()
#        builder.Build(sys.argv)
import os, enum, shutil, subprocess
from typing import List
	
# The build system to use
class ESystem(enum.Enum):
	GNUC = 1,
	MSVC = 2,
	ARM = 3,

# Build system
class Builder():

	# Notes:
	#  gcc (gnu C compiler) compiles .c or .cpp files, as .c or .cpp respectively
	#  g++ (gnu C++ compiler) compiles .c or .cpp files, but as if they are all .cpp files
	#  cpp (C Preprocessor) preprocesses .c or .cpp files
	#  g++ can be used to link object files. Automatically links std C++ libraries
	#  gcc can not link
	#
	# This is still a WIP for building MSVC projects

	# Tools
	pp      = None
	cc      = None
	ccplus  = None
	asm     = None
	linker  = None
	objcopy = None
	mkld    = None
	nm      = None

	# Defaults
	def __init__(self, build_system: ESystem):
		self.build_system = build_system
		self.config = 'Debug'
		self.platform = ''
		self.target = ''
		self.rebuild = False
		self.clean = False
		self.verbosity = 1# [0-quiet,1-minimal,2-normal,3-spamtastic]
		self.stop_on_first_error = False
		self.ignore_project_file_changes = False

		# Project file
		# Set this to the main python script that builds the project (the one that contains the class that subclasses 'Builder')
		# This is mainly for dependency checking, so that the project is rebuilt if the project file changes.
		self.project_file = None

		# Build output directory.
		# Set this to a directory that will contain the build temporary files. It's a directory you can delete to perform a clean rebuild
		self.outdir = None

		# The name of the binary to produce (including extension)
		# This could be: 'my_project.elf', 'whatsimacallit.exe', 'library.s', etc
		self.target_fname = None

		# Source files
		# This array should be filled with the full paths of the source files that contribute to the project
		self.src = []

		# Application defines
		# This array should be filled with any preprocessor defines. Use 'MY_DEFINE' or 'MY_DEFINE=12' form.
		self.defines = []

		# Application include paths
		# This array should be filled with full paths to include directories specific to the project.
		self.includes = []

		# System include paths
		# This array should be filled with full paths to include directories for system includes (i.e., -isystem command line includes)
		self.sys_includes = []

		# Libraries
		self.libraries = []
		self.library_paths = []

		# System libraries
		self.sys_libraries = []
		self.sys_library_paths = []

		# Warning related flags
		self.warnings = []
		self.sdk_warnings = []

		# Flags passed to both 'cc' and 'as' for .s and .c/.cpp files
		self.flags = []

		# Flags for c files
		self.cflags = []

		# Flags for c++ files
		self.cxxflags = []

		# Flags for s files
		self.sflags = []

		# Linker flags
		self.lflags = []

		# Dependency generation
		if build_system == ESystem.GNUC or build_system == ESystem.ARM:
			self.dep_gen_flag = '-MMD'

		# Compile all object files via ASM files. i.e., C\C++ files are compiled to ASM which are then assembled
		self.compile_via_asm = False

		# Linker script generator input
		self.flash_placement = None
		self.placement_macros = []
		self.placements_segments = []
		self.linker_script_symbols = []

	# Run the build process
	def Build(self, args:List[str]):

		# Parse command line
		i = 1
		while i < len(args):
			arg = args[i].lower()
			if False:
				pass
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
					if False: pass
					elif args[i].lower() == 'debug': self.config = 'Debug'
					elif args[i].lower() == 'release': self.config = 'Release'
					else: raise RuntimeError(f'Unknown config: {args[i]}')
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

		# Split the target fname into name and extension
		self.target_name, self.target_extn = os.path.splitext(self.target_fname)

		# Populate build input data
		self.Setup()

		# Check for required variables
		if not self.build_system:
			raise RuntimeError("No build system set")
		if not self.project_file:
			raise RuntimeError("No project file set")
		if not self.outdir:
			raise RuntimeError("No build output directory set")
		if not self.target_fname:
			raise RuntimeError("No target name set")
		for src in [os.path.normpath(p) for p in self.src]:
			if os.path.exists(src): continue
			print(f"WARNING: Source file '{src}' does not exist")
		for ip in [os.path.normpath(p) for p in self.includes]:
			if os.path.exists(ip): continue
			print(f"WARNING: Include path '{ip}' does not exist")

		# Clean
		if self.clean and os.path.exists(self.outdir):
			shutil.rmtree(self.outdir)

		# Ensure the output directory exists
		os.makedirs(self.outdir, exist_ok=True)

		# Perform the build steps
		self.BuildStart()
		self.CompileSource()
		self.GenerateLinkerScript()
		self.Link()
		self.BuildComplete()

	# Populate members based on config
	def Setup(self):
		pass

	# Check the dependency file for any files that have changed
	def Changed(self, dep_file:str):

		# Assume all changed on rebuild
		if self.rebuild:
			return True

		# No dep file, assume changed
		if not os.path.exists(dep_file):
			if self.verbosity >= 2: print(f'Dependency: {dep_file} not found')
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

	# Build start event
	def BuildStart(self):

		if self.verbosity >= 1:
			print(f"Build {self.target} Starting...")

		# Marker file used to detect project file changes
		last_built_file = Join(self.outdir, "last_built", check_exists=False)

		# If no last built file, rebuild all
		if not os.path.exists(last_built_file):
			if self.verbosity >= 2: print("Rebuilding all since not last built file found")
			self.rebuild = True

		# Rebuild all if the project file or this file changes
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

	# Build complete event
	def BuildComplete(self):
		if self.verbosity >= 1:
			print("Build Complete")

	# Compile source
	# Build .o files from all .c/.cpp/.s files
	def CompileSource(self):
		success = True
		for file in self.src:
			if isinstance(file, str): file = SrcFile(file)
			filepath = file.filepath
			fdir,fname = os.path.split(filepath)
			ftitle,ext = os.path.splitext(fname)
			is_sdk = self.IsSDK(filepath)

			# Check if a compile is needed
			if not self.Changed(Join(self.outdir, f'{ftitle}.d', check_exists=False)):
				if self.verbosity >= 2: print(f'Compile: {fname} is up to date')
				continue

			# Don't know what to do with other file types
			if ext != '.c' and ext != '.cpp' and ext != '.s':
				raise RuntimeError(f"Unsupported source file type: {ext}")

			# Compile a s/c/cpp file
			if self.verbosity >= 1:
				print(f"Compiling '{fname}'...")

			# Collect command line args
			args = []

			# Flags
			args += self.flags
			if False: pass
			elif ext == '.s': args += self.sflags
			elif ext == '.c': args += self.cflags
			elif ext == '.cpp': args += self.cxxflags
			args += file.flags
			args += self.warnings if not is_sdk else self.sdk_warnings

			# Dependency file generation
			if False: pass
			elif self.build_system == ESystem.GNUC:
				if self.dep_gen_flag in ['-MF', '-MT', '-MQ']:
					args += [self.dep_gen_flag,  Join(self.outdir, f'{ftitle}.d', check_exists=False)]
				elif self.dep_gen_flag in ['-MD', '-MMD']:
					args += [self.dep_gen_flag]
			elif self.build_system == ESystem.ARM:
				if self.dep_gen_flag in ['-MF', '-MT', '-MQ']:
					args += [self.dep_gen_flag,  Join(self.outdir, f'{ftitle}.d', check_exists=False)]
				elif self.dep_gen_flag in ['-MD', '-MMD']:
					args += [self.dep_gen_flag]#, Join(self.outdir, f'{ftitle}.d', check_exists=False)]
			elif self.build_system == ESystem.MSVC:
				pass

			# Includes
			if False: pass
			elif self.build_system == ESystem.GNUC:
				args += [f'-isystem{os.path.normpath(p)}' for p in self.sys_includes]
				args += [f'-I{os.path.normpath(p)}' for p in self.includes]
			elif self.build_system == ESystem.ARM:
				args += [f'-isystem{os.path.normpath(p)}' for p in self.sys_includes]
				args += [f'-I{os.path.normpath(p)}' for p in self.includes]
			elif self.build_system == ESystem.MSVC:
				args += [f'/I{os.path.normpath(p)}' for p in self.sys_includes]
				args += [f'/I{os.path.normpath(p)}' for p in self.includes]

			# Defines
			if False: pass
			elif self.build_system == ESystem.GNUC:
				args += [f'-D{d}' for d in self.defines + file.defines]
			elif self.build_system == ESystem.ARM:
				args += [f'-D{d}' for d in self.defines + file.defines]
			elif self.build_system == ESystem.MSVC:
				args += [f'/D{d}' for d in self.defines + file.defines]

			# If compiling to ASM files first
			if self.compile_via_asm and ext != '.s':

				# Set output to asm file
				args0 = args + ['-o', Join(self.outdir, f'{ftitle}.s', check_exists=False)]

				# Compile
				if ext == '.c': success &= self.CC(args0)
				if ext == '.cpp': success &= self.CCPLUS(args0)
				if not success:
					if not self.stop_on_first_error: continue
					raise RuntimeError("Compile errors")

				ext = '.s'

			# Compile
			if False: pass
			elif ext == '.s':
				# Preprocess the file
				ppargs = args + []
				if self.build_system == ESystem.GNUC or self.build_system == ESystem.ARM:
					ppargs += [filepath]
				elif self.build_system == ESystem.MSVC:
					ppargs += ['/c', filepath]

				# Set the output object file
				if self.build_system == ESystem.GNUC or self.build_system == ESystem.ARM:
					ppargs += ['-o', Join(self.outdir, f'{ftitle}_pp.s', check_exists=False)]
				elif self.build_system == ESystem.MSVC:
					raise RuntimeError("not implemented")

				# Preprocess
				success &= self.PP(ppargs)
				if not success:
					if not self.stop_on_first_error: continue
					raise RuntimeError("Preprocess errors")

				# Assemble to .o using 'as'
				args = []
				args += self.flags
				if self.build_system == ESystem.GNUC or self.build_system == ESystem.ARM:
					args += ['--traditional-format']
					args += [f'-I{os.path.normpath(p)}' for p in self.includes]
					args += ['-o', Join(self.outdir, f'{ftitle}.o', check_exists=False)]
					args += ['-g', '-gdwarf-4']
					args += [Join(self.outdir, f'{ftitle}_pp.s', check_exists=False)]
				elif self.build_system == ESystem.MSVC:
					raise RuntimeError("not implemented")

				# Assemble
				success &= self.ASM(args)
				if not success:
					if not self.stop_on_first_error: continue
					raise RuntimeError("Assembling errors")

				# Remove the temporary asm files
				#os.unlink(Join(self.outdir, f'{ftitle}.asm'))
			elif ext == '.c' or ext == '.cpp':
				# The file to compile
				if self.build_system == ESystem.GNUC:
					args += ['-c', filepath]
				elif self.build_system == ESystem.ARM:
					args += ['-c', filepath]
				elif self.build_system == ESystem.MSVC:
					args += ['/c', filepath]
				
				# The output object file
				if self.build_system == ESystem.GNUC or self.build_system == ESystem.ARM:
					args += ['-o', Join(self.outdir, f'{ftitle}.o', check_exists=False)]
				elif self.build_system == ESystem.MSVC:
					raise RuntimeError("not implemented")

				# Compile
				if ext == '.c': success &= self.CC(args)
				if ext == '.cpp': success &= self.CCPLUS(args)
				if not success:
					if not self.stop_on_first_error: continue
					raise RuntimeError("Compile errors")
			
		if not success:
			raise RuntimeError("Compile errors")
		return

	# Generate the linker script
	def GenerateLinkerScript(self):

		# If not specified, skip this step
		if Builder.mkld is None:
			return

		if self.verbosity >= 1:
			print(f'Generating linker script "{self.target_name}.ld"')
		args = []
		args += ['-section-placement-file', self.flash_placement]
		args += ['-memory-map-segments', ';'.join(self.placements_segments)]
		args += ['-section-placement-macros', ';'.join(self.placement_macros)]
		args += ['-symbols', ';'.join(self.linker_script_symbols)]
		args += ['-check-segment-overflow']
		args += [Join(self.outdir, f'{self.target_name}.ld', check_exists=False)]
		success = self.MKLD(args)
		if not success:
			raise RuntimeError("Linker script generationg failed")

	# Run the linker to generate output
	def Link(self):
		# Notes:
		#  - The order of libraries matters! GCC uses --start-group|--end-group to create
		#    a group of libraries that are searched repeatedly until all undefined references
		#    are resolved.

		# Build the list of objects to link
		link_input = []
		for file in self.src:
			if isinstance(file, str): file = SrcFile(file)
			_,fname = os.path.split(file.filepath)
			ftitle,ext = os.path.splitext(fname)
			if self.build_system == ESystem.GNUC:
				link_input.append(Join(self.outdir, f'{ftitle}.o'))
			elif self.build_system == ESystem.ARM:
				link_input.append(Join(self.outdir, f'{ftitle}.o'))
			elif self.build_system == ESystem.MSVC:
				link_input.append(Join(self.outdir, f'{ftitle}.obj'))
		for lib in self.libraries:
			link_input.append(lib)
		for lib in self.sys_libraries:
			link_input.append(lib)
		if len(link_input) == 0:
			return

		if self.build_system == ESystem.GNUC or self.build_system == ESystem.ARM:

			# If a linker is specified, use it to link
			if Builder.linker:
				args = []
				args += self.lflags

				# Linker script
				linker_script = Join(self.outdir, f'{self.target_name}.ld', check_exists=False)
				if os.path.exists(linker_script):
					args += [f'-T{linker_script}']

				# Map file
				args += ['-Map', Join(self.outdir, f'{self.target_name}.map', check_exists=False)]

				# Output target
				args += ['-o', Join(self.outdir, self.target_fname, check_exists=False)]

				# Objects to link
				args += ['--start-group'] + link_input + ['--end-group']

				# Link!
				success = self.LINK(args)
				if not success:
					raise RuntimeError("Linking failed")

			# Otherwise, use g++ to link
			elif Builder.ccplus:
				args = []
				args += self.lflags

				# Map file
				#args += ['-Map', Join(self.outdir, f'{self.target_name}.map', check_exists=False)]

				# Output target
				args += ['-o', Join(self.outdir, self.target_fname, check_exists=False)]

				# Objects to link
				args += link_input

				# Link!
				success = self.CCPLUS(args)
				if not success:
					raise RuntimeError("Linking failed")

		elif self.build_system == ESystem.MSVC:

			args = []
			args += self.lflags

			# Library paths
			args += [f'/LIBPATH:{dir}' for dir in self.sys_library_paths]
			args += [f'/LIBPATH:{dir}' for dir in self.library_paths]

			# Objects to link
			args += link_input
			
			success = self.LINK(args)
			if not success:
				raise RuntimeError("Linking failed")

		return

	# Preprocess using 'pp'
	def PP(self, args:List[str]):
		try:
			cmd = [Builder.pp if Builder.pp else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			if not Builder.pp: raise RuntimeError(f"Cannot preprocess {cmd}. No Preprocessor path given")
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Compile using 'cc'
	def CC(self, args:List[str]):
		try:
			cmd = [Builder.cc if Builder.cc else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			if not Builder.cc: raise RuntimeError(f"Cannot compile {cmd}. No C compiler path given")
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Compile a C++ file
	def CCPLUS(self, args:List[str]):
		try:
			cmd = [Builder.ccplus if Builder.ccplus else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			if not Builder.ccplus: raise RuntimeError(f"Cannot compile {cmd}. No C++ compiler path given")
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Compile assembly code using 'asm'
	def ASM(self, args:List[str]):
		try:
			cmd = [Builder.asm if Builder.asm else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			if not Builder.asm: raise RuntimeError(f"Cannot assemble {cmd}. No assembler path given")
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Run the linker script generator using 'mkld'
	def MKLD(self, args:List[str]):
		try:
			cmd = [Builder.mkld if Builder.mkld else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Run the linker using 'linker'
	def LINK(self, args:List[str]):
		try:
			cmd = [Builder.linker if Builder.linker else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Run the objcpy using 'objcopy'
	def ObjCopy(self, args:List[str]):
		try:
			cmd = [Builder.objcopy if Builder.objcopy else ''] + args
			if self.verbosity >= 3: print(' '.join(cmd))
			subprocess.check_output(cmd, universal_newlines=True)#, stderr=subprocess.STDOUT)
			return True
		except subprocess.CalledProcessError as ex:
			print(ex.output)
			return False

	# Return true if the given file is a third party library file
	def IsSDK(self, filepath:str):
		# Sub-classes can override this method
		# SDK files have different warning flags
		return 'sdk' in filepath.lower()

	# Seach object files and library files for a symbol
	def FindSymbol(self, symbol:str, paths:List[str]):
		found = []
		for path in paths:
			if not os.path.exists(path): continue
			for root, _, files in os.walk(path):
				for file in files:
					_, fext = os.path.splitext(file)
					fext = fext.lower()
					if  fext != '.o' and \
						fext != '.a' and \
						fext != '.obj' and \
						fext != '.lib':
						continue

					# Dump the symbols in this library/object file
					fullpath = os.path.join(root, file)
					cmd = [Builder.nm, '-g', fullpath]
					out = subprocess.check_output(cmd, universal_newlines=True)

					# Get the lines that contain the symbol
					lines = []
					for line in out.split('\n'):
						if symbol in line:
							lines.append(line)
					if len(lines) != 0:
						found.append((fullpath, lines))

		# Dump out the library/object file paths that contain this symbol
		if len(found) > 1:
			print(f"Symbol '{symbol}' found in")
			for fullpath, lines in found:
				print(fullpath)
				for line in lines:
					print('  ', line)
				print()
		return

# A source file with optional extra defines etc
class SrcFile():
	def __init__(self, filepath:str, defines:List[str] = [], flags:List[str] = []):
		self.filepath = filepath
		self.defines = defines
		self.flags = flags

# Helper functions
def Join(*args, check_exists:bool = True, normalise:bool = True, sep:str=os.path.sep):
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
