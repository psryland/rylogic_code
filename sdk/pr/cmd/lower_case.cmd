:: Convert a string to lower case
::
:: Example
::	set a=A MiXeD CaSe StRiNg
::	call lower_case a
::	echo %a%
:: Outputs: string

:lower_case
	setlocal
	set v=%~1
	call set p=%%%v%%%
	for %%i in ("A=a" "B=b" "C=c" "D=d" "E=e" "F=f" "G=g" "H=h" "I=i" "J=j" "K=k" "L=l" "M=m" "N=n" "O=o" "P=p" "Q=q" "R=r" "S=s" "T=t" "U=u" "V=v" "W=w" "X=x" "Y=y" "Z=z") do set p=!p:%%~i!
	endlocal & set %v%=%p%
goto :eof
