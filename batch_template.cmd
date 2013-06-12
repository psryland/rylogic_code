::Template file for making batch files
::Use:
::  file.cmd $(TargetPath) $(PlatformTarget) $(ConfigurationName) [dstsubdir]

@echo off
SetLocal EnableDelayedExpansion 
cls

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 5 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)

:: Add the sdk\pr\cmd folder to the path
set PATH=%qdrive%\sdk\pr\cmd\;%PATH%

::Assign variables from inputs
set targetpath=%~1
set platform=%2
set config=%3
set dstsubdir=%~4
if [%platform%]==[win32] set platform=x86

echo This is a template batchfile

echo.
echo Success.
echo.
call wait 5000
goto :eof

:error
echo.
echo Failed.
echo.
pause
goto :eof