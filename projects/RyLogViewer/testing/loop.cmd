@echo off

for /l %%i in (1, 1, 10000) do (
   echo Line %%ia of Output
   echo Line %%ib of Output
   ping -n 1 -w 200 1.1.1.1 >nul
)

::pause