import sys, os, re, signal
import Rylogic as Tools
import UserVars

try:
	handles = Tools.Path(UserVars.root, "tools\\handle\\handle64.exe")
	while True:
		r,outp = Tools.Run([handles] + sys.argv[1:])
		print(outp)
		
		found = "No matching handles found." not in outp
		pids = []
		if found:
			for line in outp.splitlines():
				m = re.search(r"pid:\s*(\d+)", line)
				if m: pids.append(m.group(1))

		# Menu options
		r = input(
			f"1) Quit {'(default)' if not found else ''}\n" +
			f"2) Rescan {'(default)' if found else ''}\n" +
			f"3) Kill all: {','.join(pids)}\n" + 
			f"> ")
		if r == "1" or (r == "" and not found):
			break
		if r == "2" or (r == "" and found):
			continue
		if r == "3":
			for id in pids:
				os.kill(int(id), signal.SIGINT)

except Exception as ex:
	Tools.OnException(ex, enter_to_close=True)