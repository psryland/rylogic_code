@echo off
SetLocal
cls
echo ===========================
echo Imager Screen Saver Install
echo ===========================
echo.

for /f "tokens=* delims= " %%I in (".\") do set srcdir=%%~dpI
set srcdir=%srcdir:~0,-1%
if not exist "%srcdir%\Imager.exe" set srcdir=bin\release
if not exist "%srcdir%\Imager.exe" goto :notfound

set dstdir=%systemroot%\System32
set ssname=Imager - by Rylogic Ltd

echo Imager install path: "%srcdir%"
echo.

echo Install Options:
echo   (I)nstall Imager as a screen saver
echo   (U)ninstall Imager as a screen saver
set /p action=:- 

if /I "%action%" equ "U" (
	"%srcdir%\cex.exe" -shdelete "%dstdir%\%ssname%.scr,%dstdir%\%ssname%.xml" -flags NoConfirmation -title "Uninstalling Screen Saver..."
	echo Uninstall complete.
	goto :success
)

if /I "%action%" equ "I" (

	::Generate the config xml file
	echo ^<?xml version="1.0"?^> > "%srcdir%\%ssname%.xml"
	echo ^<root^> >> "%srcdir%\%ssname%.xml"
	echo   ^<process^>%srcdir%\imager.exe^</process^> >> "%srcdir%\%ssname%.xml"
	echo   ^<startdir^>%srcdir%\^</startdir^> >> "%srcdir%\%ssname%.xml"
	echo ^</root^> >> "%srcdir%\%ssname%.xml"
	if errorlevel 1 goto :error
	echo         1 file generated.
	
	::Copy the screen saver files to "%dstdir%"
	"%srcdir%\cex.exe" -shcopy "%srcdir%\cex.exe,%srcdir%\%ssname%.xml" "%dstdir%\%ssname%.scr,%dstdir%\%ssname%.xml" -flags MultiDestFiles -title "Installing Screen Saver..."
	if errorlevel 1 goto :error

	echo Install complete
	goto :success
)

:success
pause
goto :eof

:abort
echo Aborted.
pause
goto :eof

:error
echo Error occured.
pause
goto :eof

:notfound
echo Imager files not found
pause
goto :eof
