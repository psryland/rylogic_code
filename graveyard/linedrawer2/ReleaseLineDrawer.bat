@echo off

echo Releasing LineDrawer
set dest="Q:\Bin\LineDrawer"
set copier="Q:\Bin\xcopy32.exe"

echo Copy LineDrawer to the dest directory
%copier% "Q:\Paul\Obj\LineDrawer\Release\LineDrawer.exe" %dest%\LineDrawer.exe /D /Y /F /R /K /I

echo Copy the help files
%copier% "Q:\Paul\LineDrawer\Help\LineDrawer.hlp" %dest%\LineDrawer.hlp /D /Y /F /R /K /I
%copier% "Q:\Paul\LineDrawer\Help\LineDrawer.cnt" %dest%\LineDrawer.cnt /D /Y /F /R /K /I
%copier% "Q:\Paul\LineDrawer\Example.pr_script" %dest%\Example.pr_script /D /Y /F /R /K /I
