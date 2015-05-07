import sys, os, re
import Rylogic as Tools
import UserVars

try:
	retry = 'y'
	while retry == 'y':
		Tools.Exec([UserVars.root + "\\tools\\handle.exe"] + sys.argv[1:])
		retry = input("Retry (y/n)? ")

except Exception as ex:
	Tools.OnException(ex)