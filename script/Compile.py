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

# Set environment variables needed for cl.exe to work
def SetupVCEnvironment():
	os.environ["DevEnvDir"         ] = UserVars.vs_dir + "\\Common7\\IDE\\"
	os.environ["ExtensionSdkDir"   ] = UserVars.winsdk + "\\Extension SDKs"
	os.environ["Framework40Version"] = "v4.0"
	os.environ["FrameworkDir"      ] = UserVars.dotnetdir + "\\"
	os.environ["FrameworkDIR32"    ] = UserVars.dotnetdir + "\\"
	os.environ["FrameworkVersion"  ] = "v4.0.30319"
	os.environ["FrameworkVersion32"] = "v4.0.30319"
	os.environ["INCLUDE"] = (
		UserVars.vs_dir + "\\VC\\INCLUDE;" +
		UserVars.vs_dir + "\\VC\\ATLMFC\\INCLUDE;" +
		UserVars.winsdk + "\\include\\" + UserVars.winkitvers + "\\ucrt;" +
		UserVars.winsdk + "\\include\\" + UserVars.winkitvers + "\\shared;" +
		UserVars.winsdk + "\\include\\" + UserVars.winkitvers + "\\um;" +
		UserVars.winsdk + "\\include\\" + UserVars.winkitvers + "\\winrt;" +
		UserVars.winkit + "\\NETFXSDK\\4.6\\include\\um;" +
		"")
	os.environ["LIB"] = (
		UserVars.vs_dir + "\\VC\\LIB;" +
		UserVars.vs_dir + "\\VC\\ATLMFC\\LIB;" +
		UserVars.winsdk + "\\lib\\" + UserVars.winkitvers + "\\ucrt\\x86;" +
		UserVars.winsdk + "\\lib\\" + UserVars.winkitvers + "\\um\\x86;" +
		UserVars.winkit + "\\NETFXSDK\\4.6\\lib\\um\\x86;" +
		"")
	os.environ["LIBPATH"] = (
		UserVars.dotnet + ";" +
		UserVars.vs_dir + "\\VC\\LIB;" +
		UserVars.vs_dir + "\\VC\\ATLMFC\\LIB;" +
		UserVars.winsdk + "\\UnionMetadata;" +
		UserVars.winsdk + "\\References;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.UniversalApiContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.FoundationContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\Windows.Networking.Connectivity.WwanContract\\1.0.0.0;" +
		UserVars.winsdk + "\\ExtensionSDKs\\Microsoft.VCLibs\\14.0\\References\\CommonConfiguration\\neutral;" +
		"")
	os.environ["NETFXSDKDir"]        = UserVars.winkit + "\\NETFXSDK\\4.6\\"
	#Path                            =C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow;C:\Program Files (x86)\Microsoft SDKs\TypeScript\1.5;C:\Program Files (x86)\MSBuild\14.0\bin;C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\;C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\BIN;C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools;C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319;C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\VCPackages;C:\Program Files (x86)\HTML Help Workshop;C:\Program Files (x86)\Microsoft Visual Studio 14.0\Team Tools\Performance Tools;C:\Program Files (x86)\Windows Kits\10\bin\x86;C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\;D:\Program Files\PlasticSCM5\client;D:\Program Files\PlasticSCM5\server;C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\System32\WindowsPowerShell\v1.0\;C:\Program Files (x86)\NVIDIA Corporation\PhysX\Common;D:\Program Files (x86)\Skype\Phone\;D:\Program Files\TortoiseHg\;C:\Program Files\Microsoft SQL Server\110\Tools\Binn\;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;D:\Program Files\010 Editor;D:\Program Files\TortoiseSVN\bin;C:\WINDOWS\system32\config\systemprofile\.dnx\bin;C:\Program Files\Microsoft DNX\Dnvm\;C:\Program Files (x86)\Windows Kits\8.1\Windows Performance Toolkit\;C:\Program Files\Intel\WiFi\bin\;C:\Program Files\Common Files\Intel\WirelessCommon\;C:\Program Files (x86)\Microsoft SDKs\TypeScript\1.0\;C:\Users\Paul\.dnx\bin;C:\Program Files\Intel\WiFi\bin\;C:\Program Files\Common Files\Intel\WirelessCommon\
	os.environ["UCRTVersion"        ] = UserVars.winkitvers
	os.environ["UniversalCRTSdkDir" ] = UserVars.winsdk + "\\"
	os.environ["VCINSTALLDIR"       ] = UserVars.vs_dir + "\\VC\\"
	os.environ["VisualStudioVersion"] = UserVars.vs_vers
	os.environ["VSINSTALLDIR"       ] = UserVars.vs_dir + "\\"
	os.environ["WindowsLibPath"] = (
		UserVars.winsdk + "\\UnionMetadata;" +
		UserVars.winsdk + "\\References;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.UniversalApiContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Foundation.FoundationContract\\1.0.0.0;" +
		UserVars.winsdk + "\\References\\Windows.Networking.Connectivity.WwanContract\\1.0.0.0;" +
		"")
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

# Compile a C++ source file
# 'filepath' is the source file to compile
# 'outdir' is an optional output directory to put all build products
# 'sw' are compiler switches
# 'includes' are include paths
# 'defines' are macro defines
def CompileCPP(filepath:str, outdir = "", sw = [], includes = [], defines = []):

	# Get the compiler and linker paths
	cl = UserVars.vs_dir + "\\VC\\bin\\cl.exe"
	link = UserVars.vs_dir + "\\VC\\bin\\link.exe"
	Tools.AssertPathsExist([cl, link])

	# Pull the filepath apart
	dir, file = os.path.split(filepath)
	fname, extn = os.path.splitext(file)

	# Default the output directory to the same path as the input file
	outdir = outdir if len(outdir) != 0 else dir
	outdir = os.path.normpath(outdir)
	if not os.path.exists(outdir): os.mkdir(outdir)

	# Generate the command line switches
	args = sw
	args += ["/I\""+inc+"\"" for inc in includes]
	args += ["/D"+deph for deph in defines]
	args += ["/Fo\""+outdir+"\\\""]
	#args += ["/Fd\""+dir+"\\"+fname+".pdb\""]
	#args += ["/Fa\""+outdir+"\\\""]
	#args += ["/Fp\""+outdir+"\\"+fname+".pch"]

	# Compile 
	Tools.Exec([cl] + args + [filepath])

# Compile a C++ source file using standard pr includes, compiler switches
def CompileCPP_PR(filepath:str, outdir:str):
	sw = [
		"/nologo"             ,# no MS logo
		"/Gd"                 ,# __cdecl default calling convention
		"/GF"                 ,# enable readonly string pooling
		"/GS"                 ,# enable security checks
		"/Gy"                 ,# separate functions for linker
		"/Gm-"                ,# minimal rebuilds: false
		"/fp:precise"         ,# precise floating point
		"/EHsc"               ,# Exception handler support
		"/W4"                 ,# warning level 4
		"/WX-"                ,# warnings as errors: false
		"/wd\"4351\""         ,# disable warning
		"/wd\"4355\""         ,# disable warning
		"/Zi"                 ,# debugging info
		"/Zc:wchar_t"         ,# language conformance: wchar_t is native type
		"/Zc:forScope"        ,# language conformance: for loop scope
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
		];
	includes = [
		UserVars.root+"\\projects",
		UserVars.root+"\\include",
		UserVars.root+"\\sdk",
		UserVars.root+"\\sdk\\wtl\\v9.0\\include",
		UserVars.root+"\\sdk\\sqlite\\include",
		UserVars.root+"\\sdk\\lua\\lua\\src",
		UserVars.root+"\\sdk\\lua",
		];
	defines = [
		"PR_UNITTESTS=1",
		"_WIN32_WINNT=0x0600",
		"PR_MATHS_USE_DIRECTMATH=1",
		"_DEBUG",
		"_CRT_SECURE_NO_WARNINGS",
		"_SCL_SECURE_NO_WARNINGS",
		"NOMINMAX",
		"WIN32_LEAN_AND_MEAN"
		];
	CompileCPP(filepath, outdir, sw, includes, defines)

# Compile the given C++ file into an executable
# The source file can specify additional compiler switches by having special
# comments before the first preprocessor command
# Returns the name of the executable file
def CompileToExe(filepath:str, outdir="",
	sw       = ["/nologo","/EHsc"],
	includes = [UserVars.root+"\\include", UserVars.root+"\\projects"],
	defines  = []):

	# Get the compiler and linker paths
	cl = UserVars.vs_dir + "\\VC\\bin\\cl.exe"
	link = UserVars.vs_dir + "\\VC\\bin\\link.exe"
	Tools.AssertPathsExist([cl, link])

	# Pull the filepath apart
	dir, file = os.path.split(filepath)
	fname, extn = os.path.splitext(file)

	# Default the output directory to the same path as the input file
	outdir = outdir if len(outdir) != 0 else dir
	outdir = os.path.normpath(outdir)
	if not os.path.exists(outdir): os.mkdir(outdir)

	# Generate the command line switches
	args = []
	args += sw
	args += ["/I"+inc+"" for inc in includes]
	args += ["/D"+deph for deph in defines]
	args += ExtractSwitches(src_file)
	args += ["/Fo" + outdir + "\\"]
	args += ["/Fe" + outdir + "\\"]

	# Compile 
	Tools.Exec([cl] + args + [filepath])

	# If the /c switch is given, then no exe is produced
	if "/c" in args:
		return ""
	
	# Return the name of the executable
	exe = outdir + "\\" + fname + ".exe"
	return exe

# Compile and execute
if __name__ == "__main__":
	try:
		Tools.AssertVersion(1)
		Tools.AssertPathsExist([UserVars.root])
		#print(sys.argv)

		# Set up the VC environment variables
		SetupVCEnvironment()

		# Get the source file to build
		src_file = sys.argv[1] if len(sys.argv) > 1 else input("Source File: ")
		out_dir  = sys.argv[2] if len(sys.argv) > 2 else ""

		# Compile the executable
		exepath = CompileToExe(src_file, out_dir)

		# Run the executable
		if len(exepath) != 0:
			print("Executing: " + exepath)
			proc = Tools.Spawn([exepath])
			proc.wait()

	except Exception as ex:
		Tools.OnException(ex)
