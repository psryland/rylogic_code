import sys, os
sys.path += [os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "script"))]
import Rylogic as Tools
import UserVars

root = Tools.Path(os.path.dirname(__file__))
inf2cat = Tools.Path(UserVars.winsdk, "bin", UserVars.winsdkvers, "x86", "Inf2Cat.exe")
signtool = Tools.Path(UserVars.winsdk, "bin", UserVars.winsdkvers, "x64", "signtool.exe")

# Entry Point
if __name__ == "__main__":
	try:
		catfile = Tools.Path(root, "rylogic_driver.cat")
		os.remove(catfile)
		
		# Create the 'cat' file
		Tools.Exec([inf2cat, f"/driver:{root}", "/os:7_X64,7_X86,8_X64,8_X86,10_X64,10_X86", "/verbose"])

		# Sign
		pw = input("Code Signing Cert Password: ")
		Tools.Exec([signtool, "sign", "/f", UserVars.code_sign_cert_pfx, "/p", pw, "/t", "http://timestamp.verisign.com/scripts/timestamp.dll", catfile])

	except Exception as ex:
		print(f"ERROR: {str(ex)}")
		sys.exit(-1)
