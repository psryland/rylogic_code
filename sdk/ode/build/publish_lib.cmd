:: Post build event for copying ODE libs to the output directory
@echo off
SetLocal EnableDelayedExpansion 

set targetpath=%1
set platform=%2
set config=%3
set dstdir=%4

if [%targetpath%]==[] goto :error

call :split_path targetpath srcdir file extn
call :lower_case targetpath
call :lower_case platform
call :lower_case config
call :lower_case srcdir
call :lower_case file
call :lower_case extn

::Copy targetpath to the output directory
copy %targetpath% %dstdir%\%file%.%platform%.%config%.%extn%

::If there's an associated pdb file copy that too
if exist "%srcdir%\%file%.pdb" (
	copy "%srcdir%\%file%.pdb" "%dstdir%\%file%.%platform%.%config%.pdb"
)
goto :end

:error
echo Invalid input parameters

:end
EndLocal
goto :eof

:: Subroutines
:split_path
	setlocal
	set v=%1
	set path=%2
	set file=%3
	set extn=%4
	call set fullpath=%%%v%%%
	for /f "tokens=* delims= " %%I in ("%fullpath%") do set p=%%~dpI& set f=%%~nI& set e=%%~xI
	if [%p:~-1,1%]==[\] (set p=%p:~0,-1%)
	if [%e:~0,1%]==[.] (set e=%e:~1%)
	endlocal & set %path%=%p%& set %file%=%f%& set %extn%=%e%
goto :eof
:lower_case
	setlocal
	set v=%~1
	call set p=%%%v%%%
	for %%i in ("A=a" "B=b" "C=c" "D=d" "E=e" "F=f" "G=g" "H=h" "I=i" "J=j" "K=k" "L=l" "M=m" "N=n" "O=o" "P=p" "Q=q" "R=r" "S=s" "T=t" "U=u" "V=v" "W=w" "X=x" "Y=y" "Z=z") do set p=!p:%%~i!
	endlocal & set %v%=%p%
goto :eof