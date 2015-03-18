# Rylogic UserVars.py
# Copy this file to ".\UserVars.py" and modify
# variables appropriately for your system.
# Set to None if the variable does not apply to your system.

# UserVars global version number 
version = 1

# Name associated with the build
user = "Bob"

# System CPU architecture of this pc
arch = "x64"

# Location of the root for the code library (i.e. the directory containing build, include, projects, etc) (No trailing '\')
root = "X:\root_directory"

# A location for temporary files
dumpdir = r"C:\temp"

# The full path to msbuild
msbuild = r"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe"
msbuild_props     = ""#r"/p:VCTargetsPath=C:\Program Files (x86)\MSBuild\Microsoft.Cpp\v4.0\V120"

# The main rylogic code solution
rylogic_sln = root + "\\build\\Rylogic.sln"

# The version of MSbuild in use. VS2013=v120, VS2012 = v110, VS2010=v100
platform_toolset  = "v120"

# The full path to the windows sdk
winsdk = r"C:\Program Files (x86)\Windows Kits\8.1"

# The full path to the Visual Studio install
vs_dir = r"C:\Program Files (x86)\Microsoft Visual Studio 12.0"
vc_env = vs_dir + r"\VC\vcvarsall.bat"
devenv = vs_dir + r"\Common7\ide\devenv.exe"

# The full path to the android SDK
android_sdkdir = r"D:\android\android-sdk"
adb = android_sdkdir + r"\platform-tools\adb.exe"

# The full path the the java sdk
java_sdkdir = r"D:\Program Files\Java\jdk1.8.0_20"

# Standard tools
textedit  = r"C:\Program Files (x86)\Notepad++\notepad++.exe"
mergetool = r"C:\Program Files\Araxis\Araxis Merge\Merge.exe"
linqpad   = r"D:\Program Files (x86)\LinqPad4\LINQPad.exe"
ttbuild   = r"C:\Program Files (x86)\Common Files\Microsoft Shared\TextTemplating\12.0\TextTransform.exe"

# The Digital Mars D compiler install path
dmdroot = root + r"\dlang\latest\dmd2"
dmd = dmdroot + r"\windows\bin\dmd.exe"
rdmd = dmdroot + r"\windows\bin\rdmd.exe"

# Rylogic code tools
csex = root + r"\bin\csex\csex.exe"
elevate = root + r"\tools\elevate.cmd"
ziptool = root + r"\tools\7za.exe"
