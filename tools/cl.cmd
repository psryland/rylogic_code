@echo off
:: Load compilation environment
call "D:\Program Files\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
:: Invoke compiler with any options passed to this batch file
"D:\Program Files\Microsoft Visual Studio 11.0\VC\bin\cl.exe" %*