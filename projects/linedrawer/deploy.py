#!/usr/bin/env python
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
sys.path.append("../../script")
import Rylogic as Tools
import UserVars

print(
    "*************************************************************************\n"
    "LineDrawer Deploy\n"
    "Copyright © Rylogic Limited 2013\n"
    "*************************************************************************")

Tools.CheckVersion(1)

platform = input("Platform\n1 - x64\n2 - x86\n(default x64): ")
if   platform == "1": platform = "x64"
elif platform == "2": platform = "x86"
elif platform == "" : platform = "x64"
else: Tools.OnError("Unknown platform")

config = input("Configuration (debug, release(default))? ")
if config == "": config = "release"

proj   = UserVars.root + "\\projects\\vs2012\\linedrawer.sln"
srcdir = UserVars.root + "\\obj2012\\linedrawer\\" + platform + "\\" + config
dstdir = UserVars.root + "\\bin\\linedrawer\\" + platform

input(
    " Deploy Settings:\n"
    "         Source: " + srcdir + "\n"
    "    Destination: " + dstdir + "\n"
    "  Configuration: " + config + "\n"
    "Press enter to continue")
try:
    #Invoke MSBuild
    print("Building the exe...")
    Tools.Exec([UserVars.msbuild, proj, "/t:linedrawer:Rebuild", "/p:Configuration="+config+";Platform="+platform])

    # Copy files to the destination
    print("Deploying...")
    Tools.Copy(srcdir + "\\linedrawer.exe", dstdir + "\\linedrawer.exe")

    Tools.OnSuccess()
    
except Exception as ex:
    Tools.OnError("ERROR: " + str(ex))
