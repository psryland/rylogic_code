@echo off
SetLocal EnableDelayedExpansion 
cls

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 6 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)

:: Add the sdk\pr\cmd folder to the path
set PATH=%qdrive%\sdk\pr\cmd\;%PATH%

::Careful! these can overwrite MSBuild variables if you choose the same names (i.e. outdir is already used!)
set proj=%qdrive%\projects\RyLogViewer
set docsdir=%proj%\docs

echo *************************************************************************
echo  RyLogViewer Documentation
echo   Copyright © Rylogic Limited 2012
echo *************************************************************************
pause

echo.
for %%f in (%docsdir%\*.t4html) do (
	echo %%f ...
	"%ttbuild%" "%%f"
	if errorlevel 1 goto :error
)

echo.
echo     Success.
echo.
call wait 5000
goto :eof

::Error exit
:error
echo.
echo     Failed.
echo.
pause
goto :eof
