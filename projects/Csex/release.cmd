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
if [%RylogicEnv%]==[] (
 	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :error
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 3 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :error
)

set srcdir=Q:\projects\Csex
set dstdir=Q:\bin
set symdir=Q:\local\symbols
set bindir=%srcdir%\bin\release

::Export for each platform (only x86 at the moment)
for %%p in (x86 x64) do (
	echo.
	set csexdir=csex.%%p
	
	::Ensure directories exist and are empty
	if not exist "%dstdir%\%csexdir%" mkdir "%dstdir%\%csexdir%"
	del "%dstdir%\%csexdir%\*.*" /Q
	if errorlevel 1 goto :error
	if not exist "%symdir%\%csexdir%" mkdir "%symdir%\%csexdir%"
	del "%symdir%\%csexdir%\*.*" /Q
	if errorlevel 1 goto :error
	
	echo Copying files to "%dstdir%\%csexdir%"
	call copy "%bindir%\csex.exe" "%dstdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :error
	call copy "%bindir%\csex.pdb" "%symdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :error
	call copy "%bindir%\pr.dll" "%dstdir%\%csexdir%\" /Y /F
	if errorlevel 1 goto :error
	call copy "%bindir%\pr.pdb" "%symdir%\%csexdir%\" /Y /F
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


