@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 3 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)

set srcdir=Q:\projects\rylogviewer
set dstdir=Q:\bin
set symdir=Q:\local\symbols
set proj=%srcdir%\RylogViewer.sln
set config=release

echo =================
echo Releasing RyLogViewer
echo config : %config%
echo dst dir: %dstdir%
echo =================

echo.
echo Building the apk...
"%msbuild%" "%proj%" /p:Configuration=%config% /t:Build
if errorlevel 1 goto :error

::Export for each platform (only x86 at the moment)
for %%p in (x86) do call :copy_files %%p
if errorlevel 1 goto :error
goto :end

:end
echo Success.
pause
start explorer "%dstdir%"
goto :eof

:error
echo Error occurred.
pause
goto :eof

::Copy subroutine
:copy_files
	setlocal
	if errorlevel 1 goto :eof
	
	echo --------------------------------------------------
	echo %1 Release
	echo.
	
	set rlvdir=rylogviewer.%1
	set bindir=%srcdir%\bin\release
	
	::Ensure directories exist and are empty
	if not exist "%dstdir%\%rlvdir%" mkdir "%dstdir%\%rlvdir%"
	del "%dstdir%\%rlvdir%\*.*" /Q
	if not exist "%symdir%\%rlvdir%" mkdir "%symdir%\%rlvdir%"
	del "%symdir%\%rlvdir%\*.*" /Q
	
	echo Copying files to "%dstdir%\%rlvdir%"
	call copy "%bindir%\rylogviewer.exe" "%dstdir%\%rlvdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\rylogviewer.pdb" "%symdir%\%rlvdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\pr.dll" "%dstdir%\%rlvdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\pr.pdb" "%symdir%\%rlvdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\lib\clrdump.dll" "%dstdir%\%rlvdir%\lib\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\lib\dbghelp.dll" "%dstdir%\%rlvdir%\lib\" /Y /F
	if errorlevel 1 goto :eof

	echo Creating zip file
	if exist "%dstdir%\%rlvdir%.zip" del "%dstdir%\%rlvdir%.zip" /Q
	"%zip%" a "%dstdir%\%rlvdir%.zip" "%dstdir%\%rlvdir%"
	if errorlevel 1 goto :eof

	echo.
	endlocal
goto :eof


