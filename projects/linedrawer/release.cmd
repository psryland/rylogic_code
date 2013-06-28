@echo off
SetLocal EnableDelayedExpansion 
cls

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
 	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :error
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 3 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :error
)
set PATH=%qdrive%\sdk\pr\cmd\;%PATH%

set srcdirroot=Q:\obj2012
set dstdirroot=Q:\bin
set appname=Alzatool

echo =================
echo Releasing %appname%
echo =================
echo.

::Export for each platform
for %%p in (x86 x64) do (
	set platform=%%p
	set dstdir=%dstdirroot%\appname\!platform!
	set srcdir=%srcdirroot%\linedrawer\!platform!\release
	echo !srcdir! -^> !dstdir!
	if exist "!srcdir!" (
		if not exist "!dstdir!" mkdir "!dstdir!"
		del "!dstdir!\*.*" /Q /Y
		if errorlevel 1 goto :error

		echo Copying %appname% files to "!dstdir!"
		call copy "!srcdir!\%appname%.exe" "!dstdir!\"
		if errorlevel 1 goto :error

		echo Creating zip file
		if exist "!dstdir!.zip" del "!dstdir!.zip" /Q /Y
		"!zip!" a "!dstdir!.zip" "!dstdir!"
		if errorlevel 1 goto :error
	)
)

echo.
echo     Success.
echo.
call wait 5000
goto :eof

::Error exit
:error
echo.
echo     Release Failed.
echo.
pause
goto :eof

