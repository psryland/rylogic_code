#!/usr/bin/env python3
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
# DEPRECATED: Use the C# script instead
#
import sys, os, tempfile, hashlib
import Rylogic as Tools
import UserVars

# Build an HLSL shader
# 'fullpath' - full path to the HLSL file
# 'platform' - one of x86 or x64
# 'config' - one of debug or release
# Optional parameters:
#  pp - produced pre-processed
#  obj - produce compiled shader object files (automatically enabled in debug)
#  trace - print debugging messages
#  dbg - debugging
def BuildShader(fullpath:str, platform:str, config:str, pp=False, obj=False, trace=False, dbg=False):

	Tools.AssertVersion(1)
	Tools.AssertLatestWinSDK()

	# Get the full path to the compiler
	compiler = Tools.Path(UserVars.winsdk, "bin", UserVars.winsdkvers, "x64", "fxc.exe") # old compiler < SM 6
	#compiler = Tools.Path(UserVars.winsdk, "bin", UserVars.winsdkvers, "x64", "dxc.exe") # new compiler >= SM 6

	# Enable compiled shader objects in debug, for debugging and runtime shaders
	if dbg:
		obj = True

	# Show the command line options
	if trace:
		print(f"trace:{str(trace)} debug:{str(dbg)} obj:{str(obj)} pp:{str(pp)}")

	# Find the source and output directories
	fullpath = Tools.Path(fullpath)
	fdir,fname = os.path.split(fullpath)
	ftitle,extn  = os.path.splitext(fname)
	if trace: print(f"File: {fdir}/{ftitle}{extn}")

	# Determine the output directory
	outdir = fdir
	while not outdir.endswith("hlsl"):
		outdir,x = os.path.split(outdir)
		if x == '': raise RuntimeError(f"Shader file {fullpath} is not within an 'hlsl' directory")
	outdir = os.path.join(outdir, "compiled", config)
	if trace: print("Output directory: " + outdir)
	os.makedirs(outdir, exist_ok=True)

	# Careful with shader versions, if you bump up from 4_0 you'll need to change the
	# minimum feature level in view3d.
	keys = [
		["vs", "/Tvs_4_0", r"^#ifdef PR_RDR_VSHADER_(?P<name>.*)$"],
		["ps", "/Tps_4_0", r"^#ifdef PR_RDR_PSHADER_(?P<name>.*)$"],
		["gs", "/Tgs_4_0", r"^#ifdef PR_RDR_GSHADER_(?P<name>.*)$"],
		["cs", "/Tcs_5_0", r"^#ifdef PR_RDR_CSHADER_(?P<name>.*)$"],
	]

	# Scan the file looking for instances of each shader type (there can be more than one)
	for shdr,profile,patn in keys:
		
		# For each matching instance, build the shader
		shaders = Tools.ExtractMany(fullpath,patn)
		for m in shaders:

			shdr_name = m.group("name") + "_" + shdr
			if trace: print("Building: " + shdr_name)

			# Hash the full path to generate a unique temporary file name
			hash = hashlib.md5(fullpath.encode()).hexdigest()

			# Create temporary filepaths so that we only overwrite
			# existing files if they've actually changed. Create temp
			# files for each unique platform/config to allow parallel build
			tempdir = os.path.join(tempfile.gettempdir(), platform, config)
			filepath_h = os.path.join(tempdir, shdr_name + hash + ".h")
			filepath_cso = os.path.join(tempdir, shdr_name + hash + ".cso")
			filepath_pdb = os.path.join(tempdir, shdr_name + hash + ".pdb")

			# Delete any potentially left over temporary output
			os.makedirs(tempdir, exist_ok=True)
			if os.path.exists(filepath_h):   os.remove(filepath_h)
			if os.path.exists(filepath_cso): os.remove(filepath_cso)

			# Set up the command line for FXC
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
			selected = f"PR_RDR_{shdr.upper()}HADER_{shdr_name[:-3]}"
			defines = ["/DSHADER_BUILD", f"/D{selected}"]

			# Set other command line options
			options = ["/nologo", "/Gis", "/Ges", "/WX", "/Zpc"]
			
			# Debug build options
			# For some reason, the /Zi option causes the output to be different each time it's built using fxc
			if dbg:
				options += ["/Gfp", "/Od", "/Zi", f"/Fd{filepath_pdb}"]

			if not pp:
				# Build the shader using fxc
				if trace: print("Running fxc.exe...")
				success,output = Tools.Run([compiler, fullpath, profile] + varname + output + includes + defines + options, show_arguments=trace)
				if not success:
					print("Compiling: " + fullpath)
					print(output)
					print("failed")
				elif trace:
					print(output)
					print("success")

				out_filepath_h   = os.path.join(outdir, shdr_name + ".h")
				out_filepath_cso = os.path.join(outdir, shdr_name + ".cso")
				out_filepath_pdb = os.path.join(outdir, shdr_name + ".pdb")

				# Create the .built file, so that VS's custom build tool can check for it's existence to determine when a build is needed
				with open(os.path.join(outdir, f"{ftitle}.built"), "w"): pass
				
				# Copy to target directory
				if os.path.exists(filepath_h):
					Tools.Copy(filepath_h, out_filepath_h, full_paths=False)
					os.remove(filepath_h)
				if os.path.exists(filepath_cso):
					Tools.Copy(filepath_cso, out_filepath_cso, full_paths=False)
					os.remove(filepath_cso)
				if os.path.exists(filepath_pdb):
					Tools.Copy(filepath_pdb, out_filepath_pdb, full_paths=False)
					os.remove(filepath_pdb)

			else: # Generate preprocessed output

				# Delete existing pp output
				filepath_pp = os.path.join(outdir, shdr_name + ".pp")
				if os.path.exists(filepath_pp): os.remove(filepath_pp)

				# Pre process and clean
				Tools.Exec([compiler, fullpath, "/P"+filepath_pp] + includes + defines + options)
				Tools.Exec([os.path.join(UserVars.root, "bin", "textformatter.exe"), "-f", filepath_pp, "-newlines", "0", "1"])
	return

# Run as standalone script
if __name__ == "__main__":
	try:
		#sys.argv = ["", "P:\\pr\\projects\\rylogic\\view3d\\shaders\\hlsl\\shadow\\shadow_map.hlsl", "x86", "debug", "dbg"]
		#sys.argv = ["", "P:\\pr\\projects\\rylogic\\view3d-12\\src\\shaders\\hlsl\\forward\\forward.hlsl", "x86", "debug", "dbg"]
		#sys.argv = ["", "P:\\pr\\projects\\rylogic\\view3d-12\\src\\shaders\\hlsl\\utility\\generate_mipmaps.hlsl", "x86", "debug", "dbg"]

		trace = False
		if trace:
			print("Args: " + str(sys.argv))
		
		# The full path of the HLSL file to compile
		fullpath = (sys.argv[1] if len(sys.argv) > 1 else input("Shader File? ")).lower()
		platform = (sys.argv[2] if len(sys.argv) > 2 else "any"                 ).lower()
		config   = (sys.argv[3] if len(sys.argv) > 3 else "release"             ).lower()

		# Check for optional parameters
		pp    = True if "pp"    in [arg.lower() for arg in sys.argv] else False
		obj   = True if "obj"   in [arg.lower() for arg in sys.argv] else False
		trace = True if "trace" in [arg.lower() for arg in sys.argv] else False
		dbg   = True if "dbg"   in [arg.lower() for arg in sys.argv] else False
		dbg |= config == "debug"

		# Build it
		BuildShader(fullpath, platform, config, pp=pp, obj=obj, trace=trace, dbg=dbg)

	except Exception as ex:
		Tools.OnException(ex, enter_to_close=False)
