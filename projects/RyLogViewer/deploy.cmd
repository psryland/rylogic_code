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
if not exist "%dstdir%\%rlvdir%\lib" mkdir "%dstdir%\%rlvdir%\lib"
del "%dstdir%\%rlvdir%\lib\*.*" /Q
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
call copy "%bindir%\lib\clrdump.dll" "%dstdir%\%rlvdir%\lib\"
if errorlevel 1 goto :error
call copy "%bindir%\lib\dbghelp.dll" "%dstdir%\%rlvdir%\lib\"
if errorlevel 1 goto :error

echo Creating zip file...
if exist "%dstdir%\%rlvdir%.zip" del "%dstdir%\%rlvdir%.zip" /Q
"%zip%" a "%dstdir%\%rlvdir%.zip" "%dstdir%\%rlvdir%"
if errorlevel 1 goto :error

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



