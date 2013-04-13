::Preprocess a given file
::Use:
::  pp $(FilePath) [$(OutputFilePath)]

@echo OFF
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

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

set filepath=%~1
set outpath=%~2
if [%outpath%]==[] set outpath=%filepath%.i

echo Loading Visual Studio build environment from:
echo  "%vc_env%"
call "%vc_env%"
echo Visual Studio Version: %VisualStudioVersion%
echo.

echo Preprocessing:
echo   Source: "%filepath%"
echo   Destination: "%outpath%"
echo.

set flags=/P /nologo /TP
::/GS /analyze- /W3 /wd"4351" /Gy /Zc:wchar_t /ZI /Gm /Od /Ob0 /GF /WX- /Zc:forScope /RTC1 /Gd /Oy- /MTd /openmp /fp:precise /errorReport:prompt /EHsc 

set includes=^
 /I"Q:\projects"^
 /I"Q:\sdk\pr"^
 /I"Q:\sdk\wtl\v8.1\include"^
 /I"Q:\sdk\lua\lua\src"^
 /I"Q:\sdk\lua"

set defines=^
 /D _DEBUG^
 /D _CRT_SECURE_NO_WARNINGS^
 /D NOMINMAX^
 /D PR_UNITTESTS

cl %flags% %includes% %defines% /Fi"%outpath%" "%filepath%"
echo.

echo Cleaning PP output:
q:\bin\textformatter.exe -f "%outpath%" -newlines 0 1
echo.

echo Showing PP output:
"%textedit%" "%outpath%"
echo.

EndLocal
