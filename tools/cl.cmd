@echo off
:: Load compilation environment
call "C:\Program Files\Microsoft Visual Studio 9.0\VC\vcvarsall.bat"
:: Invoke compiler with any options passed to this batch file
"C:\Program Files\Microsoft Visual Studio 9.0\VC\bin\cl.exe" %*