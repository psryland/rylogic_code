::Post Build Event for exporting binary file to this local directory
::Use:
::	_publish_bin $(TargetPath) $(PlatformName) $(ConfigurationName)

@echo OFF
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%

set targetpath=%1
set platform=%2
set config=%3
set dstdir=q:\bin

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :eof
)

call split_path targetpath srcdir file extn
call lower_case targetpath
call lower_case platform
call lower_case config
call lower_case srcdir
call lower_case file
call lower_case extn

if [%platform%]==[win32] set platform=x86

if [%config%]==[release] (

	::Copy the binary to the bin folder
	call copy "%targetpath%" "%dstdir%\%file%.%platform%.%extn%"  /Y /F /D

	if [%arch%]==[%platform%] (
		call copy "%dstdir%\%file%.%platform%.%extn%" "%dstdir%\%file%.%extn%"  /Y /F /D
	)
)
REM if [%config%]==[debug] (

	REM ::Copy the binary to the bin folder
	REM call copy "%targetpath%" "%dstdir%\%file%.%platform%.%config%.%extn%"  /Y /F /D

	REM ::If there's an associated pdb file copy that too
	REM if exist "%srcdir%\%file%.pdb" (
		REM call copy "%srcdir%\%file%.pdb" "%dstdir%\%file%.%platform%.%config%.pdb"  /Y /F /D
REM )

EndLocal

