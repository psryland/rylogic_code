@echo off
SetLocal EnableDelayedExpansion
set PATH=Q:\sdk\pr\cmd;%PATH%
cls

call copy "q:\bin\linedrawer.zip"  "Y:\WWW\WWW-pub\data\" /F /Y /D
call copy "q:\bin\imager.x86.zip"  "Y:\WWW\WWW-pub\data\" /F /Y /D
call copy "q:\bin\imager.x64.zip"  "Y:\WWW\WWW-pub\data\" /F /Y /D
call copy "q:\bin\clicket.zip"     "Y:\WWW\WWW-pub\data\" /F /Y /D

EndLocal

