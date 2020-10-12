# Rylogic user specific variables
# Make a copy of this file in the same directory and call it 'UserVars.py'
# Then update the values to those appropriate for your PC
# For any paths that don't apply, set to 'None'
import os

# Join the given arguments into a normalised path, check that it exists
def Path(*args, check_exists:bool = True, normalise:bool = True):
	if not args or None in args: return None
	path = os.path.join(*args)
	if normalise:
		path = path.replace('"','')
		path = path if os.path.isabs(path) else os.path.abspath(path)
	if check_exists and not os.path.exists(path):
		raise FileNotFoundError(f"Path {path} does not exist")
	return path

# Version History:
#  1 - initial version
version = 1

# A unique name that identifies you. Used to add user specific behaviour to scripts
# e.g. if user == 'Fred': do_fred_specific_thing()
user = "<USER_NAME>"

# Location of the root for the code library
root = Path("<REPO_ROOT_DIRECTORY>")

# Location for temporary files
dumpdir = Path("<DUMP_DIRECTORY>")

# The full path to the windows sdk and version
# e.g. winsdkvers = "10.0.18362.0"
# e.g. winsdk = Path("C:\\Program Files (x86)\\Windows Kits\\10")
winsdkvers = "<WINDOWS_SDK_VERSION>"
winsdk = Path("<WINDOWS_SDK_DIRECTORY>")

# The root of the .NET framework directory
# e.g. framework_vers = "v4.0.30319"
# e.g. framework_dir = Path("C:\\Windows\\Microsoft.NET\\Framework")
framework_vers = "<FRAMEWORK_VERSION>"
framework_dir = Path("<FRAMEWORK_DIRECTORY>", framework_vers)

# The dotnet compiler
# e.g. dotnet = Path("C:\\Program Files\\dotnet\\dotnet.exe")
dotnet = Path("<DOTNET_PATH>")

# Visual Studio install path and version
# e.g. vs_dir = Path("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community")
# vs_vers = "16.0"
# vc_vers = "14.21.27702"
vs_vers = "<VS_VERS>"
vc_vers = "<VC_VERS>"
vs_dir = Path("<VS_DIRECTORY>")
vs_devenv = Path(vs_dir, "Common7", "IDE", "devenv.exe")
vs_envvars = Path(vs_dir, "Common7", "Tools", "VsDevCmd.bat")
vs_compiler32 = Path(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x86", "cl.exe")
vs_compiler64 = Path(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x64", "cl.exe")
vs_linker32 = Path(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x86", "link.exe")
vs_linker64 = Path(vs_dir, "VC", "Tools", "MSVC", vc_vers, "bin", "HostX64", "x64", "link.exe")

# MSBuild path. Used by build scripts
# e.g. msbuild = Path("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\MSBuild\\15.0\\Bin\\MSBuild.exe")
msbuild = Path(vs_dir, "MSBuild", "Current", "Bin", "MSBuild.exe")

# The build system version. VS2013 == v120, VS2012 = v110, etc
platform_toolset = "v142"

# The full path to the android SDK
# e.g. android_sdkdir = Path("C:\\android\\android-sdk")
android_sdkdir = Path("<ANDROID_SDK_DIRECTORY>")
adb = Path(android_sdkdir, "platform-tools", "adb.exe")

# The full path the the java sdk
# e.g.java_sdkdir = Path("C:\\Program Files\\Java\\jdk1.8.0_20")
java_sdkdir = Path("<JAVA_SDK_DIRECTORY>")

# Text editor path
# Note: scripts expect notepad++, so they probably won't work if you use a different tool
# e.g. textedit = Path("C:\\Program Files\\Notepad++\\notepad++.exe")
textedit = Path("<TEXTEDITOR_PATH>")

# Merge tool path
# Note: scripts expect araxis merge, so they probably won't work if you use a different tool
# e.g. mergetool = Path("C:\\Program Files\\Araxis\\Araxis Merge\\Merge.exe")
mergetool = Path("<MERGETOOL_PATH>")

# Assembly sign tool
signtool = Path(winsdk, "bin", winsdkvers, "x64", "signtool.exe")

# VSIX signing tool
#vsixsigntool = Path("C:\\Program Files\\PackageManagement\\NuGet\\Packages\\Microsoft.VSSDK.VsixSignTool.16.2.29116.78\\tools\\vssdk\\vsixsigntool.exe")

# Code signing cert
code_sign_cert_pfx = Path("<CODE_SIGN_CERT_PATH>")
code_sign_cert_pw = None # Leave as none, set once per script run

# Web site root
wwwroot = Path("<WWW_ROOT_DIRECTORY>")

######### Derived Paths ############

# Text templating
ttbuild = Path(vs_dir, "Common7\\IDE\\TextTransform.exe")

# Power shell
pwsh = Path("C:\\Program Files\\PowerShell\\7\\pwsh.exe")
#powershell64 = Path("C:\\Windows", "System32", "WindowsPowerShell", "v1.0", "powershell.exe")
#powershell32 = Path("C:\\Windows", "SysWOW64", "WindowsPowerShell", "v1.0", "powershell.exe")

# Nuget package manager and API Key for publishing nuget packages (regenerated every 6months)
nuget = Path(root, "tools", "nuget", "nuget.exe")
nuget_api_key = "<NUGET_API_KEY>"

# Google Protobuf compiler
protoc = Path(root, "tools", "Grpc", "x64", "protoc.exe")
grpc_csharp_plugin = Path(root, "tools", "Grpc", "x64", "grpc_csharp_plugin.exe")

# WIX tools
wix_candle = Path(root, "tools", "WiX", "candle.exe")
wix_light = Path(root, "tools", "WiX", "light.exe")
wix_heat = Path(root, "tools", "WiX", "heat.exe")

# Errout
errout = Path(root, "tools", "Errout", "x64", "errout.exe")

# Rylogic code tools
cex = Path(root, "bin\\Cex\\cex.exe", check_exists=False)
p3d = Path(root, "bin\\P3d\\p3d.exe", check_exists=False)
csex = Path(root, "bin\\Csex\\Csex.exe", check_exists=False)
elevate = Path(root, "bin\\Elevate\\elevate.exe", check_exists=False)
