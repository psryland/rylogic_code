::
:: Use:
::   release.cmd
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

set dstdir=Q:\bin
set srcdir=Q:\projects\Csex
set symdir=Q:\local\symbols
set bindir=%srcdir%\bin\release
set dst=%dstdir%\csex
set sym=%symdir%\csex

::Ensure directories exist and are empty
if not exist "!dst!" mkdir "!dst!"
del "!dst!\*.*" /Q
if errorlevel 1 goto :error
if not exist "!sym!" mkdir "!sym!"
del "!sym!\*.*" /Q
if errorlevel 1 goto :error

echo Copying files to "!dst!"
call copy "!bindir!\csex.exe" "!dst!\csex.exe"
if errorlevel 1 goto :error
call copy "!bindir!\csex.pdb" "!sym!\csex.pdb"
if errorlevel 1 goto :error
call copy "!bindir!\pr.dll" "!dst!\pr.dll"
if errorlevel 1 goto :error
call copy "!bindir!\pr.pdb" "!sym!\pr.pdb"
if errorlevel 1 goto :error

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


