:: Compare the timestamp of two files
::
:: Example
::  call compare_timestamp "file1" "file2" result
:: Outputs:
::   -1 if file1 is older than file2
::    0 if file1 is the same age as file2
::    1 if file1 is newer than file2

:compare_timestamp
	setlocal
	set result=%3
	set y1=
	set y2=
	set m1=
	set m2=
	set d1=
	set d2=
	set h1=
	set h2=
	set t1=
	set t2=
	set r=
	
	for %%i in (%1) do set date1=%%~ti
	for /F "tokens=1,2,3,4,5 delims=:/ " %%j in ("%date1%") do set d1=%%j& set m1=%%k& set y1=%%l& set h1=%%m& set t1=%%n
	for %%i in (%2) do set date2=%%~ti
	for /F "tokens=1,2,3,4,5 delims=:/ " %%j in ("%date2%") do set d2=%%j& set m2=%%k& set y2=%%l& set h2=%%m& set t2=%%n

	if [%trace%]==[1] echo %date1%
	if [%trace%]==[1] echo %date2%

	if [%y1%] lss [%y2%] set r=-1& goto :compare_timestamp_done
	if [%y1%] gtr [%y2%] set r=1& goto :compare_timestamp_done
	if [%m1%] lss [%m2%] set r=-1& goto :compare_timestamp_done
	if [%m1%] gtr [%m2%] set r=1& goto :compare_timestamp_done
	if [%d1%] lss [%d2%] set r=-1& goto :compare_timestamp_done
	if [%d1%] gtr [%d2%] set r=1& goto :compare_timestamp_done
	if [%h1%] lss [%h2%] set r=-1& goto :compare_timestamp_done
	if [%h1%] gtr [%h2%] set r=1& goto :compare_timestamp_done
	if [%t1%] lss [%t2%] set r=-1& goto :compare_timestamp_done
	if [%t1%] gtr [%t2%] set r=1& goto :compare_timestamp_done
	set r=0

	:compare_timestamp_done
	if [%trace%]==[1] echo %r%
	endlocal & set %result%=%r%
goto :eof
