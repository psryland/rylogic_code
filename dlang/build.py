#!/usr/bin/env python
# -*- coding: utf-8 -*- 
#
# Builds the D tools and libs
#
import sys, os, shutil, subprocess

sys.path.append("Q:/sdk/pr/python")
from pr import RylogicEnv
from pr import UserVars

print(
	"*************************************************************************\n"
	"DMD Build Toolchain\n"
	"*************************************************************************")

RylogicEnv.CheckVersion(1)

dmdhead = UserVars.dmdroot + r"\..\head\dmd2"
mk      = dmdhead + r"\windows\bin\make.exe"
dmd     = dmdhead + r"\windows\bin\dmd.exe"

input(
	"Build Settings:\n"
	" RootDir: " + dmdhead + "\n"
	"Press enter to continue")

try:
	print("Building dmd.exe...")
	RylogicEnv.Exec([UserVars.msbuild, dmdhead + r"\src\dmd\src\dmd_msc.vcxproj", "/t:Clean", "/p:Configuration=Release", "/p:Platform=x64"])
	RylogicEnv.Exec([UserVars.msbuild, dmdhead + r"\src\dmd\src\dmd_msc.vcxproj", "/t:Build", "/p:Configuration=Release", "/p:Platform=x64"])
	RylogicEnv.Copy(dmdhead + r"\src\dmd\src\vcbuild\x64\Release\dmd_msc.exe", dmdhead + r"\windows\bin\dmd.exe", only_if_modified=False)

	#cd "%dmdhead%\src\dmd\src"
	#set DM_HOME=%dmdhead%
	#call "%vc_env%"
	#"%mk%" -f win32.mak CC=vcbuild\dmc_cl INCLUDE=vcbuild DEBUG=/O2 dmd.exe
	#if errorlevel 1 goto :error
	#echo.

	print("Making druntime...")
	wd = dmdhead + r"\src\druntime"
	RylogicEnv.Exec([mk, "-f", "win64.mak", "clean"], working_dir = wd)
	RylogicEnv.Exec([mk, "-f", "win64.mak"], working_dir = wd)

	print("Making phobos...")
	wd = dmdhead + r"\src\phobos"
	RylogicEnv.Exec([mk, "-f", "win64.mak", "clean"], working_dir = wd)
	RylogicEnv.Exec([mk, "-f", "win64.mak", "phobos64.lib"], working_dir = wd)
	RylogicEnv.Copy(wd + r"\phobos64.lib", dmdhead + r"\windows\lib\phobos64.lib")

	RylogicEnv.OnSuccess()

except Exception as ex:
	RylogicEnv.OnError("Error: " + str(ex))

