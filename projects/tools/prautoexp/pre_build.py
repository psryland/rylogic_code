#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# Generate the .def file

import sys, os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../script")))
import Rylogic as Tools

root = os.path.abspath(os.path.dirname(__file__))

# Auto generate the module definition file 'prautoexp.def'
infile = os.path.join(root, "src", "expansions.cpp")
utfile = os.path.join(root, "src", "prautoexp.def")
with open(utfile, mode='wt') as outf:
	outf.write("EXPORTS\n")
	matches = Tools.ExtractMany(infile, r"\s+ADDIN_API\s+HRESULT\s+WINAPI\s+(.*?)\(", encoding='cp1250')
	for m in matches:
		outf.write(f"\t{m.group(1)}\n")
