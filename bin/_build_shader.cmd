::*********************************************
::Renderer
::  Copyright © Rylogic Ltd 2012
::*********************************************
::Build shaders using fxc.exe
::Use:
:: _build_shader.cmd $(Fullpath)
:: This will compile the shader into a header file in the same directory as $(Fullpath)
@echo OFF
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%

set fullpath=%1

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

call split_path.cmd fullpath srcdir file extn
set shdr=%file:~0,2%

::Choose the compiler profile based on file extension
if [%shdr%]==[vs] (
	set profile=/Tvs_4_1
) else if [%shdr%]==[ps] (
	set profile=/Tps_4_1
)

::Choose the output file to generate
set output=/Fh"%srcdir%\%file%.h"

::Set the variable name to the name of the file
set varname=/Vn%file%

::Set include paths
set includes=/I%srcdir%\..

::Set defines
set defines=/DSHADER_BUILD=1

::Set other command line options
set options=/nologo

::Build the shader
cd %srcdir%
"%fxc%" "%fullpath%" %profile% %output% %varname% %includes% %defines% %options%

::Generate preprocessed output
::"%fxc%" "%fullpath%" /P"%fullpath%".pp %includes% %defines% %options%
::Q:\bin\textformatter.exe -f "%fullpath%.pp" -newlines 0 2
::"%textedit%" "%fullpath%.pp"
