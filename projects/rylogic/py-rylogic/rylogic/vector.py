#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import struct

class Vec2:
	def __init__(self, x: float=0, y: float=0):
		self.x = x
		self.y = y

	def vec3(self, z: float=0) -> 'Vec3':
		return Vec3(self.x, self.y, z)
	
	def w0(self, z: float=0) -> 'Vec4':
		return Vec4(self.x, self.y, z, 0)
	
	def w1(self, z: float=0) -> 'Vec4':
		return Vec4(self.x, self.y, z, 1)
	
	def __add__(self, other) -> 'Vec2':
		if isinstance(other, Vec2):
			return Vec2(self.x + other.x, self.y + other.y)
		elif isinstance(other, (int, float)):
			return Vec2(self.x + other, self.y + other)
		else:
			raise TypeError(f"Unsupported type for addition: {type(other)}")
		
	def __mul__(self, other) -> 'Vec2':
		if isinstance(other, Vec2):
			return Vec2(self.x * other.x, self.y * other.y)
		elif isinstance(other, (int, float)):
			return Vec2(self.x * other, self.y * other)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(other)}")
		
	def __str__(self) -> str:
		return f'{self.x} {self.y}'
	
	def __pack__(self) -> bytes:
		return struct.pack("ff", self.x, self.y)
	
class Vec3:
	def __init__(self, x: float=0, y: float=0, z: float=0):
		self.x = x
		self.y = y
		self.z = z

	def w0(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 0)

	def w1(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 1)

	def is_zero(self) -> bool:
		return self.x == 0 and self.y == 0 and self.z == 0

	def __add__(self, other) -> 'Vec3':
		if isinstance(other, Vec3):
			return Vec3(self.x + other.x, self.y + other.y, self.z + other.z)
		elif isinstance(other, (int, float)):
			return Vec3(self.x + other, self.y + other, self.z + other)
		else:
			raise TypeError(f"Unsupported type for addition: {type(other)}")

	def __mul__(self, other) -> 'Vec3':
		if isinstance(other, Vec3):
			return Vec3(self.x * other.x, self.y * other.y, self.z * other.z)
		elif isinstance(other, (int, float)):
			return Vec3(self.x * other, self.y * other, self.z * other)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(other)}")

	def __rmul__(self, value) -> 'Vec3':
		return self.__mul__(value)

	def __str__(self) -> str:
		return f'{self.x} {self.y} {self.z}'

	def __pack__(self) -> bytes:
		return struct.pack("fff", self.x, self.y, self.z)

class Vec4:
	def __init__(self, x: float=0, y: float=0, z: float=0, w: float=1):
		self.x = x
		self.y = y
		self.z = z
		self.w = w

	def w0(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 0)

	def w1(self) -> 'Vec4':
		return Vec4(self.x, self.y, self.z, 1)

	@property
	def xyz(self) -> Vec3:
		return Vec3(self.x, self.y, self.z)

	def is_zero(self) -> bool:
		return self.x == 0 and self.y == 0 and self.z == 0 and self.w == 0

	def is_origin(self) -> bool:
		return self.x == 0 and self.y == 0 and self.z == 0 and self.w == 1

	def __add__(self, other) -> 'Vec4':
		if isinstance(other, Vec4):
			return Vec4(self.x + other.x, self.y + other.y, self.z + other.z, self.w + other.w)
		elif isinstance(other, (int, float)):
			return Vec4(self.x + other, self.y + other, self.z + other, self.w + other)
		else:
			raise TypeError(f"Unsupported type for addition: {type(other)}")

	def __mul__(self, other) -> 'Vec4':
		if isinstance(other, Vec4):
			return Vec4(self.x * other.x, self.y * other.y, self.z * other.z, self.w * other.w)
		elif isinstance(other, (int, float)):
			return Vec4(self.x * other, self.y * other, self.z * other, self.w * other)
		else:
			raise TypeError(f"Unsupported type for multiplication: {type(other)}")

	def __str__(self) -> str:
		return f'{self.x} {self.y} {self.z} {self.w}'

	def __pack__(self) -> bytes:
		return struct.pack("ffff", self.x, self.y, self.z, self.w)

class Mat3:
	def __init__(self, x: Vec3=Vec3(1,0,0), y: Vec3=Vec3(0,1,0), z: Vec3=Vec3(0,0,1)):
		self.x = x
		self.y = y
		self.z = z

	def mat4(self) -> 'Mat4':
		return Mat4(self.x.w0(), self.y.w0(), self.z.w0(), Vec4(0, 0, 0, 1))

	def is_identity(self) -> bool:
		return self == Mat3(Vec3(1,0,0), Vec3(0,1,0), Vec3(0,0,1))

	def __str__(self) -> str:
		return f'{self.x} {self.y} {self.z}'

	def __pack__(self) -> bytes:
		return self.x.__pack__() + self.y.__pack__() + self.z.__pack__()

class Mat4:
	def __init__(self, x: Vec4=Vec4(1,0,0,0), y: Vec4=Vec4(0,1,0,0), z: Vec4=Vec4(0,0,1,0), w: Vec4=Vec4(0,0,0,1)):
		self.x = x
		self.y = y
		self.z = z
		self.w = w

	@property
	def rot(self) -> Mat3:
		return Mat3(self.x.xyz, self.y.xyz, self.z.xyz)

	def is_identity(self) -> bool:
		return self == Mat4(Vec4(1,0,0,0), Vec4(0,1,0,0), Vec4(0,0,1,0), Vec4(0,0,0,1))

	def is_translation(self) -> bool:
		return self.rot.is_identity() and self.w.w == 1
	
	def is_affine(self) -> bool:
		return self.x.w == 0 and self.y.w == 0 and self.z.w == 0 and self.w.w == 1

	def __str__(self) -> str:
		return f'{str(self.x)} {str(self.y)} {str(self.z)} {str(self.w)}'

	def __pack__(self) -> bytes:
		return self.x.__pack__() + self.y.__pack__() + self.z.__pack__() + self.w.__pack__()
	