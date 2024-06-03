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

	# Process certain file types
	if extn not in ['.html']:
		continue

	# Read the whole file into memory
	with open(in_dir / file, 'rb') as fin:
		data = fin.read()

	# Write out the data as a C++ array
	with open(out_dir / f"{file}.cpp", 'w') as out:
		out.write(f'const unsigned char {fname}_{extn[1:]}[] = {{\n')
		for i in range(0, len(data), 16):
			out.write('\t')
			for j in range(16):
				if i+j < len(data):
					out.write('0x{:02x}, '.format(data[i+j]))
			out.write('\n')
		out.write('};\n')

	# Add to the resources
	resources.append(f'\textern const unsigned char {fname}_{extn[1:]}[];\n')

# Write out a header for the resources
with open(out_dir / 'resources.h', 'w') as out:
	out.write("#pragma once\n")
	out.write("namespace lightz\n")
	out.write("{\n")
	out.write("\n".join(resources))
	out.write("}\n")
