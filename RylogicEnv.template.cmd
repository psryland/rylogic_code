@echo OFF
rem 
rem Rylogic Limited user specific variables
rem  Remember to create a environment variable called 'RylogicEnv' 
rem  that is the path to this file so that other batch files can simply
rem  'call %RylogicEnv%' to set up the variables below.
rem

rem Rylogic Environment Variables version number
rem Increment if you add a new user variable and require users to have that variable
rem Test for a minimum version requirement using:
rem if %RylogicEnvVersion% lss 1 (
rem 	echo RylogicEnv.cmd out of date. Please update
rem 	goto :eof
rem )

REM Boilerplate:
::Load Rylogic environment variables and check version
::if [%RylogicEnv%]==[] (
:: 	echo ERROR: The 'RylogicEnv' environment variable is not set.
::	goto :eof
::)
::call %RylogicEnv%
::if %RylogicEnvVersion% lss 2 (
::	echo ERROR: '%RylogicEnv%' is out of date. Please update.
::	goto :eof
::)

set RylogicEnvVersion=2

set user=Paul
set machine=Rylogic
set qdrive=Q:
set zdrive=Y:
set cp=xcopy
set zip=Q:\tools\7za.exe
set arch=x86
set vs_dir=D:\Program Files\Microsoft Visual Studio 9.0
set fxc=%DXSDK_DIR%Utilities\bin\x64\fxc.exe
set textedit=C:\Program Files (x86)\Notepad++\notepad++.exe
