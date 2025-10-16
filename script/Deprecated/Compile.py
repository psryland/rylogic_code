#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Compile and execute cpp files
# Specify command line options using special comments: //@
# e.g.
#    //RELEASE = build in release mode
#    //DEBUG = build in debug mode (default)
#    //@ /I"P:\include" /W4 /DPR_DBG=0
#    //Copy lib.dll dst_path\
#
#    #include <iostream>
#    #include "pr/common/expr_eval.h"
#    void main()
#    {
#         std::cout << "Hello World" << std::endl;
#    }
#
# Use /c to compile only, no link
#
# To set up in VSCode:
# - Open a folder
# - Create a cpp like shown above
# - Create a launch.json (by hitting F5 and adding a configuration)
#   Set:
#       "name": "Python Compile and Run CPP",
#       "program": "py",
#       "args": ["P:/pr/script/Compile.py", "${file}"]
#       "type": "cppvsdbg",
#       "request": "launch",
#       "externalConsole": false

import sys, os, re, time, shlex
import Rylogic as Tools
import UserVars

DefaultSwitches = [
	"/nologo"             ,# no compiler version message
	"/EHsc"               ,# enable exception handling
	"/Gd"                 ,# __cdecl default calling convention
	"/GF"                 ,# enable readonly string pooling
	"/GS"                 ,# enable security checks
	"/Gy"                 ,# separate functions for linker
	"/Gm-"                ,# minimal rebuilds: false
	"/fp:precise"         ,# precise floating point
	"/EHsc"               ,# Exception handler support
	"/W4"                 ,# warning level 4
	"/WX-"                ,# warnings as errors: false
	"/wd4351"             ,# disable warning
	"/wd4355"             ,# disable warning
	"/Zi"                 ,# debugging info
	"/Zc:wchar_t"         ,# language conformance: wchar_t is native type
	"/Zc:forScope"        ,# language conformance: for loop scope
	"/Zc:auto"            ,# language conformance: auto
	"/Zc:inline"          ,# language conformance: inline
	"/MP"                 ,# multi processor compiling
	"/MTd"                ,# link with LIBCMTD.LIB
	"/openmp"             ,# enable OpenMP 2.0 language extensions
	"/analyze-"           ,# native analysis: disabled
	"/sdl-"               ,# disable additional security checks
	"/RTC1"               ,# enable fast checks
	"/errorReport:prompt" ,# internal compiler errors: prompt to send immediately
	]
DefaultIncludes = [
	]
DefaultLibPaths = [
	]
DefaultDefines = [
	"_WIN32_WINNT=_WIN32_WINNT_WIN7",
	"WIN32_LEAN_AND_MEAN",
	"_CRT_SECURE_NO_WARNINGS",
	"_SCL_SECURE_NO_WARNINGS",
	"_SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING",
	"NOMINMAX",
	]

PrSwitches = [
	]
PrIncludes = [
	Tools.AssertPath(UserVars.root + "\\projects"),
	Tools.AssertPath(UserVars.root + "\\include"),
	Tools.AssertPath(UserVars.root + "\\sdk"),
	Tools.AssertPath(UserVars.root + "\\sdk\\sqlite3\\include"),
	Tools.AssertPath(UserVars.root + "\\sdk\\lua\\src"),
	Tools.AssertPath(UserVars.root + "\\sdk\\lua"),
	]
PrLibPaths = [
	UserVars.root+"\\lib\\$(platform)\\$(config)",
	]
PrDefines = [
	"PR_UNITTESTS=0",
	"PR_MATHS_USE_INTRINSICS=1",
	]

EnvIs32Bit = False

# Set environment variables needed for cl.exe to work
def SetupVCEnvironment(_32bit:bool):
	global EnvIs32Bit
	EnvIs32Bit = _32bit

	os.environ["INCLUDE"] = (
		Tools.AssertPath(UserVars.vs_platform_dir + "\\include") + ";" +
		Tools.AssertPath(UserVars.winsdk_include)                + ";" +
		Tools.AssertPath(UserVars.winsdk_include + "\\ucrt")     + ";" +
		Tools.AssertPath(UserVars.winsdk_include + "\\shared")   + ";" +
		Tools.AssertPath(UserVars.winsdk_include + "\\um")       + ";" +
		Tools.AssertPath(UserVars.winsdk_include + "\\winrt")    + ";" +
		#Tools.AssertPath(UserVars.winsdk + "\\..\\NETFXSDK\\4.6\\include\\um") + ";" +
		"")
	if _32bit:
		os.environ["LIB"] = (
			Tools.AssertPath(UserVars.vs_platform_dir + "\\lib\\x86") + ";" +
			Tools.AssertPath(UserVars.winsdk_lib + "\\ucrt\\x86")     + ";" +
			Tools.AssertPath(UserVars.winsdk_lib + "\\um\\x86")       + ";" +
			"")
	else:
		os.environ["LIB"] = (
			Tools.AssertPath(UserVars.vs_platform_dir + "\\lib\\x64") + ";" +
			Tools.AssertPath(UserVars.winsdk_lib + "\\ucrt\\x64")     + ";" +
			Tools.AssertPath(UserVars.winsdk_lib + "\\um\\x64")       + ";" +
			"")
	os.environ["LIBPATH"] = (
		Tools.AssertPath(UserVars.dotnet)                                                                        + ";" +
		Tools.AssertPath(UserVars.winsdk_references)                                                             + ";" +
		Tools.AssertPath(UserVars.winsdk_references + "\\Windows.Foundation.UniversalApiContract\\7.0.0.0")      + ";" +
		Tools.AssertPath(UserVars.winsdk_references + "\\Windows.Foundation.FoundationContract\\3.0.0.0")        + ";" +
		Tools.AssertPath(UserVars.winsdk_references + "\\Windows.Networking.Connectivity.WwanContract\\2.0.0.0") + ";" +
		#Tools.AssertPath(UserVars.winsdk + "\\UnionMetadata" + UserVars.winsdkvers)                                             + ";" +
		#Tools.AssertPath(UserVars.winsdk + "\\ExtensionSDKs\\Microsoft.VCLibs\\14.0\\References\\CommonConfiguration\\neutral") + ";" +
		"")
	os.environ["WindowsLibPath"] = (
		Tools.AssertPath(UserVars.winsdk_references) + ";"                                                       +
		Tools.AssertPath(UserVars.winsdk_references + "\\Windows.Foundation.UniversalApiContract\\7.0.0.0")      + ";" +
		Tools.AssertPath(UserVars.winsdk_references + "\\Windows.Foundation.FoundationContract\\3.0.0.0")        + ";" +
		Tools.AssertPath(UserVars.winsdk_references + "\\Windows.Networking.Connectivity.WwanContract\\2.0.0.0") + ";" +
		#Tools.AssertPath(UserVars.winsdk + "\\UnionMetadata\\" + UserVars.winsdkvers) + ";" +
		"")
	os.environ["DevEnvDir"         ] = Tools.AssertPath(UserVars.vs_dir + "\\Common7\\IDE\\")
	os.environ["ExtensionSdkDir"   ] = Tools.AssertPath(UserVars.winsdk + "\\Extension SDKs")
	os.environ["Framework40Version"] = "v4.0"
	os.environ["FrameworkDir"      ] = Tools.AssertPath(UserVars.dotnetdir + "\\")
	os.environ["FrameworkDIR32"    ] = Tools.AssertPath(UserVars.dotnetdir + "\\")
	os.environ["FrameworkVersion"  ] = "v4.0.30319"
	os.environ["FrameworkVersion32"] = "v4.0.30319"
	os.environ["NETFXSDKDir"        ] = Tools.AssertPath(UserVars.winsdk + "\\..\\NETFXSDK\\4.7.2\\")
	os.environ["UCRTVersion"        ] = UserVars.winsdkvers
	os.environ["UniversalCRTSdkDir" ] = Tools.AssertPath(UserVars.winsdk + "\\")
	os.environ["VCINSTALLDIR"       ] = Tools.AssertPath(UserVars.vs_dir + "\\VC\\")
	os.environ["VisualStudioVersion"] = UserVars.vs_vers
	os.environ["VSINSTALLDIR"       ] = Tools.AssertPath(UserVars.vs_dir + "\\")
	os.environ["WindowsSdkDir"                ] = Tools.AssertPath(UserVars.winsdk + "\\")
	os.environ["WindowsSDKLibVersion"         ] = UserVars.vs_vers
	os.environ["WindowsSDKVersion"            ] = UserVars.vs_vers
	#os.environ["WindowsSDK_ExecutablePath_x64"] = r"C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v10.0A\\bin\\NETFX 4.6 Tools\\x64\\"
	#os.environ["WindowsSDK_ExecutablePath_x86"] = r"C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v10.0A\\bin\\NETFX 4.6 Tools\\"

# Opens a C++ source file and scans for lines beginning with:
#  //@ = cl.exe command line option
#  //Copy <src> <dst> = Post build copy file
#  //RELEASE = build in release mode
#  //DEBUG = build in debug mode (default)
# Stops scanning at the first line that begins with '#'
# Returns the text on those lines as command line options that can be passed to cl.exe
def ExtractFileOptions(filepath:str):
	sw    = []   # Addition compiler switches
	copy  = []   # Post-build copy pairs
	debug = None # Debug mode by default
	with open(filepath, encoding="utf-8") as f:
		for line in f:
			if len(line) == 0: continue
			if line[0] == '#': break
			if line.startswith("//@"):
				sw += shlex.split(line[3:])
			if line.startswith("//Copy"):
				copy += [shlex.split(line[6:])]
			if line.startswith("//DEBUG"):
				debug = True
			if line.startswith("//RELEASE"):
				debug = False
	return sw, copy, debug

# Return the full path to 'cl.exe' for 32 or 64 bit
def CompilerPath():
	path = UserVars.vs_compiler32 if EnvIs32Bit else UserVars.vs_compiler64
	return Tools.AssertPath(path)

# Return the full path to 'link.exe' for 32 or 64 bit
def LinkerPath():
	path = UserVars.vs_linker32 if EnvIs32Bit else UserVars.vs_linker64
	return Tools.AssertPath(path)

# Link object files together
# Use this if you don't use 'create_exe' in the 'Compile' function
def Link(
	obj_files:[],
	bin_name:str,
	libpaths:[],
	debug:bool
	):

	# Get the linker path
	link = LinkerPath()

	args = []
	args += ["/OUT:"+bin_name]
	args += ["/LIBPATH:"+l for l in libpaths]
	if debug:
		args += ["/DEBUG"]
	
	# Link the object files together
	Tools.Exec([link] + args + obj_files)

	# Return the binary name
	return bin_name

# Compile the given C++ files
# The first source file can specify additional compiler switches by having special
# comments (//@) before the first preprocessor command
# Returns the name of the executable file if generated
def Compile(
	files:[],
	create_exe = False,
	debug      = False,
	outdir     = "",
	sw         = [],
	defines    = [],
	includes   = [],
	libpaths   = [],
	):

	# Get the compiler path
	cl = CompilerPath()
	sw = list(set(sw + DefaultSwitches))
	defines += DefaultDefines
	includes += DefaultIncludes
	libpaths += DefaultLibPaths

	# Convert all files to full paths
	files = [os.path.abspath(f) for f in files]

	# Check the paths exist
	for f in files:
		if os.path.exists(f): continue
		raise FileNotFoundError(f"ERROR: {f} does not exist")

	# Pull the filepath apart
	dir, file = os.path.split(files[0])
	fname = os.path.splitext(file)[0]
	if dir.lower().endswith("src"):
		dir = os.path.abspath(os.path.join(dir, ".."))

	# Default the output directory to the same path as the input file
	outdir = os.path.abspath(outdir if outdir else os.path.join(dir,"obj"))
	if not os.path.exists(outdir): os.mkdir(outdir)
	os.chdir(outdir)

	# Extract info from the file 
	additional_sw, copy, dbg = ExtractFileOptions(files[0])

	# Adjust switches for debug/release
	if dbg != None: debug = dbg
	if debug:
		sw += [
			"/Od" , # optimisation: disabled
			"/Ob0", # optimisation: inline expansion depth
			"/Oy-", # optimisation: disable frame pointer omission
			]
	else:
		sw.remove("/RTC1")
		sw += [
			"/Ox" , # optimisation: full
			"/Ob2", # optimisation: inline any suitable
			"/Oi" , # optimisation: enable intrinsics
			"/Ot" , # optimisation: favour fast code
			"/Oy" , # optimisation: omit frame pointers
			"/GT" , # optimisation: enable fiber safe optimisations
			"/GL" , # optimisation: whole program optimisation
			] 
	
	# Generate the command line switches
	args = []
	args += sw + additional_sw
	args += [] if create_exe else ["/c"]
	args += ["/I"+inc+"" for inc in includes]
	args += ["/D"+deph for deph in defines]
	args += ["/D_DEBUG"] if debug else []
	args += ["/Fo" + outdir + "\\"]
	args += ["/Fe" + outdir + "\\"]
	args = list(set(args))
	args.sort()

	# Compile 
	Tools.Exec([cl] + args + files)

	# Post-build copy commands
	for cp in copy:
		Tools.Copy(cp[0], cp[1])
		
	# If the /c switch is given, then no exe is produced
	if "/c" in args:
		return ""

	# Generate a name for the exe
	exe = outdir + "\\" + fname + ".exe"

	# Link the exe
	Link(
		[outdir + "\\" + os.path.splitext(os.path.split(f)[1])[0] + ".obj" for f in files],
		exe,
		libpaths,
		debug)

	# Return the name of the executable
	return exe
	
# Compile a C++ source file using standard pr includes, compiler switches
def CompilePR(
	files:[],
	create_exe = False,
	debug      = False,
	outdir     = "",
	sw         = PrSwitches,
	defines    = PrDefines,
	includes   = PrIncludes,
	libpaths   = PrLibPaths
	):
	return Compile(files, create_exe, debug, outdir, sw, defines, includes, libpaths)

# Compile and execute
if __name__ == "__main__":
	"""Use Compile.py [run] <source_file.cpp> [<output_directory>]"""
	try:
		Tools.AssertVersion(1)
		#print(sys.argv)

		run = False
		src_file = None
		out_dir = None

		# Parse the command line arguments
		i = 1
		if len(sys.argv) > i and sys.argv[i] == "run":
			run = True
			i += 1
		if len(sys.argv) > i:
			src_file = sys.argv[i]
			i += 1
		if len(sys.argv) > i:
			out_dir = sys.argv[i]
			i += 1
		if not src_file:
			src_file = input("Source File: ")

		# Set up the VC environment variables
		SetupVCEnvironment(_32bit=False)

		# Compile the executable
		exepath = CompilePR(
			files      = [src_file],
			create_exe = True,
			debug      = True,
			outdir     = out_dir)

		# Run the executable
		if run and len(exepath) != 0:
			print("Executing: " + exepath)
			proc = Tools.Spawn([exepath])
			proc.wait()

	except Exception as ex:
		Tools.OnException(ex)
