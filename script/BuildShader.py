#!/usr/bin/env python
# -*- coding: utf-8 -*- 
# Copyright Rylogic Ltd 2012
#
# Build shaders using fxc.exe
# Use:
#  BuildShader.py $(Fullpath) $(PlatformTarget) $(Configuration) [pp] [obj] [dbg] [trace]
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
	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root, UserVars.winsdk, UserVars.textedit])

	fxc = UserVars.winsdk + r"\bin\x86\fxc.exe"

	trace = False
	if trace:
		print("Args: " + str(sys.argv));

	# The full path of the hlsl file to compile
	fullpath = (sys.argv[1] if len(sys.argv) > 1 else input("Shader File? ")).lower()
	platform = (sys.argv[2] if len(sys.argv) > 2 else "any"                 ).lower()
	config   = (sys.argv[3] if len(sys.argv) > 3 else "release"             ).lower()

	# Check for optional parameters
	pp    = True if "pp"    in [arg.lower() for arg in sys.argv] else False
	obj   = True if "obj"   in [arg.lower() for arg in sys.argv] else False
	dbg   = True if "dbg"   in [arg.lower() for arg in sys.argv] else False
	trace = True if "trace" in [arg.lower() for arg in sys.argv] else False

	# Enable compiled shader objects in debug, for debugging and runtime shaders
	if dbg:
		obj = True;

	# Show the command line options
	if trace:
		print("pp:" + str(pp) + "  obj:" + str(obj) + "  debug:" + str(dbg))

	# Find the source and output directories
	srcdir,file = os.path.split(fullpath)
	fname,extn  = os.path.splitext(file)
	if trace: print("File: " + fname + extn)

	outdir = UserVars.root + "\\projects\\renderer11\\shaders\\hlsl\\compiled\\" + config
	if not os.path.exists(outdir): os.makedirs(outdir)
	if trace: print("Output directory: " + outdir)

	keys = [
		["vs", "/Tvs_4_0", r"^#ifdef PR_RDR_VSHADER_(?P<name>.*)$"],
		["ps", "/Tps_4_0", r"^#ifdef PR_RDR_PSHADER_(?P<name>.*)$"],
		["gs", "/Tgs_4_0", r"^#ifdef PR_RDR_GSHADER_(?P<name>.*)$"],
		]

	# Scan the file looking for VS, then GS, then PS shaders
	for shdr,profile,patn in keys:
		
		# For each matching instance, build the shader
		shaders = Tools.ExtractMany(fullpath,patn)
		for m in shaders:

			shdr_name = m.group("name") + "_" + shdr
			if trace: print("Building: " + shdr_name)

			# Create temporary filepaths so that we only overwrite
			# existing files if they've actually changed. Create temp
			# files for each unique platform/config to allow parallel build
			tempdir = tempfile.gettempdir() + "\\" + platform + "\\" + config;
			filepath_h   = tempdir + "\\" + shdr_name + ".h"
			filepath_cso = tempdir + "\\" + shdr_name + ".cso"

			# Delete any potentially left over temporary output
			os.makedirs(tempdir, exist_ok=True);
			if os.path.exists(filepath_h):   os.remove(filepath_h)
			if os.path.exists(filepath_cso): os.remove(filepath_cso)

			# Setup the command line for fxc
			# Choose the output files to generate
			output = ["/Fh" + filepath_h]
			if obj: output += ["/Fo" + filepath_cso]
			if trace: print("Output: " + str(output))

			# Set the variable name to the name of the shader
			varname = ["/Vn" + shdr_name]
			if trace: print("Variable Name: " + str(varname))

			# Set include paths
			includes = []#"/I" + srcdir + "\\.."]

			# Set defines
			selected = "PR_RDR_"+shdr.upper()+"HADER_"+shdr_name[:-3]
			defines = ["/DSHADER_BUILD", "/D"+selected]

			# Set other command line options
			options = ["/nologo", "/Gis", "/Ges", "/WX", "/Zpc"]
			
			# Debug build options
			# For some reason, the /Zi option causes the output to be different each time it's built using fxc in the windows 8.1 kit...
			if dbg:
				options += ["/Gfp", "/Od"]#, "/Zi"]

			if not pp:
				# Build the shader using fxc
				if trace: print("Running fxc.exe...")
				success,output = Tools.Run([fxc, fullpath, profile] + varname + output + includes + defines + options, show_arguments=trace)
				if not success:
					print("Compiling: " + fullpath)
					print(output)
					print("failed")
				elif trace:
					print(output)
					print("success")

				out_filepath_h   = outdir + "\\" + shdr_name + ".h"
				out_filepath_cso = outdir + "\\" + shdr_name + ".cso"

				# Compare the produced files with any existing ones, don't replace the files if they are identical.
				# This prevents VS rebuilding all the time.
				if Tools.DiffContent(filepath_h, out_filepath_h,trace=trace):
					Tools.Copy(filepath_h, out_filepath_h)
					if os.path.exists(filepath_cso):
						Tools.Copy(filepath_cso, out_filepath_cso)
				elif trace:
					print("Content unchanged")

				# Delete temporary output
				if os.path.exists(filepath_h):   os.remove(filepath_h)
				if os.path.exists(filepath_cso): os.remove(filepath_cso)

			else: # Generate preprocessed output

				# Delete existing pp output
				filepath_pp = outdir + "\\" + shdr_name + ".pp"
				if os.path.exists(filepath_pp):  os.remove(filepath_pp)

				# Preprocess and clean
				Tools.Exec([UserVars.fxc, fullpath, "/P"+pp_filepath] + includes + defines + options)
				Tools.Exec([UserVars.root + r"\bin\textformatter.exe", "-f", pp_filepath, "-newlines", "0", "1"])
				Tools.Exec([UserVars.textedit, pp_filepath])

except Exception as ex:
	Tools.OnException(ex)
