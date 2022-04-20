#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# A collection of helper functions for dealing with vcxproj files

import sys, os, re, uuid
import xml.etree.ElementTree as xml
import Rylogic as Tools
import UserVars

# Display help for this script
def ShowHelp():
	print(
"""
Use:
    VSProject <command>

Commands:

  -help, -?, /?
    This help message

  -files <src_root_dir> [<proj_dir>] [<outfile>]
     Create vcxproj files ItemGroup based on a folder, relative to another folder.
     <src_root_dir> = the directory containing the source to be walked.
     <proj_dir> = the directory that files are relative to. (default is the same as 'src_root_dir')
     <outfile> = where to save the generated XML (default prints to stdout)

  -filters <src_root_dir> [<proj_dir>] [<outfile>]
     Create vcxproj.filters based on a folder, relative to another folder
     <src_root_dir> = the directory containing the source to be walked.
     <proj_dir> = the directory that files are relative to. (default is the same as 'src_root_dir')
     <outfile> = where to save the generated XML (default prints to stdout)

""")

# Walk a folder tree generating an 'ItemGroup' for the files
def GenerateProjFiles(src_root:str, proj_root:str, outfile:str=None):
	root = xml.Element("Project", attrib=
	{
		"ToolsVersion": "12.0",
		"xmlns": "http://schemas.microsoft.com/developer/msbuild/2003"
	})
	
	files = xml.SubElement(root, "ItemGroup")
	if proj_root is None:
		proj_root = src_root

	def IncludePath(path:str): return os.path.relpath(path, proj_root)

	# Add item an ItemGroup for the folders
	for path in Tools.EnumFiles(src_root):
		_, extn = os.path.splitext(path.lower())
		if extn == ".h" or extn == ".hpp" or extn == ".hxx" or extn == ".inc":
			xml.SubElement(files, "ClInclude", attrib={"Include": IncludePath(path)})
		elif extn == ".cpp" or extn == ".c" or extn == ".cxx":
			xml.SubElement(files, "ClCompile", attrib={"Include": IncludePath(path)})
		else:
			xml.SubElement(files, "None", attrib={"Include": IncludePath(path)})

	if outfile:
		Tools.WriteXml(root, outfile)
	else:
		print(Tools.FormatXml(root))

	return

# Walk a folder tree generating an 'ItemGroup' for the filters and files
def GenerateFilters(src_root:str, proj_root:str, outfile:str=None):
	root = xml.Element("Project", attrib=
	{
		"ToolsVersion": "4.0",
		"xmlns": "http://schemas.microsoft.com/developer/msbuild/2003"
	})
	
	filters = xml.SubElement(root, "ItemGroup")
	files = xml.SubElement(root, "ItemGroup")
	if proj_root is None:
		proj_root = src_root

	def FilterPath(path:str): return os.path.normpath(os.path.join("src", os.path.relpath(path, src_root)))
	def IncludePath(path:str): return os.path.normpath(os.path.relpath(path, proj_root))

	# Add item an ItemGroup for the folders
	filter = xml.SubElement(filters, "Filter", attrib={"Include": FilterPath(src_root)})
	xml.SubElement(filter, "UniqueIdentifier").text = f"{{{str(uuid.uuid1())}}}"
	for (path,is_dir) in Tools.EnumPaths(src_root):
		if is_dir:
			filter = xml.SubElement(filters, "Filter", attrib={"Include": FilterPath(path)})
			xml.SubElement(filter, "UniqueIdentifier").text = f"{{{str(uuid.uuid1())}}}"
		else:
			_, extn = os.path.splitext(path.lower())
			if extn == ".h" or extn == ".hpp" or extn == ".hxx" or extn == ".inc":
				item = xml.SubElement(files, "ClInclude", attrib={"Include": IncludePath(path)})
			elif extn == ".cpp" or extn == ".c" or extn == ".cxx":
				item = xml.SubElement(files, "ClCompile", attrib={"Include": IncludePath(path)})
			else:
				item = xml.SubElement(files, "None", attrib={"Include": IncludePath(path)})
			xml.SubElement(item, "Filter").text = FilterPath(os.path.dirname(path))

	if outfile:
		Tools.WriteXml(root, outfile)
	else:
		print(Tools.FormatXml(root))

	return

# Entry Point
if __name__ == "__main__":
	
	# Examples:
	#sys.argv = ["", "-files", "S:\\physics\\packages\\pinocchio\\src", "S:\\physics\\build"]
	#sys.argv = ["", "-filters", "S:\\physics\\packages\\pinocchio\\src", "S:\\physics\\build"]
	
	cmd = sys.argv[1] if len(sys.argv) > 1 else ""
	if cmd == "-help" or cmd == "/?" or cmd == "-?":
		ShowHelp()
	elif cmd == "-files":
		src_root = sys.argv[2] if len(sys.argv) > 2 else ""
		proj_root = sys.argv[3] if len(sys.argv) > 3 else None
		outfile = sys.argv[4] if len(sys.argv) > 4 else None
		GenerateProjFiles(src_root, proj_root, outfile)
	elif cmd == "-filters":
		src_root = sys.argv[2] if len(sys.argv) > 2 else ""
		proj_root = sys.argv[3] if len(sys.argv) > 3 else None
		outfile = sys.argv[4] if len(sys.argv) > 4 else None
		GenerateFilters(src_root, proj_root, outfile)
	else:
		print(f"Unknown command {cmd}")

