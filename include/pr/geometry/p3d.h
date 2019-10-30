//********************************
// PR3d Model file format
//  Copyright (c) Rylogic Ltd 2014
//********************************
// Binary 3d model format
// Based on 3ds.
// This model format is designed for load speed
// Use zip if you want compressed model data.

#pragma once

#include <string>
#include <vector>
#include <iostream>
#include <memory>
#include <exception>
#include <cassert>
#include "pr/macros/enum.h"
#include "pr/common/cast.h"
#include "pr/common/fmt.h"
#include "pr/common/colour.h"
#include "pr/common/range.h"
#include "pr/common/scope.h"
#include "pr/geometry/common.h"
#include "pr/geometry/utility.h"
#include "pr/maths/maths.h"

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
			struct ChunkHeader;
			struct ChunkIndex;
			ChunkIndex const& NullChunk();

			#pragma region Chunk Ids
			#define PR_ENUM(x)\
			x(Null           ,= 0x00000000)/* Null chunk                                            */\
			x(CStr           ,= 0x00000001)/* Null terminated ascii string                          */\
			x(Main           ,= 0x44335250)/* PR3D File type indicator                              */\
			x(FileVersion    ,= 0x00000100)/* ├─ File Version                                       */\
			x(Scene          ,= 0x00001000)/* └─ Scene                                              */\
			x(Materials      ,= 0x00002000)/*    ├─ Materials                                       */\
			x(Material       ,= 0x00002100)/*    │  └─ Material                                     */\
			x(DiffuseColour  ,= 0x00002110)/*    │     ├─ Diffuse Colour                            */\
			x(DiffuseTexture ,= 0x00002120)/*    │     └─ Diffuse texture                           */\
			x(TexFilepath    ,= 0x00002121)/*    │        ├─ Texture filepath                       */\
			x(TexTiling      ,= 0x00002122)/*    │        └─ Texture tiling                         */\
			x(Meshes         ,= 0x00003000)/*    └─ Meshes                                          */\
			x(Mesh           ,= 0x00003100)/*       └─ Mesh of lines,triangles,tetras               */\
			x(MeshName       ,= 0x00003101)/*          ├─ Name (cstr)                               */\
			x(MeshBBox       ,= 0x00003102)/*          ├─ Bounding box (BBox)                       */\
			x(MeshVertices   ,= 0x00003110)/*          ├─ Vertex list (u32 count, count * [Vert])   */\
			x(MeshIndices    ,= 0x00003120)/*          ├─ Index list (u32 count, count * [u16 i])   */\
			x(MeshIndices32  ,= 0x00003121)/*          ├─ Index list (u32 count, count * [u32 i])   */\
			x(MeshNuggets    ,= 0x00003200)/*          └─ Nugget list (u32 count, count * [Nugget]) */

			PR_DEFINE_ENUM2(EChunkId, PR_ENUM);
			#undef PR_ENUM
			#pragma endregion
			static_assert(sizeof(EChunkId) == sizeof(u32), "");

			struct ChunkHeader
			{
				EChunkId m_id;
				u32      m_length;
			};
			static_assert(sizeof(ChunkHeader) == 8, "Incorrect chunk header size");

			// Used to build an index of a p3d file
			struct ChunkIndex :ChunkHeader
			{
				std::vector<ChunkIndex> m_chunks;

				ChunkIndex(EChunkId id, size_t data_length)
					:ChunkHeader({id, s_cast<u32>(sizeof(ChunkHeader) + data_length)})
					,m_chunks()
				{}
				ChunkIndex(EChunkId id, size_t data_length, std::initializer_list<ChunkIndex> chunks)
					:ChunkHeader({id, s_cast<u32>(sizeof(ChunkHeader) + data_length)})
					,m_chunks()
				{
					for (auto& c : chunks)
						add(c);
				}
				void add(ChunkIndex chunk)
				{
					m_chunks.emplace_back(chunk);
					m_length += chunk.m_length;
				}
				ChunkIndex const& operator[](EChunkId id) const
				{
					for (auto& c : m_chunks)
						if (c.m_id == id)
							return c;
					
					std::stringstream ss; ss << "Child chunk " << id << " not a member of chunk " << m_id;
					throw std::exception(ss.str().c_str());
				}
				ChunkIndex const& find(std::initializer_list<EChunkId> chunk_id) const
				{
					auto* chunks = &m_chunks;
					auto b = std::begin(chunk_id);
					auto e = std::end(chunk_id);
					for (;b != e; ++b)
					{
						for (auto& c : *chunks)
						{
							if (c.m_id != *b) continue;
							if ((b + 1) == e) return c;
							chunks = &c.m_chunks;
							break;
						}
					}
					return NullChunk();
				}
			};

			// Maths library types with no alignment requirements
			struct Vec2
			{
				float x,y;
				operator v2() const { return v2(x, y); }
				Vec2& operator = (v2 const& rhs) { x = rhs.x; y = rhs.y; return *this; }
			};
			inline bool operator == (Vec2 lhs, Vec2 rhs) { return lhs.x == rhs.x && lhs.y == rhs.y; }
			inline bool operator != (Vec2 lhs, Vec2 rhs) { return !(lhs == rhs); }
			struct Vec4
			{
				#pragma warning (disable:4201)
				union {
				struct { float x,y,z,w; };
				struct { float r,g,b,a; };
				};
				#pragma warning (default:4201)
				operator v4() const     { return v4(x,y,z,w); }
				operator Colour() const { return Colour(r,g,b,a); }
				Vec4& operator = (v4 const& rhs) { x = rhs.x; y = rhs.y; z = rhs.z; w = rhs.w; return *this; }
				Vec4& operator = (Colour const& rhs) { r = rhs.r; g = rhs.g; b = rhs.b; a = rhs.a; return *this; }
			};
			inline bool operator == (Vec4 lhs, Vec4 rhs) { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w; }
			inline bool operator != (Vec4 lhs, Vec4 rhs) { return !(lhs == rhs); }
			struct BBox
			{
				Vec4 centre;
				Vec4 radius;
				BBox() { centre = pr::v4Origin; radius = -pr::v4One.w0(); }
				BBox& operator = (pr::BBox const& rhs) { centre = rhs.m_centre; radius = rhs.m_radius; return *this; }
				operator pr::BBox() const { return pr::BBox(centre, radius); }
			};
			struct Vert
			{
				// This type matches 'pr::rdr::Vert' (which has 16-byte alignment). Hence the 'pad' member.
				Vec4 pos;
				Vec4 col;
				Vec4 norm;
				Vec2 uv;
				Vec2 pad;
			};
			inline Vec4 GetP(Vert const& vert) { return vert.pos; }
			inline Vec4 GetC(Vert const& vert) { return vert.col; }
			inline Vec4 GetN(Vert const& vert) { return vert.norm; }
			inline Vec2 GetT(Vert const& vert) { return vert.uv; }
			struct Range
			{
				u32 first, count;

				template <typename T> Range& operator = (pr::Range<T> rhs)
				{
					first = s_cast<u32>(rhs.m_beg);
					count = s_cast<u32>(rhs.size());
					return *this;
				}
				template <typename T> operator pr::Range<T> () const
				{
					return pr::Range<T>(s_cast<T>(first), s_cast<T>(first + count));
				}
				u32 begin() const { return first; }
				u32 end() const   { return first + count; }
			};
			inline bool operator == (Range lhs, Range rhs) { return lhs.first == rhs.first && lhs.count == rhs.count; }
			inline bool operator != (Range lhs, Range rhs) { return !(lhs == rhs); }
			struct Str16
			{
				using c16 = char[16];
				c16 str;

				Str16(char const* s = nullptr)
					:str()
				{
					*this = s;
				}
				Str16& operator = (char const* s)
				{
					s = s ? s : "";
					memset(str, 0, sizeof(str));
					strncat(str, s, sizeof(str) - 1);
					return *this;
				}
				Str16& operator = (std::string const& s) { return *this = s.c_str(); }
				operator std::string() const             { return str; }
				operator c16 const&() const              { return str; }
			};
			inline bool operator == (Str16 const& lhs, Str16 const& rhs) { return strncmp(lhs.str, rhs.str, sizeof(Str16)) == 0; }
			inline bool operator != (Str16 const& lhs, Str16 const& rhs) { return !(lhs == rhs); }
			struct Nugget
			{
				EPrim m_topo;
				EGeom m_geom; 
				Range m_vrange;
				Range m_irange;
				Str16 m_mat;
			};
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
				Texture(std::string filepath, u32 tiling)
					:m_filepath(filepath)
					,m_tiling(tiling)
				{}
			};
			struct Material
			{
				// A unique name/guid for the material.
				// The id is always 16 bytes, pad with zeros if you
				// use a string rather than a guid
				Str16 m_id;

				// Object diffuse colour
				pr::Colour m_diffuse;

				// Diffuse textures
				std::vector<Texture> m_tex_diffuse;

				Material(std::string name = std::string(), pr::Colour const& diff_colour = pr::ColourWhite)
					:m_id(name.c_str())
					,m_diffuse(diff_colour)
					,m_tex_diffuse()
				{}
			};
			struct Mesh
			{
				// A name for the model
				std::string m_name;

				// The basic vertex data for the mesh
				std::vector<Vert> m_verts;

				// The 16bit/32bit indices (only one should contain data)
				std::vector<u16> m_idx16;
				std::vector<u32> m_idx32;

				// The nuggets to divide the mesh into for each material
				std::vector<Nugget> m_nugget;

				// Mesh bounding box
				BBox m_bbox;

				Mesh(std::string name = std::string())
					:m_name(name)
					,m_verts()
					,m_idx16()
					,m_idx32()
					,m_nugget()
					,m_bbox()
				{}
			};
			struct Scene
			{
				std::vector<Material> m_materials;
				std::vector<Mesh>     m_meshes;

				Scene()
					:m_materials()
					,m_meshes()
				{}
			};
			struct File
			{
				u32 m_version;
				Scene m_scene;

				File()
					:m_version(Version)
					,m_scene()
				{}
			};

			// Static null chunk
			inline ChunkIndex const& NullChunk()
			{
				static ChunkIndex nullchunk(EChunkId::Null, 0);
				return nullchunk;
			}

			// Helper to return the data size for a chunk
			inline u32 DataLength(ChunkHeader chunk)
			{
				assert(chunk.m_length >= sizeof(ChunkHeader));
				return chunk.m_length - sizeof(ChunkHeader);
			}

			// Helpers for reading/writing from/to an istream-like source
			// Specialise these for non std::istream/std::ostream's
			template <typename TSrc> struct Src
			{
				static u64  TellPos(TSrc& src)          { return static_cast<u64>(src.tellg()); }
				static bool SeekAbs(TSrc& src, u64 pos) { return static_cast<bool>(src.seekg(pos)); }

				// Read an array
				template <typename TOut> static void Read(TSrc& src, TOut* out, size_t count)
				{
					if (src.read(char_ptr(out), count * sizeof(TOut))) return;
					throw std::exception("partial read of input stream");
				}

				// Write an array
				template <typename TIn> static void Write(TSrc& dst, TIn const* in, size_t count)
				{
					if (dst.write(char_ptr(in), count * sizeof(TIn))) return;
					throw std::exception("partial write of output stream");
				}
			};

			#pragma region Build Chunk Index

			// Build a chunk index from parts of a p3d model
			inline ChunkIndex Index(Texture const& tex)
			{
				auto sub_chunks =
				{
					ChunkIndex(EChunkId::TexFilepath, tex.m_filepath.size() + 1),
					ChunkIndex(EChunkId::TexTiling, sizeof(u32))
				};
				return ChunkIndex(EChunkId::DiffuseTexture, 0U, sub_chunks);
			}
			inline ChunkIndex Index(Material const& mat)
			{
				auto index = ChunkIndex(EChunkId::Material, sizeof(Str16),
					{
						ChunkIndex(EChunkId::DiffuseColour, sizeof(Vec4))
					});

				for (auto& tex : mat.m_tex_diffuse)
					index.add(Index(tex));

				return index;
			}
			inline ChunkIndex Index(Mesh const& mesh)
			{
				auto index = ChunkIndex(EChunkId::Mesh, 0);
				index.add(ChunkIndex(EChunkId::MeshName, mesh.m_name.size() + 1));
				index.add(ChunkIndex(EChunkId::MeshBBox, sizeof(BBox)));
				if (!mesh.m_verts.empty())  index.add(ChunkIndex(EChunkId::MeshVertices , sizeof(u32) + mesh.m_verts.size()  * sizeof(Vert  )));
				if (!mesh.m_idx16.empty())  index.add(ChunkIndex(EChunkId::MeshIndices  , sizeof(u32) + mesh.m_idx16.size()  * sizeof(u16   )));
				if (!mesh.m_idx32.empty())  index.add(ChunkIndex(EChunkId::MeshIndices32, sizeof(u32) + mesh.m_idx32.size()  * sizeof(u32   )));
				if (!mesh.m_nugget.empty()) index.add(ChunkIndex(EChunkId::MeshNuggets  , sizeof(u32) + mesh.m_nugget.size() * sizeof(Nugget)));
				return index;
			}
			inline ChunkIndex Index(Scene const& scene)
			{
				auto index = ChunkIndex(EChunkId::Scene, 0);
				if (!scene.m_materials.empty())
				{
					auto mats = ChunkIndex(EChunkId::Materials, 0);
					for (auto& mat : scene.m_materials) mats.add(Index(mat));
					index.add(mats);
				}
				if (!scene.m_meshes.empty())
				{
					auto meshs = ChunkIndex(EChunkId::Meshes, 0);
					for (auto& mesh : scene.m_meshes) meshs.add(Index(mesh));
					index.add(meshs);
				}
				return index;
			}
			inline ChunkIndex Index(File const& file)
			{
				return ChunkIndex(EChunkId::Main, 0U,
					{
						ChunkIndex(EChunkId::FileVersion, sizeof(32)),
						{Index(file.m_scene)}
					});
			}

			#pragma endregion

			// Generic chunk reading function.
			// 'src' should point to data after the chunk header.
			// 'len' is the remaining bytes in the parent chunk from 'src' to the end of the parent chunk
			// 'func' is called back with the chunk header and 'src' positioned at the start of the chunk data
			// 'func' should return true to stop reading (i.e. chunk found!).
			template <typename TSrc, typename Func> void ReadChunks(TSrc& src, u32 len, Func func, u32* len_out = nullptr)
			{
				while (len != 0)
				{
					auto start = Src<TSrc>::TellPos(src);

					// Read the chunk header
					auto hdr = Read<ChunkHeader>(src);
					if (hdr.m_length <= len) len -= hdr.m_length; else throw std::exception(pr::FmtS("invalid chunk found at offset 0x%X", start));
					u32 data_len = DataLength(hdr);

					// Parse the chunk
					if (func(hdr, src, data_len))
					{
						if (len_out) *len_out = len;
						return;
					}

					// Seek to the next chunk
					Src<TSrc>::SeekAbs(src, start + hdr.m_length);
				}
				if (len_out) *len_out = 0;
			}

			// Search from the current stream position to the next instance of chunk 'id'.
			// Assumes 'src' is positioned at a chunk header within a parent chunk.
			// 'len' is the number of bytes until the end of the parent chunk.
			// 'len_out' is an output of the length until the end of the parent chunk on return.
			// If 'next' is true and 'src' currently points to an 'id' chunk, then seeks to the next instance of 'id'
			// Returns the found chunk header with the current position of 'src' set immediately after it.
			template <typename TSrc> ChunkHeader Find(EChunkId id, TSrc& src, u32 len, u32* len_out = nullptr, bool next = false)
			{
				ChunkHeader chunk;
				ReadChunks(src, len, [&](ChunkHeader hdr, TSrc&, u32)
				{
					// If this is the chunk we're looking for return false to say "done"
					if (hdr.m_id == id && !next)
					{
						chunk = hdr;
						return true;
					}

					next = false;
					return false;
				}, len_out);
				return chunk;
			}

			// Search from the current stream position to the nested chunk described by the list
			// 'src' is assume to be pointed to a chunk header.
			template <typename TSrc> ChunkHeader Find(TSrc& src, u32 len, std::initializer_list<EChunkId> chunk_id)
			{
				ChunkHeader hdr = {};
				for (auto id : chunk_id)
				{
					hdr = Find(id, src, len);
					if (hdr.m_id != id)
					{
						// Special case the Main chunk, if it's missing assume 'src' is not a p3d stream and throw
						if (id == EChunkId::Main) throw std::exception("Source is not a p3d stream");
						return ChunkHeader();
					}
					len = DataLength(hdr);
				}
				return hdr;
			}

			#pragma region Read

			// All of these Read functions assume 'src' points to the start
			// of the chunk data of the corresponding chunk type.

			// Read an array
			template <typename TOut, typename TSrc> inline void Read(TSrc& src, TOut* out, size_t count)
			{
				Src<TSrc>::Read(src, out, count);
			}

			// Read a single type
			template <typename TOut, typename TSrc> inline TOut Read(TSrc& src)
			{
				TOut out;
				Read(src, &out, 1);
				return std::move(out);
			}

			// Read a null terminated string. 'src' is assumed to point to the start of a null terminated string
			template <typename TSrc, typename TStr = std::string> TStr ReadCStr(TSrc& src, u32 len, u32* len_out = nullptr)
			{
				TStr str;
				for (char c; len-- != 0 && (c = Read<char>(src)) != 0;)
					str.push_back(c);
				
				if (len_out) *len_out = len;
				return std::move(str);
			}

			// Read a texture. 'src' is assumed to point to the start of EChunkId::Texture chunk data
			template <typename TSrc> Texture ReadTexture(TSrc& src, u32 len)
			{
				Texture tex;
				ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::TexFilepath:
						{
							tex.m_filepath = ReadCStr(src, data_len);
							break;
						}
					case EChunkId::TexTiling:
						{
							tex.m_tiling = Read<u32>(src);
							break;
						}
					}
					return false;
				});
				return std::move(tex);
			}

			// Read a material. 'src' is assumed to point to the start of EChunkId::Material chunk data
			template <typename TSrc> Material ReadMaterial(TSrc& src, u32 len)
			{
				Material mat;

				Read(src, mat.m_id.str, sizeof(Str16));
				len -= sizeof(Str16);

				ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::DiffuseColour:
						{
							auto col = Read<Vec4>(src);
							mat.m_diffuse = pr::Colour(col.r, col.g, col.b, col.a);
							break;
						}
					case EChunkId::DiffuseTexture:
						{
							mat.m_tex_diffuse.push_back(ReadTexture(src, data_len));
							break;
						}
					}
					return false;
				});
				return std::move(mat);
			}

			// Fill a container of verts. 'src' is assumed to point to the start of EChunkId::MeshVertices chunk data
			template <typename TCont, typename TSrc> TCont ReadMeshVertices(TSrc& src, u32 data_len)
			{
				TCont cont;

				size_t count = Read<u32>(src);
				data_len -= sizeof(u32);

				static_assert(sizeof(Vert) == sizeof(std::remove_reference<decltype(cont[0])>::type), "Vertex type size mismatch");
				if (count * sizeof(Vert) != data_len)
					throw std::exception(FmtS("Vertex list count is invalid. Vertex count is %d, data available for %d verts.", count, data_len/sizeof(Vert)));

				cont.resize(count);
				Read(src, cont.data(), count);
				return std::move(cont);
			}

			// Fill a container of indices. 'src' is assumed to point to the start of EChunkId::MeshIndices chunk data
			template <typename TCont, typename TSrc> TCont ReadMeshIndices(TSrc& src, u32 data_len)
			{
				TCont cont;

				size_t count = Read<u32>(src);
				data_len -= sizeof(u32);

				using TIndex = std::remove_reference<decltype(cont[0])>::type;
				if (count * sizeof(TIndex) != data_len)
					throw std::exception(FmtS("Index list count is invalid. Index count is %d, data available for %d indices.", count, data_len/sizeof(TIndex)));

				cont.resize(count);
				Read(src, cont.data(), count);
				return std::move(cont);
			}

			// Fill a container of nuggets. 'src' is assumed to point to the start of EChunkId::MeshNuggets chunk data
			template <typename TCont, typename TSrc> TCont ReadMeshNuggets(TSrc& src, u32 data_len)
			{
				TCont cont;

				size_t count = Read<u32>(src);
				data_len -= sizeof(u32);

				if (count * sizeof(Nugget) != data_len)
					throw std::exception(FmtS("Nugget list count is invalid. Nugget count is %d, data available for %d nuggets.", count, data_len/sizeof(Nugget)));

				cont.resize(count);
				Read(src, cont.data(), count);
				return std::move(cont);
			}

			// Read a mesh. 'src' is assumed to point to the start of EChunkId::Mesh chunk data
			template <typename TSrc> Mesh ReadMesh(TSrc& src, u32 len)
			{
				Mesh mesh;
				ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, ulong data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::MeshName:
						{
							mesh.m_name = ReadCStr(src, data_len);
							break;
						}
					case EChunkId::MeshBBox:
						{
							mesh.m_bbox = Read<BBox>(src);
							break;
						}
					case EChunkId::MeshVertices:
						{
							mesh.m_verts = ReadMeshVertices<decltype(mesh.m_verts)>(src, data_len);
							break;
						}
					case EChunkId::MeshIndices:
						{
							mesh.m_idx16 = ReadMeshIndices<decltype(mesh.m_idx16)>(src, data_len);
							break;
						}
					case EChunkId::MeshIndices32:
						{
							mesh.m_idx32 = ReadMeshIndices<decltype(mesh.m_idx32)>(src, data_len);
							break;
						}
					case EChunkId::MeshNuggets:
						{
							mesh.m_nugget = ReadMeshNuggets<decltype(mesh.m_nugget)>(src, data_len);
							break;
						}
					}
					return false;
				});
				return std::move(mesh);
			}

			// Read a scene. 'src' is assumed to point to the start of EChunkId::Scene chunk data
			template <typename TSrc> Scene ReadScene(TSrc& src, u32 len)
			{
				Scene scene;
				ReadChunks(src, len, [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::Materials:
						{
							ReadChunks(src, data_len, [&](ChunkHeader hdr, TSrc& src, u32 data_len)
							{
								switch (hdr.m_id)
								{
								case EChunkId::Material:
									scene.m_materials.push_back(ReadMaterial(src, data_len));
									break;
								}
								return false;
							});
							break;
						}
					case EChunkId::Meshes:
						{
							ReadChunks(src, data_len, [&](ChunkHeader hdr, TSrc& src, u32 data_len)
							{
								switch (hdr.m_id)
								{
								case EChunkId::Mesh:
									scene.m_meshes.push_back(ReadMesh(src, data_len));
									break;
								}
								return false;
							});
							break;
						}
					}
					return false;
				});
				return std::move(scene);
			}

			// Read a p3d::File into memory from an istream-like source. Uses forward iteration only.
			template <typename TSrc> File Read(TSrc& src)
			{
				File file;

				// Check that this is actually a P3D stream
				auto main = Read<ChunkHeader>(src);
				if (main.m_id != EChunkId::Main)
					throw std::exception("Source is not a p3d stream");

				// Read the sub chunks
				ReadChunks(src, DataLength(main), [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::FileVersion:
						file.m_version = Read<u32>(src);
						break;
					case EChunkId::Scene:
						file.m_scene = ReadScene(src, data_len);
						break;
					}
					return false;
				});

				return std::move(file);
			}

			// Extract the materials in the given P3D stream
			template <typename TSrc, typename TMatObj> void ReadMaterials(TSrc& src, TMatObj mat_out)
			{
				// Restore the src position on return
				auto reset_stream = pr::CreateStateScope([&]{ return Src<TSrc>::TellPos(src); }, [&](u64 start){ Src<TSrc>::SeekAbs(src, start); });

				// Check that this is actually a P3D stream
				auto main = Read<ChunkHeader>(src);
				if (main.m_id != EChunkId::Main)
					throw std::exception("Source is not a p3d stream");

				// Find the Materials sub chunk
				auto mats = Find(src, {EChunkId::Scene, EChunkId::Materials});
				if (mats.m_id != EChunkId::Materials)
					return; // Source contains no materials

				// Read the materials
				ReadChunks(src, DataLength(mats), [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::Material:
						if (mat_out(ReadMaterial(src, data_len)))
							return true; // Stop reading
						break;
					}
					return false;
				});
			}

			// Extract the meshes from a P3D stream
			template <typename TSrc, typename TMeshOut> void ReadMeshes(TSrc& src, TMeshOut mesh_out)
			{
				// Restore the src position on return
				auto reset_stream = pr::CreateStateScope(
					[&]{ return Src<TSrc>::TellPos(src); },
					[&](u64 start){ Src<TSrc>::SeekAbs(src, start); }
				);

				// Find the Meshes sub chunk
				auto meshes = Find(src, ~0U, {EChunkId::Main, EChunkId::Scene, EChunkId::Meshes});
				if (meshes.m_id != EChunkId::Meshes)
					return; // Source contains no mesh data

				// Read the meshes
				ReadChunks(src, DataLength(meshes), [&](ChunkHeader hdr, TSrc& src, u32 data_len)
				{
					switch (hdr.m_id)
					{
					case EChunkId::Mesh:
						if (mesh_out(ReadMesh(src, data_len)))
							return true; // stop reading
						break;
					}
					return false;
				});
			}

			#pragma endregion

			#pragma region Write

			// Write an array
			template <typename TIn, typename TSrc> inline void Write(TSrc& dst, TIn const* in, size_t count)
			{
				Src<TSrc>::Write(dst, in, count);
			}

			// Write a single type
			template <typename TIn, typename TSrc> inline void Write(TSrc& dst, TIn const& in)
			{
				Write(dst, &in, 1);
			}

			// Write a null terminated string to 'out'
			template <typename TSrc> void WriteCStr(TSrc& out, std::string const& str)
			{
				Write(out, str.c_str(), str.size() + 1);
			}

			// Write a texture to 'out'
			template <typename TSrc> void WriteTexture(TSrc& out, ChunkIndex const& index, Texture const& tex)
			{
				// Texture chunk header
				Write<ChunkHeader>(out, index);

				// Texture sub chunks
				for (auto& idx : index.m_chunks)
				{
					switch (idx.m_id)
					{
					case EChunkId::TexFilepath:
						{
							Write<ChunkHeader>(out, idx);
							WriteCStr(out, tex.m_filepath);
							break;
						}
					case EChunkId::TexTiling:
						{
							Write<ChunkHeader>(out, idx);
							Write<u32>(out, tex.m_tiling);
							break;
						}
					}
				}
			}

			// Write a material to 'out'
			template <typename TSrc> void WriteMaterial(TSrc& out, ChunkIndex const& index, Material const& mat)
			{
				// Material chunk header
				Write<ChunkHeader>(out, index);

				// Material name
				Write(out, mat.m_id.str, sizeof(Str16));

				// Material sub chunks
				auto tex = std::begin(mat.m_tex_diffuse);
				for (auto& idx : index.m_chunks)
				{
					switch (idx.m_id)
					{
					case EChunkId::DiffuseColour:
						{
							Write<ChunkHeader>(out, idx);
							Write<Vec4>(out, {mat.m_diffuse.r, mat.m_diffuse.g, mat.m_diffuse.b, mat.m_diffuse.a});
							break;
						}
					case EChunkId::DiffuseTexture:
						{
							WriteTexture(out, idx, *tex++);
							break;
						}
					}
				}
			}

			// Write a mesh to 'out'
			template <typename TSrc> void WriteMesh(TSrc& out, ChunkIndex const& index, Mesh const& mesh)
			{
				// TriMesh chunk header
				Write<ChunkHeader>(out, index);

				// Mesh sub chunks
				for (auto& idx : index.m_chunks)
				{
					switch (idx.m_id)
					{
					case EChunkId::MeshName:
						{
							Write<ChunkHeader>(out, idx);
							WriteCStr(out, mesh.m_name);
							break;
						}
					case EChunkId::MeshBBox:
						{
							assert(mesh.m_bbox.radius.x >= 0 && mesh.m_bbox.radius.y >= 0 && mesh.m_bbox.radius.z >= 0 && "Writing an invalid bounding box into p3d");
							Write<ChunkHeader>(out, idx);
							Write<BBox>(out, mesh.m_bbox);
							break;
						}
					case EChunkId::MeshVertices:
						{
							Write<ChunkHeader>(out, idx);
							Write<u32>(out, s_cast<u32>(mesh.m_verts.size()));
							Write<Vert>(out, mesh.m_verts.data(), mesh.m_verts.size());
							break;
						}
					case EChunkId::MeshIndices:
						{
							Write<ChunkHeader>(out, idx);
							Write<u32>(out, s_cast<u32>(mesh.m_idx16.size()));
							Write<u16>(out, mesh.m_idx16.data(), mesh.m_idx16.size());
							break;
						}
					case EChunkId::MeshIndices32:
						{
							Write<ChunkHeader>(out, idx);
							Write<u32>(out, s_cast<u32>(mesh.m_idx32.size()));
							Write<u32>(out, mesh.m_idx32.data(), mesh.m_idx32.size());
							break;
						}
					case EChunkId::MeshNuggets:
						{
							Write<ChunkHeader>(out, idx);
							Write<u32>(out, s_cast<u32>(mesh.m_nugget.size()));
							Write<Nugget>(out, mesh.m_nugget.data(), mesh.m_nugget.size());
							break;
						}
					}
				}
			}

			// Write a scene to 'out'
			template <typename TSrc> void WriteScene(TSrc& out, ChunkIndex const& index, Scene const& scene)
			{
				Write<ChunkHeader>(out, index);
				for (auto& idx : index.m_chunks)
				{
					switch (idx.m_id)
					{
					case EChunkId::Materials:
						{
							Write<ChunkHeader>(out, idx);
							auto mat = std::begin(scene.m_materials);
							for (auto& i : idx.m_chunks)
								WriteMaterial(out, i, *mat++);
							break;
						}
					case EChunkId::Meshes:
						{
							Write<ChunkHeader>(out, idx);
							auto mesh = std::begin(scene.m_meshes);
							for (auto& i : idx.m_chunks)
								WriteMesh(out, i, *mesh++);
							break;
						}
					}
				}
			}

			// Write the p3d file to an ostream-like output. Uses forward iteration only.
			template <typename TSrc> void Write(TSrc& out, File const& file)
			{
				// Build an index of the file, calculating chunk sizes
				ChunkIndex root = Index(file);

				// Write the main file chunk header
				Write<ChunkHeader>(out, root);

				// Write the sub chunks
				for (auto& index : root.m_chunks)
				{
					switch (index.m_id)
					{
					case EChunkId::FileVersion:
						Write<ChunkHeader>(out, index);
						Write<u32>(out, file.m_version);
						break;
					case EChunkId::Scene:
						WriteScene(out, index, file.m_scene);
						break;
					}
				}
			}

			// Write the p3d file as code
			template <typename TSrc> void WriteAsCode(TSrc& out, File const& file, char const* indent = "")
			{
				for (auto& mesh : file.m_scene.m_meshes)
				{
					if (!mesh.m_idx16.empty())
						pr::geometry::GenerateModelCode(mesh.m_name, mesh.m_verts.size(), mesh.m_idx16.size(), std::begin(mesh.m_verts), std::begin(mesh.m_idx16), out, indent);
					else
						pr::geometry::GenerateModelCode(mesh.m_name, mesh.m_verts.size(), mesh.m_idx32.size(), std::begin(mesh.m_verts), std::begin(mesh.m_idx32), out, indent);

					out << indent << "#pragma region BoundingBox\n";
					out << indent << "static pr::BBox const bbox = {";
					out << pr::Fmt("{%+ff, %+ff, %+ff, 1.0f}, "   , mesh.m_bbox.centre.x, mesh.m_bbox.centre.y, mesh.m_bbox.centre.z);
					out << pr::Fmt("{%+ff, %+ff, %+ff, 0.0f}};\n" , mesh.m_bbox.radius.x, mesh.m_bbox.radius.y, mesh.m_bbox.radius.z);
					out << indent << "#pragma endregion\n";
				}
			}

			#pragma endregion
		}
	}
	namespace maths
	{
		template <> struct is_vec<geometry::p3d::Vec4> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 4;
		};
		template <> struct is_vec<geometry::p3d::Vec2> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 2;
		};
	}
}

#if PR_UNITTESTS
#include <fstream>
#include "pr/common/unittests.h"
#include "pr/renderer11/renderer.h"
namespace pr::geometry
{
	PRUnitTest(P3dTests)
	{
		using namespace pr::geometry;
		p3d::File file;
		file.m_version = p3d::Version;

		p3d::Texture tex;
		tex.m_filepath = "filepath";
		tex.m_tiling = 1;

		p3d::Material mat;
		mat.m_id = "mat1";
		mat.m_diffuse = pr::ColourWhite;
		mat.m_tex_diffuse.push_back(tex);

		file.m_scene.m_materials.push_back(mat);

		p3d::Nugget nug = {};
		nug.m_topo = EPrim::TriList;
		nug.m_geom = EGeom::Vert|EGeom::Colr|EGeom::Norm|EGeom::Tex0;
		nug.m_vrange.first = 0;
		nug.m_vrange.count = 4;
		nug.m_irange.first = 0;
		nug.m_irange.count = 4;
		nug.m_mat = "mat1";

		p3d::Mesh mesh;
		mesh.m_name = "mesh";
		mesh.m_bbox = pr::BBox(v4Origin, v4(1,2,3,0));
		mesh.m_verts.push_back(p3d::Vert());
		mesh.m_verts.push_back(p3d::Vert());
		mesh.m_verts.push_back(p3d::Vert());
		mesh.m_verts.push_back(p3d::Vert());
		mesh.m_idx16.push_back(0);
		mesh.m_idx16.push_back(1);
		mesh.m_idx16.push_back(2);
		mesh.m_idx16.push_back(3);
		mesh.m_nugget.push_back(nug);
		file.m_scene.m_meshes.push_back(mesh);

		//std::ofstream buf("\\dump\\test.p3d", std::ofstream:: binary);
		//p3d::Write(buf, file);

		//*
		std::stringstream buf(std::ios_base::in|std::ios_base::out|std::ios_base::binary);
		p3d::Write(buf, file);

		auto index = p3d::Index(file);
		PR_CHECK(size_t(buf.tellp()), size_t(index.m_length));

		p3d::File cmp = p3d::Read(buf);

		PR_CHECK(cmp.m_version                  , file.m_version);
		PR_CHECK(cmp.m_scene.m_materials.size() , file.m_scene.m_materials.size());
		PR_CHECK(cmp.m_scene.m_meshes.size()    , file.m_scene.m_meshes.size());
		for (size_t i = 0; i != file.m_scene.m_materials.size(); ++i)
		{
			auto& m0 = cmp.m_scene.m_materials[i];
			auto& m1 = file.m_scene.m_materials[i];
			PR_CHECK(pr::str::Equal(m0.m_id.str, m1.m_id.str), true);
			PR_CHECK(m0.m_diffuse == m1.m_diffuse, true);
			PR_CHECK(m0.m_tex_diffuse.size() == m1.m_tex_diffuse.size(), true);
			for (size_t j = 0; j != m1.m_tex_diffuse.size(); ++j)
			{
				auto& t0 = m0.m_tex_diffuse[j];
				auto& t1 = m1.m_tex_diffuse[j];
				PR_CHECK(t0.m_filepath , t1.m_filepath);
				PR_CHECK(t0.m_tiling   , t1.m_tiling);
			}
		}
		for (size_t i = 0; i != file.m_scene.m_meshes.size(); ++i)
		{
			auto& m0 = cmp.m_scene.m_meshes[i];
			auto& m1 = file.m_scene.m_meshes[i];
			PR_CHECK(m0.m_name          , m1.m_name);
			PR_CHECK(m0.m_verts.size()  , m1.m_verts.size());
			PR_CHECK(m0.m_idx16.size()  , m1.m_idx16.size());
			PR_CHECK(m0.m_idx32.size()  , m1.m_idx32.size());
			PR_CHECK(m0.m_nugget.size() , m1.m_nugget.size());
		}
		//*/
		//std::ifstream ifile("\\dump\\test2.3ds", std::ifstream::binary);
		//Read3DSMaterials(ifile, [](max_3ds::Material&&){});
		//Read3DSModels(ifile, [](max_3ds::Object&&){});
	}
}
#endif