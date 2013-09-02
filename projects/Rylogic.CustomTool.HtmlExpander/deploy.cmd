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

:: Requires admin
call runas_admin %~s0 %*
if errorlevel 1 goto :eof

set regasm=C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe
set srcdir=%qdrive%\projects\Rylogic.CustomTool.HtmlExpander
set dstdir=%qdrive%\bin\custom_tools
set config=debug
set binname=Rylogic.CustomTool.HtmlExpander.dll
set proj=%srcdir%\Rylogic.CustomTool.HtmlExpander.sln
set symdir=%qdrive%\local\symbols
set rlvdir=rylogviewer

echo *************************************************************************
echo  HtmlExpander Deploy
echo   Copyright © Rylogic Limited 2013
echo.
echo    Destination: %dstdir%\%binname%
echo  Configuration: %config%
echo *************************************************************************
pause

echo.
echo Building ...
"%msbuild%" "%proj%" /p:Configuration=%config% /t:Build
if errorlevel 1 goto :error

echo.
echo Copying to %dstdir%
if not exist "%dstdir%" mkdir "%dstdir%"
call copy "%srcdir%\bin\%config%\%binname%" "%dstdir%\%binname%"
call copy "%srcdir%\bin\%config%\pr.dll" "%dstdir%\pr.dll"
if errorlevel 1 goto :error

echo.
echo Registering with COM
"%regasm%" /codebase "%dstdir%\%binname%"
if errorlevel 1 goto :error

echo.
echo Adding a VS registry entry
"%srcdir%\registry_entry.reg"
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