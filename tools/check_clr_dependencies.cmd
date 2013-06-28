@echo off
cls

set dir=%~1
if [%dir%]==[] (
	set /p target=Assembly directory to check: 
)

Q:\bin\csex\Csex.exe -find_assembly_conflicts -p "%dir%"
pause
