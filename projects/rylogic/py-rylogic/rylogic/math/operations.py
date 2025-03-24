#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
from .vector import *
from typing import Optional

# Square a value
def Sqr(x: float) -> float:
	"""Square a value."""
	return x * x

# Squared Length of a vector
def LengthSq(v: Vec2|Vec3|Vec4) -> float:
	"""Squared length of a vector."""
	if isinstance(v, Vec2):
		return Sqr(v.x) + Sqr(v.y)
	elif isinstance(v, Vec3):
		return Sqr(v.x) + Sqr(v.y) + Sqr(v.z)
	elif isinstance(v, Vec4):
		return Sqr(v.x) + Sqr(v.y) + Sqr(v.z) + Sqr(v.w)
	else:
		raise TypeError(f"Unsupported type for length squared: {type(v)}")

# Length of a vector
def Length(v: Vec2|Vec3|Vec4) -> float:
	"""Length of a vector."""
	return LengthSq(v) ** 0.5

# Dot product
def Dot(lhs: Vec2|Vec3|Vec4, rhs: Vec2|Vec3|Vec4) -> float:
	"""Dot product of two vectors."""
	if isinstance(lhs, Vec2) and isinstance(rhs, Vec2):
		return lhs.x * rhs.x + lhs.y * rhs.y
	elif isinstance(lhs, Vec3) and isinstance(rhs, Vec3):
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z
	elif isinstance(lhs, Vec4) and isinstance(rhs, Vec4):
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w
	else:
		raise TypeError(f"Unsupported types for dot product: {type(lhs)}, {type(rhs)}")

# Cross product
def Cross(lhs: Vec2|Vec3, rhs: Vec2|Vec3) -> float|Vec3:
	"""Cross product of two vectors."""
	if isinstance(lhs, Vec2) and isinstance(rhs, Vec2):
		return lhs.x * rhs.y - lhs.y * rhs.x
	elif isinstance(lhs, Vec3) and isinstance(rhs, Vec3):
		return Vec3(
			lhs.y * rhs.z - lhs.z * rhs.y,
			lhs.z * rhs.x - lhs.x * rhs.z,
			lhs.x * rhs.y - lhs.y * rhs.x
		)
	else:
		raise TypeError(f"Unsupported types for cross product: {type(lhs)}, {type(rhs)}")

# Normalise a vector
def Normalise(v: Vec2|Vec3|Vec4, default: Optional[Vec2|Vec3|Vec4] = None) -> Vec2|Vec3|Vec4:
	"""Normalise a vector."""
	l = Length(v)
	if l == 0:
		return default if default is not None else v
	if isinstance(v, Vec2):
		return Vec2(v.x / l, v.y / l)
	elif isinstance(v, Vec3):
		return Vec3(v.x / l, v.y / l, v.z / l)
	elif isinstance(v, Vec4):
		return Vec4(v.x / l, v.y / l, v.z / l, v.w / l)
	else:
		raise TypeError(f"Unsupported type for normalise: {type(v)}")

# Matrix Transpose
def Transpose(m: Mat2|Mat3|Mat4) -> Mat2|Mat3|Mat4:
	"""Transpose a matrix."""
	if isinstance(m, Mat2):
		return Mat2([
			[m.x.x, m.y.x],
			[m.x.y, m.y.y]
		])
	elif isinstance(m, Mat3):
		return Mat3([
			[m.x.x, m.y.x, m.z.x],
			[m.x.y, m.y.y, m.z.y],
			[m.x.z, m.y.z, m.z.z]
		])
	elif isinstance(m, Mat4):
		return Mat4([
			[m.x.x, m.y.x, m.z.x, m.w.x],
			[m.x.y, m.y.y, m.z.y, m.w.y],
			[m.x.z, m.y.z, m.z.z, m.w.z],
			[m.x.w, m.y.w, m.z.w, m.w.w]
		])
	else:
		raise TypeError(f"Unsupported type for transpose: {type(m)}")

#
__all__ = [name for name, obj in locals().items() if not name.startswith("_") and callable(obj)]
