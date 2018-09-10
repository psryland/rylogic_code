#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# deploy.py [nowait]
import sys, os, shutil, re
sys.path.append(re.sub(r"^(.*\\pr\\).*", r"\1script", sys.path[0]))
import RylogicAssembly as RA
import Rylogic as Tools
import UserVars

if __name__ == "__main__":
	try:

		# Check for optional parameters
		config = "both" #input("Configuration (debug, release, both(default))? ")
		nowait = True if "nowait" in [arg.lower() for arg in sys.argv] else False
		trace  = True if "trace"  in [arg.lower() for arg in sys.argv] else False
		publish = True if input("Publish to nuget.org? (y/n)") == 'y' else False

		RA.DeployAll(config, publish);
		Tools.OnSuccess(pause_time_seconds=1)

	except Exception as ex:
		Tools.OnException(ex)
