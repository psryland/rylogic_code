::Post Build Event for exporting library files to a directory
::Use:
:: _publish_lib $(TargetPath) $(PlatformName) $(ConfigurationName) dstdir
:: This will copy mylib.dll to "%dstdir%\mylib.platform.config.dll and optionally the pdb if it exists
:: 'dstdir' is optional, if not given it will default to "%qdrive%\sdk\pr\lib"
@echo OFF
SetLocal EnableDelayedExpansion 

set targetpath=%~1
set platform=%2
set config=%3
set dstdir=%~4
set wordsize=

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

set PATH=%qdrive%\sdk\pr\cmd\;%PATH%

call split_path targetpath srcdir file extn
call lower_case targetpath
call lower_case platform
call lower_case config
call lower_case srcdir
call lower_case file
call lower_case extn

::if [%platform%]==[x86] set platform=win32
if [%dstdir%]==[] set dstdir=%qdrive%\sdk\pr\lib
if [%platform%]==[x64] (
	set wordsize=64
) else (
	set wordsize=32
)

::Copy the library file to the lib folder
call copy "%targetpath%" "%dstdir%\%file%.%platform%.%config%.%extn%"
if errorlevel 1 goto :error

::If there's an associated pdb file copy that too
if exist "%srcdir%\%file%.pdb" (
	call copy "%srcdir%\%file%.pdb" "%dstdir%\%file%.%platform%.%config%.pdb"
	if errorlevel 1 goto :error
)

::If the lib is a dll, look for an import library and copy that too, if it exists
if [%extn%]==[dll] (
	if exist "%srcdir%\%file%.lib" (
		call copy "%srcdir%\%file%.lib" "%dstdir%\%file%.%platform%.%config%.lib"
		if errorlevel 1 goto :error
	)
)

::Produce a OMF version of the lib
::q:\tools\objconv -fOMF%wordsize% "%dstdir%\%file%.%platform%.%config%.%extn%" "%dstdir%\%file%.%platform%.%config%.omf.%extn%"
::if errorlevel 1 goto :error

:success
goto :eof

:error
echo Failed to publish %targetpath%
goto :eof

