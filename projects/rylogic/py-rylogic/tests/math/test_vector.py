import unittest
from rylogic.math.constants import *
from rylogic.math.vector import *

class TestVec2(unittest.TestCase):
	def test_contructor(self):
		v = Vec2(1, 2)
		self.assertEqual(v.x, 1)
		self.assertEqual(v.y, 2)
		v = Vec2([3, 4])
		self.assertEqual(v.x, 3)
		self.assertEqual(v.y, 4)

	def test_vector_addition(self):
		v1 = Vec2(1, 2)
		v2 = Vec2(3, 4)
		result = v1 + v2
		self.assertEqual(result.x, 4)
		self.assertEqual(result.y, 6)

class TestVec3(unittest.TestCase):
	def test_vector_addition(self):
		v1 = Vec3(1, 2, 3)
		v2 = Vec3(3, 4, 5)
		result = v1 + v2
		self.assertEqual(result.x, 4)
		self.assertEqual(result.y, 6)
		self.assertEqual(result.z, 8)

class TestVec4(unittest.TestCase):
	def test_vector_addition(self):
		v1 = Vec4(1, 2, 3, 4)
		v2 = Vec4(3, 4, 5, 6)
		result = v1 + v2
		self.assertEqual(result.x, 4)
		self.assertEqual(result.y, 6)
		self.assertEqual(result.z, 8)
		self.assertEqual(result.w, 10)

class TestMat2(unittest.TestCase):
	def test_matrix_addition(self):
		m1 = Mat2([[1, 2], [3, 4]])
		m2 = Mat2([[5, 6], [7, 8]])
		result = m1 + m2
		expected = Mat2([[6, 8], [10, 12]])
		self.assertEqual(result, expected)
	def test_scalar_addition(self):
		m = Mat2([[1, 2], [3, 4]])
		s = 2
		result = m + s
		expected = Mat2([[3, 4], [5, 6]])
		self.assertEqual(result, expected)
	def test_matrix_multiplication(self):
		m1 = Mat2([[1, 2], [4, 5]])
		m2 = Mat2([[6, 5], [3, 2]])
		result = m1 * m2
		expected = Mat2([[26, 37], [11, 16]])
		self.assertEqual(result, expected)
	def test_vector_multiplication(self):
		m = Mat2([[1, 2], [3, 4]])
		v = Vec2(5, 6)
		result = m * v
		expected = Vec2(23, 34)
		self.assertEqual(result, expected)
	def test_scalar_multiplication(self):
		m = Mat2([[1, 2], [3, 4]])
		s = 2
		result = m * s
		expected = Mat2([[2, 4], [6, 8]])
		self.assertEqual(result, expected)

class TestMat3(unittest.TestCase):
	def test_matrix_addition(self):
		m1 = Mat3([[1, 2, 3], [4, 5, 6], [7, 8, 9]])
		m2 = Mat3([[9, 8, 7], [6, 5, 4], [3, 2, 1]])
		result = m1 + m2
		expected = Mat3([[10, 10, 10], [10, 10, 10], [10, 10, 10]])
		self.assertEqual(result, expected)
	def test_scalar_addition(self):
		m = Mat3([[1, 2, 3], [4, 5, 6], [7, 8, 9]])
		s = 2
		result = m + s
		expected = Mat3([[3, 4, 5], [6, 7, 8], [9, 10, 11]])
		self.assertEqual(result, expected)
	def test_matrix_multiplication(self):
		m1 = Mat3([[1, 2, 3], [4, 5, 6], [7, 8, 9]])
		m2 = Mat3([[9, 8, 7], [6, 5, 4], [3, 2, 1]])
		result = m1 * m2
		expected = Mat3([
			[90, 114, 138],
			[54, 69, 84],
			[18, 24, 30]
		])
		self.assertEqual(result, expected)
	def test_vector_multiplication(self):
		m = Mat3([[1, 2, 3], [4, 5, 6], [7, 8, 9]])
		v = Vec3(1, 2, 3)
		result = m * v
		expected = Vec3(30, 36, 42)
		self.assertEqual(result, expected)
	def test_scalar_multiplication(self):
		m = Mat3([[1, 2, 3], [4, 5, 6], [7, 8, 9]])
		s = 2
		result = m * s
		expected = Mat3([[2, 4, 6], [8, 10, 12], [14, 16, 18]])
		self.assertEqual(result, expected)
	def test_rotation(self):
		rot = Mat3.Rotation(Vec3(0, 0, 1), Tau/4)
		all(self.assertAlmostEqual(a,b) for a,b in zip(rot.arr, [
			0, -1, 0,
			1, 0, 0,
			0, 0, 1
		]))

class TestMat4(unittest.TestCase):
	def test_constructors(self):
		m = Mat4([[1, 2, 3, 4], [5, 6, 7, 8], [9, 10, 11, 12], [13, 14, 15, 16]])
		self.assertEqual(m.arr, [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16])

		rot = Mat3.Rotation(Vec3(0, 0, 1), Tau/4)
		pos = Vec3(1, 2, 3)
		m = Mat4(rot, pos)
		all(self.assertAlmostEqual(a,b) for a,b in zip(m.arr, [
			0, -1, 0, 0,
			1, 0, 0, 0,
			0, 0, 1, 0,
			1, 2, 3, 1
		]))

	def test_matrix_addition(self):
		m1 = Mat4([[1, 2, 3, 4], [5, 6, 7, 8], [9, 10, 11, 12], [13, 14, 15, 16]])
		m2 = Mat4([[16, 15, 14, 13], [12, 11, 10, 9], [8, 7, 6, 5], [4, 3, 2, 1]])
		result = m1 + m2
		expected = Mat4([
			[17, 17, 17, 17],
			[17, 17, 17, 17],
			[17, 17, 17, 17],
			[17, 17, 17, 17]
		])
		self.assertEqual(result, expected)
	def test_scalar_addition(self):
		m = Mat4([[1, 2, 3, 4], [5, 6, 7, 8], [9, 10, 11, 12], [13, 14, 15, 16]])
		s = 2
		result = m + s
		expected = Mat4([
			[3, 4, 5, 6],
			[7, 8, 9, 10],
			[11, 12, 13, 14],
			[15, 16, 17, 18]
		])
		self.assertEqual(result, expected)
	def test_matrix_multiplication(self):
		m1 = Mat4([[1, 2, 3, 4], [5, 6, 7, 8], [9, 10, 11, 12], [13, 14, 15, 16]])
		m2 = Mat4([[16, 15, 14, 13], [12, 11, 10, 9], [8, 7, 6, 5], [4, 3, 2, 1]])
		result = m1 * m2
		expected = Mat4([
			[386, 444, 502, 560],
			[274, 316, 358, 400],
			[162, 188, 214, 240],
			[50, 60, 70, 80],
		])
		self.assertEqual(result, expected)
	def test_vector_multiplication(self):
		m = Mat4([[1, 2, 3, 4], [5, 6, 7, 8], [9, 10, 11, 12], [13, 14, 15, 16]])
		v = Vec4(1, 2, 3, 4)
		result = m * v
		expected = Vec4(90, 100, 110, 120)
		self.assertEqual(result, expected)
	def test_scalar_multiplication(self):
		m = Mat4([[1, 2, 3, 4], [5, 6, 7, 8], [9, 10, 11, 12], [13, 14, 15, 16]])
		s = 2
		result = m * s
		expected = Mat4([[2, 4, 6, 8], [10, 12, 14, 16], [18, 20, 22, 24], [26, 28, 30, 32]])
		self.assertEqual(result, expected)