:: Split a path into drive_directory, file title, and file extension
::
:: Example
::	set a=C:\tmp folder\file.extn
::	call split_path a path file extn 
::	echo %path%\%file%.%extn%
:: Outputs: C:\tmp folder\file.extn
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
