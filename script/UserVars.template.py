# Rylogic UserVars.py
# Make a copy of this file in the same directory and call it 'UserVars.py'
# Then update the values to those appropriate for your PC
# For any paths that don't apply, set to 'None'

# UserVars global version number 
version = 1

# A unique name that identifies you. Used to add user specific behaviour to scripts
# e.g. if user == 'Fred': do_fred_specific_thing()
user = "Paul"

# The name of your PC
# This is mainly used to catch accidental misuse of the wrong UserVars file
# however it can be used for identification on the network
machine = "Rylogic"

# The CPU architecture. Used to execute tools appropriate for your system
arch = "x64"

# Location of the root for the code library (i.e. the directory containing build, include, projects, etc) (No trailing '\')
root = "P:\\pr"

# The full path to the windows sdk
winsdkvers = "10.0.16299.0"
winsdk            = "C:\\Program Files (x86)\\Windows Kits\\10"
winsdk_bin        = winsdk + "\\Bin\\"        + winsdkvers
winsdk_lib        = winsdk + "\\Lib\\"        + winsdkvers
winsdk_include    = winsdk + "\\Include\\"    + winsdkvers
winsdk_references = winsdk + "\\References\\" + winsdkvers

# The root of the .NET framework directory
dotnetdir = "C:\\Windows\\Microsoft.NET\\Framework"
dotnet = dotnetdir + "\\v4.0.30319"

# MSBuild path. Used by build scripts
msbuild_dir = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\MSBuild\\15.0"
msbuild = msbuild_dir + "\\Bin\\MSBuild.exe"

# The build system version. VS2013 == v120, VS2012 = v110, etc
platform_toolset = "v141"

# The full path to the Visual Studio install
vs_dir = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community"
vs_platform_dir = vs_dir + "\\VC\\Tools\\MSVC\\14.12.25827"
vs_compiler32 = vs_platform_dir + "\\bin\\HostX64\\x86\\cl.exe"
vs_compiler64 = vs_platform_dir + "\\bin\\HostX64\\x64\\cl.exe"
vs_linker32   = vs_platform_dir + "\\bin\\HostX64\\x86\\link.exe"
vs_linker64   = vs_platform_dir + "\\bin\\HostX64\\x64\\link.exe"
vs_vers = "15.0"

# Power shell paths
powershell64 = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe"
powershell32 = "C:\\Windows\\SysWOW64\\WindowsPowerShell\\v1.0\\powershell.exe"

# Nuget package manager
nuget = root + "\\tools\\nuget\\nuget.exe"

# Text editor path
# Note: scripts expect notepad++, so they probably won't work if you use a different tool
textedit = "C:\\Program Files\\Notepad++\\notepad++.exe"

# Merge tool path
# Note: scripts expect araxis merge, so they probably won't work if you use a different tool
mergetool = "D:\\Program Files\\Araxis\\Araxis Merge\\Merge.exe"

# Linqpad
linqpad = "C:\\Program Files\\LinqPad5\\LINQPad.exe"

# Text templating
ttbuild = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\Common7\\IDE\\TextTransform.exe"

# The full path to the android SDK
android_sdkdir = None#"D:\\android\\android-sdk"
adb = None#android_sdkdir + "\\platform-tools\\adb.exe"

# The full path the the java sdk
java_sdkdir = None#"D:\\Program Files\\Java\\jdk1.8.0_20"

# The Digital Mars D compiler install path
#dmdroot = root + "\\dlang\\latest\\dmd2"
#dmd = dmdroot + "\\windows\\bin\\dmd.exe"
#rdmd = dmdroot + "\\windows\\bin\\rdmd.exe"

# Rylogic code tools
csex = root + "\\bin\\csex\\csex.exe"
elevate = root + "\\bin\\elevate.exe"
ziptool = root + "\\tools\\7za.exe"
wix_candle = root + "\\tools\\WiX\\candle.exe"
wix_light = root + "\\tools\\WiX\\light.exe"
wix_heat = root + "\\tools\\WiX\\heat.exe"

# Web site root
wwwroot = "Z:\\www\\rylogic.co.nz"

# Location for temporary files
dumpdir = "P:\\dump"

# The main rylogic code solution
rylogic_sln = root + "\\build\\Rylogic.sln"
