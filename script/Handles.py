import sys, os, re, signal
import Rylogic as Tools
import UserVars

try:
	while True:
		handles = os.path.join(UserVars.root, "tools", "handle.exe")
		r,outp = Tools.Run([handles] + sys.argv[1:])
		print(outp)
		
		found = "No matching handles found." not in outp
		pids = []
		if found:
			for line in outp.splitlines():
				m = re.search(r"pid:\s*(\d+)", line)
				if m: pids.append(m.group(1))

		r = input(
			"\n1) Rescan "   + ("(default)" if found else "") +
			"\n2) Kill all " + (f"({','.join(pids)})" if found else "") + 
			"\n3) Quit "     + ("(default)" if not found else "") +
			"\n> ")
		
		if r == "1":
			continue
		if r == "2":
			for id in pids:
				os.kill(int(id), signal.SIGINT)
		if r == "3":
			break
		if r == "":
			if found:
				continue
			else:
				break

except Exception as ex:
	Tools.OnException(ex)