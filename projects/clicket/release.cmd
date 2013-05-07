@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)

set srcdir=Q:\obj\clicket\win32\release
set dstdir=Q:\bin

call copy "%srcdir%\clicket.exe" "%dstdir%\clicket\"

echo Creating zip file
"%zip%" a "%dstdir%\clicket.zip" "%dstdir%\clicket"

:end
pause
