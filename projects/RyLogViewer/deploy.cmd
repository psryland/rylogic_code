@echo off
SetLocal EnableDelayedExpansion 
cls

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 3 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)

set PATH=%qdrive%\sdk\pr\cmd\;%PATH%
set srcdir=%qdrive%\projects\rylogviewer
set dstdir=%qdrive%\bin
set symdir=%qdrive%\local\symbols
set proj=%srcdir%\RylogViewer.sln
set rlvdir=rylogviewer

echo Is there an 'Upgrade Path' in the setup project and have you changed the Product GUID and version^?
echo This is needed so that the new installer will replace the existing installation if there.
set /p confirm=^(y/n^):
if not [%confirm%]==[y] goto :error

set /p config=Configuration (debug, release):

echo *************************************************************************
echo  RylogViewer Deploy
echo   Copyright © Rylogic Limited 2013
echo.
echo    Destination: %dstdir%\%rlvdir%
echo  Configuration: %config%
echo *************************************************************************
pause

echo.
echo Building the exe...
"%msbuild%" "%proj%" /p:Configuration=%config% /t:Build
if errorlevel 1 goto :error

::Ensure directories exist and are empty
if not exist "%dstdir%\%rlvdir%" mkdir "%dstdir%\%rlvdir%"
del "%dstdir%\%rlvdir%\*.*" /Q
if not exist "%symdir%\%rlvdir%" mkdir "%symdir%\%rlvdir%"
del "%symdir%\%rlvdir%\*.*" /Q

echo.
echo Copying to %dstdir%\%rlvdir%...
set bindir=%srcdir%\bin\%config%
call copy "%bindir%\rylogviewer.exe" "%dstdir%\%rlvdir%\"
if errorlevel 1 goto :error
call copy "%bindir%\rylogviewer.pdb" "%symdir%\%rlvdir%\"
if errorlevel 1 goto :error
call copy "%bindir%\pr.dll" "%dstdir%\%rlvdir%\"
if errorlevel 1 goto :error
call copy "%bindir%\pr.pdb" "%symdir%\%rlvdir%\"
if errorlevel 1 goto :error

echo.
echo Creating zip file...
if exist "%dstdir%\%rlvdir%.zip" del "%dstdir%\%rlvdir%.zip" /Q
"%zip%" a "%dstdir%\%rlvdir%.zip" "%dstdir%\%rlvdir%"
if errorlevel 1 goto :error

if exist "%srcdir%\setup\setup\express\singleimage\diskimages\disk1\setup.exe" (
	echo.
	echo Copying RylogViewerSetup.exe to www...
	call copy "%srcdir%\setup\setup\express\singleimage\diskimages\disk1\setup.exe" "%wwwroot%\data\RylogViewerSetup.exe"
	if errorlevel 1 goto :error
)

echo.
echo    Success.
echo.
call wait 5000
goto :eof

:error
echo.
echo    Failed.
echo.
pause
goto :eof



