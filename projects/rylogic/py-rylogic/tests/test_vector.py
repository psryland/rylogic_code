import unittest
from rylogic.vector import Vec2

class TestVector(unittest.TestCase):
	def test_vector_addition(self):
		v1 = Vec2(1, 2)
		v2 = Vec2(3, 4)
		result = v1 + v2
		self.assertEqual(result.x, 4)
		self.assertEqual(result.y, 6)
