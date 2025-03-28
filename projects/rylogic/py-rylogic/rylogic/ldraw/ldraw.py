#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import struct
from ..gfx.colour import Colour32
from ..math.vector import Vec2, Vec3, Vec4, Mat4
from ..math.bbox import BBox
from ..math.variable_int import VariableInt
from typing import List, Optional, Tuple
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
	Accel = 1776272131
	Addr = 2198485072
	Align = 4150770452
	Alpha = 3816748683
	Ambient = 769453643
	Anchor = 533762434
	AngAccel = 3071695581
	AngVelocity = 1983444818
	Animation = 2445809997
	Arrow = 2356777612
	Aspect = 1748891881
	Axis = 3519141186
	AxisId = 2821469089
	BackColour = 2053203692
	BakeTransform = 2581247370
	Bar = 1223118472
	Billboard = 1807119378
	Billboard3D = 1749199635
	Box = 1183214608
	BoxList = 482966660
	Camera = 3903947748
	CastShadow = 3434545796
	Chart = 2010726315
	Circle = 1808893457
	Closed = 1049176709
	Colour = 2107742061
	ColourMask = 3850257753
	Colours = 160985048
	Commands = 3210495811
	Cone = 3447874744
	ConvexHull = 4030846017
	CoordFrame = 1340586631
	CornerRadius = 146205564
	CrossSection = 3855968132
	CString = 3696377867
	Custom = 1273586436
	Cylinder = 73734935
	Dashed = 3417589530
	Data = 1633640165
	Depth = 1932164280
	Diffuse = 2847039405
	Dim = 3559743841
	Direction = 112172600
	Divisions = 3522697911
	Equation = 3405427779
	Euler = 3721396176
	Faces = 2129157485
	Facets = 2746758633
	Far = 233523716
	FilePath = 3517686034
	Filter = 3604239663
	Font = 94814486
	ForeColour = 566537511
	Format = 3077150512
	Fov = 3569098554
	FovX = 3833825642
	FovY = 109444487
	FrustumFA = 4132698078
	FrustumWH = 2043318120
	GenerateNormals = 3435554780
	Grid = 2444955561
	Group = 585446986
	Hidden = 217111265
	Instance = 3849372400
	Inverse = 1751982803
	Layers = 351688103
	LeftHanded = 1198476718
	LightSource = 8129848
	Line = 2258880063
	LineBox = 3413563278
	LineD = 2305486881
	LineList = 392567511
	Lines = 261427482
	LineStrip = 846688391
	LookAt = 4115870691
	M3x3 = 1629634014
	M4x4 = 3571029766
	Mesh = 841695258
	Model = 4177044552
	Name = 904194284
	Near = 2282790839
	NewLine = 175167211
	NonAffine = 2843478707
	Normalise = 3552115793
	Normals = 3081570275
	NoZTest = 2216324274
	NoZWrite = 4229908471
	O2W = 2652027601
	Orthographic = 3706095883
	Orthonormalise = 568525649
	Padding = 244087260
	Param = 3268730128
	Parametrics = 3196483018
	Part = 3767037922
	Period = 3523750322
	PerItemColour = 1664324363
	PerItemParametrics = 1273718588
	Pie = 2304688277
	Plane = 1880625333
	Point = 3758757417
	Polygon = 2189689745
	Pos = 1668956049
	Position = 544900312
	Quad = 1051923940
	Quat = 3733679956
	QuatPos = 3432165810
	Rand4x4 = 1093289272
	RandColour = 3295180552
	RandOri = 1510902700
	RandPos = 1423107706
	Range = 2207617840
	Rect = 1579467807
	Reflectivity = 4254610627
	Resolution = 1647930679
	Ribbon = 3885545905
	Round = 3017993275
	Scale = 1428222601
	ScreenSpace = 268929993
	Series = 4082978870
	Size = 133542970
	Smooth = 1779894359
	Solid = 162487746
	Source = 1518791278
	Specular = 1117514438
	Sphere = 1734936622
	Spline = 4057807962
	Square = 1972133964
	Step = 1358202807
	Stretch = 3227819128
	Strikeout = 473419545
	Style = 3687063676
	Tetra = 1746501091
	TexCoords = 459107654
	Text = 764158836
	TextLayout = 2095266270
	Texture = 1281751746
	Transpose = 3584171558
	Triangle = 4064242629
	TriList = 407772856
	TriStrip = 1825250206
	Tube = 1811807431
	Txfm = 2790714414
	Underline = 908250695
	Unknown = 1679964129
	Up = 52501126
	Velocity = 3133171344
	Verts = 1315828595
	Video = 474428842
	ViewPlaneZ = 2718244470
	Wedges = 2258899714
	Weight = 2659322209
	Width = 4087841383
	Wireframe = 759880933
	XAxis = 2624028016
	XColumn = 1419399477
	YAxis = 456612421
	ZAxis = 3555790074
	# AUTO-GENERATED-KEYWORDS-END
	def __int__(self):
		return self.value
	def __str__(self):
		return f"*{self.name}"
	def __pack__(self):
		return struct.pack('<I', self.value)
class ECommandId(Enum):
	# AUTO-GENERATED-COMMANDS-BEGIN
	Invalid = 3059088950
	AddToScene = 1880236971
	CameraToWorld = 3577816865
	CameraPosition = 2339166545
	ObjectToWorld = 1528602381
	Render = 1982586845
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
class StringWithLength:
	def __init__(self, string: str=''):
		self.m_string = string
	def __repr__(self):
		return str(self)
	def __str__(self):
		return self.m_string
	def __pack__(self):
		strbytes = self.m_string.encode('utf-8')
		return VariableInt(len(strbytes)).__pack__() + strbytes

# Implementation ------------------------------------------
class _LdrName:
	def __init__(self, name: str = ''):
		super().__init__()
		self.m_name = name
	def __repr__(self):
		return self.m_name
	def __str__(self):
		return f'{EKeyword.Name} {{{self.m_name}}}' if self.m_name else ''
	def __pack__(self):
		return (struct.pack('<II', EKeyword.Name.value, len(self.m_name)) + self.m_name.encode('utf-8')) if self.m_name else bytes()
class _LdrColour:
	def __init__(self, colour: Colour32 = Colour32()):
		super().__init__()
		self.m_colour = colour
	def __repr__(self):
		return self.m_colour.__repr___()
	def __str__(self):
		return f'{EKeyword.Colour} {{{self.m_colour.argb:08X}}}' if self.m_colour.argb != 0xFFFFFFFF else ''
	def __pack__(self):
		return struct.pack('<III', EKeyword.Colour.value, 4, self.m_colour.argb) if self.m_colour.argb != 0xFFFFFFFF else bytes()
class _LdrSize:
	def __init__(self, size: float=0):
		super().__init__()
		self.m_size = size
	def __repr__(self):
		return self.m_size
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
	def __repr__(self):
		return self.m_per_item_colour
	def __str__(self):
		return f'{EKeyword.PerItemColour} {{}}' if self.m_per_item_colour else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.PerItemColour.value, 0) if self.m_per_item_colour else bytes()
class _LdrDepth:
	def __init__(self, depth: bool=False):
		super().__init__()
		self.m_depth = depth
	def __repr__(self):
		return self.m_depth
	def __str__(self):
		return f'{EKeyword.Depth} {{}}' if self.m_depth else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.Depth.value, 0) if self.m_depth else bytes()
class _LdrWireframe:
	def __init__(self, wire: bool=False):
		super().__init__()
		self.m_wire = wire
	def __repr__(self):
		return self.m_wire
	def __str__(self):
		return f'{EKeyword.Wireframe} {{}}' if self.m_wire else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.Wireframe.value, 0) if self.m_wire else bytes()
class _LdrSolid:
	def __init__(self, solid: bool=False):
		super().__init__()
		self.m_solid = solid
	def __repr__(self):
		return self.m_solid
	def __str__(self):
		return f'{EKeyword.Solid} {{}}' if self.m_solid else ''
	def __pack__(self):
		return struct.pack('<II', EKeyword.Solid.value, 0) if self.m_solid else bytes()
class _LdrAxisId:
	def __init__(self, axis_id: int=0):
		super().__init__()
		self.m_axis_id = axis_id
	def __repr__(self):
		return self.m_axis_id
	def __str__(self):
		return f'{EKeyword.AxisId} {{{self.m_axis_id}}}' if self.m_axis_id != 0 else ''
	def __pack__(self):
		return struct.pack('<III', EKeyword.AxisId.value, 4, self.m_axis_id) if self.m_axis_id != 0 else bytes()
class _LdrPos:
	def __init__(self, xyz: Optional[Vec3]=None):
		super().__init__()
		self.m_pos = xyz
	def __repr__(self):
		return self.m_pos
	def __str__(self):
		if not self.m_pos: return ''
		if self.m_pos == Vec3.Zero: return ''
		return f'{EKeyword.O2W} {{{EKeyword.Pos} {{{self.m_pos}}}}}'
	def __pack__(self):
		if not self.m_pos: return bytes()
		if self.m_pos == Vec3.Zero: return bytes()
		return struct.pack('<IIIIfff', EKeyword.O2W.value, 20, EKeyword.Pos.value, 12, self.m_pos.x, self.m_pos.y, self.m_pos.z)
class _LdrO2W:
	def __init__(self, o2w: Optional[Mat4]=None, pos:Optional[Vec3]=None):
		super().__init__()
		self.m_o2w = None
		if o2w: self.m_o2w = o2w
		if pos: self.m_o2w = Mat4(Vec4(1,0,0,0), Vec4(0,1,0,0), Vec4(0,0,1,0), pos.w1())
	def __repr__(self):
		return self.m_o2w
	def __str__(self):
		if not self.m_o2w: return ''
		if self.m_o2w.is_identity: return ''
		if self.m_o2w.is_translation: return f'{EKeyword.O2W} {{{EKeyword.Pos} {{{self.m_o2w.w.xyz}}}}}'
		return f'{EKeyword.O2W} {{{EKeyword.M4x4} {{{self.m_o2w}}}}}'
	def __pack__(self):
		if not self.m_o2w: return bytes()
		if self.m_o2w.is_identity: return bytes()
		if self.m_o2w.is_translation: return struct.pack('<IIIIfff', EKeyword.O2W.value, 20, EKeyword.Pos.value, 12, self.m_o2w.w.x, self.m_o2w.w.y, self.m_o2w.w.z)
		return struct.pack('<IIII', EKeyword.O2W.value, 72, EKeyword.M4x4.value, 64) + _Pack(self.m_o2w)

class _LdrObj:
	def __init__(self):
		self.m_objects :List['_LdrObj'] = []

	# Remove all children
	def reset(self):
		self.m_objects = []

	# Child objects
	def Group(self, name: str = '', colour: int = 0xFFFFFFFF):
		child = LdrGroup()
		self.m_objects.append(child)
		return child.name(name).col(Colour32(colour))
	def Points(self, name: str = '', colour: int = 0xFFFFFFFF):
		child = LdrPoint()
		self.m_objects.append(child)
		return child.name(name).col(Colour32(colour))
	def Box(self, name: str = '', colour: int = 0xFFFFFFFF):
		child = LdrBox()
		self.m_objects.append(child)
		return child.name(name).col(Colour32(colour))
	def Polygon(self, name: str = '', colour: int = 0xFFFFFFFF):
		child = LdrPolygon()
		self.m_objects.append(child)
		return child.name(name).col(Colour32(colour))
	def Command(self):
		child = LdrCommands()
		self.m_objects.append(child)
		return child

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

	def col(self, colour: Colour32):
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
		self.m_colours: List[Colour32] = []
		self.m_per_item_colours: _LdrPerItemColour = _LdrPerItemColour()
		self.m_style: EPointStyle = EPointStyle.Square
		self.m_size: _LdrSize = _LdrSize()
		self.m_depth: _LdrDepth = _LdrDepth()

	def pt(self, xyz: Vec3, colour: Optional[int]=None):
		self.m_points.append(xyz)
		if colour is not None:
			self.m_colours.append(Colour32(colour))
		elif self.m_per_item_colours:
			self.m_colours.append(Colour32())
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
		self.m_colours: List[Colour32] = []
		self.m_per_item_colour: _LdrPerItemColour = _LdrPerItemColour()

	def pt(self, xy: Vec2, colour: Optional[Colour32] = None):
		self.m_points.append(xy)
		if colour:
			self.m_colours.append(colour)
		elif self.m_per_item_colour:
			self.m_colours.append(Colour32())
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
		self.m_cmds :List[Tuple[ECommandId, List[int|float|StringWithLength|Vec2|Vec3|Vec4|Mat4|Mat4]]] = []

	def add_to_scene(self, scene_id: int):
		self.m_cmds.append((ECommandId.AddToScene, [scene_id]))
		return self

	def transform_object(self, object_name: str, o2w: Mat4):
		self.m_cmds.append((ECommandId.ObjectToWorld, [StringWithLength(object_name), o2w]))
		return self

	def _WriteTo(self, out: List[str]|bytearray):
		def WriteCmds():
			for cmd in self.m_cmds:
				_Write(out, EKeyword.Data, cmd[0], *cmd[1])
		_Write(out, EKeyword.Commands, self.m_name, self.m_colour, WriteCmds)

class Builder(_LdrObj):
	def __init__(self):
		super().__init__()

