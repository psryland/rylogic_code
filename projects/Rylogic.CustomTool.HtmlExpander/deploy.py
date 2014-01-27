#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re, winreg
sys.path.append(os.path.dirname(os.path.realpath(__file__)) + r"\..\..\script")
import Rylogic as Tools
import UserVars

#Requires admin rights
Tools.RunAsAdmin(os.path.realpath(__file__))

print(
	"*************************************************************************\n"
	"HtmlExpander Deploy\n"
	"Copyright Rylogic Limited 2013\n"
	"*************************************************************************")

Tools.CheckVersion(1)

binname = "Rylogic.CustomTool.HtmlExpander.dll"
srcdir = UserVars.root + r"\projects\Rylogic.CustomTool.HtmlExpander"
dstdir = UserVars.root + r"\bin\custom_tools"
config = input("Configuration (debug, release(default))? ")
if config == "": config = "release"

proj = srcdir + r"\Rylogic.CustomTool.HtmlExpander.sln"
regasm = r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe"

input(
	" Deploy Settings:\n"
	"         Source: " + srcdir + "\n"
	"    Destination: " + dstdir + "\n"
	"  Configuration: " + config + "\n"
	"Press enter to continue")

try:
	#Invoke MSBuild
	print("Building...")
	Tools.Exec([UserVars.msbuild, proj, "/t:Rebuild", "/p:Configuration="+config])

	#Ensure directories exist and are empty
	if os.path.exists(dstdir): shutil.rmtree(dstdir)
	os.makedirs(dstdir)

	#Copy build products to dst
	print("Copying files to " + dstdir)
	Tools.Copy(srcdir + "\\bin\\" + config + "\\Rylogic.CustomTool.HtmlExpander.dll", dstdir)
	
	#Register with COM
	print("Registering with COM")
	Tools.Exec([regasm, "/codebase", dstdir + "\\Rylogic.CustomTool.HtmlExpander.dll"], show_arguments=True)

	#Add the necessary VS registry entries
	print("Adding a VS registry entry")
	with winreg.CreateKey(winreg.HKEY_CURRENT_USER, "SOFTWARE\\Microsoft\\VisualStudio\\11.0_Config\\CLSID\\{a72434a4-0a4d-4cc2-aba2-42e1fcd61db4}\\") as key:
		winreg.SetValueEx(key, "InprocServer32", 0, winreg.REG_SZ, "C:\\Windows\\System32\\mscoree.dll")
		winreg.SetValueEx(key, "ThreadingModel", 0, winreg.REG_SZ, "Both")
		winreg.SetValueEx(key, "Class", 0, winreg.REG_SZ, "Rylogic.CustomTool.HtmlExpander")
		winreg.SetValueEx(key, "Assembly", 0, winreg.REG_SZ, "Rylogic.CustomTool.HtmlExpander, Version=1.0.0.0, Culture=neutral")
		winreg.SetValueEx(key, "CodeBase", 0, winreg.REG_SZ, "file:///"+UserVars.root+"\\bin\\custom_tools\\Rylogic.CustomTool.HtmlExpander.dll")
	with winreg.CreateKey(winreg.HKEY_CURRENT_USER, "Software\\Microsoft\\VisualStudio\\11.0_Config\\Generators\\{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}\\HtmlExpander\\") as key:
		winreg.SetValueEx(key, "CLSID", 0, winreg.REG_SZ, "{a72434a4-0a4d-4cc2-aba2-42e1fcd61db4}")
		winreg.SetValueEx(key, "GeneratesDesignTimeSource", 0, winreg.REG_DWORD, 1)
		winreg.SetValueEx(key, "@", 0, winreg.REG_SZ, "Rylogic template tool for expanding html files containing server side includes/commands")

	Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
