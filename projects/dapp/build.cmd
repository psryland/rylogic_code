@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)

set projectname=dapp
set platform=x64
set config=debug
set extn=exe

set targetdir=Q:\obj2012\%projectname%\%platform%\%config%
set targetname=%projectname%.%extn%
set targetpath=%targetdir%\%targetname%

if not exist %targetdir% mkdir %targetdir%

set src=^
 main.d
 
set includes=^
 -IQ:\sdk\pr^
 -IQ:\sdk\dgui^
 -IQ:\dlang\dmd2\src\phobos^
 -IQ:\dlang\dmd2\src\druntime\import

set libs=^
 -LQ:\sdk\lib\prd.%platform%.%config%.lib^
 -LQ:\sdk\lib\view3d.%platform%.%config%.lib^
 -LQ:\sdk\lib\dgui.%platform%.%config%.lib^
 -Lole32.lib^
 -Lkernel32.lib^
 -Luser32.lib^
 -Lcomctl32.lib^
 -Lcomdlg32.lib
 
echo building...
"%dmd%" -m64 -%config% %src% %includes% %libs% -od%targetdir% -of%targetpath% 

echo post build...
call P:\tools\_lib_copy.cmd view3d.dll %platform% %config% %targetdir%
 
 
