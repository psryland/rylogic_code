:: Wait for a period of time
::
:: Example
::	call wait 5000
:: Outputs: STRING
:wait
	setlocal
	set timeout=%~1
	ping -n 1 -w %timeout% 1.1.1.1 >nul
	endlocal
goto :eof
