#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Notes:
#  - This script creates the UserVars.py file.
#  - It cannot make use of scripts in 'repo/script' because the UserVars.py file may not exist yet in a clean build.
import os, sys, urllib.request, zipfile
wix_dir = os.path.abspath(os.path.join(os.path.dirname(__file__)))
wix_url = "https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311-binaries.zip"

# Download the WiX tools if not already there
def GetWiX():
	# Assume already installed
	if os.path.exists(os.path.join(wix_dir, "candle.exe")):
		return

	# Download WiX zip
	wix_zip = os.path.join(wix_dir, "wix.zip")
	urllib.request.urlretrieve(wix_url, wix_zip)

	# Extract to 'wix_dir'
	with zipfile.ZipFile(wix_zip, 'r') as zf:
		zf.extractall(wix_dir)

	return

# Entrt point
if __name__ == "__main__":
	try:
		GetWiX()
	except Exception as ex:
		print(f"ERROR: {str(ex)}")
		sys.exit(-1)
