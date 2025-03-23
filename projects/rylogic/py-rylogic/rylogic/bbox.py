#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
from .vector import Vec3

class BBox:
	def __init__(self, centre: Vec3=Vec3(0,0,0), radius: Vec3=Vec3(-1,-1,-1)):
		self.centre = centre
		self.radius = radius

	@staticmethod
	def Reset() -> 'BBox':
		return BBox(Vec3(0,0,0), Vec3(-1,-1,-1))
	
	def __str__(self):
		return f'{self.centre} {self.radius}'
	
	def __pack__(self):
		return self.centre.__pack__() + self.radius.__pack__()
