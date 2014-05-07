#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Copyright Rylogic Ltd 2012
#
# Build shaders using fxc.exe
# Use:
#  BuildShader.py $(Fullpath) [pp] [obj] [debug] [trace]
#
# Expected input is an hlsl file.
# The file is scanned for: PR_RDR_SHADER_VS, PR_RDR_SHADER_PS, etc
# For each symbol found a compiled shader as header data is generated
# in the output directory.
#
# Add 'pp' to the command line for preprocessed output
# Add 'obj' to the command line for a 'compiled shader object' file
#  that can be used with the runtime shader support in the renderer.
#
import sys, os, tempfile
import Rylogic as Tools
import UserVars

try:
	Tools.CheckVersion(1)

	# Check valid command line
	if len(sys.argv) < 2:
		Tools.OnError("No shader filepath given")

	# The full path of the hlsl file to compile
	fullpath = sys.argv[1]

	# Check for optional parameters
	pp    = True if "pp"    in [arg.lower() for arg in sys.argv] else False
	obj   = True if "obj"   in [arg.lower() for arg in sys.argv] else False
	dbg   = True if "debug" in [arg.lower() for arg in sys.argv] else False
	trace = True if "trace" in [arg.lower() for arg in sys.argv] else False
	if trace: print("pp:" + str(pp) + "  obj:" + str(obj))

	# Find the source and output directories
	srcdir,file = os.path.split(fullpath)
	fname,extn  = os.path.splitext(file)
	if trace: print("File: " + fname + extn)

	outdir = srcdir + r"\..\compiled" + (r"\debug" if dbg else r"\release")
	if not os.path.exists(outdir): os.makedirs(outdir)
	if trace: print("Output directory: " + outdir)

	# Scan the file looking for symbols to indicate which shaders to build
	keys = [
		["vs", r"^#if PR_RDR_SHADER_VS$", "/Tvs_4_0"],
		["ps", r"^#if PR_RDR_SHADER_PS$", "/Tps_4_0"],
		["gs", r"^#if PR_RDR_SHADER_GS$", "/Tgs_4_0"],
		]
	for shdr,key,profile in keys:
		if Tools.Extract(fullpath,key):
			if trace: print("Building shader: " + shdr)

			# Create temporary filepaths so that we only overwrite
			# existing files if they've actually changed
			tmp_h_filepath   = tempfile.gettempdir() + "\\" + fname + "." + shdr + ".h.tmp"
			tmp_cso_filepath = tempfile.gettempdir() + "\\" + fname + "." + shdr + ".cso.tmp"
			h_filepath       = outdir + "\\" + fname + "." + shdr + ".h"
			cso_filepath     = outdir + "\\" + fname + "." + shdr + ".cso"
			pp_filepath      = outdir + "\\" + fname + "." + shdr + ".pp"

			# Delete any potentially left over temporary output
			if os.path.exists(tmp_h_filepath):   os.remove(tmp_h_filepath)
			if os.path.exists(tmp_cso_filepath): os.remove(tmp_cso_filepath)
			if os.path.exists(pp_filepath):      os.remove(pp_filepath)

			# Setup the command line for fxc
			# Choose the output files to generate
			output = ["/Fh" + tmp_h_filepath]
			if obj: output += ["/Fo" + tmp_cso_filepath]
			if trace: print("Output: " + str(output))

			# Set the variable name to the name of the file
			varname = ["/Vn" + fname + "_" + shdr]
			if trace: print("Variable Name: " + str(varname))

			# Set include paths
			includes = []#"/I" + srcdir + "\\.."]

			# Set defines
			defines = ["/DSHADER_BUILD=1"]
			if   shdr == "vs": defines = defines + ["/DPR_RDR_SHADER_VS=1"]
			elif shdr == "ps": defines = defines + ["/DPR_RDR_SHADER_PS=1"]
			elif shdr == "gs": defines = defines + ["/DPR_RDR_SHADER_GS=1"]

			# Set other command line options
			options = ["/nologo", "/Gis", "/Ges", "/WX", "/Zpc"]
			
			# Debug build options
			if dbg:
				options += ["/Od", "/Zi"]

			# Build the shader using fxc
			if trace: print("Running fxc.exe...")
			success,output = Tools.Run([UserVars.fxc, fullpath, profile] + varname + output + includes + defines + options, show_arguments=trace)
			if not success:
				print(output)
				print("failed")
			elif trace:
				print(output)
				print("success")

			# Compare the produced files with any existing ones,
			# don't replace the files if they are identical
			# This prevents VS rebuilding all the time.
			if pp:
				print("Preprocessed only")
			elif Tools.DiffContent(tmp_h_filepath, h_filepath):
				Tools.Copy(tmp_h_filepath, h_filepath)
				if os.path.exists(tmp_cso_filepath):
					Tools.Copy(tmp_cso_filepath, cso_filepath)
			elif trace:
				print("Content unchanged")

			# Delete temporary output if still there
			if os.path.exists(tmp_h_filepath):   os.remove(tmp_h_filepath)
			if os.path.exists(tmp_cso_filepath): os.remove(tmp_cso_filepath)

			# Generate preprocessed output
			if pp:
				Tools.Exec([UserVars.fxc, fullpath, "/P"+pp_filepath] + includes + defines + options)
				Tools.Exec([UserVars.root + r"\bin\textformatter.exe", "-f", pp_filepath, "-newlines", "0", "1"])
				Tools.Exec([UserVars.textedit, pp_filepath])

#	if trace:
#		Tools.OnSuccess()

except Exception as ex:
	Tools.OnException(ex)
