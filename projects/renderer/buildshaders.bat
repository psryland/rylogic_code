@echo off
cd "q:/projects/renderer/effects/shaders"

REM check the shaders compile
for %%i in (*.fx)       do fxc /Tfx_2_0 /nologo /Fo"%%i.o" "%%i"
del *.o

REM Generate 'effectdata.h' and 'effectdata.cpp'
Q:\bin\buildshaders -Ocpp "..\effectdata.cpp" -Oh "..\effectdata.h" -sf "..\shaderstobuild.cpp"

REM for %%i in (*.fx *.fxh) do Q:\Paul\Tools\Bin\HeaderFileData.exe -F "%%i" -O "%%i.inc" -V
pause
