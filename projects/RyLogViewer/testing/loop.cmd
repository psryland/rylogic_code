@echo off

for /l %%i in (1, 1, 10000) do (
   echo Line %%i of Output
   ping -n 1 -w 200 1.1.1.1 >nul
)

::pause