#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import pygame

class AceInspaders:
	def __init__(self):
		self.screen = pygame.display.set_mode((320,240))
		pygame.display.set_caption("Ace Inspaders")

		# Create a back buffer
		self.background = pygame.Surface(self.screen.get_size()).convert()
		self.background.fill((0x00,0x00,0x00))

		# Initial screen blit
		self.screen.blit(self.background, (0,0))
		pygame.display.flip()

	def Run(self):
		while 1:
			for event in pygame.event.get():
				if event.type == pygame.constants.QUIT:
					return

			self.screen.blit(self.background, (0,0))
			pygame.display.flip()

		return
