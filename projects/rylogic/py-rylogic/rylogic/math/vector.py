#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import struct, math

class Vec2:
	def __init__(self, *args):
		if len(args) == 0:
			self.x, self.y = 0.0, 0.0
		elif len(args) == 2:
			self.x, self.y = float(args[0]), float(args[1])
		elif len(args) == 1 and isinstance(args[0], Vec2):
			self.x, self.y = float(args[0].x), float(args[0].y)
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.x, self.y = float(args[0][0]), float(args[0][1])
		else:
			raise TypeError(f"Unsupported type for Vec2 constructor: {type(args[0])}")

	# Convert to array
	@property
	def arr(self) -> list:
		return [self.x, self.y]

	# Convert to Vec3
	def vec3(self, z: float=0) -> 'Vec3':
		return Vec3(self.x, self.y, z)
	
	# Convert to Vec4
	def w0(self, z: float=0) -> 'Vec4':
		return Vec4(self.x, self.y, z, 0)
	
	# Convert to Vec4
	def w1(self, z: float=0) -> 'Vec4':
		return Vec4(self.x, self.y, z, 1)

	# Equality operator
	def __eq__(self, rhs) -> bool:
		if isinstance(rhs, Vec2):
			return self.x == rhs.x and self.y == rhs.y
		else:
			return False

	# Operator uniary minus
	def __neg__(self) -> 'Vec2':
		return Vec2(-self.x, -self.y)

	# Add vector to vector or scalar
	def __add__(self, rhs) -> 'Vec2':
		if isinstance(rhs, Vec2):
			return Vec2(self.x + rhs.x, self.y + rhs.y)
		elif isinstance(rhs, (int, float)):
			return Vec2(self.x + rhs, self.y + rhs)
		else:
			raise TypeError(f"Unsupported type for addition: {type(rhs)}")

	# Subtract vector from vector or scalar
	def __sub__(self, rhs) -> 'Vec2':
		if isinstance(rhs, Vec2):
			return Vec2(self.x - rhs.x, self.y - rhs.y)
		elif isinstance(rhs, (int, float)):
			return Vec2(self.x - rhs, self.y - rhs)
		else:
			raise TypeError(f"Unsupported type for subtraction: {type(rhs)}")

	# Multiply vector by vector or scalar
	def __mul__(self, rhs) -> 'Vec2':
		if isinstance(rhs, Vec2):
			return Vec2(self.x * rhs.x, self.y * rhs.y)
		elif isinstance(rhs, (int, float)):
			return Vec2(self.x * rhs, self.y * rhs)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(rhs)}")

	# Multiple by scalar from the left
	def __rmul__(self, value) -> 'Vec2':
		return self.__mul__(value)

	# Represent the object
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self) -> str:
		return f'{self.x} {self.y}'
	
	# Pack to bytes
	def __pack__(self) -> bytes:
		return struct.pack("ff", self.x, self.y)

	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes) -> 'Vec2':
		x, y = struct.unpack("ff", data[:8])
		return Vec2(x, y)

	# Constants
	def Zero() -> 'Vec2':
		return Vec2(0, 0)
	def XAxis() -> 'Vec2':
		return Vec2(1, 0)
	def YAxis() -> 'Vec2':
		return Vec2(0, 1)
	def One() -> 'Vec2':
		return Vec2(1, 1)

class Vec3:
	def __init__(self, *args):
		if len(args) == 0:
			self.x, self.y, self.z = 0.0, 0.0, 0.0
		elif len(args) == 3:
			self.x, self.y, self.z = float(args[0]), float(args[1]), float(args[2])
		elif len(args) == 1 and isinstance(args[0], Vec3):
			self.x, self.y, self.z = args[0].x, args[0].y, args[0].z
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.x, self.y, self.z = float(args[0][0]), float(args[0][1]), float(args[0][2])
		else:
			raise TypeError(f"Unsupported type for Vec3 constructor: {type(args[0])}")

	# Convert to array
	@property
	def arr(self) -> list:
		return [self.x, self.y, self.z]

	# Convert to Vec2
	@property
	def xy(self) -> Vec2:
		return Vec2(self.x, self.y)

	# Convert to Vec4
	def w0(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 0)

	# Convert to Vec4
	def w1(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 1)

	# Equality operator
	def __eq__(self, rhs) -> bool:
		if isinstance(rhs, Vec3):
			return self.x == rhs.x and self.y == rhs.y and self.z == rhs.z
		else:
			return False

	# Operator uniary minus
	def __neg__(self) -> 'Vec3':
		return Vec3(-self.x, -self.y, -self.z)

	# Add vector to vector or scalar
	def __add__(self, rhs) -> 'Vec3':
		if isinstance(rhs, Vec3):
			return Vec3(self.x + rhs.x, self.y + rhs.y, self.z + rhs.z)
		elif isinstance(rhs, (int, float)):
			return Vec3(self.x + rhs, self.y + rhs, self.z + rhs)
		else:
			raise TypeError(f"Unsupported type for addition: {type(rhs)}")

	# Subtract vector from vector or scalar
	def __sub__(self, rhs) -> 'Vec3':
		if isinstance(rhs, Vec3):
			return Vec3(self.x - rhs.x, self.y - rhs.y, self.z - rhs.z)
		elif isinstance(rhs, (int, float)):
			return Vec3(self.x - rhs, self.y - rhs, self.z - rhs)
		else:
			raise TypeError(f"Unsupported type for subtraction: {type(rhs)}")

	# Multiply vector by vector or scalar
	def __mul__(self, rhs) -> 'Vec3':
		if isinstance(rhs, Vec3):
			return Vec3(self.x * rhs.x, self.y * rhs.y, self.z * rhs.z)
		elif isinstance(rhs, (int, float)):
			return Vec3(self.x * rhs, self.y * rhs, self.z * rhs)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(rhs)}")

	# Multiple by scalar from the left
	def __rmul__(self, value) -> 'Vec3':
		return self.__mul__(value)

	# Represent the object
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self) -> str:
		return f'{self.x} {self.y} {self.z}'

	# Pack to bytes
	def __pack__(self) -> bytes:
		return struct.pack("fff", self.x, self.y, self.z)

	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes) -> 'Vec3':
		x, y, z = struct.unpack("fff", data[:12])
		return Vec3(x, y, z)

	# Constants
	def Zero() -> 'Vec3':
		return Vec3(0, 0, 0)
	def XAxis() -> 'Vec3':
		return Vec3(1, 0, 0)
	def YAxis() -> 'Vec3':
		return Vec3(0, 1, 0)
	def ZAxis() -> 'Vec3':
		return Vec3(0, 0, 1)
	def One() -> 'Vec3':
		return Vec3(1, 1, 1)

class Vec4:
	def __init__(self, *args):
		if len(args) == 0:
			self.x, self.y, self.z, self.w = 0.0, 0.0, 0.0, 0.0
		elif len(args) == 4:
			self.x, self.y, self.z, self.w = float(args[0]), float(args[1]), float(args[2]), float(args[3])
		elif len(args) == 1 and isinstance(args[0], Vec4):
			self.x, self.y, self.z, self.w = args[0].x, args[0].y, args[0].z, args[0].w
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.x, self.y, self.z, self.w = float(args[0][0]), float(args[0][1]), float(args[0][2]), float(args[0][3])
		else:
			raise TypeError(f"Unsupported type for Vec4 constructor: {type(args[0])}")

	# Convert to array
	@property
	def arr(self) -> list:
		return [self.x, self.y]

	# Convert to Vec3
	@property
	def xyz(self) -> Vec3:
		return Vec3(self.x, self.y, self.z)

	# Convert to Vec4
	def w0(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 0)

	# Convert to Vec4
	def w1(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 1)

	# Equality operator
	def __eq__(self, rhs) -> bool:
		if isinstance(rhs, Vec4):
			return self.x == rhs.x and self.y == rhs.y and self.z == rhs.z and self.w == rhs.w
		else:
			return False

	# Operator uniary minus
	def __neg__(self) -> 'Vec4':
		return Vec4(-self.x, -self.y, -self.z, -self.w)

	# Add vector to vector or scalar
	def __add__(self, rhs) -> 'Vec4':
		if isinstance(rhs, Vec4):
			return Vec4(self.x + rhs.x, self.y + rhs.y, self.z + rhs.z, self.w + rhs.w)
		elif isinstance(rhs, (int, float)):
			return Vec4(self.x + rhs, self.y + rhs, self.z + rhs, self.w + rhs)
		else:
			raise TypeError(f"Unsupported type for addition: {type(rhs)}")

	# Subtract vector from vector or scalar
	def __sub__(self, rhs) -> 'Vec4':
		if isinstance(rhs, Vec4):
			return Vec4(self.x - rhs.x, self.y - rhs.y, self.z - rhs.z, self.w - rhs.w)
		elif isinstance(rhs, (int, float)):
			return Vec4(self.x - rhs, self.y - rhs, self.z - rhs, self.w - rhs)
		else:
			raise TypeError(f"Unsupported type for subtraction: {type(rhs)}")

	# Multiply vector by vector or scalar
	def __mul__(self, rhs) -> 'Vec4':
		if isinstance(rhs, Vec4):
			return Vec4(self.x * rhs.x, self.y * rhs.y, self.z * rhs.z, self.w * rhs.w)
		elif isinstance(rhs, (int, float)):
			return Vec4(self.x * rhs, self.y * rhs, self.z * rhs, self.w * rhs)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(rhs)}")

	# Multiple by scalar from the left
	def __rmul__(self, value) -> 'Vec4':
		return self.__mul__(value)

	# Represent the object
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self) -> str:
		return f'{self.x} {self.y} {self.z} {self.w}'

	# Pack to bytes
	def __pack__(self) -> bytes:
		return struct.pack("ffff", self.x, self.y, self.z, self.w)

	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes) -> 'Vec4':
		x, y, z, w = struct.unpack("ffff", data[:16])
		return Vec4(x, y, z, w)

	# Constants
	def Zero() -> 'Vec4':
		return Vec4(0, 0, 0, 0)
	def XAxis() -> 'Vec4':
		return Vec4(1, 0, 0, 0)
	def YAxis() -> 'Vec4':
		return Vec4(0, 1, 0, 0)
	def ZAxis() -> 'Vec4':
		return Vec4(0, 0, 1, 0)
	def WAxis() -> 'Vec4':
		return Vec4(0, 0, 0, 1)
	def Origin() -> 'Vec4':
		return Vec4(0, 0, 0, 1)
	def One() -> 'Vec4':
		return Vec4(1, 1, 1, 1)

class Mat2:
	def __init__(self, *args):
		if len(args) == 0:
			self.x, self.y = Vec2.Zero(), Vec2.Zero()
		elif len(args) == 4:
			self.x, self.y = Vec2(args[0], args[1]), Vec2(args[2], args[3])
		elif len(args) == 2:
			self.x, self.y = Vec2(args[0]), Vec2(args[1])
		elif len(args) == 1 and isinstance(args[0], Mat2):
			self.x, self.y = args[0].x, args[0].y
		elif len(args) == 1 and isinstance(args[0], (list, tuple)) and len(args[0]) == 2:
			self.x, self.y = Vec2(args[0][0]), Vec2(args[0][1])
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.x, self.y = Vec2(args[0][0], args[0][1]), Vec2(args[0][2], args[0][3])
		else:
			raise TypeError(f"Unsupported type for Mat2 constructor: {type(args[0])}")

	# Convert to array
	@property
	def arr(self) -> list:
		return [
			self.x.x, self.x.y,
			self.y.x, self.y.y
		]

	# Equality operator
	def __eq__(self, rhs) -> bool:
		if isinstance(rhs, Mat2):
			return self.x == rhs.x and self.y == rhs.y
		else:
			return False

	# Operator uniary minus
	def __neg__(self) -> 'Mat2':
		return Mat2(-self.x, -self.y)

	# Add matrix to matrix or scalar
	def __add__(self, rhs) -> 'Mat2':
		if isinstance(rhs, Mat2):
			return Mat2(self.x + rhs.x, self.y + rhs.y)
		elif isinstance(rhs, (int, float)):
			return Mat2(self.x + rhs, self.y + rhs)
		else:
			raise TypeError(f"Unsupported type for addition: {type(rhs)}")

	# Subtract matrix from matrix or scalar
	def __sub__(self, rhs) -> 'Mat2':
		if isinstance(rhs, Mat2):
			return Mat2(self.x - rhs.x, self.y - rhs.y)
		elif isinstance(rhs, (int, float)):
			return Mat2(self.x - rhs, self.y - rhs)
		else:
			raise TypeError(f"Unsupported type for subtraction: {type(rhs)}")

	# Multiply matrix by matrix, or vector, or scalar
	def __mul__(self, rhs) -> 'Mat2|Vec2':
		from .operations import Transpose, Dot
		if isinstance(rhs, Mat2):
			lhsT = Transpose(self)
			return Mat2(
				Vec2(
					Dot(lhsT.x, rhs.x),
					Dot(lhsT.y, rhs.x)
				),
				Vec2(
					Dot(lhsT.x, rhs.y),
					Dot(lhsT.y, rhs.y)
				)
			)
		if isinstance(rhs, Vec2):
			lhsT = Transpose(self)
			return Vec2(
				Dot(lhsT.x, rhs),
				Dot(lhsT.y, rhs)
			)
		if isinstance(rhs, (int, float)):
			return Mat2(
				self.x * rhs,
				self.y * rhs
			)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(rhs)}")

	# Multiple by scalar from the left
	def __rmul__(self, value) -> 'Mat2':
		if isinstance(value, (int, float)):
			return Mat2(self.__mul__(value))
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(value)}")

	# Represent the object
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self) -> str:
		return f'{self.x} {self.y}'
	
	# Pack to bytes
	def __pack__(self) -> bytes:
		return self.x.__pack__() + self.y.__pack__()
	
	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes) -> 'Mat2':
		x = Vec2.__unpack__(data[0:8])
		y = Vec2.__unpack__(data[8:16])
		return Mat2(x, y)

	# Constants
	def Zero() -> 'Mat2':
		return Mat2(Vec2.Zero(), Vec2.Zero())
	def Identity() -> 'Mat2':
		return Mat2(Vec2.XAxis(), Vec2.YAxis())
	def One() -> 'Mat2':
		return Mat2(Vec2.One(), Vec2.One())

class Mat3:
	def __init__(self, *args):
		if len(args) == 0:
			self.x, self.y, self.z = Vec3.Zero(), Vec3.Zero(), Vec3.Zero()
		elif len(args) == 9:
			self.x, self.y, self.z = Vec3(args[0], args[1], args[2]), Vec3(args[3], args[4], args[5]), Vec3(args[6], args[7], args[8])
		elif len(args) == 3:
			self.x, self.y, self.z = Vec3(args[0]), Vec3(args[1]), Vec3(args[2])
		elif len(args) == 1 and isinstance(args[0], Mat3):
			self.x, self.y, self.z = args[0].x, args[0].y, args[0].z
		elif len(args) == 1 and isinstance(args[0], (list, tuple)) and len(args[0]) == 3:
			self.x, self.y, self.z = Vec3(args[0][0]), Vec3(args[0][1]), Vec3(args[0][2])
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.x, self.y, self.z = Vec3(args[0][0], args[0][1], args[0][2]), Vec3(args[0][3], args[0][4], args[0][5]), Vec3(args[0][6], args[0][7], args[0][8])
		else:
			raise TypeError(f"Unsupported type for Mat3 constructor: {type(args[0])}")

	# Convert to array
	@property
	def arr(self) -> list:
		return [
			self.x.x, self.x.y, self.x.z,
			self.y.x, self.y.y, self.y.z,
			self.z.x, self.z.y, self.z.z
		]
	
	# Convert to Mat4
	def mat4(self) -> 'Mat4':
		return Mat4(self.x.w0(), self.y.w0(), self.z.w0(), Vec4(0, 0, 0, 1))

	# Equality operator
	def __eq__(self, rhs) -> bool:
		if isinstance(rhs, Mat3):
			return self.x == rhs.x and self.y == rhs.y and self.z == rhs.z
		else:
			return False

	# Operator uniary minus
	def __neg__(self) -> 'Mat3':
		return Mat3(-self.x, -self.y, -self.z)

	# Add matrix to matrix or scalar
	def __add__(self, rhs) -> 'Mat3':
		if isinstance(rhs, Mat3):
			return Mat3(self.x + rhs.x, self.y + rhs.y, self.z + rhs.z)
		elif isinstance(rhs, (int, float)):
			return Mat3(self.x + rhs, self.y + rhs, self.z + rhs)
		else:
			raise TypeError(f"Unsupported type for addition: {type(rhs)}")

	# Subtract matrix from matrix or scalar
	def __sub__(self, rhs) -> 'Mat3':
		if isinstance(rhs, Mat3):
			return Mat3(self.x - rhs.x, self.y - rhs.y, self.z - rhs.z)
		elif isinstance(rhs, (int, float)):
			return Mat3(self.x - rhs, self.y - rhs, self.z - rhs)
		else:
			raise TypeError(f"Unsupported type for subtraction: {type(rhs)}")

	# Multiply matrix by matrix, or vector, or scalar
	def __mul__(self, rhs) -> 'Mat3|Vec3':
		from .operations import Transpose, Dot
		if isinstance(rhs, Mat3):
			lhsT = Transpose(self)
			return Mat3(
				Vec3(
					Dot(lhsT.x, rhs.x),
					Dot(lhsT.y, rhs.x),
					Dot(lhsT.z, rhs.x)
				),
				Vec3(
					Dot(lhsT.x, rhs.y),
					Dot(lhsT.y, rhs.y),
					Dot(lhsT.z, rhs.y)
				),
				Vec3(
					Dot(lhsT.x, rhs.z),
					Dot(lhsT.y, rhs.z),
					Dot(lhsT.z, rhs.z)
				)
			)
		if isinstance(rhs, Vec3):
			lhsT = Transpose(self)
			return Vec3(
				Dot(lhsT.x, rhs),
				Dot(lhsT.y, rhs),
				Dot(lhsT.z, rhs)
			)
		if isinstance(rhs, (int, float)):
			return Mat3(
				self.x * rhs,
				self.y * rhs,
				self.z * rhs
			)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(rhs)}")

	# Multiple by scalar from the left
	def __rmul__(self, value) -> 'Mat3':
		if isinstance(value, (int, float)):
			return Mat3(self.__mul__(value))
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(value)}")

	# Represent the object
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self) -> str:
		return f'{self.x} {self.y} {self.z}'

	# Pack to bytes
	def __pack__(self) -> bytes:
		return self.x.__pack__() + self.y.__pack__() + self.z.__pack__()

	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes) -> 'Mat3':
		x = Vec3.__unpack__(data[ 0:12])
		y = Vec3.__unpack__(data[12:24])
		z = Vec3.__unpack__(data[24:36])
		return Mat3(x, y, z)

	# Create a rotation matrix around an axis by an angle in radians
	@staticmethod
	def Rotation(axis: Vec3, angle: float) -> 'Mat3':
		c = math.cos(angle)
		s = math.sin(angle)
		t = axis * (1 - c)
		
		m = Mat3()
		m.x.x = t.x * axis.x + c
		m.y.y = t.y * axis.y + c
		m.z.z = t.z * axis.z + c
		t *= axis
		s_axis = s * axis
		m.x.y = t.x + s_axis.z
		m.x.z = t.z - s_axis.y
		m.y.x = t.x - s_axis.z
		m.y.z = t.y + s_axis.x
		m.z.x = t.z + s_axis.y
		m.z.y = t.y - s_axis.x
		return m

	# Constants
	def Zero() -> 'Mat3':
		return Mat3(Vec3.Zero(), Vec3.Zero(), Vec3.Zero())
	def Identity() -> 'Mat3':
		return Mat3(Vec3.XAxis(), Vec3.YAxis(), Vec3.ZAxis())
	def One() -> 'Mat3':
		return Mat3(Vec3.One(), Vec3.One(), Vec3.One())

class Mat4:
	def __init__(self, *args):
		if len(args) == 0:
			self.x, self.y, self.z, self.w = Vec4.Zero(), Vec4.Zero(), Vec4.Zero(), Vec4.Zero()
		elif len(args) == 16:
			self.x, self.y, self.z, self.w = Vec4(args[0], args[1], args[2], args[3]), Vec4(args[4], args[5], args[6], args[7]), Vec4(args[8], args[9], args[10], args[11]), Vec4(args[12], args[13], args[14], args[15])
		elif len(args) == 4:
			self.x, self.y, self.z, self.w = Vec4(args[0]), Vec4(args[1]), Vec4(args[2]), Vec4(args[3])
		elif len(args) == 2 and isinstance(args[0], Mat3) and isinstance(args[1], Vec3):
			self.x, self.y, self.z, self.w = args[0].x.w0(), args[0].y.w0(), args[0].z.w0(), args[1].w1()
		elif len(args) == 1 and isinstance(args[0], Mat4):
			self.x, self.y, self.z, self.w = args[0].x, args[0].y, args[0].z, args[0].w
		elif len(args) == 1 and isinstance(args[0], (list, tuple)) and len(args[0]) == 4:
			self.x, self.y, self.z, self.w = Vec4(args[0][0]), Vec4(args[0][1]), Vec4(args[0][2]), Vec4(args[0][3])
		elif len(args) == 1 and isinstance(args[0], (list, tuple)):
			self.x, self.y, self.z, self.w = Vec4(args[0][0], args[0][1], args[0][2], args[0][3]), Vec4(args[0][4], args[0][5], args[0][6], args[0][7]), Vec4(args[0][8], args[0][9], args[0][10], args[0][11]), Vec4(args[0][12], args[0][13], args[0][14], args[0][15])
		else:
			raise TypeError(f"Unsupported type for Mat4 constructor: {type(args[0])}")

	# Convert to array
	@property
	def arr(self) -> list:
		return [
			self.x.x, self.x.y, self.x.z, self.x.w,
			self.y.x, self.y.y, self.y.z, self.y.w,
			self.z.x, self.z.y, self.z.z, self.z.w,
			self.w.x, self.w.y, self.w.z, self.w.w
		]

	# Convert to Mat3
	@property
	def rot(self) -> Mat3:
		return Mat3(self.x.xyz, self.y.xyz, self.z.xyz)

	# Test is identity matrix
	@property
	def is_identity(self) -> bool:
		return self == Mat4.Identity()

	# Test is pure translation
	@property
	def is_translation(self) -> bool:
		return self.rot == Mat3.Identity() and self.w.w == 1

	# Test is affine matrix
	@property
	def is_affine(self) -> bool:
		return self.x.w == 0 and self.y.w == 0 and self.z.w == 0 and self.w.w == 1

	# Equality operator
	def __eq__(self, rhs) -> bool:
		if isinstance(rhs, Mat4):
			return self.x == rhs.x and self.y == rhs.y and self.z == rhs.z and self.w == rhs.w
		else:
			return False

	# Operator uniary minus
	def __neg__(self) -> 'Mat4':
		return Mat4(-self.x, -self.y, -self.z, -self.w)

	# Add matrix to matrix or scalar
	def __add__(self, rhs) -> 'Mat4':
		if isinstance(rhs, Mat4):
			return Mat4(self.x + rhs.x, self.y + rhs.y, self.z + rhs.z, self.w + rhs.w)
		elif isinstance(rhs, (int, float)):
			return Mat4(self.x + rhs, self.y + rhs, self.z + rhs, self.w + rhs)
		else:
			raise TypeError(f"Unsupported type for addition: {type(rhs)}")

	# Subtract matrix from matrix or scalar
	def __sub__(self, rhs) -> 'Mat4':
		if isinstance(rhs, Mat4):
			return Mat4(self.x - rhs.x, self.y - rhs.y, self.z - rhs.z, self.w - rhs.w)
		elif isinstance(rhs, (int, float)):
			return Mat4(self.x - rhs, self.y - rhs, self.z - rhs, self.w - rhs)
		else:
			raise TypeError(f"Unsupported type for subtraction: {type(rhs)}")

	# Multiply matrix by matrix, or vector, or scalar
	def __mul__(self, rhs) -> 'Mat4|Vec4':
		from .operations import Transpose, Dot
		if isinstance(rhs, Mat4):
			lhsT = Transpose(self)
			return Mat4(
				Vec4(
					Dot(lhsT.x, rhs.x),
					Dot(lhsT.y, rhs.x),
					Dot(lhsT.z, rhs.x),
					Dot(lhsT.w, rhs.x)
				),
				Vec4(
					Dot(lhsT.x, rhs.y),
					Dot(lhsT.y, rhs.y),
					Dot(lhsT.z, rhs.y),
					Dot(lhsT.w, rhs.y)
				),
				Vec4(
					Dot(lhsT.x, rhs.z),
					Dot(lhsT.y, rhs.z),
					Dot(lhsT.z, rhs.z),
					Dot(lhsT.w, rhs.z)
				),
				Vec4(
					Dot(lhsT.x, rhs.w),
					Dot(lhsT.y, rhs.w),
					Dot(lhsT.z, rhs.w),
					Dot(lhsT.w, rhs.w)
				)
			)
		if isinstance(rhs, Vec4):
			lhsT = Transpose(self)
			return Vec4(
				Dot(lhsT.x, rhs),
				Dot(lhsT.y, rhs),
				Dot(lhsT.z, rhs),
				Dot(lhsT.w, rhs)
			)
		if isinstance(rhs, (int, float)):
			return Mat4(
				self.x * rhs,
				self.y * rhs,
				self.z * rhs,
				self.w * rhs
			)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(rhs)}")

	# Multiple by scalar from the left
	def __rmul__(self, value) -> 'Mat4':
		if isinstance(value, (int, float)):
			return Mat4(self.__mul__(value))
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(value)}")

	# Represent the object
	def __repr__(self):
		return str(self)

	# Convert to string
	def __str__(self) -> str:
		return f'{str(self.x)} {str(self.y)} {str(self.z)} {str(self.w)}'

	# Pack to bytes
	def __pack__(self) -> bytes:
		return self.x.__pack__() + self.y.__pack__() + self.z.__pack__() + self.w.__pack__()

	# Unpack from bytes
	@staticmethod
	def __unpack__(data: bytes) -> 'Mat4':
		x = Vec4.__unpack__(data[ 0:16])
		y = Vec4.__unpack__(data[16:32])
		z = Vec4.__unpack__(data[32:48])
		w = Vec4.__unpack__(data[48:64])
		return Mat4(x, y, z, w)

	# Constants
	def Zero() -> 'Mat4':
		return Mat4(Vec4.Zero(), Vec4.Zero(), Vec4.Zero(), Vec4.Zero())
	def Identity() -> 'Mat4':
		return Mat4(Vec4.XAxis(), Vec4.YAxis(), Vec4.ZAxis(), Vec4.WAxis())
	def One() -> 'Mat4':
		return Mat4(Vec4.One(), Vec4.One(), Vec4.One(), Vec4.One())

__all__ = [name for name,_ in globals().items() if not name.startswith("_")]