:: Returns 1 if the file/path given is a directory
::
:: Example
::  call is_directory "path" result
:: Outputs:
::   1 if path is a directory, 0 if not

:is_directory
	setlocal EnableDelayedExpansion EnableExtensions
	set attr=%~a1
	if [%attr%]==[] set attr=---
	set dirattr=%attr:~0,1%
	set result=%2
	if [%trace%]==[1] ( echo Testing if %1 is a directory, storing the result in '%2'. attr='%attr%' dirattr='%dirattr%' )
	if /I [%dirattr%]==[d] (
		endlocal & set %result%=1
	) else (
		endlocal & set %result%=0
	)
goto :eof
