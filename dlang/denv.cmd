::Creates a d environment
@echo off
SetLocal EnableDelayedExpansion 
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

cmd /K set PATH=%qdrive%\sdk\pr\cmd\;%qdrive%\dlang\dmd2\windows\bin;%PATH%

