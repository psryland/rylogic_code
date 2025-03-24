#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import struct

class VariableInt:
	def __init__(self, value: int):
		self.m_value = value

	def __str__(self) -> str:
		return f'{str(self.m_value)}'

	def __pack__(self) -> bytes:
		# Variable sized int, write 6 bits at a time: xx444444 33333322 22221111 11000000
		i = 5
		bits = [0]*5
		value = self.m_value
		while value != 0 and i > 0:
			i -= 1
			bits[i] = 0x80 | (value & 0b00111111)
			value >>= 6
		return struct.pack('<'+'B'*(5-i), *bits[i:])

	def __unpack__(self, data: bytes) -> int:
		# Variable sized int, read 6 bits at a time: xx444444 33333322 22221111 11000000
		value = 0
		for i in range(5):
			if i >= len(data): break
			b = data[i]
			value |= (b & 0x3F) << (i * 6)
			if b & 0x80 == 0: break
		return value
	