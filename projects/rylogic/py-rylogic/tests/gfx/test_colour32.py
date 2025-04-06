#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import unittest
from rylogic.gfx.colour import *

class TestColour32(unittest.TestCase):
	def test_construction(self):
		c = Colour32(0xFF00FF00)
		self.assertEqual(c.r, 0)
		self.assertEqual(c.g, 1)
		self.assertEqual(c.b, 0)
		self.assertEqual(c.a, 1)

