#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import struct

class Colour32:
	def __init__(self, *args):
		def Clamp(i:int) -> int: return max(0, min(i, 255)) & 0xFF
		def FtoI(f:float) -> int: return Clamp(int(f * 255))
		if len(args) == 0:
			self.argb = 0xFFFFFFFF
		elif len(args) == 1 and isinstance(args[0], Colour32):
			self.argb = args[0].argb
		elif len(args) == 1 and isinstance(args[0], int):
			self.argb = args[0]
		elif len(args) == 4 and isinstance(args[0], int): # r, g, b, a
			self.argb = (Clamp(args[3]) << 24) | (Clamp(args[0]) << 16) | (Clamp(args[1]) << 8) | Clamp(args[2])
		elif len(args) == 4 and isinstance(args[0], float):
			self.argb = (FtoI(args[3]) << 24) | (FtoI(args[0]) << 16) | (FtoI(args[1]) << 8) | FtoI(args[0])
		else:
			raise ValueError("Invalid arguments for Colour32 constructor")

	# A-channel
	@property
	def a(self) -> float:
		"""Alpha component as a float [0,1]"""
		return ((self.argb >> 24) & 0xFF) / 255.0

	# R-channel
	@property
	def r(self) -> float:
		"""Red component as a float [0,1]"""
		return ((self.argb >> 16) & 0xFF) / 255.0
	
	# G-channel
	@property
	def g(self) -> float:
		"""Green component as a float [0,1]"""
		return ((self.argb >> 8) & 0xFF) / 255.0
	
	# B-channel
	@property
	def b(self) -> float:
		"""Blue component as a float [0,1]"""
		return (self.argb & 0xFF) / 255.0

	# Convert to ARGB string
	def __str__(self):
		return f'{self.argb:08X}'

	# Pack to bytes
	def __pack__(self):
		return struct.pack("<I", self.argb)

	# Unpack from bytes
	@staticmethod
	def __unpack__(self, data):
		self.argb = struct.unpack("<I", data)[0]