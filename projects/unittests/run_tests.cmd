if errorlevel 1 goto :error
set tests_exe=%~1
if exist "%tests_exe%" (
	"%tests_exe%" runtests
)
goto :eof
:error
echo Tests not run