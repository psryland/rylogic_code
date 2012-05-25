@echo off
setlocal EnableDelayedExpansion
set LF=^


::two empty lines are necessary
set text=
if [%2]==[] (
	set text=%1
) else (
	for %%A in (%*) do call set a=%%~A& set text=!text!!a!!LF!
)

Q:\bin\cex.exe -clip -lwr -bkslash "!text!"
endlocal
