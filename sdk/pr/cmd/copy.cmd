:: Copy, only smarter
::
:: Example
::	call copy src dst /Y /F /D 
:: Note: if 'dst' is in a directory, it needs to exist before calling copy
:copy
	setlocal
	set src=%~1
	set dst=%~2
	set p0=%3
	set p1=%4
	set p2=%5
	set p3=%6
	set p4=%7
	set p5=%8
	if not exist "%dst%" (
		echo 0 > "%dst%"
		if [%p0%]==[/D] set p0=
		if [%p1%]==[/D] set p1=
		if [%p2%]==[/D] set p2=
		if [%p3%]==[/D] set p3=
		if [%p4%]==[/D] set p4=
		if [%p5%]==[/D] set p5=
	)
	xcopy "%src%" "%dst%" %p0% %p1% %p2% %p3% %p4% %p5%
	endlocal
goto :eof
