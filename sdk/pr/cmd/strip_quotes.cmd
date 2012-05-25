:: Strip the quotes from a string
::  A routine that will reliably remove quotes from the contents of a variable .
::  This routine will only affects items that both begin AND end with a double quote.
::  e.g. will turn "C:\Program Files\somefile.txt" into C:\Program Files\somefile.txt 
::  while still preserving cases such as Height=5'6" and Symbols="!@#
::
:: Example
::	set a="text file.ext"
::	call strip_quotes.cmd a
::	echo %a%
:strip_quotes
	setlocal
	set v=%1
	call set s=%%%v%%%
	if [!s:~0^,1!]==[^"] (if [!s:~-1!]==[^"] (set s=!s:~1,-1!))
	endlocal & set %v%=%s%
goto :eof