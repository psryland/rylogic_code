@echo off
SetLocal EnableDelayedExpansion
set PATH=Q:\sdk\pr\cmd;%PATH%
cls

xcopy "q:\bin\linedrawer.zip"  "Y:\WWW\WWW-pub\data\" /F /Y /D
xcopy "q:\bin\imager.x86.zip"  "Y:\WWW\WWW-pub\data\" /F /Y /D
xcopy "q:\bin\imager.x64.zip"  "Y:\WWW\WWW-pub\data\" /F /Y /D
xcopy "q:\bin\clicket.zip"     "Y:\WWW\WWW-pub\data\" /F /Y /D

EndLocal

