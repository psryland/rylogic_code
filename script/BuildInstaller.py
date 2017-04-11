#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Use:
#  BuildInstaller.py $(TargetDir)
#
# This script creates a '<projname>Installer.msi' using the WIX tool kit.
#
# There are a number of ways I could have used WIX. Here are the options and reasons for the current system:
# 1) Install the WIX tool kit as a visual studio extension
#    => maintaining a .wsx file for each project that an installer is needed for.
#    - Two projects per application, one to build the .msi and another to build the .exe
#    - Requires anyone who wants to build the project to also install it. Projects are unsupported otherwise
#    - Doesn't solve the problem of automatically adding doc files, updating version numbers, etc
#
# 2) Maintain a .wsx file for each project, build from the deploy.py script using WIX command line tools.
#    + Tools are in SDK, anyone with the code also has the tools to build the installer
#    - Requires manually adding files, and updating version numbers
#
# 3) Use a text template file to generate the .wsx file
#   => might as well use python, easier to debug/maintain
#
# 4) Use a python script to generate the .wsx file, then build it
#   + One-click build of installer
#   + Automatically gets the correct files and version number
#   + Can be invoked from the deploy script
#   + Can be parametrized on the project name
#   + Can be debugged using VS
#   - It's gunna be a mofo of a script...

import sys, os, re, string, uuid
import xml.etree.ElementTree as xml
import xml.dom.minidom as minidom
sys.path.append(re.sub(r"(\w:[\\/]).*", r"\1script", __file__))
import Rylogic as Tools
import UserVars

# Add a single file component to 'elem'
def CreateFileComponent(elem:xml.Element, filepath:str):

	# Get the file title from the full filepath
	dir,file = os.path.split(filepath);
	file,ext = os.path.splitext(file);
	uid = str(uuid.uuid1()).replace("-","").upper()

	# Create the component to contain the file
	cmp = xml.SubElement(elem, "Component", {"Id":"Cmp_"+uid, "Guid":"*"})
	fcp = xml.SubElement(cmp, "File", {"Id":file+"_"+uid, "KeyPath":"yes", "Vital":"yes", "Source":filepath})

	return

# Create a component group in 'frag'
def CreateComponentGroup(frag:xml.Element, id:str, directory:str, filepaths:[]):

	# Create the 'ComponentGroup' XML element
	cg = xml.SubElement(frag, "ComponentGroup", {"Id":id, "Directory":directory})

	# Add each component
	for filepath in filepaths:
		CreateFileComponent(cg, filepath)
	
	return

# Create files.wxs
def CreateFilesWXS(targetdir:str, objdir:str):

	# Generate a .wxs file containing fragments for the files to include in the install
	root = xml.Element("Wix", {"xmlns":"http://schemas.microsoft.com/wix/2006/wi"})
	frag = xml.SubElement(root, "Fragment")
	xml.SubElement(frag, "DirectoryRef", {"Id":"INSTALLFOLDER"})
	xml.SubElement(frag, "DirectoryRef", {"Id":"lib_x86"})
	xml.SubElement(frag, "DirectoryRef", {"Id":"lib_x64"})
	xml.SubElement(frag, "DirectoryRef", {"Id":"doc"})

	# Create a component group for the binary files
	dir = targetdir + "\\"
	filepaths = [dir+f for f in os.listdir(dir) if f.lower().endswith(".dll")]
	CreateComponentGroup(frag, "binaries", "INSTALLFOLDER", filepaths);

	# Create a component group for the native 32-bit dlls
	dir = targetdir + "\\lib\\x86\\"
	filepaths = [dir+f for f in os.listdir(dir)]
	CreateComponentGroup(frag, "binaries_x86", "lib_x86", filepaths)

	# Create a component group for the native 32-bit dlls
	dir = targetdir + "\\lib\\x64\\"
	filepaths = [dir+f for f in os.listdir(dir)]
	CreateComponentGroup(frag, "binaries_x64", "lib_x64", filepaths)

	# Create a component group for the documentation files
	dir = targetdir + "\\doc\\"
	filepaths = [dir+f for f in os.listdir(dir)]
	CreateComponentGroup(frag, "doc_files", "doc", filepaths)

	# Save the .wxs file
	wxs_files = objdir + "\\files.wxs"
	xml.ElementTree(root).write(wxs_files);
	#Tools.WriteXml(root, r"R:\dump\test.xml")

	return wxs_files

# Build the .msi file for 'projname'
def BuildInstaller(projname:str):

	print("\nCreating Installer for " + projname)

	# Check the paths in UserVars are valid
	Tools.CheckVersion(4)

	slndir    = UserVars.root + "\\PC\\RexConfig"
	projdir   = slndir + "\\" + projname
	config    = "Release"
	targetdir = projdir + "\\bin\\" + config
	objdir    = projdir + "\\obj"
	installer = projdir + "\\installer\\installer.wxs"
	ui_name   = Tools.StrTxfm(projname, word_sep = Tools.ESeparate.Add, sep = " ")
	version   = Tools.Extract(slndir + "\\AssemblyVersion.cs", "AssemblyVersion\(\"(.*)\"\)").group(1)

	# Check the installer files exist
	if not os.path.exists(installer):
		raise Exception("installer.wsx file does not exist for project " + projname)
	else:
		Tools.Copy(installer, objdir) # make a copy of the installer file so we can update the version
		installer = objdir + "\\installer.wxs"

	# Update the parameters in the temporary copy of installer.wsx
	Tools.UpdateFileByLine(installer, r'define\s+ProjDir\s*=\s*".*"', "define ProjDir = \"" + projdir + "\"", False)
	Tools.UpdateFileByLine(installer, r'define\s+ProductVersion\s*=\s*"\d\.\d\.\d\.\d"', "define ProductVersion = \"" + version + "\"", False)

	msi_filepath   = targetdir + "\\" + projname + "Installer_v" + version + ".msi"
	wxs_files = CreateFilesWXS(targetdir, objdir);

	# Compile all of the .wxs files
	Tools.Exec([UserVars.root + "\\SDK\\Wix\\candle.exe",
		"-nologo",
		"-arch","x86",
		"-out",objdir+"\\",
		installer,
		wxs_files,
		])

	# Create the .msi
	Tools.Exec([UserVars.root + "\\SDK\Wix\\light.exe",
		"-nologo",
		"-ext", "WixUIExtension",
		"-ext", "WixUtilExtension",
		"-ext", "WixNetFxExtension",
		"-out", msi_filepath,
		objdir + "\\installer.wixobj",
		objdir + "\\files.wixobj",
		])

	return msi_filepath

# Run as standalone script
#if __name__ == "__main__":
#	BuildInstaller("TEMPLATE")
