::Post Build Event for exporting binary file to this local directory
::Use:
::  _publish_bin $(TargetPath) $(PlatformTarget) $(ConfigurationName) [dstsubdir]

@echo OFF
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%

set targetpath=%~1
set platform=%2
set config=%3
set dstsubdir=%~4
if [%platform%]==[win32] set platform=x86

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

call split_path targetpath srcdir file extn
call lower_case targetpath
call lower_case platform
call lower_case config
call lower_case srcdir
call lower_case file
call lower_case extn

::Default to a subdir matching the target filename
if [%dstsubdir%]==[] (
	set dstsubdir=%file%
)

::Set the output directory and ensure it exists
set dstdirroot=q:\bin
set dstdir=%dstdirroot%\%dstsubdir%\%platform%
if not exist "%dstdir%" mkdir "%dstdir%"

::Only publish release builds
if [%config%]==[release] (

	::Copy the binary to the bin folder
	call copy "%targetpath%" "%dstdir%\%file%.%extn%"

	:: If the system architecture matches this release, copy to the root dstdir
	if [%platform%]==[%arch%] (
		call copy "%dstdir%\%file%.%platform%.%extn%" "%dstdirroot%\%file%.%extn%"
	)
)

EndLocal

