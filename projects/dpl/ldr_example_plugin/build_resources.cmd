@echo off
::set ConfigurationName=debug
set rc=C:\Program Files\Microsoft SDKs\Windows\v6.1\Bin\rc.exe
set includes=^
C:\Program Files\Microsoft Visual Studio 9.0\VC\include;^
C:\Program Files\Microsoft SDKs\Windows\v6.1\Include;
	
"%rc%" -i"%includes%" ui.rc
pause

