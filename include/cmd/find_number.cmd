:: Finds a decimal number at the end of a string
::
:: Example:
::	set a=Abc00142
::	call find_number.cmd a num
::	echo %num%
:: Outputs 142
:find_number
	setlocal
	set p1=%1
	set p2=%2
	call set v=%%%p1%%%
	
	:: Extract the number from the end of the first parameter
	:find_number0
	if [%v%] equ [] goto :find_number1
	set d=%v:~-1%
	if [%d%] equ [0] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [1] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [2] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [3] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [4] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [5] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [6] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [7] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [8] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [9] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [+] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	if [%d%] equ [-] set n=%d%%n%& set v=%v:~,-1%& goto :find_number0
	
	:: Remove any leading 0s
	:find_number1
	if [%n:~0,1%] equ [0] set n=%n:~1%& goto :find_number1
	
	:: Write the number to the second parameter
	:find_number2
	endlocal & set %p2%=%n%

goto :eof