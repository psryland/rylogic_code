@echo off

for /l %%i in (1, 1, 100) do (
   echo Line %%i of Output
   ping -n 1 -w 1000 1.1.1.1 >nul
)

::pause