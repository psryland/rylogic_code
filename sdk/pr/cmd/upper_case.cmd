:: Convert a string to upper case
::
:: Example
::	set a=StRiNg
::	call upper_case a
::	echo %a%
:: Outputs: STRING
:upper_case
	setlocal
	set v=%~1
	call set p=%%%v%%%
	for %%i in ("a=A" "b=B" "c=C" "d=D" "e=E" "f=F" "g=G" "h=H" "i=I" "j=J" "k=K" "l=L" "m=M" "n=N" "o=O" "p=P" "q=Q" "r=R" "s=S" "t=T" "u=U" "v=V" "w=W" "x=X" "y=Y" "z=Z") do set p=!p:%%~i!
	endlocal & set %v%=%p%
goto :eof
