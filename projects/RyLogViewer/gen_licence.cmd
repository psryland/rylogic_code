@echo off
SetLocal EnableDelayedExpansion 
set PATH=Q:\sdk\pr\cmd\;%PATH%
cls
echo =================
echo RyLogViewer Licence Generator
echo =================
echo.

set csex=Q:\bin\csex\csex.exe
set pk=.\src\licence\private_key.xml

"%csex%" -gencode -pk "%pk%"

endlocal
pause