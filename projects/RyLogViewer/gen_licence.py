#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, re
import xml.etree.ElementTree as xml
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "script")))
import Rylogic as Tools
import UserVars

def GenerateLicence(outfile:str = None):
	print(
		"=============================\n"
		"RyLogViewer Licence Generator\n"
		"=============================\n")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.csex])
	if outfile == None:
		outfile = "licence.xml"

	licence_holder = input(" Licence Holder: ")
	email_address  = input("  Email Address: ")
	company        = input("        Company: ")
	version_mask   = input("Version (1.*.*): ")

	user_details = (
		licence_holder + "\n" +
		email_address  + "\n" +
		company        + "\n" +
		version_mask   + "\n" +
		"Rylogic Limited Is Awesome")

	pk = ".\src\licence\private_key.xml"
	success,key = Tools.Run([UserVars.csex, "-gencode", "-pk", pk, "-data", user_details])
	if not success:
		raise Exception("Key generation failed")

	# Create the licence file
	root = xml.Element("root")
	xml.SubElement(root, "licence_holder") .text = licence_holder
	xml.SubElement(root, "email_address")  .text = email_address
	xml.SubElement(root, "company")        .text = company
	xml.SubElement(root, "versions")       .text = version_mask
	xml.SubElement(root, "activation_code").text = key.strip()
	Tools.WriteXml(root, outfile)
	return

# Run as standalone script
if __name__ == "__main__":
	try:
		GenerateLicence()
		Tools.OnSuccess(pause_time_seconds=0)

	except Exception as ex:
		Tools.OnException(ex)
