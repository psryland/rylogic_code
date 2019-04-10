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
import Rylogic as Tools
import UserVars

# Get a UUID suitable for an Id
def Id():
	return str(uuid.uuid1()).replace("-","").upper()

# Add a single file component to 'elem'
def CreateFileComponent(elem:xml.Element, filepath:str, keypath:bool=True, directory_id:str=None):

	# Get the file title from the full filepath
	_,file = os.path.split(filepath)
	file,_ = os.path.splitext(file)
	file = re.sub(r'[^0-9a-zA-Z_]','_', file)
	uid = Id()

	# Create the component to contain the file
	cmp_attr = {}
	cmp_attr["Id"] = f"Cmp_{file}_{uid}"
	cmp_attr["Guid"] = "*"
	if directory_id: cmp_attr["Directory"] = directory_id
	cmp = xml.SubElement(elem, "Component", cmp_attr)

	fcp_attr = {}
	fcp_attr["Id"] = f"{file}_{uid}"
	fcp_attr["Source"] = os.path.abspath(filepath)
	if keypath: fcp_attr["KeyPath"] = "yes"
	_ = xml.SubElement(cmp, "File", fcp_attr)

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
# 'group_id' is the corresponding ComponentGroupRef for the feature
# 'dir' is the directory to harvest
# 'recursive' is true if directories should be harvested recursively
# 'regex_filters' is a collection of filters for the files to include
# 'install_dir' is the directory Id in the main installer
def HarvestDirectory(group_id:str, dir:str, recursive:bool, regex_filters:[str], install_dir:str):

	root = xml.Element("Wix", {"xmlns":"http://schemas.microsoft.com/wix/2006/wi"})
	frag = xml.SubElement(root, "Fragment")

	# The directory structure sub tree
	dr = xml.SubElement(frag, "DirectoryRef", {"Id":install_dir})

	# The component group sub tree
	cg = xml.SubElement(frag, "ComponentGroup", {"Id":group_id})

	# Walk the directory recursively
	def WalkDir(dtree:xml.Element, cg:xml.Element, dir:str):
		for fname in os.listdir(dir):
			path = os.path.abspath(os.path.join(dir, fname))
			if os.path.isfile(path):
				if len(regex_filters) != 0 and not any(re.match(f, fname, 0) for f in regex_filters): continue
				CreateFileComponent(cg, path, True, dtree.attrib['Id'])
			if os.path.isdir(path) and recursive:
				sub = xml.SubElement(dtree, "Directory", {"Name":fname, "Id":f"Dir_{fname}_{Id()}"})
				WalkDir(sub, cg, path)
		return

	WalkDir(dr, cg, dir)
	return root

# Build an installer .msi file
# 'projname' is the name of the application (<projname>Installer_v<version>.msi is returned)
# 'version' is the application version number
# 'installer' is the full path to the main installer.wxs file
# 'projdir' is the root folder of the project (passed as a define to the installer.wxs, used as a reference directory)
# 'targetdir' is the staging directory from where files are taken for the installer
# 'dstdir' is the output directory for the installer file
# 'harvest' is a tuple list used to add files from a directory:
#   [group_id, install_directory, targetdir_relative_directory, recursive, (optional) regex_filters...]
#   'group_id' is the name of the corresponding 'ComponentGroupRef' element in the 'Feature' element of the installer.wxs
#   'install_directory' is the id of the directory that will contain the harvested files
#   'targetdir_relative_directory' is the relative path of where to harvest from.
#   'recursive' should be True to search directories recursively
#   'regex_filters' is used to match specific filenames to add to the group (no filters means add all)
# Returns the installer full path
def Build(projname:str, version:str, installer:str, projdir:str, targetdir:str, dstdir:str, harvest:[]):

	# Notes:
	#  - The installer.wxs file should define the basic directory layout for the install plus
	#    the main executeables or files with additional shortcuts etc. Supplimental files (dlls etc)
	#    can be provided by the 'harvest' argument.
	#  - The 'group_id' is used to identify blocks of files that are included in the MSI by the
	#    'Feature' element in the installer.wxs

	# Check the installer file exists
	if not os.path.exists(installer):
		raise Exception(f"'{installer}' does not exist")

	# Create a temporary working directory for the 'wixobj' files
	objdir = tempfile.mkdtemp()

	# The .msi filename
	msi_filename = f"{projname}Installer_v{version}.msi"

	# Copy the main installer to the 'objdir' directory so that all the .wsx files are in the same folder
	Tools.Copy(installer, os.path.join(objdir, ""), quiet=True)
	installer = os.path.split(installer)[1]

	# Collect all the .wsx files to build
	wsx_files = [installer]

	# Create .wsx fragment files for the harvest files and directories
	for h in harvest:
		group_id = h[0]
		install_dir = h[1]
		relative_dir = h[2]
		recursive = h[3]
		regex_filters = h[4:]

		# The name of the wix file for this group
		wxs_file = f"{group_id}.wxs"
		
		# Harvest files
		root = HarvestDirectory(group_id, os.path.join(targetdir, relative_dir), recursive, regex_filters, install_dir)
		#Tools.WriteXml(root, os.path.join(UserVars.dumpdir, wxs_file))
		#print(f"HACK - Writing temp wix: {os.path.join(UserVars.dumpdir, wxs_file)}")

		# Save to a temporary file
		xml.ElementTree(root).write(os.path.join(objdir, wxs_file))

		# Collect the .wxs files
		wsx_files.append(wxs_file)

	# Compile all of the .wxs files to object files in 'objdir'
	Tools.Exec([UserVars.wix_candle,
		"-nologo",
		"-dProjDir="+projdir,
		"-dTargetDir="+targetdir,
		"-dProductVersion="+version,
		"-arch", "x64",
		"-out", os.path.join(objdir, "")] +
		[os.path.join(objdir, f) for f in wsx_files])

	# Create the .msi
	Tools.Exec([UserVars.wix_light,
		"-nologo",
		"-dProjDir="+projdir,
		"-dTargetDir="+targetdir,
		"-dProductVersion="+version,
		"-ext", "WixUIExtension",
		"-ext", "WixUtilExtension",
		"-ext", "WixNetFxExtension",
		"-out", os.path.abspath(os.path.join(objdir, msi_filename))] +
		[os.path.join(objdir, Tools.ChgExtn(f,".wixobj")) for f in wsx_files])

	# Copy the .msi file to the destination directory
	Tools.Copy(os.path.join(objdir, msi_filename), os.path.join(dstdir, ""), quiet=True)

	# Clean up
	Tools.ShellDelete(objdir)
	return os.path.abspath(os.path.join(dstdir, msi_filename))
