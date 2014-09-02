:: Copy, only smarter
::
:: Example
::  call copy src dst
:: Note: if 'dst' is in a directory, it needs to exist before calling copy
:copy
	setlocal
	set src=%~f1
	set dst=%~f2
	if [%trace%]==[1] ( echo copying '%src%' to '%dst%' )
	call is_directory "%dst%" result
	if [%result%]==[0] echo 0 > "%dst%"
	echo %src% -^> %dst%
	xcopy "%src%" "%dst%" /Y /F >nul
	endlocal
goto :eof
