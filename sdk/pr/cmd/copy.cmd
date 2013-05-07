:: Copy, only smarter
::
:: Example
::  call copy src dst
:: Note: if 'dst' is in a directory, it needs to exist before calling copy
:copy
	setlocal
	set src=%~f1
	set dst=%~f2
	echo %src% -^> %dst%
	xcopy "%src%" "%dst%" /Y /F /D >nul
	endlocal
goto :eof
