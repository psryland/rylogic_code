import pygame
import maths
import biplane
import misc

# Player class
class Player():
	# Player state machine states
	class EState():
		Landed = 0
		Taxiing = 1
		Flying = 2

	def __init__(self, position: maths.Vec2, direction: maths.Vec2):
		self.state = Player.EState.Landed
		self.plane = biplane.Biplane(position, direction)
		#self.i2w = pygame.transform
		return

	# The player's world space position
	@property
	def position(self):
		return self.plane.position

	# Step the player
	def Step(self, elapsed_s:float):
		if self.state == Player.EState.Landed:
			pass
		else:
			raise RuntimeError("Unknown state")

		return

	# Draw the player's graphics
	def Render(self, screen:pygame.Surface, xpos:float):
		self.plane.Render(screen, xpos)
		return



		# step0 = +10
		# step1 = -10
		# hwld = self.world_dim.x / 2

		# 	# Move the players
		# 	self.players[0].position.x += step0
		# 	if self.players[0].position.x > +hwld: step0 = -step0
		# 	if self.players[0].position.x < -hwld: step0 = -step0

		# 	self.players[1].position.x += step1
		# 	if self.players[1].position.x > +hwld: step1 = -step1
		# 	if self.players[1].position.x < -hwld: step1 = -step1
