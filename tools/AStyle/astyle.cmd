@echo off
SetLocal

echo Formatting %1
"Q:\tools\astyle\astyle.exe" --options="Q:\tools\astyle\astyle.cfg" %1
EndLocal
::pause