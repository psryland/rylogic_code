//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
// Binary 3d model format
// Based on 3ds.
// This model format is designed for load speed
// over space, use zip if you want compressed model data.

#pragma once

#include <string>
#include <exception>
#include "pr/macros/enum.h"
#include "pr/common/scope.h"
#include "pr/common/fmt.h"
#include "pr/common/colour.h"

namespace pr
{
	namespace geometry
	{
		namespace p3d
		{
			typedef unsigned char u8;       static_assert(sizeof(u8)  == 1, "1byte u8 expected");
			typedef unsigned short u16;     static_assert(sizeof(u16) == 2, "2byte u16 expected");
			typedef unsigned long u32;      static_assert(sizeof(u32) == 4, "4byte u32 expected");
			typedef unsigned long long u64; static_assert(sizeof(u64) == 8, "8byte u64 expected");
			static const u32 Version = 0x00001000;

			typedef char Str16[16];
			struct Vec4 { float x,y,z,w; };
			struct Col4 { float r,g,b,a; };
			struct Vec2 { float u,v; };
			struct Vert { Vec4 pos; Col4 col; Vec4 norm; Vec2 uv; };
			struct Face16 { u16 idx[3]; };
			struct Face32 { u32 idx[3]; };
			struct Range { u32 first, count; };

			#pragma region Chunk Ids
			#define PR_ENUM(x)/*
			*/x(Null             ,= 0x00000000)/* Null chunk
			*/x(CStr             ,= 0x00000001)/* Null terminated ascii string

			*/x(Main             ,= 0x50523344)/* P3D File type indicator
			*/x(FileVersion      ,= 0x00000100)/* ├─ File Version
			*/x(Scene            ,= 0x00001000)/* └─ Scene
			*/x(Materials        ,= 0x00002000)/*    ├─ Materials
			*/x(Material         ,= 0x00002100)/*    │  └─ Material
			*/x(DiffuseColour    ,= 0x00002110)/*    │     ├─ Diffuse Colour
			*/x(DiffuseTexture   ,= 0x00002120)/*    │     └─ Diffuse texture
			*/x(TexFilepath      ,= 0x00002121)/*    │        ├─ Texture filepath
			*/x(TexTiling        ,= 0x00002122)/*    │        └─ Texture tiling
			*/x(Objects          ,= 0x00003000)/*    └─ Objects
			*/x(TriMesh          ,= 0x00003100)/*       └─ Mesh of triangles
			*/x(TriMeshName      ,= 0x00003101)/*          ├─ Name (cstr)
			*/x(TriMeshVertices  ,= 0x00003110)/*          ├─ Vertex list (u32 count, count * [vec4 pos, vec4 col, vec4 norm, vec2 uv])
			*/x(TriMeshFaces16   ,= 0x00003120)/*          ├─ Face list (u32 count, count * [u16 i0, i1, i2])
			*/x(TriMeshFaces32   ,= 0x00003121)/*          ├─ Face list (u32 count, count * [u32 i0, i1, i2])
			*/x(TriMeshNuggets   ,= 0x00003200)/*          └─ Nugget list (u32 count, count * [str16 mat, range vrange, irange])
			*/
			PR_DEFINE_ENUM2(EChunkId, PR_ENUM);
			#undef PR_ENUM
			#pragma endregion
			static_assert(sizeof(EChunkId::Enum_) == sizeof(u32), "");
			
			struct ChunkHeader
			{
				EChunkId::Enum_ id;
				u32 length;
			};
			static_assert(sizeof(ChunkHeader) == 8, "");

			struct Texture
			{
				// Filepath or string identifier for looking up the texture
				std::string m_filepath;

				// How the texture is to be mapped
				// 0 = clamp, 1 = wrap
				u32 m_tiling;

				Texture()
					:m_filepath()
					,m_tiling()
				{}
			};
			struct Material
			{
				// A unique name for the material.
				// The name is always 16 bytes, pad with zeros if you
				// use a string rather than a guid
				Str16 m_name;

				// Object diffuse colour
				pr::Colour m_diffuse;

				// Diffuse textures
				std::vector<Texture> m_tex_diffuse;

				Material()
					:m_name()
					,m_diffuse(pr::ColourWhite)
					,m_tex_diffuse()
				{}
			};
			struct Nugget
			{
				// The material used by this nugget
				Str16 m_mat;

				// The range of vertices and faces that use this material
				Range m_vrange;
				Range m_irange;
			};
			struct TriMesh
			{
				// A name for the model
				std::string m_name;

				// The basic vertex data for the mesh
				std::vector<Vert> m_vert;

				// The 16bit/32bit face indices (only one should contain data)
				std::vector<Face16> m_face16;
				std::vector<Face32> m_face32;

				// The nuggets to divide the mesh into for each material
				std::vector<Nugget> m_nugget;

				TriMesh()
					:m_name()
					,m_vert()
					,m_face16()
					,m_face32()
					,m_nugget()
				{}
			};
			struct Scene
			{
				std::vector<Material> m_materials;
				std::vector<TriMesh> m_trimeshs;
			};
			struct File
			{
				u32 m_version;
				Scene m_scene;
			};

			// Helpers for reading from a stream source
			// Specialise these for non std::istream's
			template <typename TSrc> struct Src
			{
				static u64  TellPos(TSrc& src)          { return static_cast<u64>(src.tellg()); }
				static bool SeekAbs(TSrc& src, u64 pos) { return static_cast<bool>(src.seekg(pos)); }
				//static bool SeekRel(TSrc& src, int ofs)   { return static_cast<bool>(src.seekg(ofs, src.cur)); }

				// Read an array
				template <typename TOut> static void Read(TSrc& src, TOut* out, size_t count)
				{
					if (src.read(char_ptr(out), count * sizeof(TOut))) return;
					throw std::exception("partial read of input stream");
				}
			};

			namespace impl
			{
				// Read an array
				template <typename TOut, typename TSrc>
				inline void Read(TSrc& src, TOut* out, size_t count)
				{
					Src<TSrc>::Read(src, out, count);
				}

				// Read a single type
				template <typename TOut, typename TSrc>
				inline TOut Read(TSrc& src)
				{
					TOut out;
					Read(src, &out, 1);
					return out;
				}

				// Generic chunk reading function
				// 'src' should point to a sub chunk
				// 'len' is the remaining bytes in the parent chunk from 'src' to the end of the parent chunk
				// 'func' is called back with the chunk header, it should return false to stop reading.
				template <typename TSrc, typename Func>
				void ReadChunks(TSrc& src, u32 len, Func func, u32* len_out = nullptr)
				{
					while (len != 0)
					{
						auto start = Src<TSrc>::TellPos(src);
						
						// Read the chunk header
						auto hdr = Read<ChunkHeader>(src);
						if (hdr.length <= len) len -= hdr.length; else throw std::exception(pr::FmtS("invalid chunk found at offset 0x%X", start));
						u32 data_len = hdr.length - sizeof(ChunkHeader);

						// Parse the chunk
						if (!func(hdr, src, data_len))
						{
							if (len_out) *len_out = len;
							return;
						}

						// Seek to the next chunk
						Src<TSrc>::SeekAbs(src, start + hdr.length);
					}
					if (len_out) *len_out = 0;
				}

				// Search from the current stream position to the next instance of chunk 'id'.
				// Assumes 'src' is positioned at a chunk header within a parent chunk.
				// 'len' is the number of bytes until the end of the parent chunk.
				// 'len_out' is an output of the length until the end of the parent chunk on return.
				// If 'next' is true and 'src' currently points to an 'id' chunk, then seeks to the next instance of 'id'
				// Returns the found chunk header with the current position of 'src' set immediately after it.
				template <typename TSrc>
				ChunkHeader Find(EChunkId id, TSrc& src, u32 len, u32* len_out = nullptr, bool next = false)
				{
					ChunkHeader chunk;
					ReadChunks(src, len, [&](ChunkHeader hdr, TSrc&, u32)
					{
						// If this is the chunk we're looking for return false to say "done"
						if (hdr.id == id && !next)
						{
							chunk = hdr;
							return false;
						}
						next = false;
						return true;
					}, len_out);
					return chunk;
				}

				// Read a null terminated string from a chunk.
				// Assumes 'src' points to the start of the string.
				template <typename TSrc>
				std::string ReadCStr(TSrc& src, u32 len, u32* len_out = nullptr)
				{
					std::string str;
					for (char c; len-- != 0 && (c = Read<char>(src)) != 0;)
						str.push_back(c);
				
					if (len_out) *len_out = len;
					return str;
				}

				// Read a texture from 'src'
				// Assumes 'src' points to a sub chunk within a TextureMap1, BumpMap, or ReflectionMap chunk
				template <typename TSrc>
				Texture ReadTexture(TSrc& src, u32 len)
				{
					Texture tex;
					ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, ulong data_len)
					{
						switch (hdr.id)
						{
						case EChunkId::TexFilepath:
							{
								tex.m_filepath = ReadCStr(src, data_len);
								break;
							}
						case EChunkId::TexTiling:
							{
								tex.m_tiling = Read<u8>(src);
								break;
							}
						}
						return true;
					});
					return tex;
				}

				// Read a material from 'src'
				// Assumes 'src' points to a sub chunk within a MaterialBlock chunk
				template <typename TSrc>
				Material ReadMaterial(TSrc& src, u32 len)
				{
					Material mat;
					mat.m_name = Read<Str16>(src);
					ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, ulong data_len)
					{
						switch (hdr.id)
						{
						case EChunkId::DiffuseColour:
							{
								mat.m_diffuse = Read<Col4>(src);
								break;
							}
						case EChunkId::DiffuseTexture:
							{
								mat.m_tex_diffuse.push_back(ReadTexture(src, data_len));
								break;
							}
						}
						return true;
					});
					return mat;
				}

				// Read a tri mesh from 'src'
				template <typename TSrc>
				TriMesh ReadTriMesh(TSrc& src, u32 len)
				{
					TriMesh mesh;
					ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, ulong data_len)
					{
						switch (hdr.id)
						{
						case EChunkId::TriMeshName:
							{
								mesh.m_name = ReadCStr(src, data_len);
								break;
							}
						case EChunkId::TriMeshVertices:
							{
								size_t count = Read<u32>(src);
								if (count * sizeof(Vert) != data_len)
									throw std::exception(FmtS("Vertex list count is invalid. Vertex count is %d, data available for %d verts.", count, data_len/sizeof(Vert)));

								mesh.m_vert.resize(count);
								Read(src, mesh.m_vert.data(), count);
								break;
							}
						case EChunkId::TriMeshFaces16:
							{
								size_t count = Read<u32>(src);
								if (count * sizeof(Face16) != data_len)
									throw std::exception(FmtS("Face list count is invalid. Face count is %d, data available for %d faces.", count, data_len/sizeof(Face16)));
								
								mesh.m_face16.resize(count);
								Read(src, mesh.m_face16.data(), count);
								break;
							}
						case EChunkId::TriMeshFaces32:
							{
								size_t count = Read<u32>(src);
								if (count * sizeof(Face32) != data_len)
									throw std::exception(FmtS("Face list count is invalid. Face count is %d, data available for %d faces.", count, data_len/sizeof(Face32)));
								
								mesh.m_face32.resize(count);
								Read(src, mesh.m_face32.data(), count);
								break;
							}
						case EChunkId::TriMeshNuggets:
							{
								size_t count = Read<u32>(src);
								if (count * sizeof(Nugget) != data_len)
									throw std::exception(FmtS("Nugget list count is invalid. Nugget count is %d, data available for %d nuggets.", count, data_len/sizeof(Nugget)));
								
								mesh.m_nugget.resize(count);
								Read(src, mesh.m_nugget.data(), count);
								break;
							}
						}
						return true;
					});
					return mesh;
				}
			}

			// Extract the materials in the given P3D stream
			template <typename TSrc, typename TMatObj>
			void ReadMaterials(TSrc& src, TMatObj mat_out)
			{
				using namespace pr::geometry::p3d;

				// Restore the src position on return
				auto start = Src<TSrc>::TellPos(src);
				auto reset_stream = pr::CreateScope([]{}, [&]{ Src<TSrc>::SeekAbs(src, start); });

				// Check that this is actually a P3D stream
				auto main = impl::Read<ChunkHeader>(src);
				if (main.id != EChunkId::Main)
					throw std::exception("Source is not a p3d stream");

				// Find the Scene sub chunk
				auto scene = impl::Find(EChunkId::Scene, src, main.length - sizeof(ChunkHeader));
				if (scene.id != EChunkId::Scene)
					return; // Source contains no scene data

				// Find the Materials sub chunk
				auto mats = impl::Find(EChunkId::Materials, src, scene.length - sizeof(ChunkHeader));
				if (mats.id != EChunkId::Materials)
					return; // Source contains no materials

				// Read the materials
				ReadChunks(src, mats.length - sizeof(ChunkHeader), [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.id)
					{
					case EChunkId::Material:
						mat_out(impl::ReadMaterial(src, data_len));
						break;
					}
					return true;
				});
			}

			// Extract the objects from a P3D stream
			template <typename TSrc, typename TObjOut>
			void ReadObjects(TSrc& src, TObjOut obj_out)
			{
				using namespace pr::geometry::p3d;

				// Restore the src position on return
				auto start = Src<TSrc>::TellPos(src);
				auto reset_stream = pr::CreateScope([]{}, [&]{ Src<TSrc>::SeekAbs(src, start); });

				// Check that this is actually a P3D stream
				auto main = Read<ChunkHeader>(src);
				if (main.id != EChunkId::Main)
					throw std::exception("Source is not a p3d stream");

				// Find the Scene sub chunk
				auto scene = Find(EChunkId::Scene, src, main.length - sizeof(ChunkHeader));
				if (scene.id != EChunkId::Scene)
					return; // Source contains no scene data

				// Find the Objects sub chunk
				auto objs = Find(EChunkId::Objects, src, main.length - sizeof(ChunkHeader));
				if (objs.id != EChunkId::Objects)
					return; // Source contains no objects data

				// Read the objects
				ReadChunks(src, objs.len - sizeof(ChunkHeader), [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.id)
					{
					case EChunkId::TriMesh:
						obj_out(impl::ReadTriMesh(src, data_len));
						break;
					}
					return true;
				});
			}
		}
	}
}

#if PR_UNITTESTS
#include <fstream>
#include "pr/common/unittests.h"
#include "pr/renderer11/renderer.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_p3d)
		{
			using namespace pr::geometry;
			//std::ifstream ifile("P:\\dump\\test2.3ds", std::ifstream::binary);
			//Read3DSMaterials(ifile, [](max_3ds::Material&&){});
			//Read3DSModels(ifile, [](max_3ds::Object&&){});
		}
	}
}
#endif