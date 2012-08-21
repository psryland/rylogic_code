::
:: Use:
::   release.cmd [noexplorer]
@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls
echo =================
echo Releasing Csex
echo =================
echo.

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)

if [%1]==[noexplorer] (
	set noexpl=true
)
set srcdir=Q:\projects\Csex
set dstdir=Q:\bin
set symdir=Q:\local\symbols

::Export for each platform (only x86 at the moment)
for %%p in (x86) do call :copy_files %%p
if errorlevel 1 goto :error
goto :end

:end
echo Success.
pause
if [%noexpl%]==[] (
	start explorer "%dstdir%"
)
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
	
	set csexdir=csex.%1
	set bindir=%srcdir%\bin\release
	
	::Ensure directories exist and are empty
	if not exist "%dstdir%\%csexdir%" mkdir "%dstdir%\%csexdir%"
	del "%dstdir%\%csexdir%\*.*" /Q
	if not exist "%symdir%\%csexdir%" mkdir "%symdir%\%csexdir%"
	del "%symdir%\%csexdir%\*.*" /Q
	
	echo Copying files to "%dstdir%\%csexdir%"
	call copy "%bindir%\csex.exe" "%dstdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\csex.pdb" "%symdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\pr.dll" "%dstdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\pr.pdb" "%symdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :eof

	echo.
	endlocal
goto :eof


