:: Strip the last slash from a string
:: Example
::	set a=C:\Program Files\
::	call remove_trailing_slash.cmd a
::	echo %a%
:: Outputs: C:\Program Files
:remove_trailing_slash
	setlocal
	set v=%1
	call set s=%%%v%%%
	if [!s:~-1!]==[^\] (set s=!s:~0,-1!)
	if [!s:~-1!]==[^/] (set s=!s:~0,-1!)
	endlocal & set %v%=%s%
goto :eof