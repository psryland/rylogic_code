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

import sys, os, re, string, uuid, tempfile
import xml.etree.ElementTree as xml
import xml.dom.minidom as minidom
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import Rylogic as Tools
import UserVars

# Get a UUID suitable for an Id
def Id():
	return str(uuid.uuid1()).replace("-","").upper()

# Add a single file component to 'elem'
def CreateFileComponent(elem:xml.Element, filepath:str, keypath:bool=True, directory_id:str=None):

	# Get the file title from the full filepath
	dir,file = os.path.split(filepath);
	file,ext = os.path.splitext(file);
	file = file.replace(' ','_')
	uid = Id()

	# Create the component to contain the file
	cmp_attr = {}
	cmp_attr["Id"] = "Cmp_"+file+"_"+uid
	cmp_attr["Guid"] = "*"
	if directory_id: cmp_attr["Directory"] = directory_id
	cmp = xml.SubElement(elem, "Component", cmp_attr)

	fcp_attr = {}
	fcp_attr["Id"] = file+"_"+uid
	fcp_attr["Source"] = filepath
	if keypath: fcp_attr["KeyPath"] = "yes"
	fcp = xml.SubElement(cmp, "File", fcp_attr)

	return

# Create a component group in 'frag'
def CreateComponentGroup(elem:xml.Element, id:str, directory:str, filepaths:[]):

	# Create the 'ComponentGroup' XML element
	cg = xml.SubElement(elem, "ComponentGroup", {"Id":id, "Directory":directory})

	# Add each component
	keypath = True
	for filepath in filepaths:
		CreateFileComponent(cg, filepath, keypath)
		keypath = False
	
	return

# Create an XML tree of a WiX fragment by enumerating the files within a directory
# 'dir' is the directory to harvest
# 'install_dir' is the directory Id in the main installer
def HarvestDirectory(dir:str, install_dir:str):

	root = xml.Element("Wix", {"xmlns":"http://schemas.microsoft.com/wix/2006/wi"})
	frag = xml.SubElement(root, "Fragment")

	# The directory structure sub tree
	dr = xml.SubElement(frag, "DirectoryRef", {"Id":install_dir})

	# The component group sub tree
	group_id = os.path.split(dir)[1].replace(' ','_')
	cg = xml.SubElement(frag, "ComponentGroup", {"Id":group_id})

	# Walk the directory recursively
	def WalkDir(dtree:xml.Element, cg:xml.Element, dir:str):
		directory_id = "Dir_" + Id()
		dirname = os.path.split(dir)[1]
		sub = xml.SubElement(dtree, "Directory", {"Id":directory_id, "Name":dirname})
		for d,dnames,fnames in os.walk(dir):
			for fname in fnames:
				CreateFileComponent(cg, os.path.join(d, fname), directory_id=directory_id)
			for dname in dnames:
				WalkDir(sub, cg, os.path.join(d, dname))
			break
		return;

	WalkDir(dr, cg, dir);
	return root;

# Build an installer .msi file
# 'projname' is the name of the application (<projname>Installer_v<version>.msi is returned)
# 'version' is the application version number
# 'installer' is the full path to the main installer.wxs file
# 'projdir' is the root folder of the project
# 'targetdir' is the staging directory from where files are taken for the installer
# 'dstdir' is the output directory for the installer file
# 'harvest' is a list of ['targetdir' relative directory, install directory] pairs.
#   The install directory should be the Id of a <Directory/> in the 'installer'
# Returns the installer full path
def Build(projname:str, version:str, installer:str, projdir:str, targetdir:str, dstdir:str, harvest:[]):

	# Check the installer file exists
	if not os.path.exists(installer):
		raise Exception("'" + installer + "' does not exist")

	# Create a temporary working directory for the 'wixobj' files
	objdir = tempfile.mkdtemp()

	# The .msi filename
	msi_filename = projname + "Installer_v" + version + ".msi"

	# Copy the main installer to the 'objdir' directory so that all the .wsx files are in the same folder
	Tools.Copy(installer, objdir + "\\", quiet=True)
	installer = os.path.split(installer)[1]

	# Collect all the .wsx files to build
	wsx_files = [installer]

	# Create .wsx fragment files for the harvest directories
	for dir,install_dir in harvest:
		root = HarvestDirectory(os.path.join(targetdir, dir), install_dir)
		#Tools.WriteXml(root, "P:\\dump\\" + dir + ".wxs")

		# Save to a temporary file
		wxs_file = dir + ".wxs"
		xml.ElementTree(root).write(objdir + "\\" + wxs_file);

		# Collect the .wxs files
		wsx_files += [wxs_file]

	# Compile all of the .wxs files to object files in 'objdir'
	Tools.Exec([UserVars.wix_candle,
		"-nologo",
		"-dProjDir="+projdir,
		"-dTargetDir="+targetdir,
		"-dProductVersion="+version,
		"-arch", "x86",
		"-out", objdir+"\\"] +
		[objdir+"\\"+f for f in wsx_files])

	# Create the .msi
	Tools.Exec([UserVars.wix_light,
		"-nologo",
		"-dProjDir="+projdir,
		"-dTargetDir="+targetdir,
		"-dProductVersion="+version,
		"-ext", "WixUIExtension",
		"-ext", "WixUtilExtension",
		"-ext", "WixNetFxExtension",
		"-out", os.path.abspath(objdir + "\\" + msi_filename)] +
		[objdir+"\\"+Tools.ChgExtn(f,".wixobj") for f in wsx_files])

	# Copy the .msi file to the destination directory
	Tools.Copy(objdir+"\\"+msi_filename, dstdir+"\\", quiet=True)

	# Clean up
	Tools.ShellDelete(objdir)
	return os.path.abspath(dstdir + "\\" + msi_filename)
