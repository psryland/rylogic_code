@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls
echo =================
echo Releasing RyLogViewer
echo =================
echo.

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)

set srcdir=Q:\projects\rylogviewer
set dstdir=Q:\bin\RyLogViewer

::Export for each platform (only x86 at the moment)
for %%p in (x86) do call :copy_files %%p
if errorlevel 1 goto :error
goto :end

::Copy subroutine
:copy_files
	setlocal
	if errorlevel 1 goto :eof
	
	echo --------------------------------------------------
	echo %1 Release
	echo.
	
	set rlvdir=rylogviewer.%1
	set bindir=%srcdir%\bin\release
	
	::Ensure the directory exists and is empty
	if not exist "%dstdir%\%rlvdir%" mkdir "%dstdir%\%rlvdir%"
	del "%dstdir%\%rlvdir%\*.*" /Q
	
	echo Copying files to "%dstdir%\%rlvdir%"
	call copy "%bindir%\rylogviewer.exe" "%dstdir%\%rlvdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\pr.dll" "%dstdir%\%rlvdir%\" /Y /F
	if errorlevel 1 goto :eof

	echo Creating zip file
	if exist "%dstdir%\%rlvdir%.zip" del "%dstdir%\%rlvdir%.zip" /Q
	"%zip%" a "%dstdir%\%rlvdir%.zip" "%dstdir%\%rlvdir%"
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

