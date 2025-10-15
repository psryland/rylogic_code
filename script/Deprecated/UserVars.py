# Rylogic user specific variables
# Make a copy of this file in the same directory and call it 'UserVars.py'
# Then update the values to those appropriate for your PC
# For any paths that don't apply, set to 'None'
import os, importlib

# Join the given arguments into a normalised path, check that it exists
def Path(*args, check_exists:bool = True, normalise:bool = True) ->str:
	if not args or None in args:
		return ""

	path = os.path.join(*args)
	
	# Normalise the path
	if normalise:
		path = path.replace('"','').replace('/',"\\")
		path = os.path.abspath(path)

	# Paths with wildcards don't exist
	check_exists &= any([x in path for x in ['*','?']]) == False
	if check_exists and not os.path.exists(path):
		raise FileNotFoundError(f"Path {path} does not exist")

	return path

# Import a module from the given filepath
def Import(name:str, path:str):
	spec = importlib.util.spec_from_file_location(name, path)
	mod = importlib.util.module_from_spec(spec)
	spec.loader.exec_module(mod)
	return mod

# Version History:
#  1 - initial version
version = 1

# Location of the root for the code library
root = Path(os.path.dirname(__file__), "..")

# Location for temporary files
dumpdir = Path(root, "dump", check_exists=False)
os.makedirs(dumpdir, exist_ok=True)

# The full path to the windows sdk and version
# e.g. winsdkvers = "10.0.18362.0"
# e.g. winsdk = Path("C:\\Program Files (x86)\\Windows Kits\\10")
winsdkvers = "10.0.26100.0"
winsdk = Path("C:\\Program Files (x86)\\Windows Kits\\10")

# The root of the .NET framework directory
# e.g. framework_dir = Path("C:\\Windows\\Microsoft.NET\\Framework")
framework_dir = Path("C:\\Windows\\Microsoft.NET\\Framework", "v4.0.30319")

# The dotnet compiler
# e.g. dotnet = Path("C:\\Program Files\\dotnet\\dotnet.exe")
dotnet = Path("C:\\Program Files\\dotnet\\dotnet.exe")

# Visual Studio install path and version
# e.g. vs_dir = Path("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community")
# vs_vers = "16.0"
# vc_vers = "14.21.27702"
vs_vers = "17.0"
vc_vers = "14.41.34120"
vs_dir = Path("C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise")
vs_devenv = Path(vs_dir, "Common7\\IDE\\devenv.exe")
vs_envvars = Path(vs_dir, "Common7\\Tools\\VsDevCmd.bat")

# MSBuild path. Used by build scripts
# e.g. msbuild = Path("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\MSBuild\\15.0\\Bin\\MSBuild.exe")
msbuild = Path(vs_dir, "MSBuild\\Current\\Bin\\MSBuild.exe")

# The build system version. VS2013 == v120, VS2012 = v110, etc
platform_toolset = "v143"

# Power shell
# e.g. pwsh = Path("C:\\Program Files\\PowerShell\\7\\pwsh.exe")
pwsh = Path("C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe")

# git path
# e.g. git = Path("C:\\Program Files\\Git\\bin\\git.exe")
git = Path("C:\\Program Files\\Git\\bin\\git.exe")

# Assembly sign tool
signtool = Path(winsdk, "bin", winsdkvers, "x64\\signtool.exe")
#vsixsigntool = Path("C:\\Program Files\\PackageManagement\\NuGet\\Packages\\Microsoft.VSSDK.VsixSignTool.16.2.29116.78\\tools\\vssdk\\vsixsigntool.exe")

# Code signing cert
# Get 'thumbprint' from the cert manager. Find your code signing cert (Rylogic Limited, Sectigo RSA Code Signing CA), and open it. Under 'details' find 'Thumbprint'.
code_sign_cert_pfx = Path(root, "projects\\rylogic-code-signing-certificate.pfx", check_exists=False)
code_sign_cert_thumbprint = "28baca87f692ca5b46e8f98091c843d1b886dcda"
code_sign_cert_pw = None # Leave as none, set once per script run

# Nuget package manager and API Key for publishing nuget packages (regenerated every 6months)
nuget = Path(root, "tools\\nuget\\nuget.exe")
nuget_api_key = None
