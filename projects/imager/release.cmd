@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls
echo =================
echo Releasing Imager
echo =================
echo.

::Load Rylogic environment variables and check version
call %RylogicEnv%
if %RylogicEnvVersion% lss 1 (
 	echo RylogicEnv.cmd out of date. Please update
	goto :end
)

set dstdir=Q:\bin
for %%p in (x86 x64) do call :copy_imager_files %%p
if errorlevel 1 goto :error
goto :end

::Copy subroutine
:copy_imager_files
	setlocal
	if errorlevel 1 goto :eof
	
	echo --------------------------------------------------
	echo %1 Release
	echo.
	
	set imgdir=imager.%1
	set bindir=Q:\projects\imager\bin\%1\Release
	set srcdir=Q:\projects\imager

	::ensure the directory exists and is empty
	if not exist "%dstdir%\%imgdir%" mkdir "%dstdir%\%imgdir%"
	del "%dstdir%\%imgdir%\*.*" /Q

	echo Copying imager files to "%dstdir%\%imgdir%"
	call copy "%bindir%\imager.exe"                                  "%dstdir%\%imgdir%\"  /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\pr.dll"                                      "%dstdir%\%imgdir%\"  /Y /F
	if errorlevel 1 goto :eof
	call copy "%bindir%\view3d.dll"                                  "%dstdir%\%imgdir%\"  /Y /F
	if errorlevel 1 goto :eof
	call copy "%srcdir%\install_screen_saver.cmd"                    "%dstdir%\%imgdir%\"  /Y /F
	if errorlevel 1 goto :eof
	call copy "q:\sdk\directx9.0c\aug2009\bin\%1\d3dcompiler_42.dll" "%dstdir%\%imgdir%\"  /Y /F
	if errorlevel 1 goto :eof
	call copy "q:\sdk\directx9.0c\aug2009\bin\%1\d3dx9_42.dll"       "%dstdir%\%imgdir%\"  /Y /F
	if errorlevel 1 goto :eof
	call copy "q:\bin\cex.%1.exe"                                    "%dstdir%\%imgdir%\cex.exe"  /Y /F
	if errorlevel 1 goto :eof
	
	echo Creating zip file
	if exist "%dstdir%\%imgdir%.zip" del "%dstdir%\%imgdir%.zip" /Q
	"%zip%" a "%dstdir%\%imgdir%.zip" "%dstdir%\%imgdir%"
	if errorlevel 1 goto :eof
	
	echo.
	endlocal
goto :eof

:end
echo success.
pause
start explorer "%dstdir%"
goto :eof

:error
echo Error occurred.
pause
goto :eof

