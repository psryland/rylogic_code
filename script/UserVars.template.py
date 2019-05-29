# Rylogic user specific variables
# Make a copy of this file in the same directory and call it 'UserVars.py'
# Then update the values to those appropriate for your PC
# For any paths that don't apply, set to 'None'
import os
def CheckPath(path:str, optional:bool = False):
	if path and not os.path.exists(path):
		if optional: print(f"Path {path} does not exist")
		else: raise FileNotFoundError(f"Path {path} does not exist")
	return path
def JoinPath(*args):
	return None if None in args else os.path.join(*args)

# Version History:
#  1 - initial version
version = 1

# A unique name that identifies you. Used to add user specific behaviour to scripts
# e.g. if user == 'Fred': do_fred_specific_thing()
user = "<USER_NAME>"

# Location of the root for the code library
root = CheckPath("<REPO_ROOT_DIRECTORY>")

# Location for temporary files
dumpdir = CheckPath("<DUMP_DIRECTORY>")

# The full path to the windows sdk and version
# e.g. winsdkvers = "10.0.18362.0"
# e.g. winsdk = CheckPath("C:\\Program Files (x86)\\Windows Kits\\10")
winsdkvers = "<WINDOWS_SDK_VERSION>"
winsdk = CheckPath("<WINDOWS_SDK_DIRECTORY>")
winsdk_bin        = CheckPath(JoinPath(winsdk, "Bin", winsdkvers))
winsdk_lib        = CheckPath(JoinPath(winsdk, "Lib", winsdkvers))
winsdk_include    = CheckPath(JoinPath(winsdk, "Include", winsdkvers))
winsdk_references = CheckPath(JoinPath(winsdk, "References", winsdkvers))

# The root of the .NET framework directory
# e.g. framework_vers = "v4.0.30319"
# e.g. framework_dir = CheckPath("C:\\Windows\\Microsoft.NET\\Framework")
framework_vers = "<FRAMEWORK_VERSION>"
framework_dir = CheckPath(JoinPath("<FRAMEWORK_DIRECTORY>", framework_vers))

# The dotnet compiler
# e.g. dotnet = CheckPath("C:\\Program Files\\dotnet\\dotnet.exe")
dotnet = CheckPath("<DOTNET_PATH>")

# Visual Studio install path and version
# e.g. vs_dir = CheckPath("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community")
# vs_vers = "16.0"
# vc_vers = "14.21.27702"
vs_vers = "<VS_VERS>"
vc_vers = "<VC_VERS>"
vs_dir = CheckPath("<VS_DIRECTORY>")
vs_devenv = CheckPath(JoinPath(vs_dir, "Common7", "IDE", "devenv.exe"))
vs_envvars = CheckPath(JoinPath(vs_dir, "Common7", "Tools", "VsDevCmd.bat"))
vs_compiler32 = CheckPath(JoinPath(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x86", "cl.exe"))
vs_compiler64 = CheckPath(JoinPath(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x64", "cl.exe"))
vs_linker32 = CheckPath(JoinPath(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x86", "link.exe"))
vs_linker64 = CheckPath(JoinPath(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x64", "link.exe"))

# MSBuild path. Used by build scripts
# e.g. msbuild = CheckPath("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\MSBuild\\15.0\\Bin\\MSBuild.exe")
msbuild = CheckPath(JoinPath(vs_dir, "MSBuild", "Current", "Bin", "MSBuild.exe"))

# The build system version. VS2013 == v120, VS2012 = v110, etc
platform_toolset = "v142"

# The full path to the android SDK
# e.g. android_sdkdir = CheckPath("C:\\android\\android-sdk")
android_sdkdir = CheckPath("<ANDROID_SDK_DIRECTORY>")
adb = CheckPath(JoinPath(android_sdkdir, "platform-tools", "adb.exe"))

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

# Web site root
wwwroot = CheckPath("<WWW_ROOT_DIRECTORY>")

######### Derived Paths ############

# Text templating
ttbuild = CheckPath(JoinPath(vs_dir, "Common7", "IDE", "TextTransform.exe"))

# Power shell paths
powershell64 = CheckPath(JoinPath("C:\\Windows", "System32", "WindowsPowerShell", "v1.0", "powershell.exe"))
powershell32 = CheckPath(JoinPath("C:\\Windows", "SysWOW64", "WindowsPowerShell", "v1.0", "powershell.exe"))

# Nuget package manager and API Key for publishing nuget packages (regenerated every 6months)
nuget = CheckPath(JoinPath(root, "tools", "nuget", "nuget.exe"))
nuget_api_key = "<NUGET_API_KEY>"

# Google Protobuf compiler
protoc = CheckPath(JoinPath(root, "tools", "Grpc", "x64", "protoc.exe"))
grpc_csharp_plugin = CheckPath(JoinPath(root, "tools", "Grpc", "x64", "grpc_csharp_plugin.exe"))

# Zip
ziptool = CheckPath(JoinPath(root, "tools", "7za.exe"))

# WIX tools
wix_candle = CheckPath(JoinPath(root, "tools", "WiX", "candle.exe"))
wix_light = CheckPath(JoinPath(root, "tools", "WiX", "light.exe"))
wix_heat = CheckPath(JoinPath(root, "tools", "WiX", "heat.exe"))

# Errout
errout = CheckPath(JoinPath(root, "tools", "Errout", "x64", "errout.exe"))

# Rylogic code tools
cex = CheckPath(JoinPath(root, "tools", "cex", "cex.exe"))
p3d = CheckPath(JoinPath(root, "tools", "p3d", "p3d.exe"))
csex = CheckPath(JoinPath(root, "tools", "csex", "csex.exe"))
elevate = CheckPath(JoinPath(root, "tools", "elevate", "elevate.exe"))
