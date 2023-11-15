#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
#  Copy.py $(source) $(TargetDir) -OnlyIfNewer -IgnoreMissing
#
import sys, shlex
import Rylogic as Tools

#sys.argv = ["", "E:\Dump\test.txt", "E:\Dump\Target", "-OnlyIfNewer", "-IgnoreMissing"]
source     = sys.argv[1].rstrip('\\') if len(sys.argv) > 1 else input("Source? ")
target_dir = sys.argv[2].rstrip('\\') if len(sys.argv) > 2 else input("TargetDir? ")
only_if_newer = True if "-OnlyIfNewer" in sys.argv[3:] else False
ignore_missing = True if "-IgnoreMissing" in sys.argv[3:] else False

Tools.Copy(source, target_dir, only_if_modified=only_if_newer, ignore_missing=ignore_missing)
