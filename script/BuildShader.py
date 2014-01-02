#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Copyright Rylogic Ltd 2012
# Build shaders using fxc.exe
# Use:
#  BuildShader.py $(Fullpath) [pp] [obj]
#  This will compile the shader into a header file in the same directory as $(Fullpath)
import sys, os
import Rylogic as Tools
import UserVars

try:
	if len(sys.argv) < 2:
		Tools.OnError("No shader filepath given")

	fullpath = sys.argv[1]
	trace = False
	
	# Check for optional parameters
	pp = False
	obj = False
	if len(sys.argv) > 2:
		for i in range(2,len(sys.argv)):
			if sys.argv[i] == "pp": pp = True
			if sys.argv[i] == "obj": obj = True

	# Find the directory
	srcdir,file = os.path.split(fullpath)
	fname,extn  = os.path.splitext(file)
	if trace: print("File: " + fname + extn)
	outdir = srcdir + r"\..\compiled"
	if trace: print("Output directory: " + outdir)

	# The filepaths
	tmp_h_filepath   = outdir + "\\" + fname + ".h.tmp"
	tmp_cso_filepath = outdir + "\\" + fname + ".cso.tmp"
	h_filepath       = outdir + "\\" + fname + ".h"
	cso_filepath     = outdir + "\\" + fname + ".cso"
	pp_filepath      = outdir + "\\" + fname + ".pp"

	# The shader to build
	shdr = fname[-2:]
	if trace: print("Shader: " + shdr)

	# Choose the output files to generate
	output = "/Fh" + tmp_h_filepath
	if obj: output += " /Fo" + tmp_cso_filepath

	# Choose the compiler profile based on file extension
	if   shdr == "vs": profile = "/Tvs_5_0"
	elif shdr == "ps": profile = "/Tps_5_0"
	elif shdr == "gs": profile = "/Tgs_5_0"
	else: Tools.OnError("ERROR: Unknown shader type: " + shdr)
	if trace: print("Profile: " + profile)
	
	# Delete potentially left over temporary output
	if os.path.exists(tmp_h_filepath):   os.remove(tmp_h_filepath)
	if os.path.exists(tmp_cso_filepath): os.remove(tmp_cso_filepath)
	if os.path.exists(pp_filepath):      os.remove(pp_filepath)

	# Set the variable name to the name of the file
	varname = "/Vn" + fname[:-3] + "_" + shdr
	if trace: print("Variable Name: " + varname)

	# Set include paths
	includes = ["/I" + srcdir + "\\.."]

	# Set defines
	defines = ["/DSHADER_BUILD=1"]

	# Set other command line options
	options = ["/nologo", "/Gis", "/Ges", "/WX"]

	# Uncomment this for debugging
	#print("Debugging options added")
	#options += ["/Od", "/Zi"]
 
	# Build the shader
	output = Tools.Run([UserVars.fxc, fullpath, profile, output, varname] + includes + defines + options, show_arguments=trace)
	if trace: print(output)

	# Compare the produced files with any existing ones, don't replace the files if they are identical
	# This prevents VS rebuilding all the time.
	if Tools.DiffContent(tmp_h_filepath, h_filepath):
		Tools.Copy(tmp_h_filepath, h_filepath)
		if os.path.exists(tmp_cso_filepath):
			Tools.Copy(tmp_cso_filepath, cso_filepath)

	# Delete temporary output if still there
	if os.path.exists(tmp_h_filepath):   os.remove(tmp_h_filepath)
	if os.path.exists(tmp_cso_filepath): os.remove(tmp_cso_filepath)

	# Generate preprocessed output
	if pp:
		Tools.Exec([UserVars.fxc, fullpath, "/P"+pp_filepath] + includes + defines + options)
		Tools.Exec([UserVars.root + r"\bin\textformatter.exe", "-f", pp_filepath, "-newlines", "0", "1"])
		Tools.Exec([UserVars.textedit, pp_filepath])

	if trace:
		Tools.OnSuccess()

except Exception as ex:
	Tools.OnError("ERROR: " + str(ex))
