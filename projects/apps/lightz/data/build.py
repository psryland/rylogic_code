#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
from pathlib import Path

root = Path(__file__).parent.parent.absolute()
in_dir = root / 'data'
out_dir = root / 'src/data'

resources = []

for file in os.listdir(in_dir):

	fname,extn = os.path.splitext(file)
	extn = extn[1:].lower()
	var_name = f"{fname}_{extn}"

	# Process certain file types
	if extn not in ['html', 'ico', 'png', 'jpg', 'jpeg', 'gif', 'css', 'js', 'txt']:
		continue

	# Read the whole file into memory
	with open(in_dir / file, 'rb') as fin:
		data = fin.read()

	# Write out the data as a C++ array
	with open(out_dir / f"{file}.cpp", 'w') as out:
		out.write(f'#include "resources.h"\n')
		out.write("\n")
		out.write("namespace lightz::data\n")
		out.write("{\n")
		out.write(f'\tchar const {var_name}_data[] = {{\n')
		for i in range(0, len(data), 16):
			out.write('\t\t')
			for j in range(16):
				if i+j < len(data):
					out.write('0x{:02x}, '.format(data[i+j]))
			out.write('\n')
		out.write('\t};\n')
		out.write(f'\tstd::string_view const {var_name}(&{var_name}_data[0], sizeof({var_name}_data));\n')
		out.write("}\n")

	# Add to the resources
	resources.append(f'\textern std::string_view const {var_name};')

# Write out a header for the resources
with open(out_dir / 'resources.h', 'w') as out:
	out.write("#pragma once\n")
	out.write("#include <string_view>\n")
	out.write("\n")
	out.write("namespace lightz::data\n")
	out.write("{\n")
	out.write("\n".join(resources))
	out.write("}\n")
