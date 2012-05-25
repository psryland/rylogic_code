@echo off
set sln=%1
set clp=/clp:Summary
echo Building x64
call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x64 >nul
msbuild /t:Build /p:Configuration=Release /p:Platform=x64 /m:7 /nologo /v:q %clp% "%sln%"
msbuild /t:Build /p:Configuration=Debug /p:Platform=x64 /m:7 /nologo /v:q %clp% "%sln%"

echo Building x86
call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86 >nul
msbuild /t:Build /p:Configuration=Release /p:Platform=Win32 /m:7 /nologo /v:q %clp% "%sln%"
msbuild /t:Build /p:Configuration=Debug /p:Platform=Win32 /m:7 /nologo /v:q %clp% "%sln%"

echo done.
