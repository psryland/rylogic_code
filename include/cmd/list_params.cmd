:: Outputs the commandline parameters with each on a new line
:: This is handy for copying the paths of selected files (for example)
:: More of a demonstration of how to do it
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

echo !text!
endlocal
