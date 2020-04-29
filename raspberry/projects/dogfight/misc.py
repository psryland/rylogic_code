import os
import pygame

# The folder that this file is in
root = os.path.abspath(os.path.dirname(__file__))

# The folder that contains all the images
res = os.path.join(root, "res")

# Resolve an asset file
def AssetPath(filename:str):
	path = os.path.join(res, filename)
	if os.path.exists(path): return path
	raise RuntimeError(f"Asset {filename} not found")

# Load a sprite sheet into an array of images
def LoadSpritesheet(filepath:str, frame_width:int, frame_height:int):
	# Load the full sheet
	sheet = pygame.image.load(filepath).convert_alpha()
	dimx, dimy = sheet.get_rect().size

	# The frames of animation
	frames = []

	y = 0
	while y + frame_height <= dimy:
		x = 0
		while x + frame_width <= dimx:
			frame = pygame.Surface((frame_width, frame_height))
			frame.blit(sheet, (0,0), (x * frame_width, y * frame_height, frame_width, frame_height))
			frame.set_colorkey((0,0,0))
			frames.append(frame)
			x += frame_width
		y += frame_height

	return frames