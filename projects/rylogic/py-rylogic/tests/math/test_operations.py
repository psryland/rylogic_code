import unittest
from rylogic.math.vector import *
from rylogic.math.operations import *

class TestSqr(unittest.TestCase):
	def test_sqr(self):
		self.assertEqual(Sqr(2), 4)
		self.assertEqual(Sqr(-2), 4)
		self.assertEqual(Sqr(0), 0)

class TestLengthSq(unittest.TestCase):
	def test_length_sq(self):
		v2 = Vec2(3, 4)
		v3 = Vec3(1, 2, 3)
		v4 = Vec4(-1, 2, -3, 4)
		self.assertEqual(LengthSq(v2), 25)
		self.assertEqual(LengthSq(v3), 14)
		self.assertEqual(LengthSq(v4), 30)

class TestLength(unittest.TestCase):
	def test_length(self):
		v2 = Vec2(3, 4)
		v3 = Vec3(1, 2, 3)
		v4 = Vec4(-1, 2, -3, 4)
		self.assertEqual(Length(v2), 5)
		self.assertEqual(Length(v3), (14 ** 0.5))
		self.assertEqual(Length(v4), (30 ** 0.5))

class TestDot(unittest.TestCase):
	def test_dot(self):
		v2a = Vec2(1, 2)
		v2b = Vec2(3, 4)
		v3a = Vec3(1, 2, 3)
		v3b = Vec3(4, 5, 6)
		v4a = Vec4(1, 2, 3, 4)
		v4b = Vec4(5, 6, 7, 8)
		self.assertEqual(Dot(v2a, v2b), 11)
		self.assertEqual(Dot(v3a, v3b), 32)
		self.assertEqual(Dot(v4a, v4b), 70)

class TestCross(unittest.TestCase):
	def test_cross(self):
		v2a = Vec2(1, 2)
		v2b = Vec2(3, 4)
		v3a = Vec3(1, 2, 3)
		v3b = Vec3(4, 5, 6)
		self.assertEqual(Cross(v2a, v2b), -2)
		cross_product = Cross(v3a, v3b)
		self.assertEqual(cross_product.x, -3)
		self.assertEqual(cross_product.y, 6)
		self.assertEqual(cross_product.z, -3)

class TestNormalise(unittest.TestCase):
	def test_normalise(self):
		v2 = Vec2(3, 4)
		v3 = Vec3(1, 2, 3)
		v4 = Vec4(-1, 2, -3, 4)
		norm_v2 = Normalise(v2)
		norm_v3 = Normalise(v3)
		norm_v4 = Normalise(v4)
		self.assertEqual(norm_v2.x, 0.6)
		self.assertEqual(norm_v2.y, 0.8)
		self.assertAlmostEqual(Length(norm_v2), 1)
		self.assertEqual(norm_v3.x, 1 / (14 ** 0.5))
		self.assertEqual(norm_v3.y, 2 / (14 ** 0.5))
		self.assertEqual(norm_v3.z, 3 / (14 ** 0.5))
		self.assertAlmostEqual(Length(norm_v3), 1)
		self.assertEqual(norm_v4.x, -1 / (30 ** 0.5))
		self.assertEqual(norm_v4.y, 2 / (30 ** 0.5))
		self.assertEqual(norm_v4.z, -3 / (30 ** 0.5))
		self.assertEqual(norm_v4.w, 4 / (30 ** 0.5))
		self.assertAlmostEqual(Length(norm_v4), 1)

class TestTranspose(unittest.TestCase):
	def test_transpose_mat2(self):
		m = Mat2([
			[1, 2],
			[3, 4]
		])
		mT = Transpose(m)
		self.assertEqual(mT.x.x, 1)
		self.assertEqual(mT.x.y, 3)
		self.assertEqual(mT.y.x, 2)
		self.assertEqual(mT.y.y, 4)
	def test_transpose_mat3(self):
		m = Mat3([
			[1, 2, 3],
			[4, 5, 6],
			[7, 8, 9]
		])
		mT = Transpose(m)
		self.assertEqual(mT.x.x, 1)
		self.assertEqual(mT.x.y, 4)
		self.assertEqual(mT.x.z, 7)
		self.assertEqual(mT.y.x, 2)
		self.assertEqual(mT.y.y, 5)
		self.assertEqual(mT.y.z, 8)
		self.assertEqual(mT.z.x, 3)
		self.assertEqual(mT.z.y, 6)
		self.assertEqual(mT.z.z, 9)
	def test_transpose_mat4(self):
		m = Mat4([
			[1, 2, 3, 4],
			[5, 6, 7, 8],
			[9, 10, 11, 12],
			[13, 14, 15, 16]
		])
		mT = Transpose(m)
		self.assertEqual(mT.x.x, 1)
		self.assertEqual(mT.x.y, 5)
		self.assertEqual(mT.x.z, 9)
		self.assertEqual(mT.x.w, 13)
		self.assertEqual(mT.y.x, 2)
		self.assertEqual(mT.y.y, 6)
		self.assertEqual(mT.y.z, 10)
		self.assertEqual(mT.y.w, 14)
		self.assertEqual(mT.z.x, 3)
		self.assertEqual(mT.z.y, 7)
		self.assertEqual(mT.z.z, 11)
		self.assertEqual(mT.z.w, 15)
		self.assertEqual(mT.w.x, 4)
		self.assertEqual(mT.w.y, 8)
		self.assertEqual(mT.w.z, 12)
		self.assertEqual(mT.w.w, 16)