:: Finds the last filename that matches a file pattern
:: and returns the filename with the last number incremented
::
:: Example:
::	call increment_filename.cmd "C:\temp" "ABC????.txt" filename
:: If C:\temp\ contains ABC0023.txt, then %filename%=ABC0024.txt
:increment_filename
	setlocal
	set p1=%~1
	set p2=%~2
	set p3=%3
	set file0=%p2:?=0%
	for %%I in (%file0%) do set file0=%%~NI& set extn=%%~xI
	set file=%file0%
	for /f "tokens=* delims= " %%I in ('dir/b/on %~1\%~2') do set file=%%~NI& set extn=%%~xI& set matched=true
	if [%matched%] neq [true] set file=%file0%& goto :increment_filename2
	
	set file0=%file%
	set file=
	
	:increment_filename0
	set d=%file0:~-1%
	set file0=%file0:~,-1%
	if [%d%] equ [0] set file=1%file%& goto :increment_filename1
	if [%d%] equ [1] set file=2%file%& goto :increment_filename1
	if [%d%] equ [2] set file=3%file%& goto :increment_filename1
	if [%d%] equ [3] set file=4%file%& goto :increment_filename1
	if [%d%] equ [4] set file=5%file%& goto :increment_filename1
	if [%d%] equ [5] set file=6%file%& goto :increment_filename1
	if [%d%] equ [6] set file=7%file%& goto :increment_filename1
	if [%d%] equ [7] set file=8%file%& goto :increment_filename1
	if [%d%] equ [8] set file=9%file%& goto :increment_filename1
	if [%d%] equ [9] set file=0%file%& goto :increment_filename0
	
	:increment_filename1
	set file=%file0%%file%

	:increment_filename2
	endlocal & set %p3%=%file%%extn%
goto :eof
