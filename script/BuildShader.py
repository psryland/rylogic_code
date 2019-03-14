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
import sys, os, tempfile
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
	Tools.AssertPath(UserVars.root, "UserVars.root")
	Tools.AssertPath(UserVars.winsdk, "UserVars.winsdk")
	
	# Get the full path to fxc.exe
	fxc = os.path.join(UserVars.winsdk_bin, "x64", "fxc.exe")
	Tools.AssertPath(fxc)

	# Enable compiled shader objects in debug, for debugging and runtime shaders
	if dbg:
		obj = True

	# Show the command line options
	if trace:
		print("pp:" + str(pp) + "  obj:" + str(obj) + "  debug:" + str(dbg))

	# Find the source and output directories
	_,fname = os.path.split(fullpath)
	ftitle,extn  = os.path.splitext(fname)
	if trace: print("File: " + ftitle + extn)

	outdir = os.path.join(UserVars.root, "projects", "renderer11", "shaders", "hlsl", "compiled", config)
	os.makedirs(outdir, exist_ok=True)
	if trace: print("Output directory: " + outdir)

	keys = [
		["vs", "/Tvs_4_0", r"^#ifdef PR_RDR_VSHADER_(?P<name>.*)$"],
		["ps", "/Tps_5_0", r"^#ifdef PR_RDR_PSHADER_(?P<name>.*)$"],
		["gs", "/Tgs_4_0", r"^#ifdef PR_RDR_GSHADER_(?P<name>.*)$"],
		["cs", "/Tcs_5_0", r"^#ifdef PR_RDR_CSHADER_(?P<name>.*)$"],
		]

	# Scan the file looking for each shader type
	for shdr,profile,patn in keys:
		
		# For each matching instance, build the shader
		shaders = Tools.ExtractMany(fullpath,patn)
		for m in shaders:

			shdr_name = m.group("name") + "_" + shdr
			if trace: print("Building: " + shdr_name)

			# Create temporary filepaths so that we only overwrite
			# existing files if they've actually changed. Create temp
			# files for each unique platform/config to allow parallel build
			tempdir = os.path.join(tempfile.gettempdir(), platform, config)
			filepath_h = os.path.join(tempdir, shdr_name + ".h")
			filepath_cso = os.path.join(tempdir, shdr_name + ".cso")

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
			selected = "PR_RDR_"+shdr.upper()+"HADER_"+shdr_name[:-3]
			defines = ["/DSHADER_BUILD", "/D"+selected]

			# Set other command line options
			options = ["/nologo", "/Gis", "/Ges", "/WX", "/Zpc"]
			
			# Debug build options
			# For some reason, the /Zi option causes the output to be different each time it's built using fxc
			if dbg:
				options += ["/Gfp", "/Od", "/Zi"]

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

				out_filepath_h   = os.path.join(outdir, shdr_name + ".h")
				out_filepath_cso = os.path.join(outdir, shdr_name + ".cso")

				# Create the .built file, so that VS's custom build tool can check for it's existence to determine when a build is needed
				with open(os.path.join(outdir, ftitle + ".built"), "w"): pass
				Tools.Copy(filepath_h, out_filepath_h)
				if os.path.exists(filepath_cso):
					Tools.Copy(filepath_cso, out_filepath_cso)

				# Delete temporary output
				if os.path.exists(filepath_h):   os.remove(filepath_h)
				if os.path.exists(filepath_cso): os.remove(filepath_cso)

			else: # Generate preprocessed output

				# Delete existing pp output
				filepath_pp = os.path.join(outdir, shdr_name + ".pp")
				if os.path.exists(filepath_pp): os.remove(filepath_pp)

				# Pre process and clean
				Tools.Exec([fxc, fullpath, "/P"+filepath_pp] + includes + defines + options)
				Tools.Exec([os.path.join(UserVars.root, "bin", "textformatter.exe"), "-f", filepath_pp, "-newlines", "0", "1"])
				if UserVars.textedit:
					Tools.Exec([UserVars.textedit, filepath_pp])
	return

# Run as standalone script
if __name__ == "__main__":
	try:
		#sys.argv = [
		#	"R:\\software\\SDK\\rylogic\\script\\BuildShader.py",
		#	"R:\\software\\SDK\\rylogic\\projects\\renderer11\\shaders\\hlsl\\screenspace\\point_sprites.hlsl",
		#	"x86", "debug", "dbg"]

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

		# Build it
		BuildShader(fullpath, platform, config, pp=pp, obj=obj, trace=trace, dbg=dbg)

	except Exception as ex:
		Tools.OnException(ex)
