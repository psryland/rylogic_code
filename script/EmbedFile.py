#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
#
# EmbedFile.py TAG <file-to-embed> <file-to-embed-into>
import sys

def ConvertToCppString(data: str) -> str:
	data = data.replace('\\', '\\\\')
	data = data.replace('"', '\\"')
	data = data.replace('\n', '\\n')
	return f"\"{data}\"\n"

def EmbedFile(tag: str, file_to_embed: str, file_to_embed_into: str) -> None:

	tag_begin = f"// AUTO_GENERATED-{tag}-BEGIN"
	tag_end = f"// AUTO_GENERATED-{tag}-END"
	indent = ""

	# Open 'file_to_embed_into' and search for the section between the tags
	with open(file_to_embed_into, "r") as file:
		data = file.readlines()

	# Find the index range of the lines between tag_begin and tag_end
	beg, end = 0, 0
	for i, line in enumerate(data):
		if tag_begin in line:
			beg = i
			indent = line[:line.find(tag_begin)]
		if tag_end in line:
			end = i

	# Remove the lines between tag_begin and tag_end
	del data[beg+1:end]

	# Open 'file_to_embed' and read the data
	with open(file_to_embed, "r") as file:
		embed_data = file.readlines()

	# Insert the data between tag_begin and tag_end
	data[beg+1:beg+1] = [f"{indent}{ConvertToCppString(line)}" for line in embed_data]

	# Write the data back to 'file_to_embed_into'
	with open(file_to_embed_into, "w") as file:
		file.writelines(data)

# Entry Point
if __name__ == "__main__":

	#
	#sys.argv= [
	#	'', 'DEMO_SCENE',
	#	'E:/Rylogic/Code/projects/rylogic/view3d-12/src/ldraw/ldraw_demo_scene.ldr',
	#	'E:/Rylogic/Code/projects/rylogic/view3d-12/src/ldraw/ldraw_demo_scene.cpp'
	#]

	tag = sys.argv[1]
	file_to_embed = sys.argv[2]
	file_to_embed_into = sys.argv[3]
	EmbedFile(tag, file_to_embed, file_to_embed_into)
	print(f"EmbedFile: {file_to_embed} -> {file_to_embed_into}")