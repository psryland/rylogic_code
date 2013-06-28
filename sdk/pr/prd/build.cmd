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

set projectname=prd
set platform=x64
set config=debug
set extn=lib

set targetdir=Q:\obj2012\%projectname%\%platform%\%config%
set targetname=%projectname%.%extn%
set targetpath=%targetdir%\%targetname%

if not exist %targetdir% mkdir %targetdir%

set src=^
 db\sqlite.d^
 maths\maths.d^
 maths\rand.d^
 script\script.d^
 storage\settings.d^
 view3d\view3d.d
 
set includes=^
 -IQ:\sdk\pr^
 -IQ:\sdk\dgui^
 -IQ:\dlang\dmd2\src\phobos^
 -IQ:\dlang\dmd2\src\druntime\import

set libs=^
 -LQ:\sdk\pr\lib\prd.%platform%.%config%.lib^
 -LQ:\sdk\pr\lib\view3d.%platform%.%config%.lib^
 -LQ:\sdk\pr\lib\dgui.%platform%.%config%.lib^
 -Lole32.lib^
 -Lkernel32.lib^
 -Luser32.lib^
 -Lcomctl32.lib^
 -Lcomdlg32.lib

set deps=^
 -deps="%targetdir%\%projectname%.dep"

set map=^
 -map "%targetdir%\%projectname%.map"

set genjson=^
  -X^
  -Xf"%targetdir%\%targetname%.json"
  
echo building...
"%dmd%" -lib -m64 -g -%config% %src% %includes% %libs% %deps% %map% -od"%targetdir%" -of"%targetpath%"

echo post build..
call Q:\tools\_publish_lib.cmd "%targetpath%" %platform% %config%