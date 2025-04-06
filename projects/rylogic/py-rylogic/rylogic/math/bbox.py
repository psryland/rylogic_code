#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
from .vector import Vec3

class BBox:
	def __init__(self, *args):
		if len(args) == 0:
			self.centre, self.radius = Vec3.Zero(), -Vec3.One()
		elif len(args) == 2:
			self.centre, self.radius = Vec3(args[0]), Vec3(args[1])
		elif len(args) == 1 and isinstance(args[0], (list, tuple)) and len(args[0]) == 3:
			self.centre, self.radius = Vec3(args[0][0]), Vec3(args[0][1]), Vec3(args[0][2])
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.centre, self.radius = Vec3(args[0][0], args[0][1], args[0][2]), Vec3(args[0][3], args[0][4], args[0][5])
		else:
			raise TypeError(f"Unsupported type for Mat4 constructor: {type(args[0])}")

	# Min corner of the bounding box
	@property
	def min(self) -> Vec3:
		return self.centre - self.radius

	# Max corner of the bounding box
	@property
	def max(self) -> Vec3:
		return self.centre + self.radius

	# Representation
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self):
		return f'{self.centre} {self.radius}'

	# Pack to bytes
	def __pack__(self):
		return self.centre.__pack__() + self.radius.__pack__()
	
	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes, offset: int) -> tuple['BBox', int]:
		centre, offset = Vec3.__unpack__(data, offset)
		radius, offset = Vec3.__unpack__(data, offset)
		return BBox(centre, radius), offset

	# Constants
	def Reset() -> 'BBox':
		return BBox(Vec3.Zero(), -Vec3.One())

