::Post Build Event for copying library files to a target output directory
::Use:
:: _lib_copy mylib.dll $(PlatformTarget) $(ConfigurationName) $(TargetDir) [$(SrcDir)]
:: This will copy "$(SrcDir)\mylib.$(PlatformTarget).$(ConfigurationName).dll" to "$(TargetDir)\mylib.dll" and optionally the pdb if it exists

@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%

set filename=%~1
set platform=%2
set config=%3
set dstdir=%~4
set srcdir=%~5
if [%srcdir%]==[] set srcdir=q:\sdk\pr\lib

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)

call remove_trailing_slash dstdir
call split_path filename dummy file extn
call lower_case file
call lower_case extn
call lower_case platform
call lower_case config
call lower_case dstdir

::if [%platform%]==[x86] set platform=win32

if not exist "%dstdir%\" mkdir "%dstdir%\"
call copy "%srcdir%\%file%.%platform%.%config%.%extn%" "%dstdir%\%file%.%extn%"
if exist "%srcdir%\%file%.%platform%.%config%.pdb" (
	call copy "%srcdir%\%file%.%platform%.%config%.pdb" "%dstdir%\%file%.pdb"
)

endlocal