@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls
echo =================
echo Releasing Linedrawer
echo =================
echo.

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)

set dstdir=Q:\bin
for %%p in (x86 x64) do call :copy_linedrawer_files %%p
if errorlevel 1 goto :error
goto :end

::Copy subroutine
:copy_linedrawer_files
	setlocal
	if errorlevel 1 goto :eof
	
	echo --------------------------------------------------
	echo %1 Release
	echo.
	
	set ldrdir=linedrawer.%1
	set bindir=..\..\obj\linedrawer\%1\release
	if [%1]==[x86] set bindir=..\..\obj\linedrawer\win32\release

	::ensure the directory exists and is empty
	if not exist "%dstdir%\%ldrdir%" mkdir "%dstdir%\%ldrdir%"
	del "%dstdir%\%ldrdir%\*.*" /Q
	
	echo Copying linedrawer files to "%dstdir%\%ldrdir%"
	call copy "%bindir%\linedrawer.exe" "%dstdir%\%ldrdir%\" /Y /F
	if errorlevel 1 goto :eof

	echo Creating zip file
	if exist "%dstdir%\%ldrdir%.zip" del "%dstdir%\%ldrdir%.zip" /Q
	"%zip%" a "%dstdir%\%ldrdir%.zip" "%dstdir%\%ldrdir%"
	if errorlevel 1 goto :eof

	echo.
	endlocal
goto :eof

:end
echo success.
pause
start explorer "%dstdir%"
goto :eof

:error
echo Error occurred.
pause
goto :eof

