import sys, os, re, signal
import Rylogic as Tools
import UserVars

#sys.argv = ["", "S:\\physics\\sdk\\pinocchio\\lib\\pinocchio.lib"]
try:
	dumpbin = Tools.Path(UserVars.vs_dir, "VC\\Tools\\MSVC", UserVars.vc_vers, "bin\\Hostx86\\x64\\dumpbin.exe")
	options = ["/SUMMARY"]
	while True:
		r,outp = Tools.Run([dumpbin, "/NOLOGO"] + options + sys.argv[1:])
		print(outp)
		
		# Menu options
		r = input(
			f"1) Quit '(default)'\n" +
			f"2) Exports\n" +
			f"3) Symbols\n" +
			f"4) Dependents\n" +
			f"9) PDB path\n" +
			f"0) All\n" +
			f"> ")
		if r == "1" or r == "":
			break
		if r == "2":
			options = ["/EXPORTS"]
			continue
		if r == "3":
			options = ["/SYMBOLS"]
			continue
		if r == "4":
			options = ["/DEPENDENTS"]
			continue
		if r == "4":
			options = ["/PDBPATH:VERBOSE"]
			continue
		if r == "0":
			options = ["/ALL"]
			continue

except Exception as ex:
	Tools.OnException(ex)