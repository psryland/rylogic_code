# Rylogic user specific variables
# Make a copy of this file in the same directory and call it 'UserVars.py'
# Then update the values to those appropriate for your PC
# For any paths that don't apply, set to 'None'
import os
def CheckPath(path:str):
	if not os.path.exists(path): raise FileNotFoundError(f"Path {path} does not exist");
	return path

# Version History:
#  1 - initial version
version = 1

# A unique name that identifies you. Used to add user specific behaviour to scripts
# e.g. if user == 'Fred': do_fred_specific_thing()
user = "<USER_NAME>"

# The name of your PC
# This is mainly used to catch accidental misuse of the wrong UserVars file
# however it can be used for identification on the network
machine = "<MACHINE_NAME>"

# Location of the root for the code library
root = CheckPath("<REPO_ROOT_DIRECTORY>")

# Location for temporary files
dumpdir = CheckPath("<DUMP_DIRECTORY>")

# The full path to the windows sdk
# e.g. winsdk = CheckPath("C:\\Program Files (x86)\\Windows Kits\\10")
winsdk = CheckPath("<WINDOWS_SDK_DIRECTORY>")

# The Windows SDK verion to use
# e.g. winsdkvers = "10.0.17763.0"
winsdkvers = "<WINDOWS_SDK_VERSION>"
winsdk_bin        = CheckPath(os.path.join(winsdk, "Bin", winsdkvers))
winsdk_lib        = CheckPath(os.path.join(winsdk, "Lib", winsdkvers))
winsdk_include    = CheckPath(os.path.join(winsdk, "Include", winsdkvers))
winsdk_references = CheckPath(os.path.join(winsdk, "References", winsdkvers))

# The root of the .NET framework directory
# e.g. dotnetdir = CheckPath("C:\\Windows\\Microsoft.NET\\Framework")
dotnetdir = CheckPath("<DOTNET_DIRECTORY>")
dotnet = CheckPath(os.path.join(dotnetdir, "v4.0.30319"))

# MSBuild path. Used by build scripts
# e.g. msbuild = CheckPath("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\MSBuild\\15.0\\Bin\\MSBuild.exe")
msbuild = CheckPath("<MSBUILD_PATH>")

# The build system version. VS2013 == v120, VS2012 = v110, etc
platform_toolset = "v141"

# Visual Studio install path
# e.g. vs_dir = CheckPath("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community")
vs_dir = CheckPath("<VS_DIRECTORY>")
msvc_dir = CheckPath(os.path.join(vs_dir, "VC", "Tools", "MSVC", "14.16.27023"))
vs_compiler32 = CheckPath(os.path.join(msvc_dir, "bin", "HostX64", "x86", "cl.exe"))
vs_compiler64 = CheckPath(os.path.join(msvc_dir, "bin", "HostX64", "x64", "cl.exe"))
vs_linker32 = CheckPath(os.path.join(msvc_dir, "bin", "HostX64", "x86", "link.exe"))
vs_linker64 = CheckPath(os.path.join(msvc_dir, "bin", "HostX64", "x64", "link.exe"))
vs_vers = "15.0"

# The full path to the android SDK
# e.g. android_sdkdir = CheckPath("C:\\android\\android-sdk")
android_sdkdir = CheckPath("<ANDROID_SDK_DIRECTORY>")
adb = CheckPath(os.path.join(android_sdkdir, "platform-tools", "adb.exe"))

# The full path the the java sdk
# e.g.java_sdkdir = CheckPath("C:\\Program Files\\Java\\jdk1.8.0_20")
java_sdkdir = CheckPath("<JAVA_SDK_DIRECTORY>")

# Text editor path
# Note: scripts expect notepad++, so they probably won't work if you use a different tool
# e.g. textedit = CheckPath("C:\\Program Files\\Notepad++\\notepad++.exe")
textedit = CheckPath("<TEXTEDITOR_PATH>")

# Merge tool path
# Note: scripts expect araxis merge, so they probably won't work if you use a different tool
# e.g. mergetool = CheckPath("C:\\Program Files\\Araxis\\Araxis Merge\\Merge.exe")
mergetool = CheckPath("<MERGETOOL_PATH>")

# API Key for publishing nuget packages
nuget_api_key = "<NUGET_API_KEY>"

# Web site root
wwwroot = CheckPath("<WWW_ROOT_DIRECTORY>")

######### Derived Paths ############

# Text templating
ttbuild = CheckPath(os.path.join(vs_dir, "Common7", "IDE", "TextTransform.exe"))

# Power shell paths
powershell64 = CheckPath(os.path.join("C:\\Windows", "System32", "WindowsPowerShell", "v1.0", "powershell.exe"))
powershell32 = CheckPath(os.path.join("C:\\Windows", "SysWOW64", "WindowsPowerShell", "v1.0", "powershell.exe"))

# Nuget package manager
nuget = CheckPath(os.path.join(root, "tools", "nuget", "nuget.exe"))

# Google Protobuf compiler
protoc = CheckPath(os.path.join(root, "tools", "Grpc", "x64", "protoc.exe"))
grpc_csharp_plugin = CheckPath(os.path.join(root, "tools", "Grpc", "x64", "grpc_csharp_plugin.exe"))

# Zip
ziptool = CheckPath(os.path.join(root, "tools", "7za.exe"))

# WIX tools
wix_candle = CheckPath(os.path.join(root, "tools", "WiX", "candle.exe"))
wix_light = CheckPath(os.path.join(root, "tools", "WiX", "light.exe"))
wix_heat = CheckPath(os.path.join(root, "tools", "WiX", "heat.exe"))

# Rylogic code tools
csex = CheckPath(os.path.join(root, "tools", "csex", "csex.exe"))
elevate = CheckPath(os.path.join(root, "tools", "elevate.exe"))

# The main rylogic code solution
rylogic_sln = CheckPath(os.path.join(root, "build", "Rylogic.sln"))
