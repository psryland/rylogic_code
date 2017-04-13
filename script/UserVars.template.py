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
root = "P:"

# MSBuild path. Used by build scripts
msbuild_dir = r"C:\Program Files (x86)\MSBuild\14.0"
msbuild = msbuild_dir + r"\Bin\MSBuild.exe"

# MSBuild root build script path.
msbuild_props = "" #r"/p:VCTargetsPath=C:\Program Files (x86)\MSBuild\Microsoft.Cpp\v4.0\V120"

# The build system version. VS2013 == v120, VS2012 = v110, etc
platform_toolset = "v140"

# The full path to the windows sdk
winsdk =r"C:\Program Files (x86)\Windows Kits\8.1"
winsdkvers = "8.1"

# The root of the .NET framework directory
dotnetdir = r"C:\Windows\Microsoft.NET\Framework"
dotnet = dotnetdir + "\\v4.0.30319"

# The full path to the Visual Studio install
vs_dir = r"C:\Program Files (x86)\Microsoft Visual Studio 14.0"
vs_vers = "14.0"

# The full path to the android SDK
android_sdkdir = r"D:\android\android-sdk"
adb = android_sdkdir + r"\platform-tools\adb.exe"

# The full path the the java sdk
java_sdkdir = r"D:\Program Files\Java\jdk1.8.0_20"

# Text editor path
# Note: scripts expect notepad++, so they probably won't work if you use a different tool
textedit = r"C:\Program Files (x86)\Notepad++\notepad++.exe"

# Merge tool path
# Note: scripts expect araxis merge, so they probably won't work if you use a different tool
mergetool = r"D:\Program Files\Araxis\Araxis Merge\Merge.exe"

# Linqpad
linqpad = r"D:\Program Files (x86)\LinqPad4\LINQPad.exe"

# Text templating
ttbuild = r"C:\Program Files (x86)\Common Files\Microsoft Shared\TextTemplating\12.0\TextTransform.exe"

# The Digital Mars D compiler install path
dmdroot = root + r"\dlang\latest\dmd2"
dmd = dmdroot + r"\windows\bin\dmd.exe"
rdmd = dmdroot + r"\windows\bin\rdmd.exe"

# Rylogic code tools
csex = root + r"\bin\csex\csex.exe"
elevate = root + r"\bin\elevate.exe"
ziptool = root + r"\tools\7za.exe"
wix_candle = root + r"\tools\WiX\candle.exe"
wix_light = root + r"\tools\WiX\light.exe"
wix_heat = root + r"\tools\WiX\heat.exe"

# Location for temporary files
dumpdir = r"P:\dump"

# The main rylogic code solution
rylogic_sln = root + "\\build\\Rylogic.sln"
