:: Run a batch file as admin
::  The '%~s0' parameter passes the name of the currently running batch file (in short format)
::  to the subroutine, '%*' passes all arguments for the batch file to the sub routine.
::  If the script doesn't have admin rights '%~s0' will be launched with admin rights and
::  errorlevel 1 will be returned. If admin rights are available, the subroutine does nothing.
:: Example:
::    call runas_admin %~s0 %*
::    if errorlevel 1 goto :eof

:runas_admin
	setlocal
	set target=%1
	set args=
	:admin_rights_concat_args
	shift
	if not [%1]==[] (
		set args=!args! %1
		goto admin_rights_concat_args
	)
	
	::Check for permissions
	net session >nul 2>&1
	if [%errorlevel%] == [0] (
		echo Admin rights available.
	) else (
		:: Generate a vb script to run 'target args' as admin
		echo Set UAC = CreateObject^("Shell.Application"^)          > "%temp%\getadmin.vbs"
		echo UAC.ShellExecute "%target%", "%args%", "", "runas", 1 >> "%temp%\getadmin.vbs"
		
		:: Run it as admin
		"%temp%\getadmin.vbs"
		
		:: Clean up the script
		del "%temp%\getadmin.vbs"
		
		:: Return errorlevel=1 so the caller knows to exit
		exit /B 1
	)
	endlocal
goto :eof
