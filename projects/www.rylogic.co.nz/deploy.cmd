@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
 	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 2 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)


::Read the command line parameter
set param=%1
set srcdir=Q:\projects\www.rylogic.co.nz\site
set dstdir=%zdrive%\WWW\dev.rylogic.co.nz
if not exist "%dstdir%" mkdir "%dstdir%"

if [%param%]==[clean] (
	echo Cleaning %srcdir%...
	del "%dstdir%\*.*" /S /Q
	goto :end
)

set newer=/D
if [%param%]==[all] (
	set newer=
)

if [%param%]==[] (
	set /p clean=Clean destination directory first ^(y/n^)?
	if /I [!clean!]==[y] del "%dstdir%\*.*" /S /Q
)

echo Deploying %srcdir% -^> %dstdir%...

::Copy web site files
xcopy "%srcdir%\*.shtml" "%dstdir%\" /Y /F /E %newer%
xcopy "%srcdir%\*.png"   "%dstdir%\" /Y /F /E %newer%
xcopy "%srcdir%\*.jpg"   "%dstdir%\" /Y /F /E %newer%
xcopy "%srcdir%\*.css"   "%dstdir%\" /Y /F /E %newer%
xcopy "%srcdir%\*.xml"   "%dstdir%\" /Y /F /E %newer%

::Copy site data
xcopy "q:\bin\linedrawer.x86.zip"  "%dstdir%\data\" /F /Y %newer%
xcopy "q:\bin\linedrawer.x64.zip"  "%dstdir%\data\" /F /Y %newer%
xcopy "q:\bin\imager.x86.zip"      "%dstdir%\data\" /F /Y %newer%
xcopy "q:\bin\imager.x64.zip"      "%dstdir%\data\" /F /Y %newer%
xcopy "q:\bin\clicket.zip"         "%dstdir%\data\" /F /Y %newer%
xcopy "q:\bin\rylogviewer.x86.zip" "%dstdir%\data\" /F /Y %newer%

:end
EndLocal
pause
goto :eof
