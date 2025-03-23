#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import struct

class Colour32:
	def __init__(self, argb: int = 0xFFFFFFFF):
		self.argb = argb
	def __str__(self):
		return f'{self.argb:08X}'
	def __pack__(self):
		return struct.pack("<I", self.argb)