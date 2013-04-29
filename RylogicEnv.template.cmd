rem
rem Rylogic Limited user specific variables
rem  Remember to create a environment variable called 'RylogicEnv' 
rem  that is the path to this file so that other batch files can simply
rem  'call %RylogicEnv%' to set up the variables below.
rem

rem Environment Variables version number
rem Increment if you add a new user variable and require users to have that variable

rem NOTE: if you variables contain '(' or ')' characters in them, remember to escape them.
rem  E.g. somepath=C:\Program Files ^(x86^)\SomePath

rem Boilerplate:
::Load Rylogic environment variables and check version
::if [%RylogicEnv%]==[] (
:: 	echo ERROR: The 'RylogicEnv' environment variable is not set.
::	goto :eof
::)
::call %RylogicEnv%
::if %RylogicEnvVersion% lss 4 (
::	echo ERROR: '%RylogicEnv%' is out of date. Please update.
::	goto :eof
::)

set RylogicEnvVersion=4

set user=Paul
set machine=Rylogic3
set qdrive=Q:
set zdrive=Z:
set cp=xcopy
set zip=Q:\tools\7za.exe
set arch=x64
set vs_dir=D:\Program Files^(x86^)\Microsoft Visual Studio 11.0
set vc_env=D:\Program Files ^(x86^)\Microsoft Visual Studio 11.0\VC\vcvarsall.bat
set fxc=%DXSDK_DIR%Utilities\bin\x64\fxc.exe
set texconv=%DXSDK_DIR%Utilities\bin\x64\texconv.exe
set textedit=C:\Program Files ^(x86^)\Notepad++\notepad++.exe
set mergetool=D:\Program Files ^(x86^)\Araxis\Araxis Merge\Merge.exe
set msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
set ttbuild=C:\Program Files ^(x86^)\Common Files\Microsoft Shared\TextTemplating\10.0\TextTransform.exe
set silverlight_root=C:\Program Files ^(x86^)\Microsoft SDKs\Silverlight\v5.0
set java_sdkdir=C:\Program Files\Java\jdk1.6.0_38
set android_sdkdir=D:\Program Files ^(x86^)\Android\android-sdk
set adb=D:\Program Files ^(x86^)\Android\android-sdk\platform-tools\adb.exe
