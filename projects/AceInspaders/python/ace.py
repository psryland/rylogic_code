#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import os, pygame
from enum import Enum
pygame.init()

assets_dir = os.path.join(os.path.dirname(__file__), "../art")

class AceInspaders:
	ALIEN_COLS = 10
	ALIEN_ROWS = 5
	
	class EState(Enum):
		StartNewGame   = 0 # Reset data ready for a new game
		StartNewLevel  = 1 # Reset data for the next level
		StartDelay     = 2 # Wait for intro sounds etc to finish before starting user interactive game play
		MainRun        = 3 # Main 'playing' state for the game
		PlayerHit      = 4 # Enter this state as soon as collision is detected between the player and a bomb
		AliensDefeated = 5 # Enter this state as soon as the last alien is destroyed
		LevelComplete  = 6 # Enter this state from 'AliensDefeated' after a delay
		GameEnd        = 7 # Enter this state from 'PlayerHit' after a delay
	class EEntityState(Enum):
		Alive = 0
		Exploding1 = 1
		Exploding2 = 2
		Dead = 3

	# The player ship
	class Player:

		def __init__(self):
			self.gfx = pygame.image.load(os.path.join(assets_dir, "ship.bmp"))
			self.pos = (160,220)
			pass

		# Render the playyer
		def _Render(self, screen:pygame.Surface):
			screen.blit(self.gfx, self.pos)
			return

	# The block of aliens
	class Aliens:
		SIZEX = 0
		SIZEY = 0
		SPACEX = 0
		SPACEY = 0

		# A single alien
		class Alien:
			def __init__(self, r:int, c:int):
				self.row = r
				self.col = c
				pass

		def __init__(self):
			self.m_state = [0]*AceInspaders.ALIEN_COLS
			self.m_pos = (0,0)
			self.m_gfx = {
				AceInspaders.EEntityState.Dead : None,
				AceInspaders.EEntityState.Exploding2 : pygame.image.load(os.path.join(assets_dir, "explode2.bmp")),
				AceInspaders.EEntityState.Exploding1 : pygame.image.load(os.path.join(assets_dir, "explode1.bmp")),
				AceInspaders.EEntityState.Alive : [
					pygame.image.load(os.path.join(assets_dir, "alien1.bmp")),
					pygame.image.load(os.path.join(assets_dir, "alien2.bmp")),
					pygame.image.load(os.path.join(assets_dir, "alien3.bmp")),
					pygame.image.load(os.path.join(assets_dir, "alien4.bmp")),
					pygame.image.load(os.path.join(assets_dir, "alien5.bmp")),
				]
			}
			return

		# Return the state of the alien at (r,c)
		def state(self, r:int, c:int):
			if r < 0 or r >= AceInspaders.ALIEN_ROWS: raise RuntimeError("Row out of range")
			if c < 0 or c >= AceInspaders.ALIEN_COLS: raise RuntimeError("Col out of range")
			return AceInspaders.EEntityState((self.m_state[c] >> (r*2)) & 3)

		# Render the aliens
		def _Render(self, screen:pygame.Surface):
			for r in range(AceInspaders.ALIEN_ROWS):
				for c in range(AceInspaders.ALIEN_COLS):
					state = self.state(r, c)
					sprite = self.m_gfx[state]
					if state == AceInspaders.EEntityState.Alive:
						sprite = sprite[0]
					if sprite is None: continue
					pos = (
						self.m_pos[0] + AceInspaders._mpx(c * (AceInspaders.Aliens.SIZEX + AceInspaders.Aliens.SPACEX)),
						self.m_pos[1] + AceInspaders._mpx(c * (AceInspaders.Aliens.SIZEY + AceInspaders.Aliens.SPACEY)))
					screen.blit(sprite, pos)
			return

	# Constructor
	def __init__(self):
		self.state = AceInspaders.EState.StartNewGame
		self.score = 0
		self.level = 0
		self.player = AceInspaders.Player()
		self.aliens = AceInspaders.Aliens()
		self.screen = pygame.display.set_mode((320,240))
		self.clock = pygame.time.Clock()

		# Setup the screen
		pygame.display.set_caption("Ace Inspaders")

		# Create a back buffer
		self.background = pygame.Surface(self.screen.get_size()).convert()
		self.background.fill((0xff,0xff,0xff))

		# Initial screen blit
		self.screen.blit(self.background, (0,0))
		pygame.display.flip()

	# Main loop
	def Run(self):
		while 1:
			# Use input and game events
			for event in pygame.event.get():
				if event.type == pygame.constants.QUIT:
					return

			elapsed_ms = self.clock.tick()

			# Step
			if False: pass
			elif self.state == AceInspaders.EState.StartNewGame:
				self.score = 0
				self.level = 0
				pass
			elif self.state == AceInspaders.EState.StartNewLevel:
				pass
			elif self.state == AceInspaders.EState.StartNewLevel:
				pass

			# Render
			self._Render()

		return

	# Draw the frame
	def _Render(self):

		# Draw the background
		self.screen.blit(self.background, (0,0))

		# Draw the player
		self.player._Render(self.screen)

		# Draw the aliens
		self.aliens._Render(self.screen)

		pygame.display.flip()
		return

	# Handle change the state machine state
	def _ChangeState(self, new_state:EState):
		# Some states start a timer
		if (new_state == AceInspaders.EState.StartDelay or
			new_state == AceInspaders.EState.PlayerHit or
			new_state == AceInspaders.EState.AliensDefeated):
			self.timer_start_ms = self.clock_ms

		# Change the state
		self.state = new_state
		return

	# Convert to/from milli pixels
	@staticmethod
	def _mpx(pixels:int): return pixels * 1000
	@staticmethod
	def _px(millipixels:int): return millipixels / 1000

if __name__ == "__main__":
	ace = AceInspaders()
	ace.Run()
	