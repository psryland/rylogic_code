import pygame
import maths
import misc

# Biplane class
class Biplane():
	def __init__(self, position:maths.Vec2, direction:maths.Vec2):

		self.gfx_width = 64
		self.gfx_height = 64

		# Load the animation frames for the biplane
		self.gfx = misc.LoadSpritesheet(misc.AssetPath("plane_left.png"), self.gfx_width, self.gfx_height)

		# World-space position
		self.position = position

		# World-space facing direction
		self.direction = direction

		# Engine power
		self.throttle = 0

		return

	# Fly the plane
	def Step(self):
		return

	# Draw the plane
	def Render(self, screen:pygame.Surface, xpos:float):
		if len(self.gfx) == 0: return
		screen.blit(self.gfx[0], dest=(xpos - self.gfx_width/2, self.position.y - self.gfx_height/2))
		return
