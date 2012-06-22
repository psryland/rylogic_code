@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)


::Read the command line parameter
set param=%1
set srcdir=Q:\projects\www.rylogic.co.nz
set dstdir=%zdrive%\WWW\WWW-pub
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
call copy "%srcdir%\*.shtml" "%dstdir%\" /Y /F /E %newer%
call copy "%srcdir%\*.png"   "%dstdir%\" /Y /F /E %newer%
call copy "%srcdir%\*.jpg"   "%dstdir%\" /Y /F /E %newer%
call copy "%srcdir%\*.css"   "%dstdir%\" /Y /F /E %newer%
call copy "%srcdir%\*.xml"   "%dstdir%\" /Y /F /E %newer%

::Copy site data
call copy "q:\bin\linedrawer.x86.zip"  "%zdrive%\WWW\WWW-pub\data\" /F /Y %newer%
call copy "q:\bin\linedrawer.x64.zip"  "%zdrive%\WWW\WWW-pub\data\" /F /Y %newer%
call copy "q:\bin\imager.x86.zip"      "%zdrive%\WWW\WWW-pub\data\" /F /Y %newer%
call copy "q:\bin\imager.x64.zip"      "%zdrive%\WWW\WWW-pub\data\" /F /Y %newer%
call copy "q:\bin\clicket.zip"         "%zdrive%\WWW\WWW-pub\data\" /F /Y %newer%

:end
EndLocal
pause
goto :eof
