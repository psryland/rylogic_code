'''
Copyright (C) 2020 Paul Ryland
paul@rylogic.co.nz

Created by Paul Ryland

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
'''

bl_info = {
	'name': 'Rylogic P3D Exporter',
	'author': 'Paul Ryland',
	'version': (1, 0, 0),
	'blender': (2, 80, 0),
	"description": "Exports the Rylogic p3d model format",
	'location': 'File -> Export -> Rylogic P3D',
	"warning": "",
	"wiki_url": "",
	"tracker_url": "",
	'category': 'Import-Export',
}

import os, math, struct
import bpy

# ExportHelper is a helper class, defines filename and
# invoke() function which calls the file selector.
from bpy_extras.io_utils import ExportHelper
from bpy.props import StringProperty, BoolProperty, EnumProperty
from bpy.types import Operator

# Mesh data format:
#  https://docs.blender.org/api/current/bpy.types.Mesh.html

class EVertFormat():
	# Use 32-bit floats for position data (default).
	# Size/Vert = 12 bytes (float[3])
	Verts32Bit = 0

	# Use 16-bit floats for position data.
	# Size/Vert = 6 bytes (half_t[3])
	Verts16Bit = 1
class ENormFormat():
	# Use 32-bit floats for normal data (default)
	# Size/Norm = 12 bytes (float[3])
	Norms32Bit = 0

	# Use 16-bit floats for normal data
	# Size/Norm = 6 bytes (half[3])
	Norms16Bit = 1

	# Pack each normal into 32bits. 
	# Size/Norm = 4 bytes (uint32_t)
	NormsPack32 = 2
class EColourFormat():
	# Use 32-bit AARRGGBB colours (default)
	# Size/Colour = 4 bytes (uint32_t
	Colours32Bit = 0
class EUVFormat():
	# Use 32-bit floats for UV data
	# Size/UV = 8 bytes (float[2])
	UVs32Bit = 0

	# Use 16-bit floats for UV data
	# Size/UV = 4 bytes (half[2])
	UVs16Bit = 1
class EIndexFormat():
	# Don't convert indices, use the input stride
	IdxSrc = 0

	# Use 32-bit integers for index data
	Idx32Bit = 1

	# Use 16-bit integers for index data
	Idx16Bit = 2

	# Use 8-bit integers for index data
	Idx8Bit = 3

	# Use variable length integers for index data
	IdxNBit = 4
class EFlags():
	NONE = 0

	_VertsOfs = 0
	_NormsOfs = 4
	_ColoursOfs = 8
	_UVsOfs = 12
	_IndexOfs = 16
	_Mask = 0b1111

	# Vertex flags
	Verts32Bit = EVertFormat.Verts32Bit << _VertsOfs
	Verts16Bit = EVertFormat.Verts16Bit << _VertsOfs

	# Normals flags
	Norms32Bit = ENormFormat.Norms32Bit << _NormsOfs
	Norms16Bit = ENormFormat.Norms16Bit << _NormsOfs
	NormsPack32 = ENormFormat.NormsPack32 << _NormsOfs

	# Colours flags
	Colours32Bit = EColourFormat.Colours32Bit << _ColoursOfs

	# TexCoord flags
	UVs32Bit = EUVFormat.UVs32Bit << _UVsOfs
	UVs16Bit = EUVFormat.UVs16Bit << _UVsOfs

	# Index data flags
	IdxSrc = EIndexFormat.IdxSrc << _IndexOfs
	Idx32Bit = EIndexFormat.Idx32Bit << _IndexOfs
	Idx16Bit = EIndexFormat.Idx16Bit << _IndexOfs
	Idx8Bit = EIndexFormat.Idx8Bit << _IndexOfs
	IdxNBit = EIndexFormat.IdxNBit << _IndexOfs

	# Standard combinations
	DEFAULT = Verts32Bit | Norms32Bit | Colours32Bit | UVs32Bit | IdxSrc
	COMPRESSED1 = Verts32Bit | Norms16Bit | Colours32Bit | UVs16Bit | Idx16Bit
	COMPRESSEDMAX = Verts16Bit | NormsPack32 | Colours32Bit | UVs16Bit | IdxNBit

class ETopo():
	TriList = 4
class EGeom():
	Vert = 1 << 0
	Colr = 1 << 1
	Norm = 1 << 2
	Tex0 = 1 << 3
	All = Vert | Colr | Norm | Tex0
class EAddrMode():
	Wrap       = 1
	Mirror     = 2
	Clamp      = 3
	Border     = 4
	MirrorOnce = 5
class Vec2():
	def __init__(self, x:float=0, y:float=0):
		self.x = x
		self.y = y
class Vec3():
	def __init__(self, x:float=0, y:float=0, z:float=0):
		self.x = x
		self.y = y
		self.z = z
class Col4():
	def __init__(self, r:float=0, g:float=0, b:float=0, a:float=0):
		self.r = r
		self.g = g
		self.b = b
		self.a = a
class Mat4():
	def __init__(self, x:Vec3=Vec3(1,0,0), y:Vec3=Vec3(0,1,0), z:Vec3=Vec3(0,0,1), w:Vec3=Vec3(0,0,0)):
		self.x = x
		self.y = y
		self.z = z
		self.w = w
class BBox():
	def __init__(self):
		self.centre = [+0,+0,+0,1]
		self.radius = [-1,-1,-1,0]

	# Grow the bbox to enclose 'pt'. Returns 'pt'
	def encompass(self, pt:Vec3):
		for i,x in enumerate([pt.x, pt.y, pt.z]):
			if self.radius[i] < 0:
				self.centre[i] = x
				self.radius[i] = 0
			else:
				signed_dist = x - self.centre[i]
				length      = abs(signed_dist)
				if length > self.radius[i]:
					new_radius = (length + self.radius[i]) / 2
					self.centre[i] += signed_dist * (new_radius - self.radius[i]) / length
					self.radius[i] = new_radius
		return pt
class FatVert():
	def __init__(self, v:Vec3=Vec3(), c:Col4=Col4(), n:Vec3=Vec3(), t:Vec2=Vec2()):
		self.vert = v
		self.colr = c
		self.norm = n
		self.tex0 = t
	def __eq__(self, rhs):
		return (
			hasattr(rhs, 'vert') and self.vert == rhs.vert and
			hasattr(rhs, 'colr') and self.vert == rhs.colr and
			hasattr(rhs, 'norm') and self.vert == rhs.norm and
			hasattr(rhs, 'tex0') and self.vert == rhs.tex0)
	def __hash__(self):
		return hash(self.vert, self.colr, self.norm, self.tex0)
class Nugget():
	def __init__(self, topo:ETopo=ETopo.TriList, geom:EGeom=EGeom.All, mat:str="", stride:int=2, vidx:[int]=[]):
		self.topo = topo
		self.geom = geom
		self.mat = mat
		self.stride = stride
		self.vidx = vidx
		return
class Mesh():
	# Meshes (bpy.types.Mesh) contain four main arrays:
	# 	mesh.vertices (3-vector)
	# 	mesh.edges (reference to 2 vertices)
	# 	mesh.loops (reference to 1 vertex and 1 edge)
	# 	mesh.polygons (reference a contiguous range of loops)
	# Also:
	# 	mesh.loops
	# 	mesh.uv_layers
	# 	mesh.vertex_colors
	# are all synchronised, so the same index can be used for the loop (aka vertex), UV, and vert colour.
	# 
	# A polygon (bpy.types.MeshPolygon) has:
	# 	A range [start,count] reference into the mesh.loops array
	# 	A single material index (value in [0,32768))
	# 	'loop_indices' can be used to iterate over loops
	# 
	# For P3D export, we want to make groups of polygons that all
	# have the same material as these will be the 'nuggets'.
	def __init__(self, mesh: bpy.types.Mesh):
		self.name = mesh.name
		self.bbox = BBox()
		self.o2p = Mat4()
		self.verts = []
		self.colrs = []
		self.norms = []
		self.tex0s = []
		self.nuggets = []

		# If the mesh has no geometry...
		if not mesh.polygons:
			return

		# Ensure the polygons have been triangulated
		if not mesh.loop_triangles:
			mesh.calc_loop_triangles()

		# Determine the geometry format
		geom = EGeom.Vert | EGeom.Norm
		geom = (geom | EGeom.Colr) if len(mesh.vertex_colors) != 0 else geom
		geom = (geom | EGeom.Tex0) if mesh.uv_layers.get("UVMap") != None else geom

		# Make a map from material index to a collection of
		# polygons. Each material index is a separate nugget.
		polygons_by_material = {i:[] for i in range(len(mesh.materials))}
		for poly in mesh.polygons:
			polygons_by_material[poly.material_index].append(poly)

		# Initialise the verts lists
		self.verts = [Vec3] * len(mesh.vertices)
		self.norms = [Vec2] * len(mesh.vertices)
		for i,v in enumerate(mesh.vertices):
			self.verts[i] = self.bbox.encompass(Vec3(v.co.x, v.co.y, v.co.z))
			self.norms[i] = Vec3(v.normal.x, v.normal.y, v.normal.z)

		# Initialise the colours
		if geom & EGeom.Colr:
			vertex_colors_layer0 = mesh.vertex_colors[0].data
			self.colrs = [Col4] * len(vertex_colors_layer0)
			for i,c in enumerate(vertex_colors_layer0):
				self.colrs[i] = Col4(c[0], c[1], c[2], c[3])

		# Initialise the UVs
		if geom & EGeom.Tex0:
			uvs = mesh.uv_layers.get("UVMap").data
			self.tex0s = [Vec2] * len(uvs)
			for i,t in enumerate(uvs):
				self.tex0s[i] = Vec2(t.uv.x, t.uv.y)

		# Set the stride based on the number of verts
		stride = 2 if len(self.verts) < 0x10000 else 4

		# Initialise the nuggets list
		for mat_idx, polys in polygons_by_material.items():
			vidx = []
			for poly in polys:
				for lx in poly.loop_indices:
					loop = mesh.loops[lx]
					vidx.append(loop.vertex_index)

			mat = mesh.materials[mat_idx]
			nugget = Nugget(topo=ETopo.TriList, geom=geom, mat=mat.name, stride=stride, vidx=vidx)
			self.nuggets.append(nugget)

		return

class P3D():
	Version = 0x00010101

	class EChunkId():
		NULL           = 0x00000000 # Null chunk
		CSTR           = 0x00000001 # Null terminated ascii string
		MAIN           = 0x44335250 # PR3D File type indicator
		FILEVERSION    = 0x00000100 # ├─ File Version
		SCENE          = 0x00001000 # └─ Scene
		MATERIALS      = 0x00002000 #    ├─ Materials
		MATERIAL       = 0x00002100 #    │  └─ Material
		DIFFUSECOLOUR  = 0x00002110 #    │   ├─ Diffuse Colour
		DIFFUSETEXTURE = 0x00002120 #    │   └─ Diffuse texture
		TEXFILEPATH    = 0x00002121 #    │   ├─ Texture filepath
		TEXTILING      = 0x00002122 #    │   └─ Texture tiling
		MESHES         = 0x00003000 #    └─ Meshes
		MESH           = 0x00003100 #       └─ Mesh of lines,triangles,tetras
		MESHNAME       = 0x00003101 #          ├─ Name (cstr)
		MESHBBOX       = 0x00003102 #          ├─ Bounding box (BBox)
		MESHTRANSFORM  = 0x00003103 #          ├─ Mesh to Parent Transform (m4x4)
		MESHVERTS      = 0x00003300 #          ├─ Vertex positions (u32 count, u16 format, u16 stride, count * [stride])
		MESHNORMS      = 0x00003310 #          ├─ Vertex normals   (u32 count, u16 format, u16 stride, count * [stride])
		MESHCOLOURS    = 0x00003320 #          ├─ Vertex colours   (u32 count, u16 format, u16 stride, count * [stride])
		MESHUVS        = 0x00003330 #          ├─ Vertex UVs       (u32 count, u16 format, u16 stride, count * [float2])
		MESHNUGGET     = 0x00004000 #          └─ Nugget list (u32 count, count * [Nugget])
		MESHMATID      = 0x00004001 #             ├─ Material id (cstr)                                                              
		MESHVIDX       = 0x00004010 #             ├─ Vert indices   (u32 count, u8 format, u8 idx_flags, u16 stride, count * [stride]
	class ChunkHeader():
		def __init__(self, id: int, data_size:int):
			self.ChunkID = id
			self.ChunkSize = 8 + data_size

	# P3D Constructor
	def __init__(self, data:[bpy.types.BlendData], flags:int = EFlags.NONE):
		self.data = bytearray()

		# Write the file header
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MAIN, 0)
		self.WriteHeader(hdr)

		# Write the file version
		hdr.ChunkSize += self.WriteU32(P3D.EChunkId.FILEVERSION, P3D.Version)

		# Write the scene
		hdr.ChunkSize += self.WriteScene(data, flags)

		self.UpdateHeader(offset, hdr)
		return

	# Header chunk
	def WriteHeader(self, hdr:ChunkHeader):
		self.data += struct.pack("<II", hdr.ChunkID, hdr.ChunkSize)
		return struct.calcsize("<II")

	# Replace the 'hdr' at 'offset' in 'self.data'
	def UpdateHeader(self, offset:int, hdr:ChunkHeader):
		if (hdr.ChunkSize & 0b11) != 0: raise RuntimeError("Chunk size is not aligned to 4 bytes")
		self.data[offset:offset+8] = struct.pack("<II", hdr.ChunkID, hdr.ChunkSize)
		return

	# Add padding bytes to align 'size' to 4 bytes
	def PadToU32(self, size:int):
		count = 0
		while (size & 0b11) != 0:
			self.data += struct.pack("B", 0)
			count += 1
			size += 1
		return count

	# Scene chuck
	def WriteScene(self, data:[bpy.types.BlendData], flags:int):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.SCENE, 0)
		self.WriteHeader(hdr)

		# Write all materials
		hdr.ChunkSize += self.WriteMaterials(data.materials)

		# Write all meshes
		hdr.ChunkSize += self.WriteMeshes(data.objects, flags)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Meshes chunk
	def WriteMeshes(self, objects:[bpy.types.Object], flags:int):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHES, 0)
		self.WriteHeader(hdr)

		# Write each mesh
		for obj in objects:
			if obj.type != 'MESH': continue
			hdr.ChunkSize += self.WriteMesh(obj.data, flags)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh chunk
	def WriteMesh(self, mesh:bpy.types.Mesh, flags:EFlags):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESH, 0)
		self.WriteHeader(hdr)

		if flags != EFlags.DEFAULT:
			print("Vertex compression not supported, falling back to uncompressed")

		# Convert the blender mesh into a form suitable for P3D export
		m = Mesh(mesh)

		# Mesh name
		hdr.ChunkSize += self.WriteStr(P3D.EChunkId.MESHNAME, m.name)

		# Mesh bounding box
		hdr.ChunkSize += self.WriteMeshBBox(m.bbox)

		# Mesh to parent transform
		hdr.ChunkSize += self.WriteMeshTransform(m.o2p)

		# Mesh vertex data
		hdr.ChunkSize += self.WriteVerts(m.verts, flags)
		hdr.ChunkSize += self.WriteColours(m.colrs, flags)
		hdr.ChunkSize += self.WriteNorms(m.norms, flags)
		hdr.ChunkSize += self.WriteUVs(m.tex0s, flags)

		# Mesh nugget data
		for nugget in m.nuggets:
			hdr.ChunkSize += self.WriteNugget(nugget, flags)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh Bounding box
	def WriteMeshBBox(self, bbox:BBox):
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHBBOX, struct.calcsize("4f4f"))
		self.WriteHeader(hdr)

		c, r = bbox.centre, bbox.radius
		self.data += struct.pack("4f", c[0], c[1], c[2], c[3])
		self.data += struct.pack("4f", r[0], r[1], r[2], r[3])
		return hdr.ChunkSize

	# Mesh object to parent transform
	def WriteMeshTransform(self, o2p:Mat4):
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHTRANSFORM, struct.calcsize("4f4f4f4f"))
		self.WriteHeader(hdr)
		self.data += struct.pack("4f", o2p.x.x, o2p.x.y, o2p.x.z, 0)
		self.data += struct.pack("4f", o2p.y.x, o2p.y.y, o2p.y.z, 0)
		self.data += struct.pack("4f", o2p.z.x, o2p.z.y, o2p.z.z, 0)
		self.data += struct.pack("4f", o2p.w.x, o2p.w.y, o2p.w.z, 1)
		return hdr.ChunkSize

	# Mesh vertices
	def WriteVerts(self, verts:[Vec3], flags:EFlags):
		if len(verts) == 0:
			return 0

		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHVERTS, 0)
		self.WriteHeader(hdr)

		# Vertex count
		self.data += struct.pack("<I", len(verts))
		hdr.ChunkSize += struct.calcsize("<I")

		# Format
		fmt = (flags >> EFlags._VertsOfs) & EFlags._Mask
		self.data += struct.pack("<H", fmt)
		hdr.ChunkSize += struct.calcsize("<H")

		if fmt == EVertFormat.Verts32Bit:

			# Stride
			stride = struct.calcsize("3f")
			self.data += struct.pack("<H", stride)
			hdr.ChunkSize += struct.calcsize("<H")

			# Use 32bit floats for position data
			for v in verts:
				self.data += struct.pack("3f", v.x, v.y, v.z)
			hdr.ChunkSize += stride * len(verts)
		else:
			raise RuntimeError(f"Unsupported vertex format:{str(fmt)}")

		# Chunk padding
		hdr.ChunkSize += self.PadToU32(hdr.ChunkSize)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh colours
	def WriteColours(self, colours:[int], flags:EFlags):
		if len(colours) == 0:
			return 0

		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHCOLOURS, 0)
		self.WriteHeader(hdr)

		# Vertex count
		self.data += struct.pack("<I", len(colours))
		hdr.ChunkSize += struct.calcsize("<I")

		# Format
		fmt = (flags >> EFlags._ColoursOfs) & EFlags._Mask
		self.data += struct.pack("<H", fmt)
		hdr.ChunkSize += struct.calcsize("<H")

		if fmt == EColourFormat.Colours32Bit:

			# Stride
			stride = struct.calcsize("<I")
			self.data += struct.pack("<H", stride)
			hdr.ChunkSize += struct.calcsize("<H")

			# Use AARRGGBB 32-bit colour values
			for c in colours:
				self.data += struct.pack("<I", c)
			hdr.ChunkSize += struct.calcsize("<I") * len(colours)
		else:
			raise RuntimeError(f"Unsupported vertex colour format:{str(fmt)}")

		# Chunk padding
		hdr.ChunkSize += self.PadToU32(hdr.ChunkSize)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh normals
	def WriteNorms(self, norms:[Vec3], flags:EFlags):
		if len(norms) == 0:
			return 0

		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHNORMS, 0)
		self.WriteHeader(hdr)

		# Vertex count
		self.data += struct.pack("<I", len(norms))
		hdr.ChunkSize += struct.calcsize("<I")

		# Format
		fmt = (flags >> EFlags._NormsOfs) & EFlags._Mask
		self.data += struct.pack("<H", fmt)
		hdr.ChunkSize += struct.calcsize("<H")

		if fmt == ENormFormat.Norms32Bit:

			# Stride
			stride = struct.calcsize("3f")
			self.data += struct.pack("<H", stride)
			hdr.ChunkSize += struct.calcsize("<H")

			# Use 32bit floats for normals data
			for n in norms:
				self.data += struct.pack("3f", n.x, n.y, n.z)
			hdr.ChunkSize += stride * len(norms)
		else:
			raise RuntimeError(f"Unsupported vertex normals format:{str(fmt)}")

		# Chunk padding
		hdr.ChunkSize += self.PadToU32(hdr.ChunkSize)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh UVs
	def WriteUVs(self, uvs:[Vec2], flags:EFlags):
		
		if len(uvs) == 0:
			return 0

		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHUVS, 0)
		self.WriteHeader(hdr)

		# Vertex count
		self.data += struct.pack("<I", len(uvs))
		hdr.ChunkSize += struct.calcsize("<I")

		# Format
		fmt = (flags >> EFlags._UVsOfs) & EFlags._Mask
		self.data += struct.pack("<H", fmt)
		hdr.ChunkSize += struct.calcsize("<H")

		if fmt == EUVFormat.UVs32Bit:

			# Stride
			stride = struct.calcsize("2f")
			self.data += struct.pack("<H", stride)
			hdr.ChunkSize += struct.calcsize("<H")

			# Use 32bit floats for UV data
			for uv in uvs:
				self.data += struct.pack("2f", uv.x, uv.y)
			hdr.ChunkSize += stride * len(uvs)
		else:
			raise RuntimeError(f"Unsupported vertex UV format:{str(fmt)}")

		# Chunk padding
		hdr.ChunkSize += self.PadToU32(hdr.ChunkSize)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh Nuggets
	def WriteNugget(self, nugget:Nugget, flags:EFlags):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHNUGGET, 0)
		self.WriteHeader(hdr)

		# Mesh topology
		self.data += struct.pack("<H", nugget.topo)
		hdr.ChunkSize += struct.calcsize("<H")

		# Mesh geometry
		self.data += struct.pack("<H", nugget.geom)
		hdr.ChunkSize += struct.calcsize("<H")

		# Material id
		hdr.ChunkSize += self.WriteStr(P3D.EChunkId.MESHMATID, nugget.mat)

		# Face/Line/Tetra/etc indices
		hdr.ChunkSize += self.WriteIndices(nugget.vidx, nugget.stride, flags)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh Indices
	def WriteIndices(self, vidx:[int], stride_in:int, flags:EFlags):
		if len(vidx) == 0:
			return 0

		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHVIDX, 0)
		self.WriteHeader(hdr)

		# If the format is 'IdxSrc', set 'fmt' to match 'Idx'
		fmt = (flags >> EFlags._IndexOfs) & EFlags._Mask
		fmt = (fmt if fmt != EIndexFormat.IdxSrc else
			EIndexFormat.Idx16Bit if stride_in == 2 else
			EIndexFormat.Idx32Bit)

		# Index count
		index_count = len(vidx)
		self.data += struct.pack("<I", index_count)
		hdr.ChunkSize += struct.calcsize("<I")

		# Format
		self.data += struct.pack("<H", fmt)
		hdr.ChunkSize += struct.calcsize("<H")

		if fmt == EIndexFormat.Idx32Bit:

			# Index stride
			stride_out = struct.calcsize("<I")
			self.data += struct.pack("<H", stride_out)
			hdr.ChunkSize += struct.calcsize("<H")

			# Write indices as 32bit integers
			for vx in vidx: self.data += struct.pack("<I", vx)
			hdr.ChunkSize += struct.calcsize("<I") * len(vidx)

		elif fmt == EIndexFormat.Idx16Bit:

			# Index stride
			stride_out = struct.calcsize("<H")
			self.data += struct.pack("<H", stride_out)
			hdr.ChunkSize += struct.calcsize("<H")

			# Write indices as 16bit integers
			for vx in vidx: self.data += struct.pack("<H", vx)
			hdr.ChunkSize += struct.calcsize("<H") * len(vidx)

		else:
			raise RuntimeError(f"Unsupported index format:{str(fmt)}")

		# Chunk padding
		hdr.ChunkSize += self.PadToU32(hdr.ChunkSize)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Materials chunk
	def WriteMaterials(self, materials:[bpy.types.Material]):
		if len(materials) == 0:
			return 0

		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MATERIALS, 0)
		self.WriteHeader(hdr)

		# Export each material
		for mat in materials:
			hdr.ChunkSize += self.WriteMaterial(mat)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Material chunk
	def WriteMaterial(self, material:bpy.types.Material):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MATERIAL, 0)
		self.WriteHeader(hdr)

		# Material name
		self.data += struct.pack("16s", bytes(material.name[0:16], encoding='utf-8'))
		hdr.ChunkSize += 16

		# Blender materials typically use the node based shader 'Principled BSDF'
		# Try to get basic material info from the nodes
		if material.use_nodes:
			for node in material.node_tree.nodes:

				if node.type == 'TEX_IMAGE':

					# Add a texture description
					hdr.ChunkSize += self.WriteTexture(node.image, node.extension)

				elif node.type == "BSDF_PRINCIPLED":

					# Find the 'Base Color' input
					base_color_input = next(x for x in node.inputs if x.name == 'Base Color')
					if base_color_input:
						hdr.ChunkSize += self.WriteDiffuseColour(base_color_input.default_value)

		# otherwise, they're just flat colours.
		else:
			# Diffuse colour
			hdr.ChunkSize += self.WriteDiffuseColour(material.diffuse_color)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Texture chunk
	def WriteTexture(self, image:bpy.types.Image, tiling:str):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.DIFFUSETEXTURE, 0)
		self.WriteHeader(hdr)

		# Texture filepath
		if image.filepath:
			# The texture file path will be relative to the .blend file.
			# It's too hard to sensibly maintain the relative paths so instead
			# require the artist to use unique texture filenames and strip all
			# path information.
			filename = os.path.split(image.filepath)[1]
			hdr.ChunkSize += self.WriteStr(P3D.EChunkId.TEXFILEPATH, filename)

		# Texture tiling
		if tiling:
			mode = (
				EAddrMode.Wrap       if tiling == 'REPEAT' else
				EAddrMode.Mirror     if tiling == 'MIRROR' else      # not supported in blender
				EAddrMode.Clamp      if tiling == 'EXTEND' else      # extend the edge of the texture is the same as clamping to [0,1]
				EAddrMode.Border     if tiling == 'CLIP' else        # in blender the border colour is transparent, in dx it's configurable
				EAddrMode.MirrorOnce if tiling == 'MIRROR_ONCE' else # not supported in blender
				EAddrMode.Wrap)
			hdr.ChunkSize += self.WriteU32(P3D.EChunkId.TEXTILING, mode)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Diffuse colour
	def WriteDiffuseColour(self, colour:[float]):
		hdr = P3D.ChunkHeader(P3D.EChunkId.DIFFUSECOLOUR, struct.calcsize("4f"))
		self.WriteHeader(hdr)

		# Colours are 'rgba'
		self.data += struct.pack("4f", colour[0], colour[1], colour[2], colour[3])
		return hdr.ChunkSize

	# 16 character string
	def WriteStr(self, chunk_id:int, string:str):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(chunk_id, 0)
		self.WriteHeader(hdr)

		string_bytes = bytes(string, encoding='utf-8')
		string_len = len(string_bytes)

		# String length
		self.data += struct.pack("<I", string_len)
		hdr.ChunkSize += struct.calcsize("<I")

		# String data
		self.data += string_bytes
		hdr.ChunkSize += string_len

		# Chunk padding
		hdr.ChunkSize += self.PadToU32(hdr.ChunkSize)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Uint32 value
	def WriteU32(self, chunk_id:int, value:int):
		hdr = P3D.ChunkHeader(chunk_id, struct.calcsize("<I"))
		self.WriteHeader(hdr)
		self.data += struct.pack("<I", value)
		return hdr.ChunkSize


class ExportP3D(Operator, ExportHelper):
	"""Export a Rylogic model file"""

	# important since its how bpy.ops.import_test.some_data is constructed
	bl_idname = "export_p3d.model_data"
	bl_label = "Export P3D"

	# ExportHelper mixin class uses this
	filename_ext = ".p3d"

	# File filter pattern
	filter_glob: StringProperty(
		default="*.p3d",
		options={'HIDDEN'},
		maxlen=255,  # Max internal buffer length, longer would be clamped.
	)

	# Compressed p3d option check box
	compress: BoolProperty(
		name="Compressed",
		description="Export compressed chunks",
		default=False,
	)
	
	# Run the export
	def execute(self, context):
		
		# Switch to object mode to ensure mesh edits are flushed
		if bpy.context != 'OBJECT':
			bpy.ops.object.mode_set(mode='OBJECT')

		# Create a P3D instance from the mesh data
		p3d = P3D(bpy.data)

		# Dump the bytearray to file
		print("Write P3D File...")
		with open(self.filepath, 'wb') as f: f.write(p3d.data)

		print("Done")
		return {'FINISHED'}

	# Display a message
	def ShowMessageBox(self, message = "", title = "Message Box", icon = 'INFO'):
		def draw(self, context): self.layout.label(text=message)
		bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)


# Add a menu item to the File->Export menu
def menu_func_export(self, context):
	self.layout.operator(ExportP3D.bl_idname, text="Rylogic Model (*.p3d)")
def register():
	bpy.utils.register_class(ExportP3D)
	bpy.types.TOPBAR_MT_file_export.append(menu_func_export)
def unregister():
	bpy.utils.unregister_class(ExportP3D)
	bpy.types.TOPBAR_MT_file_export.remove(menu_func_export)

if __name__ == "__main__":
	register()
