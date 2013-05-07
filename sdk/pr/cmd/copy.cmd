:: Copy, only smarter
::
:: Example
::  call copy src dst
:: Note: if 'dst' is in a directory, it needs to exist before calling copy
:copy
	setlocal
	set src=%~1
	set dst=%~2
	set changed=1
	if exist "%dst%" (
		fc /B "%src%" "%dst%" >nul
		if errorlevel 0 set changed=0
	) else (
		echo 0 > "%dst%"
	)
	if [%changed%]==[1] (
		echo "%src%" -^> "%dst%"
		xcopy "%src%" "%dst%" /Y /F >nul
	)
	endlocal
goto :eof
