#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, re, struct
from typing import List, NamedTuple, Optional, Tuple
from enum import Enum

# Binary packing helper
def _Pack(obj) -> bytes:
	if callable(obj):
		result = obj()
		if not isinstance(result, bytes): raise TypeError(f"Unsupported callable return type: {type(result)}")
		return result
	if isinstance(obj, bytes):
		return obj
	if hasattr(obj, '__pack__'):
		return obj.__pack__()
	if isinstance(obj, int):
		return struct.pack('<I', obj)
	if isinstance(obj, float):
		return struct.pack('<f', obj)
	if isinstance(obj, Enum):
		return struct.pack('<I', obj.value)
	if isinstance(obj, str):
		return obj.encode('utf-8')
	if isinstance(obj, list):
		return b''.join([_Pack(i) for i in obj])
	raise TypeError(f"Unsupported type: {type(obj)}")

# String packing helper
def _Str(obj) -> str:
	if callable(obj):
		result = obj()
		if not isinstance(result, str): raise TypeError(f"Unsupported callable return type: {type(result)}")
		return result
	if isinstance(obj, str):
		return obj
	if isinstance(obj, Enum) and not isinstance(obj, EKeyword):
		return obj.name
	if isinstance(obj, (bytes, bytearray)):
		return ''.join([f"{i:02X}" for i in obj])
	if isinstance(obj, list):
		return ' '.join([_Str(i) for i in obj])
	if hasattr(obj, '__str__'):
		return str(obj)
	raise TypeError(f"Unsupported type: {type(obj)}")

# Append a list of arguments to 'ldr'
def _Append(out: List[str]|bytearray, *args):
	if isinstance(out, bytearray):
		for a in args:
			abytes = bytes() if a is None else _Pack(a)
			if not abytes: continue
			out.extend(abytes)
	elif isinstance(out, list):
		for a in args:
			astr = '' if a is None else _Str(a)
			if not astr: continue
			last = out[-1][-1] if len(out) != 0 and len(out[-1]) != 0 else None
			out.append(' ' if last and not last.isspace() and last != '{' and astr[0] != '}' else '')
			out.append(astr)

# Write a ldraw block to 'out'
def _Write(out: List[str]|bytearray, keyword: 'EKeyword', *args):
	if isinstance(out, bytearray):
		ofs = len(out)
		_Append(out, keyword)
		_Append(out, 0)

		# if len(args) != 0 and isinstance(args[0], _LdrName):
		# 	if args[0].m_name: _Append(out, args[0].m_name)
		# 	args = args[1:]
		# if len(args) != 0 and isinstance(args[0], _LdrColour):
		# 	if args[0].m_colour.argb != 0xFFFFFFFF: _Append(out, args[0].m_colour)
		# 	args = args[1:]

		for a in args:
			if callable(a):
				a()
			else:
				_Append(out, a)

		size = len(out) - ofs - 8
		out[ofs+4:ofs+8] = struct.pack('<I', size)
	if isinstance(out, list):
		_Append(out, keyword)

		if len(args) != 0 and isinstance(args[0], _LdrName):
			if args[0].m_name: _Append(out, args[0].m_name)
			args = args[1:]
		if len(args) != 0 and isinstance(args[0], _LdrColour):
			if args[0].m_colour.argb != 0xFFFFFFFF: _Append(out, args[0].m_colour)
			args = args[1:]

		_Append(out, '{')
		for a in args:
			if callable(a):
				a()
			else:
				_Append(out, a)
		_Append(out, '}')

# Hash a string to a constant value
def HashI(s: str):
	_FNV_offset_basis32 = 2166136261
	_FNV_prime32 = 16777619
	h = _FNV_offset_basis32
	for c in s.lower(): h = 0xFFFFFFFF & ((h ^ ord(c)) * _FNV_prime32)
	return h

# Pretty format Ldraw script
def FormatScript(s: str, indent_str: str = '\t') -> str:
	out = []
	indent = 0
	for c in s:
		if c == '{':
			indent += 1
			out.append(c)
			out.append('\n')
			out.append(indent * indent_str)
		elif c == '}':
			indent -= 1
			out.append('\n')
			out.append(indent * indent_str)
			out.append(c)
			out.append('\n')
			out.append(indent * indent_str)
		else:
			out.append(c)

	return ''.join(out)

# Ldraw keyword values
class EKeyword(Enum):
	# AUTO-GENERATED-KEYWORDS-BEGIN
	Accel = HashI("Accel")
	Addr = HashI("Addr")
	Align = HashI("Align")
	Alpha = HashI("Alpha")
	Ambient = HashI("Ambient")
	Anchor = HashI("Anchor")
	AngAccel = HashI("AngAccel")
	AngVelocity = HashI("AngVelocity")
	Animation = HashI("Animation")
	Arrow = HashI("Arrow")
	Aspect = HashI("Aspect")
	Axis = HashI("Axis")
	AxisId = HashI("AxisId")
	BackColour = HashI("BackColour")
	BakeTransform = HashI("BakeTransform")
	Bar = HashI("Bar")
	Billboard = HashI("Billboard")
	Billboard3D = HashI("Billboard3D")
	Box = HashI("Box")
	BoxList = HashI("BoxList")
	Camera = HashI("Camera")
	CastShadow = HashI("CastShadow")
	Chart = HashI("Chart")
	Circle = HashI("Circle")
	Closed = HashI("Closed")
	Colour = HashI("Colour")
	ColourMask = HashI("ColourMask")
	Colours = HashI("Colours")
	Commands = HashI("Commands")
	Cone = HashI("Cone")
	ConvexHull = HashI("ConvexHull")
	CoordFrame = HashI("CoordFrame")
	CornerRadius = HashI("CornerRadius")
	CrossSection = HashI("CrossSection")
	CString = HashI("CString")
	Custom = HashI("Custom")
	Cylinder = HashI("Cylinder")
	Dashed = HashI("Dashed")
	Data = HashI("Data")
	Depth = HashI("Depth")
	Diffuse = HashI("Diffuse")
	Dim = HashI("Dim")
	Direction = HashI("Direction")
	Divisions = HashI("Divisions")
	Equation = HashI("Equation")
	Euler = HashI("Euler")
	Faces = HashI("Faces")
	Facets = HashI("Facets")
	Far = HashI("Far")
	FilePath = HashI("FilePath")
	Filter = HashI("Filter")
	Font = HashI("Font")
	ForeColour = HashI("ForeColour")
	Format = HashI("Format")
	Fov = HashI("Fov")
	FovX = HashI("FovX")
	FovY = HashI("FovY")
	FrustumFA = HashI("FrustumFA")
	FrustumWH = HashI("FrustumWH")
	GenerateNormals = HashI("GenerateNormals")
	Grid = HashI("Grid")
	Group = HashI("Group")
	Hidden = HashI("Hidden")
	Instance = HashI("Instance")
	Inverse = HashI("Inverse")
	Layers = HashI("Layers")
	LeftHanded = HashI("LeftHanded")
	LightSource = HashI("LightSource")
	Line = HashI("Line")
	LineBox = HashI("LineBox")
	LineD = HashI("LineD")
	LineList = HashI("LineList")
	Lines = HashI("Lines")
	LineStrip = HashI("LineStrip")
	LookAt = HashI("LookAt")
	M3x3 = HashI("M3x3")
	M4x4 = HashI("M4x4")
	Mesh = HashI("Mesh")
	Model = HashI("Model")
	Name = HashI("Name")
	Near = HashI("Near")
	NewLine = HashI("NewLine")
	NonAffine = HashI("NonAffine")
	Normalise = HashI("Normalise")
	Normals = HashI("Normals")
	NoZTest = HashI("NoZTest")
	NoZWrite = HashI("NoZWrite")
	O2W = HashI("O2W")
	Orthographic = HashI("Orthographic")
	Orthonormalise = HashI("Orthonormalise")
	Padding = HashI("Padding")
	Param = HashI("Param")
	Parametrics = HashI("Parametrics")
	Part = HashI("Part")
	Period = HashI("Period")
	PerItemColour = HashI("PerItemColour")
	PerItemParametrics = HashI("PerItemParametrics")
	Pie = HashI("Pie")
	Plane = HashI("Plane")
	Point = HashI("Point")
	Polygon = HashI("Polygon")
	Pos = HashI("Pos")
	Position = HashI("Position")
	Quad = HashI("Quad")
	Quat = HashI("Quat")
	QuatPos = HashI("QuatPos")
	Rand4x4 = HashI("Rand4x4")
	RandColour = HashI("RandColour")
	RandOri = HashI("RandOri")
	RandPos = HashI("RandPos")
	Range = HashI("Range")
	Rect = HashI("Rect")
	Reflectivity = HashI("Reflectivity")
	Resolution = HashI("Resolution")
	Ribbon = HashI("Ribbon")
	Round = HashI("Round")
	Scale = HashI("Scale")
	ScreenSpace = HashI("ScreenSpace")
	Series = HashI("Series")
	Size = HashI("Size")
	Smooth = HashI("Smooth")
	Solid = HashI("Solid")
	Source = HashI("Source")
	Specular = HashI("Specular")
	Sphere = HashI("Sphere")
	Spline = HashI("Spline")
	Square = HashI("Square")
	Step = HashI("Step")
	Stretch = HashI("Stretch")
	Strikeout = HashI("Strikeout")
	Style = HashI("Style")
	Tetra = HashI("Tetra")
	TexCoords = HashI("TexCoords")
	Text = HashI("Text")
	TextLayout = HashI("TextLayout")
	Texture = HashI("Texture")
	Transpose = HashI("Transpose")
	Triangle = HashI("Triangle")
	TriList = HashI("TriList")
	TriStrip = HashI("TriStrip")
	Tube = HashI("Tube")
	Txfm = HashI("Txfm")
	Underline = HashI("Underline")
	Unknown = HashI("Unknown")
	Up = HashI("Up")
	Velocity = HashI("Velocity")
	Verts = HashI("Verts")
	Video = HashI("Video")
	ViewPlaneZ = HashI("ViewPlaneZ")
	Wedges = HashI("Wedges")
	Weight = HashI("Weight")
	Width = HashI("Width")
	Wireframe = HashI("Wireframe")
	XAxis = HashI("XAxis")
	XColumn = HashI("XColumn")
	YAxis = HashI("YAxis")
	ZAxis = HashI("ZAxis")
	# AUTO-GENERATED-KEYWORDS-END
	def __int__(self):
		return self.value
	def __str__(self):
		return f"*{self.name}"
	def __pack__(self):
		return struct.pack('<I', self.value)
class ECommandId(Enum):
	# AUTO-GENERATED-COMMANDS-BEGIN
	Invalid = HashI("Invalid")
	AddToScene = HashI("AddToScene")
	CameraToWorld = HashI("CameraToWorld")
	CameraPosition = HashI("CameraPosition")
	ObjectToWorld = HashI("ObjectToWorld")
	Render = HashI("Render")
	# AUTO-GENERATED-COMMANDS-END
	def __int__(self):
		return self.value
	def __str__(self):
		return self.name
	def __pack__(self):
		return struct.pack('<I', self.value)

# Primitive types ------------------------------------------
class EPointStyle(Enum):
	Square = 0
	Circle = 1
	Triangle = 2
	Star = 3
	Annulus = 4
class Col(NamedTuple):
	argb: int = 0xFFFFFFFF
	def __str__(self):
		return f'{self.argb:08X}'
	def __pack__(self):
		return struct.pack("<I", self.argb)
class Vec2(NamedTuple):
	x: float
	y: float
	def vec3(self, z: float=0):
		return Vec3(self.x, self.y, z)
	def w0(self, z: float=0):
		return Vec4(self.x, self.y, z, 0)
	def w1(self, z: float=0):
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
	def __str__(self):
		return f'{self.x} {self.y}'
	def __pack__(self):
		return struct.pack("ff", self.x, self.y)
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
	def __str__(self):
		return f'{self.x} {self.y} {self.z}'
	def __pack__(self):
		return struct.pack("fff", self.x, self.y, self.z)
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
	def __str__(self):
		return f'{self.x} {self.y} {self.z} {self.w}'
	def __pack__(self):
		return struct.pack("ffff", self.x, self.y, self.z, self.w)
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
	def __pack__(self):
		return _Pack(self.x) + _Pack(self.y) + _Pack(self.z)
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
	def __pack__(self):
		return _Pack(self.x) + _Pack(self.y) + _Pack(self.z) + _Pack(self.w)
class BBox(NamedTuple):
	centre: Vec3
	radius: Vec3
	@staticmethod
	def Reset() -> 'BBox':
		return BBox(Vec3(0,0,0), Vec3(-1,-1,-1))
	def __str__(self):
		return f'{str(self.centre)} {str(self.radius)}'
	def __pack__(self):
		return _Pack(self.centre) + _Pack(self.radius)
	
# Implementation ------------------------------------------
class _LdrName:
	def __init__(self, name: str = ''):
		super().__init__()
		self.m_name = name
	def __str__(self):
		return f'{EKeyword.Name} {{{self.m_name}}}' if self.m_name else ''
	def __pack__(self):
		return (struct.pack('<II', EKeyword.Name.value, len(self.m_name)) + self.m_name.encode('utf-8')) if self.m_name else bytes()
class _LdrColour:
	def __init__(self, colour: Col = Col()):
		super().__init__()
		self.m_colour = colour
	def __str__(self):
		return f'{EKeyword.Colour} {{{self.m_colour.argb:08X}}}' if self.m_colour.argb != 0xFFFFFFFF else ''
	def __pack__(self):
		return struct.pack('<III', EKeyword.Colour.value, 4, self.m_colour.argb) if self.m_colour.argb != 0xFFFFFFFF else bytes()
class _LdrSize:
	def __init__(self, size: float=0):
		super().__init__()
		self.m_size = size
	def __str__(self):
		return f'{EKeyword.Size} {{{self.m_size}}}' if self.m_size != 0 else ''
	def __pack__(self):
		return struct.pack('<III', EKeyword.Size.value, 4, self.m_size) if self.m_size != 0 else bytes()
class _LdrPerItemColour:
	def __init__(self, per_item_colour: bool=False):
		super().__init__()
		self.m_per_item_colour = per_item_colour
	def __bool__(self):
		return self.m_per_item_colour
	def __str__(self):
		return f'{EKeyword.PerItemColour} {{}}' if self.m_per_item_colour else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.PerItemColour.value, 0) if self.m_per_item_colour else bytes()
class _LdrDepth:
	def __init__(self, depth: bool=False):
		super().__init__()
		self.m_depth = depth
	def __str__(self):
		return f'{EKeyword.Depth} {{}}' if self.m_depth else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.Depth.value, 0) if self.m_depth else bytes()
class _LdrWireframe:
	def __init__(self, wire: bool=False):
		super().__init__()
		self.m_wire = wire
	def __str__(self):
		return f'{EKeyword.Wireframe} {{}}' if self.m_wire else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.Wireframe.value, 0) if self.m_wire else bytes()
class _LdrSolid:
	def __init__(self, solid: bool=False):
		super().__init__()
		self.m_solid = solid
	def __str__(self):
		return f'{EKeyword.Solid} {{}}' if self.m_solid else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.Solid.value, 0) if self.m_solid else bytes()
class _LdrAxisId:
	def __init__(self, axis_id: int=0):
		super().__init__()
		self.m_axis_id = axis_id
	def __str__(self):
		return f'{EKeyword.AxisId} {{{self.m_axis_id}}}' if self.m_axis_id != 0 else ''
	def __pack__(self):
		return struct.pack('<III', EKeyword.AxisId.value, 4, self.m_axis_id) if self.m_axis_id != 0 else bytes()
class _LdrPos:
	def __init__(self, xyz: Optional[Vec3]=None):
		super().__init__()
		self.m_pos = xyz
	def __str__(self):
		if not self.m_pos: return ''
		if self.m_pos.is_zero(): return ''
		return f'{EKeyword.O2W} {{{EKeyword.Pos} {{{self.m_pos}}}}}'
	def __pack__(self):
		if not self.m_pos: return bytes()
		if self.m_pos.is_zero(): return bytes()
		return struct.pack('<IIIIfff', EKeyword.O2W.value, 20, EKeyword.Pos.value, 12, self.m_pos.x, self.m_pos.y, self.m_pos.z)
class _LdrO2W:
	def __init__(self, o2w: Optional[Mat4]=None, pos:Optional[Vec3]=None):
		super().__init__()
		self.m_o2w = None
		if o2w: self.m_o2w = o2w
		if pos: self.m_o2w = Mat4(Vec4(1,0,0,0), Vec4(0,1,0,0), Vec4(0,0,1,0), pos.w1())
	def __str__(self):
		if not self.m_o2w: return ''
		if self.m_o2w.is_identity(): return ''
		if self.m_o2w.is_translation(): return f'{EKeyword.O2W} {{{EKeyword.Pos} {{{self.m_o2w.w.xyz}}}}}'
		return f'{EKeyword.O2W} {{{EKeyword.M4x4} {{{self.m_o2w}}}}}'
	def __pack__(self):
		if not self.m_o2w: return bytes()
		if self.m_o2w.is_identity(): return bytes()
		if self.m_o2w.is_translation(): return struct.pack('<IIIIfff', EKeyword.O2W.value, 20, EKeyword.Pos.value, 12, self.m_o2w.w.x, self.m_o2w.w.y, self.m_o2w.w.z)
		return struct.pack('<IIII', EKeyword.O2W.value, 72, EKeyword.M4x4.value, 64) + _Pack(self.m_o2w)

class _LdrObj:
	def __init__(self):
		self.m_objects :List['_LdrObj'] = []

	# Child objects
	def Group(self, name: str = '', colour: int = 0xFFFFFFFF):
		grp = LdrGroup()
		self.m_objects.append(grp)
		return grp.name(name).col(Col(colour))
	def Points(self, name: str = '', colour: int = 0xFFFFFFFF):
		pts = LdrPoint()
		self.m_objects.append(pts)
		return pts.name(name).col(Col(colour))
	def Polygon(self, name: str = '', colour: int = 0xFFFFFFFF):
		poly = LdrPolygon()
		self.m_objects.append(poly)
		return poly.name(name).col(Col(colour))
	def Command(self):
		cmd = LdrCommands()
		self.m_objects.append(cmd)
		return cmd

	# Serialise to a string
	def ToString(self) -> str:
		out = []
		self._WriteTo(out)
		return ''.join(out)

	# Serialise to a byte array
	def ToBytes(self) -> bytes:
		out = bytearray()
		self._WriteTo(out)
		return bytes(out)

	# Serialize this object to 'out'
	def _WriteTo(self, out: List[str]|bytearray):
		for s in self.m_objects:
			s._WriteTo(out)

class _LdrBase(_LdrObj):
	def __init__(self):
		super().__init__()
		self.m_name :_LdrName = _LdrName()
		self.m_o2w :_LdrO2W = _LdrO2W()
		self.m_axis_id :_LdrAxisId = _LdrAxisId()
		self.m_colour :_LdrColour = _LdrColour()
		self.m_wire :_LdrWireframe = _LdrWireframe()
		self.m_solid :_LdrSolid = _LdrSolid()

	def name(self, name: str):
		self.m_name = _LdrName(name)
		return self

	def col(self, colour: Col):
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

	def _WriteTo(self, out: List[str]|bytearray):
		_Append(out, self.m_axis_id, self.m_wire, self.m_solid, self.m_o2w)
		super()._WriteTo(out)

class LdrPoint(_LdrBase):
	def __init__(self):
		super().__init__()
		self.m_points: List[Vec3] = []
		self.m_colours: List[Col] = []
		self.m_per_item_colours: _LdrPerItemColour = _LdrPerItemColour()
		self.m_style: EPointStyle = EPointStyle.Square
		self.m_size: _LdrSize = _LdrSize()
		self.m_depth: _LdrDepth = _LdrDepth()

	def pt(self, xyz: Vec3, colour: Optional[int]=None):
		self.m_points.append(xyz)
		if colour is not None:
			self.m_colours.append(Col(colour))
		elif self.m_per_item_colours:
			self.m_colours.append(Col())
		return self

	def style(self, style: EPointStyle):
		self.m_style = style
		return self

	def size(self, size: float):
		self.m_size = _LdrSize(size)
		return self

	def depth(self, depth: bool=True):
		self.m_depth = _LdrDepth(depth)
		return self

	def _WriteTo(self, out: List[str]|bytearray):
		base = super()
		_Write(out, EKeyword.Point, self.m_name, self.m_colour, lambda: (
			_Write(out, EKeyword.Style, self.m_style),
			_Append(out, self.m_size, self.m_depth),
			_Write(out, EKeyword.Data, lambda: (
				_Append(out, zip(self.m_points, self.m_colours) if self.m_per_item_colours else self.m_points)
			)),
			base._WriteTo(out)
		))

class LdrBox(_LdrBase):
	def __init__(self):
		super().__init__()
		self.m_dim: Vec3 = Vec3(0, 0, 0)

	def dim(self, d: Vec3|float|int) -> 'LdrBox':
		self.m_dim = d if isinstance(d, Vec3) else Vec3(d, d, d)
		return self

	def radii(self, radii: Vec3|float|int) -> 'LdrBox':
		return self.dim(Vec3(radii.x * 2, radii.y * 2, radii.z * 2) if isinstance(radii, Vec3) else radii * 2)

	# Create from bounding box
	def bbox(self, bbox: BBox) -> 'LdrBox':
		if bbox == BBox.Reset(): return self
		return self.dim(2 * bbox.radius).pos(bbox.centre)

	def _WriteTo(self, out: List[str]|bytearray):
		base = super()
		_Write(out, EKeyword.Box, self.m_name, self.m_colour, lambda: (
			_Write(out, EKeyword.Data, self.m_dim),
			base._WriteTo(out)
		))

class LdrPolygon(_LdrBase):
	def __init__(self):
		super().__init__()
		self.m_points: List[Vec2] = []
		self.m_colours: List[Col] = []
		self.m_per_item_colour: _LdrPerItemColour = _LdrPerItemColour()

	def pt(self, xy: Vec2, colour: Optional[Col] = None):
		self.m_points.append(xy)
		if colour:
			self.m_colours.append(colour)
		elif self.m_per_item_colour:
			self.m_colours.append(Col())
		return self

	def solid(self, solid: bool=True):
		self.m_solid = _LdrSolid(solid)
		return self

	def _WriteTo(self, out: List[str]|bytearray):
		base = super()
		_Write(out, EKeyword.Polygon, self.m_name, self.m_colour, lambda: (
			_Write(out, EKeyword.Data, lambda: (
				_Append(out, zip(self.m_points, self.m_colours) if self.m_per_item_colour else self.m_points)
			)),
			base._WriteTo(out)
		))

class LdrGroup(_LdrBase):
	def __init__(self):
		super().__init__()

	def _WriteTo(self, out: List[str]|bytearray):
		base = super()
		_Write(out, EKeyword.Group, self.m_name, self.m_colour, lambda: base._WriteTo(out))

class LdrCommands(_LdrBase):
	def __init__(self):
		super().__init__()
		self.m_cmds :List[Tuple[ECommandId, List[int|float|str]]] = []

	def add_to_scene(self, scene_id: int):
		self.m_cmds.append((ECommandId.AddToScene, [scene_id]))
		return self
	
	def _WriteTo(self, out: List[str]|bytearray):
		def WriteCmds():
			for cmd in self.m_cmds:
				_Write(out, EKeyword.Data, cmd[0], *cmd[1])
		_Write(out, EKeyword.Commands, self.m_name, self.m_colour, WriteCmds)

class LdrBuilder(_LdrObj):
	def __init__(self):
		super().__init__()

# Exports -------------------------------------------------
__all__ = [
	'Col',
	'Vec2',
	'Vec3',
	'Vec4',
	'Mat3',
	'Mat4',
	'LdrPoint',
	'LdrBox',
	'LdrPolygon',
	'LdrGroup',
	'LdrCommands',
	'LdrBuilder',
]

# Tests ---------------------------------------------------
def Tests():
	builder = LdrBuilder()
	ldr_group = builder.Group("MyGroup", 0xFF0000FF)
	ldr_points = ldr_group.Points("MyPoints", 0xFF00FF00)
	ldr_points.style(EPointStyle.Circle)
	ldr_points.pt(Vec3(0, 0, 0), 0xFF0000FF)
	ldr_points.pt(Vec3(1, 1, 1), 0xFF00FF00)
	ldr_points.pt(Vec3(2, 2, 2), 0xFFFF0000)
	ldr_group.pos(Vec3(1,1,1))
	ldr_cmd = builder.Command()
	ldr_cmd.add_to_scene(0)
	ldr_cmd.add_to_scene(1)
	ldr_cmd.add_to_scene(2)
	s = builder.ToString()
	#s = FormatScript(s, indent_str='    ')
	b = builder.ToBytes()
	print(s)
	with open("E:/Dump/Ldraw/test.bdr", "wb") as f:
		f.write(b)

Tests()
