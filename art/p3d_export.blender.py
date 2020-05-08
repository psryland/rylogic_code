import bpy
import struct

# ExportHelper is a helper class, defines filename and
# invoke() function which calls the file selector.
from bpy_extras.io_utils import ExportHelper
from bpy.props import StringProperty, BoolProperty, EnumProperty
from bpy.types import Operator

class P3D():
	Version = 0x00001001

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
		MESHVERTICES   = 0x00003110 #          ├─ Vertex list (u32 count, count * [Vert])
		MESHVERTICESV  = 0x00003111 #          ├─ Vertex list compressed (u32 count, count * [Verts])
		MESHINDICES    = 0x00003125 #          ├─ Index list (u32 count, u32 stride, count * [Idx i])
		MESHINDICESV   = 0x00003126 #          ├─ Index list (u32 count, u32 stride, count * [Idx i])
		MESHNUGGETS    = 0x00003200 #          └─ Nugget list (u32 count, count * [Nugget])
	class EFlags():
		NONE = 0
		COMPRESS = 1 << 0
	class ChunkHeader():
		def __init__(self, id: int, data_size:int):
			self.ChunkID = id
			self.ChunkSize = 8 + data_size

	# P3D Constructor
	def __init__(self, meshes:[bpy.types.Mesh], flags:int = EFlags.NONE):
		self.data = bytearray()

		# Write the file header
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MAIN, 0)
		self.WriteHeader(hdr)

		# Write the file version
		hdr.ChunkSize += self.WriteVersion(P3D.Version)

		# Write the scene
		hdr.ChunkSize += self.WriteScene(meshes, flags)

		self.UpdateHeader(offset, hdr)
		return

	# Header chunk
	def WriteHeader(self, hdr:ChunkHeader):
		self.data += struct.pack("<II", hdr.ChunkID, hdr.ChunkSize)
		return struct.calcsize("<II")

	# Replace the 'hdr' at 'offset' in 'self.data'
	def UpdateHeader(self, offset:int, hdr:ChunkHeader):
		self.data[offset:offset+8] = struct.pack("<II", hdr.ChunkID, hdr.ChunkSize)
		return
		
	# Version chunk
	def WriteVersion(self, version:int):
		self.data += struct.pack("<III", P3D.EChunkId.FILEVERSION, 12, version)
		return struct.calcsize("<III")

	# Scene chuck
	def WriteScene(self, meshes:[bpy.types.Mesh], flags:int):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.SCENE, 0)
		self.WriteHeader(hdr)

		# Write all materials
		hdr.ChunkSize += self.WriteMaterials(meshes)

		# Write all meshes
		hdr.ChunkSize += self.WriteMeshes(meshes, flags)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Meshes chunk
	def WriteMeshes(self, meshes:[bpy.types.Mesh], flags:int):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHES, 0)
		self.WriteHeader(hdr)

		# Write each mesh
		for mesh in meshes:
			hdr.ChunkSize += self.WriteMesh(mesh, flags)

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh chunk
	def WriteMesh(self, mesh:bpy.types.Mesh, flags:int):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESH, 0)
		self.WriteHeader(hdr)

		# Mesh name
		hdr.ChunkSize += self.WriteCStr(P3D.EChunkId.MESHNAME, mesh.name)

		# Mesh bounding box
		hdr.ChunkSize += self.WriteMeshBBox(mesh)

		# Mesh to parent transform
		hdr.ChunkSize += 0 #WriteMeshTransform(out, mesh.m_o2p);

		# Mesh vertex data
		hdr.ChunkSize += self.WriteVertices(mesh, flags)

		# Mesh index data
		#switch (mesh.m_idx.m_stride)
		#{
		#default: throw std::runtime_error(Fmt("Index stride value %d is not supported", mesh.m_idx.m_stride));
		#case 1: hdr.ChunkSize += WriteIndices(out, mesh.m_idx.span<u8>(), flags); break;
		#case 2: hdr.ChunkSize += WriteIndices(out, mesh.m_idx.span<u16>(), flags); break;
		#case 4: hdr.ChunkSize += WriteIndices(out, mesh.m_idx.span<u32>(), flags); break;
		#}

		# Mesh nugget data
		hdr.ChunkSize += 0 #WriteNuggets(out, mesh.m_nugget, flags);

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Mesh Bounding box
	def WriteMeshBBox(self, mesh:bpy.types.Mesh):
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHBBOX, struct.calcsize("ffffffff"))
		self.WriteHeader(hdr)

		c,r = self.CalcBBox(mesh)
		self.data += struct.pack("ffff", c[0], c[1], c[2], c[3])
		self.data += struct.pack("ffff", r[0], r[1], r[2], r[3])
		return hdr.ChunkSize

	# Mesh vertices
	def WriteVertices(self, mesh:bpy.types.Mesh, flags:int):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MESHVERTICES, 0)
		self.WriteHeader(hdr)

		if flags & P3D.EFlags.COMPRESS:
			print("Vertex compression not supported, falling back to uncompressed")

		for vert in mesh.vertices:
			# Position
			self.data += struct.pack("ffff", vert.co.x, vert.co.y, vert.co.z, 1.0)
			hdr.ChunkSize += struct.calcsize("ffff")

			# Colour
			self.data += struct.pack("ffff", 1.0, 1.0, 1.0, 1.0)
			hdr.ChunkSize += struct.calcsize("ffff")

			# Normal
			self.data += struct.pack("ffff", vert.normal.x, vert.normal.y, vert.normal.z, 0.0)
			hdr.ChunkSize += struct.calcsize("ffff")

			# UV
			self.data += struct.pack("ff", 0.0, 0.0)
			hdr.ChunkSize += struct.calcsize("ff")

			# pad
			self.data += struct.pack("ff", 0.0, 0.0)
			hdr.ChunkSize += struct.calcsize("ff")

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Materials chunk
	def WriteMaterials(self, meshes:[bpy.types.Mesh]):
		offset = len(self.data)
		hdr = P3D.ChunkHeader(P3D.EChunkId.MATERIALS, 0)
		self.WriteHeader(hdr)

		# Meshes contain materials in blender. Might need to make them unique by prefixing with the mesh name
		for mesh in meshes:
			for mat in mesh.materials:
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

		# Diffuse colour
		hdr.ChunkSize += self.WriteDiffuseColour(material.diffuse_color)

		# Textures
		# TODO

		self.UpdateHeader(offset, hdr)
		return hdr.ChunkSize

	# Diffuse colour
	def WriteDiffuseColour(self, colour:[float]):
		hdr = P3D.ChunkHeader(P3D.EChunkId.DIFFUSECOLOUR, struct.calcsize("ffff"))
		self.WriteHeader(hdr)

		# Colours are 'rgba'
		self.data += struct.pack("ffff", colour[0], colour[1], colour[2], colour[3])
		return hdr.ChunkSize

	# 16 character string
	def WriteCStr(self, chunk_id:int, string:str):
		string_bytes = bytes(string, encoding='utf-8')
		hdr = P3D.ChunkHeader(chunk_id, len(string_bytes))
		self.WriteHeader(hdr)
		self.data += string_bytes
		return hdr.ChunkSize

	# Calculate the bounding box of a mesh
	def CalcBBox(self, mesh:bpy.types.Mesh):
		centre = [+0.0, +0.0, +0.0, 1.0]
		radius = [-1.0, -1.0, -1.0, 0.0]
		for vert in mesh.vertices:
			for i in range(3):
				if radius[i] < 0:
					centre[i] = vert.co[i]
					radius[i] = 0
				else:
					signed_dist = vert.co[i] - centre[i]
					length      = abs(signed_dist)
					if length > radius[i]:
						new_radius = (length + radius[i]) / 2
						centre[i] += signed_dist * (new_radius - radius[i]) / length
						radius[i] = new_radius
		return centre, radius


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
		print("Write P3D File...")
		p3d = P3D(bpy.data.meshes)

		# Dump the bytearray to file
		with open(self.filepath, 'wb') as f: f.write(p3d.data)
		return {'FINISHED'}

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
	#hack register()
	p3d = P3D(bpy.data.meshes)
	print(str(p3d.data))
