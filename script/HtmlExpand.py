#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Expand a HTML template file
# Use:
#   HtmlExpand.py $(Fullpath) [outfilepath]
#
#  Supported substitutions:
#  <!--#include file="filepath"-->
#    Substitutes the content of the file 'filepath' at the declared location.
#    'filepath' can be relative to directory of the template file, or a fullpath
#
#  <!--#var name="count" file="filepath" value="const int Count = (?<value>\d+);"-->
#    Declares a variable for later use. Applies a regex match to the contents of 'filepath' to get a definition for 'value'
#    'filepath' can be relative to directory of the template file, or a fullpath
#
#  <!--#value name="count"-->
#    Substitute a previously defined variable called 'count'
#
#  <!--#image file="filepath"-->
#    Substitute a base64 image file 'filepath'

import sys, os, re, base64, io
import Rylogic as Tools
import UserVars

variables = dict()

# Substitute an include file
def SubInclude(kv, indent, srcpath, ofs):
	expected_form = "<!--#include file=\"filepath\"-->"

	# Read the include filepath and check it exists
	m = re.match(r".*file=\"(?P<file>.*?)\".*", kv)
	if not m: raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid include: '"+kv+"'.\nCould not match 'file' field.\nExpected form: "+expected_form)
	fpath = m.group("file");
	fpath = os.path.realpath(fpath if os.path.isabs(fpath) else os.path.join(os.path.split(srcpath)[0], fpath))

	# check the included file exists
	if not os.path.exists(fpath):
		raise FileNotFoundError(Tools.VSLink(srcpath,ofs=ofs) + "Error - File reference not found: "+fpath)

	# Recursively expand this include file
	buf = []
	with open(fpath, encoding="utf-8") as f:
		buf = f.read(-1)

	# Record the new file for error reporting
	result = Expand(buf, fpath)

	# Apply the indent to each line in 'result'
	buf = "";
	for line in io.StringIO(result).readlines():
		buf += indent + line

	return buf

# Extract a variable from a file
def SubVar(kv, indent, srcpath, ofs):
	expected_form = "<!--#var name=\"variable_name\" file=\"filepath\" value=\"regex_pattern_defining_value\"-->"

	# Read the name of the variable
	m = re.match(r".*name=\"(?P<name>\w+)\".*", kv);
	if not m: raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid variable declaration: '"+kv+"'.\nCould not match 'name' field.\nExpected form: " + expected_form)
	name = m.group("name");

	# Read the source filepath
	m = re.match(r".*file=\"(?P<file>.*?)\".*", kv)
	if not m: raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid variable declaration: '"+kv+"'.\nCould not match 'file' field.\nExpected form: " + expected_form)
	fpath = m.group("file")
	fpath = os.path.realpath(fpath if os.path.isabs(fpath) else os.path.join(os.path.split(srcpath)[0], fpath))

	# Check referenced file exists
	if not os.path.exists(fpath):
		raise FileNotFoundError(Tools.VSLink(srcpath,ofs=ofs) + "Error - File reference not found: " + fpath)

	# Read the regex pattern that defines 'value'
	m = re.match(r".*value=\"(?P<pattern>.*?)\".*", kv);
	if not m:
		raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid variable declaration: '"+kv+"'.\nCould not match 'value' field.\nExpected form: " + expected_form)

	# Use the pattern to get the value for the variable
	try:
		pat = m.group("pattern");
		m = Tools.Extract(fpath, pat)
	except:
		raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid variable declaration: '"+kv+"'.\n'value' field is not a valid Regex expression.\nExpected form: " + expected_form)

	if not m:
		raise LookupError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid variable declaration: '"+kv+"'.\n'value' regex expression did not find a match in "+fpath+".\nExpected form: "+expected_form)

	# Save the variable value
	value = m.group("value")
	global variables;
	variables[name] = value
	return ""

# Substitute a variable for its value
def SubValue(kv, indent, srcpath, ofs):
	expected_form = "<!--#value name=\"variable_name\"-->"

	# Read the name of the variable
	m = re.match(r".*name=\"(?P<name>\w+)\".*", kv);
	if not m: raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid value declaration: '"+kv+"'.\nCould not match 'name' field.\nExpected form: " + expected_form)
	name = m.group("name");

	# Lookup the value for the variable
	global variables;
	value = variables.get(name)
	if value is None: raise LookupError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid value declaration: '"+kv+"'.\nVariable with 'name' "+name+" is not defined.\nExpected form: " + expected_form)

	# Return the variable value, indented
	return indent + value;

# Substitute an image with its base64 text
def SubImage(kv, indent, srcpath, ofs):
	expected_form = "<!--#image file=\"filepath\"-->"

	# Read the filename
	m = re.match(r".*file=\"(?P<file>.*?)\".*", kv);
	if not m: raise SyntaxError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Invalid image declaration: '"+kv+"'.\nCould not match 'file' field.\nExpected form: " + expected_form)
	fpath = m.group("file");
	fpath = os.path.realpath(fpath if os.path.isabs(fpath) else os.path.join(os.path.split(srcpath)[0], fpath))

	# check the image file exists
	if not os.path.exists(fpath):
		raise FileNotFoundError(Tools.VSLink(srcpath,ofs=ofs) + "Error - File reference not found: " + fpath)

	# Determine the image type
	extn = []
	try: extn = os.path.splitext(fpath)[1].lower()
	except: raise NotImplementedError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Could not determine image file format from path '"+fpath+"'")

	# Read the image file
	with open(fpath, mode="rb") as img:
		b64 = base64.b64encode(img.read())

		# Return the base64 encoded version
		if extn == ".png":
			return indent + "data:image/png;base64," + b64.decode('utf-8')
		elif extn == ".jpg":
			return indent + "data:image/jpg;base64," + b64.decode('utf-8')
		else:
			raise NotImplementedError(Tools.VSLink(srcpath,ofs=ofs) + "Error - Unsupported image format: "+extn)

# Recursively expand 'buf'. Returns the expanded buffer
# 'srcpath' is the full filepath that 'buf' came from
def Expand(buf, srcpath):

	# General form: [optional leading whitespace]<!--#command key="value" key="value"... -->
	# The pattern matches leading white space which is then inserted before every line in the substituted result.
	# This means includes on lines of their own are correctly tabbed, and <!--inline--> substitutions are also correct
	subst_pattern = re.compile(r"(?P<indent>[ \t]*)<!--#(?P<cmd>\w+)\s+(?P<kv>.*?)-->")

	# Supported substitution keywords
	subst_func = {
		"include" : SubInclude,
		"var"     : SubVar,
		"value"   : SubValue,
		"image"   : SubImage
		}

	s = 0
	while True:
		# Try to match the substitution pattern
		match = subst_pattern.search(buf, s)
		if not match:
			break
		
		indent = match.group("indent")
		cmd    = match.group("cmd")
		kv     = match.group("kv")  # list of key="value" pairs
		result = subst_func[cmd](kv, indent, srcpath, s) # throws if 'cmd' is not a known substitution

		# Replace the match with the substituted text
		buf = buf[:match.start()] + result + buf[match.end():]
		s = match.start()

	# Return the expanded buffer
	return buf

# Expand 'srcpath', writing 'dstpath'
def ExpandHtmlFile(srcpath, dstpath = None):

	# Split the source file path up
	srcdir,file = os.path.split(srcpath)
	fname,extn  = os.path.splitext(file)

	# Handle optional dstpath
	if dstpath is None:
		dstpath = srcdir
	elif not os.path.isabs(dstpath):
		dstpath = os.path.join(srcdir, dstpath)
	if not os.path.isfile(dstpath):
		dstpath = os.path.join(dstpath, fname + ".html")
	dstpath = os.path.realpath(dstpath)

	# Start with a buffer filled from the source file
	buf = []
	with open(srcpath, encoding="utf-8") as f:
		buf = f.read(-1)

	# Expand 'buf'
	buf = Expand(buf, srcpath)

	# Write the expanded buffer to 'dst'
	dstdir,dstfile = os.path.split(dstpath)
	if not os.path.exists(dstdir): os.makedirs(dstdir)
	with open(dstpath, mode='w', encoding="utf-8-sig") as f:
		f.write(buf)

# Stand alone script
if __name__ == "__main__":
	try:
		Tools.CheckVersion(1)

		srcpath = sys.argv[1].lower() if len(sys.argv) > 1 else input("Source htm file? ")
		dstpath = sys.argv[2].lower() if len(sys.argv) > 2 else None

		ExpandHtmlFile(srcpath, dstpath)

	except Exception as ex:
		Tools.OnException(ex)

