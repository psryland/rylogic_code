::
:: Use:
::   release.cmd [noninteractive]
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

set noninteractive=%1
set PATH=%qdrive%\sdk\pr\cmd\;%PATH%
set dstdir=%qdrive%\bin
set srcdir=%qdrive%\projects\Csex
set symdir=%qdrive%\local\symbols
set config=release
set dst=%dstdir%\csex
set sym=%symdir%\csex
set bindir=%srcdir%\bin\%config%

echo *************************************************************************
echo  Csex Deploy
echo   Copyright © Rylogic Limited 2013
echo.
echo    Destination: %dst%
echo  Configuration: %config%
echo *************************************************************************
if [%noninteractive%] == [] pause

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
if [%noninteractive%] == [] call wait 5000
goto :eof

::Error exit
:error
echo.
echo     Release Failed.
echo.
pause
goto :eof


