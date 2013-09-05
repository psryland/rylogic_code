import sys, time, shutil, subprocess

version           = 1
user              = r"Paul"
machine           = r"Rylogic3"
qdrive            = r"Q:"
zdrive            = r"Z:"
csex              = qdrive + r"\bin\csex\csex.exe"
zip               = qdrive + r"\tools\7za.exe"
arch              = r"x64"
vs_dir            = r"D:\Program Files (x86)\Microsoft Visual Studio 11.0"
vc_env            = vs_dir + r"\VC\vcvarsall.bat"
devenv            = vs_dir + r"\Common7\ide\devenv.exe"
dxsdk             = r"D:\Program Files (x86)\Microsoft DirectX SDK (June 2010)"
fxc               = dxsdk + r"\Utilities\bin\x64\fxc.exe"
texconv           = dxsdk + r"\Utilities\bin\x64\texconv.exe"
textedit          = r"C:\Program Files (x86)\Notepad++\notepad++.exe"
mergetool         = r"D:\Program Files (x86)\Araxis\Araxis Merge\Merge.exe"
msbuild           = r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
ttbuild           = r"C:\Program Files (x86)\Common Files\Microsoft Shared\TextTemplating\10.0\TextTransform.exe"
silverlight_root  = r"C:\Program Files (x86)\Microsoft SDKs\Silverlight\v5.0"
java_sdkdir       = r"C:\Program Files\Java\jdk1.6.0_38"
android_sdkdir    = r"D:\Program Files (x86)\Android\android-sdk"
adb               = android_sdkdir + r"\platform-tools\adb.exe"
dmdroot           = r"Q:\dlang\dmd2"
dmd               = dmdroot + "\windows\bin\dmd.exe"
wwwroot           = r"Z:\www\rylogic.co.nz"


def CheckVersion(check_version):
	if check_version > version:
		OnError("RylogicEnv.py is out of date")

def Run(exe, args):
	subprocess.check_call('"'+exe+'" '+args)
	
def Copy(src, dst):
	print(src.ljust(40) + " --> " + dst)
	shutil.copy(src, dst)
	
def OnError():
	print("\n   Failed\n\n")
	input()
	sys.exit(1)

def OnSuccess():
	print("\n   Success\n\n")
	time.sleep(5)
	sys.exit(0)
