::*********************************************
::Renderer
::  Copyright © Rylogic Ltd 2012
::*********************************************
::Build shaders using fxc.exe
::Use:
:: _build_shader.cmd $(Fullpath) [pp] [obj]
:: This will compile the shader into a header file in the same directory as $(Fullpath)
@echo off
SetLocal EnableDelayedExpansion 
::cls

::Load Rylogic environment variables and check version
if [%RylogicEnv%]==[] (
	echo ERROR: The 'RylogicEnv' environment variable is not set.
	goto :eof
)
call %RylogicEnv%
if %RylogicEnvVersion% lss 4 (
	echo ERROR: '%RylogicEnv%' is out of date. Please update.
	goto :eof
)
set PATH=%qdrive%\sdk\pr\cmd\;%PATH%

set fullpath=%~1
set pp=
set obj=
if [%2]==[pp]  set pp=1
if [%3]==[pp]  set pp=1
if [%2]==[obj] set obj=1
if [%3]==[obj] set obj=1
set trace=0

call split_path.cmd fullpath srcdir file extn

::The shader to build
set shdr=%file:~-2%
if [%trace%]==[1] echo Shader %shdr%

::Choose the output files to generate
set outdir=%srcdir%\..\compiled
set output=/Fh"%outdir%\%file%.h.tmp"
if [%obj%]==[1] set output=!output! /Fo"%outdir%\%file%.cso.tmp"

::Choose the compiler profile based on file extension
if [%shdr%]==[vs] (
	set profile=/Tvs_5_0
) else if [%shdr%]==[ps] (
	set profile=/Tps_5_0
) else if [%shdr%]==[gs] (
	set profile=/Tgs_5_0
) else (
	echo ERROR: Unknown shader type '%shdr%'
	goto :eof
)

::Delete potentially left over temporary output
if exist "%outdir%\%file%.h.tmp"   del "%outdir%\%file%.h.tmp"
if exist "%outdir%\%file%.cso.tmp" del "%outdir%\%file%.cso.tmp"
if exist "%outdir%\%file%.pp"      del "%outdir%\%file%.pp"
if exist "%outdir%\fxc_output.txt" del "%outdir%\fxc_output.txt"
if [%trace%]==[1] echo Output directory "%outdir%"

::Set the variable name to the name of the file
set varname=/Vn%file:~0,-3%_%shdr%

::Set include paths
set includes=/I"%srcdir%\.."

::Set defines
set defines=/DSHADER_BUILD=1

::Set other command line options
set options=/nologo /Gis /Ges /WX

:: uncomment this for debugging
::echo Debugging options added
::set options=%options% /Od /Zi

::Build the shader
"%fxc%" "%fullpath%" %profile% %output% %varname% %includes% %defines% %options% >"%outdir%\fxc_output.txt"
if errorlevel 1 goto :error

::Compare the produced files with any existing ones, don't replace the files if they are identical
::This prevents VS rebuilding all the time.
set changed=1
if exist "%outdir%\%file%.h" (
	set changed=0
	fc /B "%outdir%\%file%.h.tmp" "%outdir%\%file%.h" >nul
	if errorlevel 1 set changed=1
)
if [!changed!]==[1] (
	echo %file%.%extn%
	move /Y "%outdir%\%file%.h.tmp"   "%outdir%\%file%.h"   >nul
	if exist "%outdir%\%file%.cso.tmp" move /Y "%outdir%\%file%.cso.tmp" "%outdir%\%file%.cso" >nul
)

::Delete temporary output if still there
if exist "%outdir%\%file%.h.tmp"   del "%outdir%\%file%.h.tmp"
if exist "%outdir%\%file%.cso.tmp" del "%outdir%\%file%.cso.tmp"
if exist "%outdir%\fxc_output.txt" del "%outdir%\fxc_output.txt"

::Generate preprocessed output
if [%pp%]==[1] (
	set ppoutput=%outdir%\%file%.pp
	"%fxc%" "%fullpath%" /P"!ppoutput!" %includes% %defines% %options%
	Q:\bin\textformatter.exe -f "!ppoutput!" -newlines 0 1
)
::"!textedit!" "!ppoutput!"

goto :eof

:error
type "%outdir%\fxc_output.txt" 1>&2
if exist "%outdir%\fxc_output.txt" del "%outdir%\fxc_output.txt"
