import unittest
from rylogic.math.bbox import *

class TestBBox(unittest.TestCase):
	def test_contructor(self):
		bb = BBox(Vec3(1,1,1), Vec3(0.5,0.5,0.5))
		self.assertEqual(bb.min, Vec3(0.5, 0.5, 0.5))
		self.assertEqual(bb.max, Vec3(1.5, 1.5, 1.5))