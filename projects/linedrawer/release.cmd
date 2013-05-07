@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls
echo =================
echo Releasing Linedrawer
echo =================
echo.

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

set dstdir=Q:\bin

::Export for each platform
for %%p in (x86 x64) do (
	echo.
	set platform=%%p
	if [!platform!] == [x86] set platform=win32
	
	echo --------------------------------------------------
	echo !platform! Release
	
	set ldrdir=linedrawer.!platform!
	set bindir=..\..\obj\linedrawer\!platform!\release
	
	::ensure the directory exists and is empty
	if not exist "!dstdir!\!ldrdir!" mkdir "!dstdir!\!ldrdir!"
	del "!dstdir!\!ldrdir!\*.*" /Q
	if errorlevel 1 goto :error
	
	echo Copying linedrawer files to "!dstdir!\!ldrdir!"
	call copy "!bindir!\linedrawer.exe" "!dstdir!\!ldrdir!\"
	if errorlevel 1 goto :error

	echo Creating zip file
	if exist "!dstdir!\!ldrdir!.zip" del "!dstdir!\!ldrdir!.zip" /Q
	"!zip!" a "!dstdir!\!ldrdir!.zip" "!dstdir!\!ldrdir!"
	if errorlevel 1 goto :error
)

echo.
echo     Success.
echo.
ping -n 1 -w 5000 1.1.1.1 >nul
goto :eof

::Error exit
:error
echo.
echo     Release Failed.
echo.
pause
goto :eof

