import pygame
import player
import maths
import misc

class Game():
	BaseOrigin = 50
	BaseAltitude = 80

	def __init__(self):

		# Let 'pygame' setup everything it needs to
		pygame.init()

		# Create the screen
		self.screen_dim = maths.Vec2(640, 480)
		self.screen = pygame.display.set_mode((self.screen_dim.x, self.screen_dim.y))

		# Load the background image
		self.background = pygame.image.load(misc.AssetPath("background.png")).convert()
		self.world_dim = maths.Vec2.from_array(self.background.get_rect().size)

		# Create the players
		self.players = [
			player.Player(maths.Vec2(-self.world_dim.x/2 + Game.BaseOrigin, self.world_dim.y - Game.BaseAltitude), maths.Vec2(+1.0, 0.0)),
			player.Player(maths.Vec2(+self.world_dim.x/2 - Game.BaseOrigin, self.world_dim.y - Game.BaseAltitude), maths.Vec2(-1.0, 0.0)),
		]

		self.running = True
		return

	# Game main loop
	def Run(self):

		clock = pygame.time.Clock()
		while self.running:

			# Process any input
			self._ProcessEvents()
			
			# Get the current game time
			elapsed_s = clock.get_time() * 0.001

			# Step the players
			for plyr in self.players:
				plyr.Step(elapsed_s)

			# Draw the scene
			self._UpdateScreen()

			# Render a frame
			pygame.display.flip()
			clock.tick(60)

		# Shutdown
		pygame.quit()
		return

	# Process any input
	def _ProcessEvents(self):
		for event in pygame.event.get():
			# Shutdown
			if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
				self.running = False

			# Use keys for now
			elif event.type == pygame.KEYDOWN:
				
				pass

			#if event.type is pygame.locals.MOUSEBUTTONDOWN:
			#print(pos)
		return

	# Draw the split screen background
	def _UpdateScreen(self):
		hwld = self.world_dim.x / 2
		wscn = self.screen_dim.x
		hscn = self.screen_dim.x / 2
		qscn = self.screen_dim.x / 4
		xmin = -hwld + qscn
		xmax = +hwld - qscn

		# If the distance between the players is greater than
		# half the screen width, then split screen is needed.
		x0 = maths.clamp(self.players[0].position.x, xmin, xmax)
		x1 = maths.clamp(self.players[1].position.x, xmin, xmax)
		xavr = (x0 + x1) / 2

		split = abs(x1 - x0) > hscn
		crossed = x0 > x1
		if split:
			# Draw player 1's view
			dest = (0,0) if not crossed else (hscn,0)
			self.screen.blit(self.background, dest=dest, area=(hwld + x0 - qscn, 0, hscn, self.screen_dim.y))

			# Draw player 2's view
			dest = (hscn,0) if not crossed else (0,0)
			self.screen.blit(self.background, dest=dest, area=(hwld + x1 - qscn, 0, hscn, self.screen_dim.y))

			# Draw a divider
			pygame.draw.line(self.screen, (0,0,0), (hscn,0), (hscn,self.screen_dim.y))
		else:
			self.screen.blit(self.background, dest=(0,0), area=(hwld + xavr - hscn, 0, wscn, self.screen_dim.y))

		# Draw the players
		for i, plyr in enumerate(self.players):
			px = plyr.position.x
			xpos = (
				(1*qscn + (px - xmin)) if px < xmin else
				(3*qscn + (px - xmax)) if px > xmax else
				(2*qscn + (px - xavr)) if not split else
				(1*qscn) if (i == 0 and not crossed) or (i == 1 and crossed) else
				(3*qscn) if (i == 1 and not crossed) or (i == 0 and crossed)  else
				None)

			plyr.Render(self.screen, xpos)

		return


# The program starts here
if __name__ == '__main__':
	game = Game()
	game.Run()

