:: Helpful batch file sites:
::	http://ss64.com/nt/syntax.html
::	http://www.robvanderwoude.com/battech.php

@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls

echo *********************
echo Test Batch File
echo *********************
echo.

echo Test: newlines
set LF=^


::two empty lines are necessary
echo A string across!LF!multiple lines
echo.

echo Test: wait
echo Waiting for 2 seconds
call wait 2000
echo.

echo Test: timestamp
set timestamp=%date:~-4,4%-%date:~-7,2%-%date:~-10,2% - %time%
echo Time stamp : %timestamp%
echo Some random number : %random%

set a=headSTRINGtail
set b=%a:head=%
set c=%a:*STRING=%
call set a_trim=%%b:%c%=%%
echo %a% trimmed to %a_trim%
echo.

echo Test: find_number
set a=Abc00142
call find_number a num
echo %num%
echo.

echo Test: strip_quotes
set a="file without quotes.ext"
call strip_quotes a
echo %a%
echo.

echo Test: remove_trailing_slash
set a=C:\Program Files\
call remove_trailing_slash a
echo %a%
set a=C:/dir/
call remove_trailing_slash a
echo %a%
set a=C:\no_slash
call remove_trailing_slash a
echo %a%
echo.

@echo on
echo Test: split_path
set a=rules.Paul
call split_path a directory file extn
echo %extn%.%file%
set a=C:\tmp folder\file.extn
call split_path a directory file extn 
echo %directory%\%file%.%extn%
echo.

echo Test: increment_filename
call increment_filename "W:\release" "MC_2.10.00.R.????.hex" filename
echo next filename: %filename%
echo.

echo Test: lower_case
set a=A MiXeD CaSe StRiNg
call lower_case a
echo %a%
echo.

echo Test: upper_case
set a=A MiXeD CaSe StRiNg
call upper_case a
echo %a%
echo.

echo Test: copy
call copy test.cmd test_copy.cmd /Y /F /D
if exist test_copy.cmd echo Copy succeeded
if not exist test_copy.cmd echo Copy Failed
call copy test.cmd test_copy.cmd /Y /F /D
del test_copy.cmd
echo.

goto :eof


