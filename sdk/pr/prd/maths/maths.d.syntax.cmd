set PATH=D:\dlang\MinGW\bin;%PATH%
echo Compiling maths\maths.d...
gdc -m32 -g -fno-inline-functions -fdebug -c -fsyntax-only maths\maths.d
:reportError
if errorlevel 1 echo Building obj\prd\Win32\Debug\maths-maths.o failed!
if not errorlevel 1 echo Compilation successful.
