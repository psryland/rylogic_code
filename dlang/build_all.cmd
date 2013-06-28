::Builds the D tools and libs
@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

::Load RylogicEnv environment variables and check version
if [%RylogicEnv%]==[] (
 	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 5 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)

echo *************************************************************************
echo  Building DMD and related tools
echo    RootDir: %dmdroot%
echo *************************************************************************
pause

pushd
set mk=%dmdroot%\windows\bin\make.exe

echo ------------------------------------------------------------------------
echo. Building dmd.exe
echo ------------------------------------------------------------------------
set VisualStudioVersion=11.0
"%msbuild%" "%dmdroot%\src\dmd\src\dmd_msc.vcxproj" /t:Build /p:Configuration=Release /p:Platform=x64
if errorlevel 1 goto :error
xcopy "%dmdroot%\src\dmd\src\vcbuild\x64\Release\dmd_msc.exe" "%dmdroot%\windows\bin\dmd.exe" /Y /F
echo.
::cd "%dmdroot%\src\dmd\src"
::set DM_HOME=%dmdroot%
::call "%vc_env%"
::"%mk%" -f win32.mak CC=vcbuild\dmc_cl INCLUDE=vcbuild DEBUG=/O2 dmd.exe
::if errorlevel 1 goto :error
::echo.

echo ------------------------------------------------------------------------
echo Making druntime
echo ------------------------------------------------------------------------
cd "%dmdroot%\src\druntime"
"%mk%" -f win64.mak clean
"%mk%" -f win64.mak
if errorlevel 1 goto :error
echo.

echo ------------------------------------------------------------------------
echo Making phobos
echo ------------------------------------------------------------------------
cd "%dmdroot%\src\phobos"
"%mk%" -f win64.mak clean
"%mk%" -f win64.mak phobos64.lib
if errorlevel 1 goto :error
xcopy phobos64.lib "%dmdroot%\windows\lib\phobos64.lib" /Y /F
echo.

goto :success

:error
popd
echo Failed
pause
goto :eof

:success
popd
echo Success.
call wait 5000
