#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Compile and execute cpp files
# Specify command line options using special comments: //@
# e.g.
#    //@ /I"P:\include" /W4 /DPR_DBG=0
#    #include <iostream>
#    #include "pr/common/expr_eval.h"
#    void main()
#    {
#         std::cout << "Hello World" << std::endl;
#    }
#
# Use /c to compile only, no link
#
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
	"/Od"                 ,# optimisation: disabled
	"/Ob0"                ,# optimisation: inline expansion depth
	"/Oy-"                ,# optimisation: disable frame pointer omission
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
DefaultLibs = [
	]
DefaultDefines = [
	"_WIN32_WINNT=_WIN32_WINNT_WIN7",
	"WIN32_LEAN_AND_MEAN",
	"_CRT_SECURE_NO_WARNINGS",
	"_SCL_SECURE_NO_WARNINGS",
	"NOMINMAX",
	]
	
PrSwitches = DefaultSwitches + [
	]
PrIncludes = [
	UserVars.root + r"\projects",
	UserVars.root + r"\include",
	UserVars.root + r"\sdk",
	UserVars.root + r"\sdk\sqlite\include",
	UserVars.root + r"\sdk\lua\lua\src",
	UserVars.root + r"\sdk\lua",
	] + DefaultIncludes
PrLibs = [
	UserVars.root+"\\lib\\$(platform)\\$(config)",
	] + DefaultLibs
PrDefines = [
	"PR_UNITTESTS=1",
	"PR_MATHS_USE_INTRINSICS=1",
	] + DefaultDefines
	
# Set environment variables needed for cl.exe to work
def SetupVCEnvironment(_32bit = True):
	if UserVars.winsdkvers == "8.1":
		os.environ["INCLUDE"] = (
			UserVars.vs_dir + "\\VC\\INCLUDE;" +
			UserVars.vs_dir + "\\VC\\ATLMFC\\INCLUDE;" +
			UserVars.winsdk + "\\include\\um;" +
			UserVars.winsdk + "\\include\\shared;" +
			UserVars.winsdk + "\\include\\winrt;" +
			"")
		if _32bit:
			os.environ["LIB"] = (
				UserVars.vs_dir + "\\VC\\LIB;" +
				UserVars.vs_dir + "\\VC\\ATLMFC\\LIB;" +
				UserVars.winsdk + "\\lib\\8.1\\lib\\winv6.3\\um\\x86;" +
				"")
		else:
			os.environ["LIB"] = (
				UserVars.vs_dir + "\\VC\\LIB\\amd64;" +
				UserVars.vs_dir + "\\VC\\ATLMFC\\LIB\\amd64;" +
				UserVars.winsdk + "\\lib\\8.1\\lib\\winv6.3\\um\\x64;" +
				"")
	elif UserVars.winsdkvers in ["10.0.10150.0", "10.0.10240.0", "10.0.10586.0"]:
		os.environ["INCLUDE"] = (
			UserVars.vs_dir + "\\VC\\INCLUDE;" +
			UserVars.vs_dir + "\\VC\\ATLMFC\\INCLUDE;" +
			UserVars.winsdk + "\\include\\" + UserVars.winsdkvers + "\\ucrt;" +
			UserVars.winsdk + "\\include\\" + UserVars.winsdkvers + "\\shared;" +
			UserVars.winsdk + "\\include\\" + UserVars.winsdkvers + "\\um;" +
			UserVars.winsdk + "\\include\\" + UserVars.winsdkvers + "\\winrt;" +
			UserVars.winsdk + "\\..\\NETFXSDK\\4.6\\include\\um;" +
			"")
		if _32bit:
			os.environ["LIB"] = (
				UserVars.vs_dir + "\\VC\\LIB;" +
				UserVars.vs_dir + "\\VC\\ATLMFC\\LIB;" +
				UserVars.winsdk + "\\lib\\" + UserVars.winsdkvers + "\\ucrt\\x86;" +
				UserVars.winsdk + "\\lib\\" + UserVars.winsdkvers + "\\um\\x86;" +
				"")
		else:
			os.environ["LIB"] = (
				UserVars.vs_dir + "\\VC\\LIB\\amd64;" +
				UserVars.vs_dir + "\\VC\\ATLMFC\\LIB\\amd64;" +
				UserVars.winsdk + "\\lib\\" + UserVars.winsdkvers + "\\ucrt\\x64;" +
				UserVars.winsdk + "\\lib\\" + UserVars.winsdkvers + "\\um\\x64;" +
				"")
	else:
		raise Exception("Unknown windows SDK version")

	os.environ["LIBPATH"] = (
		UserVars.dotnet + ";" +
		UserVars.vs_dir + "\\VC\\LIB;" +
		UserVars.vs_dir + "\\VC\\ATLMFC\\LIB;" +
		UserVars.winsdk + "\\UnionMetadata;" +
		UserVars.winsdk + "\\References;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.UniversalApiContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.FoundationContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Networking.Connectivity.WwanContract\\1.0.0.0;" +
		UserVars.winsdk + "\\ExtensionSDKs\\Microsoft.VCLibs\\14.0\\References\\CommonConfiguration\\neutral;" +
		"")
	os.environ["WindowsLibPath"] = (
		UserVars.winsdk + "\\UnionMetadata;" +
		UserVars.winsdk + "\\References;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.UniversalApiContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.FoundationContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Networking.Connectivity.WwanContract\\1.0.0.0;" +
		"")
	os.environ["DevEnvDir"         ] = UserVars.vs_dir + "\\Common7\\IDE\\"
	os.environ["ExtensionSdkDir"   ] = UserVars.winsdk + "\\Extension SDKs"
	os.environ["Framework40Version"] = "v4.0"
	os.environ["FrameworkDir"      ] = UserVars.dotnetdir + "\\"
	os.environ["FrameworkDIR32"    ] = UserVars.dotnetdir + "\\"
	os.environ["FrameworkVersion"  ] = "v4.0.30319"
	os.environ["FrameworkVersion32"] = "v4.0.30319"
	os.environ["NETFXSDKDir"        ] = UserVars.winsdk + "\\..\\NETFXSDK\\4.6\\"
	os.environ["UCRTVersion"        ] = UserVars.winsdkvers
	os.environ["UniversalCRTSdkDir" ] = UserVars.winsdk + "\\"
	os.environ["VCINSTALLDIR"       ] = UserVars.vs_dir + "\\VC\\"
	os.environ["VisualStudioVersion"] = UserVars.vs_vers
	os.environ["VSINSTALLDIR"       ] = UserVars.vs_dir + "\\"
	os.environ["WindowsSdkDir"                ] = UserVars.winsdk + "\\"
	os.environ["WindowsSDKLibVersion"         ] = UserVars.vs_vers + "\\"
	os.environ["WindowsSDKVersion"            ] = UserVars.vs_vers + "\\"
	os.environ["WindowsSDK_ExecutablePath_x64"] = r"C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v10.0A\\bin\\NETFX 4.6 Tools\\x64\\"
	os.environ["WindowsSDK_ExecutablePath_x86"] = r"C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v10.0A\\bin\\NETFX 4.6 Tools\\"

# Opens a C++ source file and scans for lines beginning with //@
# Stops scanning at the first line that begins with '#'
# Returns the text on those lines as command line options that can be passed to cl.exe
def ExtractSwitches(filepath:str):
	args = []
	with open(filepath, encoding="utf-8") as f:
		for line in f:
			if len(line) == 0: continue;
			if line[0] == '#': break;
			if line.startswith("//@"):
				args += shlex.split(line[3:])
	return args;

# Compile the given C++ file
# The source file can specify additional compiler switches by having special
# comments (//@) before the first preprocessor command
# Returns the name of the executable file if generated
def Compile(
	filepath:str,
	_32bit     = True,
	create_exe = True,
	debug      = True,
	outdir     = "",
	sw         = DefaultSwitches,
	includes   = DefaultIncludes,
	defines    = DefaultDefines,
	):

	# Get the compiler and linker paths
	if _32bit:
		cl   = UserVars.vs_dir + "\\VC\\bin\\cl.exe"
		link = UserVars.vs_dir + "\\VC\\bin\\link.exe"
	else:
		cl   = UserVars.vs_dir + "\\VC\\bin\\amd64\\cl.exe"
		link = UserVars.vs_dir + "\\VC\\bin\\amd64\\link.exe"
	Tools.AssertPathsExist([cl, link])

	# Pull the filepath apart
	dir, file = os.path.split(filepath)
	fname, extn = os.path.splitext(file)

	# Default the output directory to the same path as the input file
	outdir = outdir if len(outdir) != 0 else dir
	outdir = os.path.normpath(outdir)
	if not os.path.exists(outdir): os.mkdir(outdir)
	os.chdir(outdir)

	# Generate the command line switches
	args = []
	args += sw + ExtractSwitches(src_file)
	args += [] if create_exe else ["/c"]
	args += ["/I"+inc+"" for inc in includes]
	args += ["/D"+deph for deph in defines]
	args += ["/D_DEBUG"] if debug else []
	args += ["/Fo" + outdir + "\\"]
	args += ["/Fe" + outdir + "\\"]
	args = list(set(args))
	args.sort()
	print(args)
	
	# Set up the VC environment variables
	SetupVCEnvironment(_32bit)

	# Compile 
	Tools.Exec([cl] + args + [filepath])

	# If the /c switch is given, then no exe is produced
	if "/c" in args:
		return ""
	
	# Return the name of the executable
	exe = outdir + "\\" + fname + ".exe"
	return exe

# Compile a C++ source file using standard pr includes, compiler switches
def CompilePR(
	filepath:str,
	_32bit     = True,
	create_exe = True,
	debug      = True,
	outdir     = "",
	sw         = PrSwitches,
	includes   = PrIncludes,
	defines    = PrDefines):
	return Compile(filepath, _32bit, create_exe, debug, outdir, sw, includes, defines)

# Compile and execute
if __name__ == "__main__":
	try:
		Tools.AssertVersion(1)
		Tools.AssertPathsExist([UserVars.root])
		#print(sys.argv)

		# Get the source file to build
		src_file = sys.argv[1] if len(sys.argv) > 1 else input("Source File: ")
		out_dir  = sys.argv[2] if len(sys.argv) > 2 else ""

		# Compile the executable
		exepath = CompilePR(
			filepath   = src_file,
			_32bit     = True,
			create_exe = True,
			debug      = True,
			outdir     = out_dir)

		# Run the executable
		if len(exepath) != 0:
			print("Executing: " + exepath)
			proc = Tools.Spawn([exepath])
			proc.wait()

	except Exception as ex:
		Tools.OnException(ex)
