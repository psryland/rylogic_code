#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
from typing import List, NamedTuple

class Vec2(NamedTuple):
	x: float
	y: float
	def vec3(self, z: float=0):
		return Vec3(self.x, self.y, z)
	def w0(self, z: float=0):
		return Vec4(self.x, self.y, z, 0)
	def w1(self, z: float=0):
		return Vec4(self.x, self.y, z, 1)
	def __str__(self):
		return f'{self.x} {self.y}'
class Vec3(NamedTuple):
	x: float
	y: float
	z: float
	def w0(self):
		return Vec4(self.x, self.y, self.z, 0)
	def w1(self):
		return Vec4(self.x, self.y, self.z, 1)
	def is_zero(self):
		return all(v == 0 for v in self)
	def __str__(self):
		return f'{self.x} {self.y} {self.z}'
class Vec4(NamedTuple):
	x: float
	y: float
	z: float
	w: float
	def w0(self):
		return Vec4(self.x, self.y, self.z, 0)
	def w1(self):
		return Vec4(self.x, self.y, self.z, 1)
	@property
	def xyz(self):
		return Vec3(self.x, self.y, self.z)
	def is_zero(self):
		return all(v == 0 for v in self)
	def is_origin(self):
		return all(v == 0 for v in self[:3]) and self.w == 1
	def __str__(self):
		return f'{self.x} {self.y} {self.z} {self.w}'
class Mat3(NamedTuple):
	x: Vec3
	y: Vec3
	z: Vec3
	def mat4(self):
		return Mat4(self.x.w0(), self.y.w0(), self.z.w0(), Vec4(0, 0, 0, 1))
	def is_identity(self):
		return self == Mat3(Vec3(1,0,0), Vec3(0,1,0), Vec3(0,0,1))
	def __str__(self):
		return f'{str(self.x)} {str(self.y)} {str(self.z)}'
class Mat4(NamedTuple):
	x: Vec4
	y: Vec4
	z: Vec4
	w: Vec4
	@property
	def xyz(self):
		return Mat3(self.x, self.y, self.z)
	def rot(self):
		return Mat3(self.x.xyz, self.y.xyz, self.z.xyz)
	def is_identity(self):
		return self == Mat4(Vec4(1,0,0,0), Vec4(0,1,0,0), Vec4(0,0,1,0), Vec4(0,0,0,1))
	def is_translation(self):
		return self.xyz.is_identity() and self.w.w == 1
	def is_affine(self):
		return self.x.w == 0 and self.y.w == 0 and self.z.w == 0 and self.w.w == 1
	def __str__(self):
		return f'{str(self.x)} {str(self.y)} {str(self.z)} {str(self.w)}'

class _LdrObj:
	def __init__(self):
		self.m_objects :List['_LdrObj'] = []

	# Child objects
	def Group(self, name: str = '', colour: int = 0xFFFFFFFF):
		grp = LdrGroup()
		self.m_objects.append(grp)
		return grp.name(name).col(colour)
	def Points(self, name: str = '', colour: int = 0xFFFFFFFF):
		pts = LdrPoint()
		self.m_objects.append(pts)
		return pts.name(name).col(colour)
	def Polygon(self, name: str = '', colour: int = 0xFFFFFFFF):
		poly = LdrPolygon()
		self.m_objects.append(poly)
		return poly.name(name).col(colour)

	# Serialise to a string
	def ToString(self) -> str:
		ldr = []
		self._Serialise(ldr)
		return ''.join(ldr)

	# Write ldr script to a file
	def Write(self, path: str):
		with open(path, 'w') as f:
			f.write(self.ToString())

	# Stream over a network connection to LDraw
	def Transmit(self, host: str = "localhost", port: int = 7614):
		
		# Protocol:
		#  1b - 
		return
	
	# Recursively serialise this object to string 'ldr'
	def _Serialise(self, ldr: List[str]):
		self._NestedSerialize(ldr)

	# Write nested objects to 'str'
	def _NestedSerialize(self, ldr: List[str]):
		for s in self.m_objects:
			s._Serialise(ldr)

	# Append a list of arguments to 'ldr'
	def _Append(self, ldr: List[str], *args):
		for a in args:
			last = ldr[-1][-1] if len(ldr) != 0 and len(ldr[-1]) != 0 else None
			if last and not last.isspace() and last != '{' and last != '}': ldr.append(' ')
			ldr.append(str(a))

class _LdrBase(_LdrObj):
	def __init__(self):
		super().__init__()
		self.m_name :_LdrName = ''
		self.m_o2w :_LdrO2W = _LdrO2W()
		self.m_colour :_LdrColour = _LdrColour()
		self.m_wire :_LdrWireframe = _LdrWireframe()
		self.m_solid :_LdrSolid = _LdrSolid()

	def name(self, name: str):
		self.m_name = _LdrName(name)
		return self

	def col(self, colour: int):
		self.m_colour = _LdrColour(colour)
		return self

	def pos(self, xyz: Vec3):
		self.m_o2w = _LdrO2W(pos=xyz)
		return self

	def o2w(self, o2w: Mat4):
		self.m_o2w = _LdrO2W(o2w=o2w)
		return self

	def scale(self, xyz: Vec3):
		self.m_o2w = _LdrO2W(o2w=Mat4(Vec4(xyz.x, 0, 0, 0), Vec4(0, xyz.y, 0, 0), Vec4(0, 0, xyz.z, 0), Vec4(0, 0, 0, 1)))
		return self

	def _NestedSerialize(self, ldr: List[str]):
		super()._NestedSerialize(ldr)
		self._Append(ldr, self.m_wire, self.m_solid, self.m_o2w)

class LdrPoint(_LdrBase):
	def __init__(self):
		super().__init__()
		self.m_points: List[Vec3] = []
		self.m_colours: List[int] = []
		self.m_size: _LdrSize = _LdrSize()
		self.m_depth: _LdrDepth = _LdrDepth()

	def pt(self, xyz: Vec3, colour: int=None):
		self.m_points.append(xyz)
		if colour: self.m_colours.append(colour)
		return self

	def size(self, size: float):
		self.m_size = _LdrSize(size)
		return self
	
	def depth(self, depth: bool=True):
		self.m_depth = _LdrDepth(depth)
		return self

	def _Serialise(self, ldr: List[str]):
		self._Append(ldr, '*Point', self.m_name, self.m_colour, "{\n", self.m_size, "\n", self.m_depth, "\n")
		if len(self.m_points) == len(self.m_colours):
			for p,c in zip(self.m_points, self.m_colours):
				self._Append(ldr, p, f'{c:X}', "\n")
		else:
			for p in self.m_points:
				self._Append(ldr, p, "\n")
		self._Append(ldr, "}\n")

class LdrPolygon(_LdrBase):
	def __init__(self):
		super().__init__()
		self.m_points: List[Vec2] = []

	def pt(self, xy: Vec2):
		self.m_points.append(xy)
		return self

	def solid(self, solid: bool=True):
		self.m_solid = _LdrSolid(solid)
		return self

	def _Serialise(self, ldr: List[str]):
		self._Append(ldr, '*Polygon', self.m_name, self.m_colour, "{\n", self.m_solid, "\n")
		for p in self.m_points:
			self._Append(ldr, p, "\n")
		self._Append(ldr, "}\n")

class LdrGroup(_LdrBase):
	def __init__(self):
		super().__init__()

	# Recursively serialise this object to string 'ldr'
	def _Serialise(self, ldr: List[str]):
		self._Append(ldr, '*Group', self.m_name, self.m_colour, "{\n")
		self._NestedSerialize(ldr)
		while ldr[-1] == '\n': ldr.pop()
		self._Append(ldr, '\n', '}\n')

class LdrBuilder(_LdrObj):
	def __init__(self):
		super().__init__()

# Implementation ------------------------------------------

class _LdrName:
	def __init__(self, name: str):
		super().__init__()
		self.m_name = name
	def __str__(self):
		return self.m_name if self.m_name else ''
class _LdrColour:
	def __init__(self, colour: int=0xFFFFFFFF):
		super().__init__()
		self.m_colour = colour
	def __str__(self):
		return f'{self.m_colour:X}' if self.m_colour != 0xFFFFFFFF else ''
class _LdrSize:
	def __init__(self, size: float=0):
		super().__init__()
		self.m_size = size
	def __str__(self):
		return f'*Size {{{self.m_size}}}' if self.m_size != 0 else ''
class _LdrDepth:
	def __init__(self, depth: bool=False):
		super().__init__()
		self.m_depth = depth
	def __str__(self):
		return '*Depth' if self.m_depth else ''
class _LdrWireframe:
	def __init__(self, wire: bool=False):
		super().__init__()
		self.m_wire = wire
	def __str__(self):
		return '*Wireframe' if self.m_wire else ''
class _LdrSolid:
	def __init__(self, solid: bool=False):
		super().__init__()
		self.m_solid = solid
	def __str__(self):
		return '*Solid' if self.m_solid else ''
class _LdrPos:
	def __init__(self, xyz: Vec3=None):
		super().__init__()
		self.m_pos = xyz
	def __str__(self):
		if not self.m_pos: return ''
		if self.m_pos.is_zero(): return ''
		return f'*O2W{{*Pos{{{self.m_pos}}}}}'
class _LdrO2W:
	def __init__(self, o2w: Mat4=None, pos:Vec3=None):
		super().__init__()
		self.m_o2w = None
		if o2w: self.m_o2w = o2w
		if pos: self.m_o2w = Mat4(Vec4(1,0,0,0), Vec4(0,1,0,0), Vec4(0,0,1,0), pos.w1())
	def __str__(self):
		if not self.m_o2w: return ''
		if self.m_o2w.is_identity(): return ''
		if self.m_o2w.is_translation(): return f'*O2W{{*Pos{{{self.m_o2w.w.xyz}}}}}'
		return f'*O2W{{*M4x4{{{self.m_o2w}}}}}'

# Tests ---------------------------------------------------
if True:
	builder = LdrBuilder()
	ldr_group = builder.Group("Group", 0xFF0000FF)
	ldr_points = ldr_group.Points("Points", 0xFF00FF00)
	ldr_points.pt(Vec3(0, 0, 0), 0xFF0000FF)
	ldr_points.pt(Vec3(1, 1, 1), 0xFF00FF00)
	ldr_points.pt(Vec3(2, 2, 2), 0xFFFF0000)
	ldr_group.pos(Vec3(1,1,1))
	print(builder.ToString())

# Exports -------------------------------------------------
__all__ = [
	'Vec2',
	'Vec3',
	'Vec4',
	'Mat3',
	'Mat4',
	'LdrPoint',
	'LdrGroup',
	'LdrBuilder',
]
