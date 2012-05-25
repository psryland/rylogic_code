@echo off
echo Building sqlite3.omf.obj using the digital mars compiler dmc
if not exist obj\dmc mkdir obj\dmc
cd obj\dmc
dmc ..\..\sqlite3.c -c -a4 -mn -o -osqlite3.omf.obj
cd ..\..
copy obj\dmc\sqlite3.omf.obj .\lib\ /Y
rmdir /S /Q obj\dmc




