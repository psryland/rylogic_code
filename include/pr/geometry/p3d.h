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
#include "pr/common/range.h"
#include "pr/common/scope.h"
#include "pr/common/compress.h"
#include "pr/maths/maths.h"
#include "pr/gfx/colour.h"
#include "pr/geometry/common.h"
#include "pr/geometry/utility.h"
#include "pr/container/span.h"
#include "pr/container/byte_data.h"

namespace pr::geometry::p3d
{
	using u8 = unsigned char;       static_assert(sizeof(u8) == 1, "1byte u8 expected");
	using u16 = unsigned short;     static_assert(sizeof(u16) == 2, "2byte u16 expected");
	using u32 = unsigned long;      static_assert(sizeof(u32) == 4, "4byte u32 expected");
	using u64 = unsigned long long; static_assert(sizeof(u64) == 8, "8byte u64 expected");
	struct ChunkHeader;
	struct ChunkIndex;
	static const u32 Version = 0x00001001;
	ChunkIndex const& NullChunk();

	#pragma region Chunk Ids
	#define PR_ENUM(x)\
	x(Null                  ,= 0x00000000)/* Null chunk                                                      */\
	x(CStr                  ,= 0x00000001)/* Null terminated ascii string                                    */\
	x(Main                  ,= 0x44335250)/* PR3D File type indicator                                        */\
	x(FileVersion           ,= 0x00000100)/* ├─ File Version                                                 */\
	x(Scene                 ,= 0x00001000)/* └─ Scene                                                        */\
	x(Materials             ,= 0x00002000)/*    ├─ Materials                                                 */\
	x(Material              ,= 0x00002100)/*    │  └─ Material                                               */\
	x(DiffuseColour         ,= 0x00002110)/*    │     ├─ Diffuse Colour                                      */\
	x(DiffuseTexture        ,= 0x00002120)/*    │     └─ Diffuse texture                                     */\
	x(TexFilepath           ,= 0x00002121)/*    │        ├─ Texture filepath                                 */\
	x(TexTiling             ,= 0x00002122)/*    │        └─ Texture tiling                                   */\
	x(Meshes                ,= 0x00003000)/*    └─ Meshes                                                    */\
	x(Mesh                  ,= 0x00003100)/*       └─ Mesh of lines,triangles,tetras                         */\
	x(MeshName              ,= 0x00003101)/*          ├─ Name (cstr)                                         */\
	x(MeshBBox              ,= 0x00003102)/*          ├─ Bounding box (BBox)                                 */\
	x(MeshTransform         ,= 0x00003103)/*          ├─ Mesh to Parent Transform (m4x4)                     */\
	x(MeshVertices          ,= 0x00003110)/*          ├─ Vertex list (u32 count, count * [Vert])             */\
	x(MeshVerticesV         ,= 0x00003111)/*          ├─ Vertex list compressed (u32 count, count * [Verts]) */\
	x(MeshIndices16         ,= 0x00003120)/*          ├─ Index list (u32 count, count * [u16 i]) deprecated  */\
	x(MeshIndices32         ,= 0x00003121)/*          ├─ Index list (u32 count, count * [u32 i]) deprecated  */\
	x(MeshIndices           ,= 0x00003125)/*          ├─ Index list (u32 count, u32 stride, count * [Idx i]) */\
	x(MeshIndicesV          ,= 0x00003126)/*          ├─ Index list (u32 count, u32 stride, count * [Idx i]) */\
	x(MeshNuggets           ,= 0x00003200)/*          └─ Nugget list (u32 count, count * [Nugget]) */
	PR_DEFINE_ENUM2(EChunkId, PR_ENUM);
	#undef PR_ENUM
	#pragma endregion
	static_assert(sizeof(EChunkId) == sizeof(u32), "Chunk Ids must be 4 bytes");

	// Flags
	enum class EFlags
	{
		None = 0,
		Compress = 1 << 0,
		_bitwise_operators_allowed,
	};

	// Chunk header (8-bytes)
	struct ChunkHeader
	{
		// Notes:
		//  - 'm_length' includes the size of the ChunkHeader

		EChunkId m_id;
		u32 m_length;

		ChunkHeader() = default;
		ChunkHeader(EChunkId id, size_t data_length)
			:m_id(id)
			,m_length(s_cast<u32>(sizeof(ChunkHeader) + data_length))
		{}
	};
	static_assert(sizeof(ChunkHeader) == 8, "Incorrect chunk header size");

	// Used to build an index of a p3d file
	struct ChunkIndex :ChunkHeader
	{
		// Notes:
		//  - data_length in these constructors should *not* include the ChunkHeader size
		using Cont = std::vector<ChunkIndex>;
		Cont m_chunks;

		ChunkIndex(EChunkId id, size_t data_length)
			:ChunkHeader(id, data_length)
			,m_chunks()
		{}
		ChunkIndex(EChunkId id, size_t data_length, std::initializer_list<ChunkIndex> chunks)
			:ChunkHeader(id, data_length)
			,m_chunks()
		{
			for (auto& c : chunks)
				add(c);
		}
		void add(ChunkIndex const& chunk)
		{
			m_chunks.push_back(chunk);
			m_length += chunk.m_length;
		}
		ChunkIndex const& operator[](EChunkId id) const
		{
			for (auto& c : m_chunks)
				if (c.m_id == id)
					return c;

			std::stringstream ss;
			ss << "Child chunk " << id << " not a member of chunk " << m_id;
			throw std::runtime_error(ss.str());
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
		operator v2() const
		{
			return v2(x, y);
		}
		Vec2& operator = (v2 const& rhs)
		{
			x = rhs.x;
			y = rhs.y;
			return *this;
		}
		friend bool operator == (Vec2 lhs, Vec2 rhs)
		{
			return lhs.x == rhs.x && lhs.y == rhs.y;
		}
		friend bool operator != (Vec2 lhs, Vec2 rhs)
		{
			return !(lhs == rhs);
		}
	};
	struct Vec4
	{
		#pragma warning (disable:4201)
		union {
		struct { float x,y,z,w; };
		struct { float r,g,b,a; };
		};
		#pragma warning (default:4201)
		operator v4() const
		{
			return v4(x, y, z, w);
		}
		operator Colour() const
		{
			return Colour(r, g, b, a);
		}
		Vec4& operator = (v4 const& rhs)
		{
			x = rhs.x;
			y = rhs.y;
			z = rhs.z;
			w = rhs.w;
			return *this;
		}
		Vec4& operator = (Colour const& rhs)
		{
			r = rhs.r;
			g = rhs.g;
			b = rhs.b;
			a = rhs.a;
			return *this;
		}
		friend bool operator == (Vec4 lhs, Vec4 rhs)
		{
			return
				lhs.x == rhs.x &&
				lhs.y == rhs.y &&
				lhs.z == rhs.z &&
				lhs.w == rhs.w;
		}
		friend bool operator != (Vec4 lhs, Vec4 rhs)
		{
			return !(lhs == rhs);
		}
	};
	struct BBox
	{
		Vec4 centre;
		Vec4 radius;
		BBox()
		{
			centre = v4Origin;
			radius = -v4One.w0();
		}
		BBox& operator = (pr::BBox const& rhs)
		{
			centre = rhs.m_centre;
			radius = rhs.m_radius;
			return *this;
		}
		operator pr::BBox() const
		{
			return pr::BBox(centre, radius);
		}
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
		friend u32 begin(Range r) 
		{
			return r.first;
		}
		friend u32 end(Range r)
		{
			return r.first + r.count;
		}
		friend bool operator == (Range lhs, Range rhs)
		{
			return lhs.first == rhs.first && lhs.count == rhs.count;
		}
		friend bool operator != (Range lhs, Range rhs)
		{
			return !(lhs == rhs);
		}
	};
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
		Str16& operator = (std::string const& s)
		{
			return *this = s.c_str();
		}
		operator std::string() const
		{
			return str;
		}
		operator c16 const& () const
		{
			return str;
		}
		friend bool operator == (Str16 const& lhs, Str16 const& rhs)
		{
			return strncmp(lhs.str, rhs.str, sizeof(Str16)) == 0;
		}
		friend bool operator != (Str16 const& lhs, Str16 const& rhs)
		{
			return !(lhs == rhs);
		}
	};
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
		Colour m_diffuse;

		// Diffuse textures
		std::vector<Texture> m_tex_diffuse;

		Material(std::string name = std::string(), Colour const& diff_colour = ColourWhite)
			:m_id(name.c_str())
			,m_diffuse(diff_colour)
			,m_tex_diffuse()
		{}
	};
	struct Mesh
	{
		// Container types
		using VCont = std::vector<Vert>;
		using ICont = struct : ByteData<4> { u32 m_stride; }; // 0 = variable, n = bytes per index
		using NCont = std::vector<Nugget>;

		// A name for the model
		std::string m_name;

		// The basic vertex data for the mesh
		VCont m_verts;

		// Index data for the mesh
		ICont m_idx;

		// The nuggets to divide the mesh into for each material
		NCont m_nugget;

		// Mesh bounding box
		BBox m_bbox;

		// Mesh to scene transform
		m4x4 m_o2p;

		Mesh(std::string name = std::string())
			:m_name(name)
			,m_verts()
			,m_idx()
			,m_nugget()
			,m_bbox()
			,m_o2p()
		{}
	};
	struct Scene
	{
		// Container types
		using MatCont = std::vector<Material>;
		using MeshCont = std::vector<Mesh>;
		MatCont m_materials;
		MeshCont m_meshes;

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
	inline Vec4 GetP(Vert const& vert) { return vert.pos; }
	inline Vec4 GetC(Vert const& vert) { return vert.col; }
	inline Vec4 GetN(Vert const& vert) { return vert.norm; }
	inline Vec2 GetT(Vert const& vert) { return vert.uv; }

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

	// Traits for a stream-like source
	template <typename Stream> struct traits
	{
		static u64 tellg(Stream& src)
		{
			return static_cast<u64>(src.tellg());
		}
		static u64 tellp(Stream& src)
		{
			return static_cast<u64>(src.tellp());
		}
		static bool seekg(Stream& src, u64 pos)
		{
			return static_cast<bool>(src.seekg(pos));
		}
		static bool seekp(Stream& src, u64 pos)
		{
			return static_cast<bool>(src.seekp(pos));
		}

		// Read an array
		template <typename TOut> static void read(Stream& src, TOut* out, size_t count)
		{
			if (src.read(char_ptr(out), count * sizeof(TOut))) return;
			throw std::runtime_error("partial read of input stream");
		}

		// Write an array
		template <typename TIn> static void write(Stream& dst, TIn const* in, size_t count)
		{
			if (dst.write(char_ptr(in), count * sizeof(TIn))) return;
			throw std::runtime_error("partial write of output stream");
		}
	};

	#pragma region Build Chunk Index

	// Notes:
	//  - The data sizes computed by indexing assume no compression

	// Build a chunk index from parts of a p3d model
	inline ChunkIndex Index(Texture const& tex)
	{
		ChunkIndex index(EChunkId::DiffuseTexture, 0U);
		index.add(ChunkIndex(EChunkId::TexFilepath, tex.m_filepath.size() + 1));
		index.add(ChunkIndex(EChunkId::TexTiling, sizeof(u32)));
		return index;
	}
	inline ChunkIndex Index(Material const& mat)
	{
		ChunkIndex index(EChunkId::Material, sizeof(Str16));
		index.add(ChunkIndex(EChunkId::DiffuseColour, sizeof(Vec4)));
		for (auto& tex : mat.m_tex_diffuse) index.add(Index(tex));
		return index;
	}
	inline ChunkIndex Index(Mesh const& mesh)
	{
		ChunkIndex index(EChunkId::Mesh, 0U);
		index.add(ChunkIndex(EChunkId::MeshName, mesh.m_name.size() + 1));
		index.add(ChunkIndex(EChunkId::MeshBBox, sizeof(BBox)));
		if (!mesh.m_verts.empty())
			index.add(ChunkIndex(EChunkId::MeshVertices, sizeof(u32) + mesh.m_verts.size() * sizeof(Vert))); // count, [verts]
		if (!mesh.m_idx.empty())
			index.add(ChunkIndex(EChunkId::MeshIndices, sizeof(u32) + sizeof(u32) + mesh.m_idx.size() * mesh.m_idx.m_stride)); // count, stride, [indices]
		if (!mesh.m_nugget.empty())
			index.add(ChunkIndex(EChunkId::MeshNuggets, sizeof(u32) + mesh.m_nugget.size() * sizeof(Nugget))); // count, [nuggets]
		return index;
	}
	inline ChunkIndex Index(Scene const& scene)
	{
		ChunkIndex index(EChunkId::Scene, 0U);
		if (!scene.m_materials.empty())
		{
			ChunkIndex mats(EChunkId::Materials, 0U);
			for (auto& mat : scene.m_materials) mats.add(Index(mat));
			index.add(mats);
		}
		if (!scene.m_meshes.empty())
		{
			ChunkIndex meshs(EChunkId::Meshes, 0U);
			for (auto& mesh : scene.m_meshes) meshs.add(Index(mesh));
			index.add(meshs);
		}
		return index;
	}
	inline ChunkIndex Index(File const& file)
	{
		ChunkIndex index(EChunkId::Main, 0U);
		index.add(ChunkIndex(EChunkId::FileVersion, sizeof(Version)));
		index.add(Index(file.m_scene));
		return index;
	}

	#pragma endregion

	// Generic chunk reading function.
	// 'src' should point to data after the chunk header.
	// 'len' is the remaining bytes in the parent chunk from 'src' to the end of the parent chunk
	// 'func' is called back with the chunk header and 'src' positioned at the start of the chunk data
	// 'func' should return true to stop reading (i.e. chunk found!).
	template <typename TSrc, typename Func>
	void ReadChunks(TSrc& src, u32 len, Func func, u32* len_out = nullptr)
	{
		while (len != 0)
		{
			auto start = traits<TSrc>::tellg(src);

			// Read the chunk header
			auto hdr = Read<ChunkHeader>(src);
			if (hdr.m_length <= len) len -= hdr.m_length;
			else throw std::runtime_error(pr::Fmt("invalid chunk found at offset 0x%X", start));
			u32 data_len = DataLength(hdr);

			// Parse the chunk
			if (func(hdr, src, data_len))
			{
				if (len_out) *len_out = len;
				return;
			}

			// Seek to the next chunk
			traits<TSrc>::seekg(src, start + hdr.m_length);
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
				if (id == EChunkId::Main) throw std::runtime_error("Source is not a p3d stream");
				return ChunkHeader();
			}
			len = DataLength(hdr);
		}
		return hdr;
	}

	#pragma region Read

	// Notes:
	//  - All of these Read functions assume 'src' points to the start
	//    of the chunk data of the corresponding chunk type.

	// Read an array
	template <typename TOut, typename TSrc> inline void Read(TSrc& src, TOut* out, size_t count)
	{
		traits<TSrc>::read(src, out, count);
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
					mat.m_tex_diffuse.emplace_back(ReadTexture(src, data_len));
					break;
				}
			}
			return false;
		});
		return std::move(mat);
	}

	// Fill a container of verts. 'src' is assumed to point to the start of EChunkId::MeshVertices chunk data
	template <typename TSrc> Mesh::VCont ReadMeshVertices(TSrc& src, u32 data_len, EChunkId id)
	{
		Mesh::VCont cont;
		switch (id)
		{
		case EChunkId::MeshVertices:
			{
				// Read the vertex count
				auto count = Read<u32>(src);
				data_len -= sizeof(u32);

				if (count * sizeof(Vert) != data_len)
					throw std::runtime_error(Fmt("Vertex list count is invalid. Vertex count is %d, data available for %d verts.", count, data_len/sizeof(Vert)));

				// Read the vertex data into memory
				cont.resize(count);
				Read(src, cont.data(), count);
				break;
			}
		case EChunkId::MeshVerticesV:
			{
				// Read the vertex count
				auto count = Read<u32>(src);
				data_len -= sizeof(u32);

				// Read the geometry data flags
				auto geom = static_cast<EGeom>(Read<u32>(src));
				data_len -= sizeof(u32);

				// Allocate space for 'count' vertices
				cont.resize(count);

				// Populate the vertex data
				if (AllSet(geom, EGeom::Vert))
				{
					if (count * sizeof(float) * 3 > data_len)
						throw std::runtime_error(Fmt("Mesh vertex data is invalid. Expected %d vertex positions, data available for %d verts.", count, data_len/(3*sizeof(float))));

					for (auto& v : cont)
					{
						v.pos.x = Read<float>(src);
						v.pos.y = Read<float>(src);
						v.pos.z = Read<float>(src);
						v.pos.w = 1.0f;
					}
					data_len -= count * sizeof(float) * 3;
				}
				else
				{
					for (auto& v : cont)
						v.pos = v4Origin;
				}
				
				// Populate the normals
				if (AllSet(geom, EGeom::Norm))
				{
					if (count * sizeof(u32) > data_len)
						throw std::runtime_error(Fmt("Mesh vertex data is invalid. Expected %d vertex normals, data available for %d normals.", count, data_len/sizeof(u32)));

					for (auto& v : cont)
						v.norm = Norm32bit::Decompress(Read<u32>(src));

					data_len -= count * sizeof(u32);
				}
				else
				{
					for (auto& v : cont)
						v.norm = v4Zero;
				}

				// Populate the vertex colours
				if (AllSet(geom, EGeom::Colr))
				{
					if (count * sizeof(Colour32) > data_len)
						throw std::runtime_error(Fmt("Mesh vertex data is invalid. Expected %d vertex colours, data available for %d colours.", count, data_len/sizeof(Colour32)));

					for (auto& v : cont)
					{
						auto col = Read<Colour32>(src);
						v.col.r = r_cp(col);
						v.col.g = g_cp(col);
						v.col.b = b_cp(col);
						v.col.a = a_cp(col);
					}
					data_len -= count * sizeof(Colour32);
				}
				else
				{
					for (auto& v : cont)
						v.col = v4One;
				}

				// Populate the texture coordinates
				if (AllSet(geom, EGeom::Tex0))
				{
					if (count * sizeof(Colour32) > data_len)
						throw std::runtime_error(Fmt("Mesh vertex data is invalid. Expected %d vertex texture coordinates, data available for %d coords.", count, data_len/(2*sizeof(float))));

					for (auto& v : cont)
					{
						v.uv.x = Read<float>(src);
						v.uv.y = Read<float>(src);
					}
					data_len -= count * sizeof(float) * 2;
				}
				else
				{
					for (auto& v : cont)
						v.uv = v2Zero;
				}

				break;
			}
		default:
			{
				throw std::runtime_error("Unsupported Mesh Vertex format");
			}
		}

		return std::move(cont);
	}

	// Fill a container of indices. 'src' is assumed to point to the start of EChunkId::MeshIndices8/16/32 chunk data
	template <typename TSrc> Mesh::ICont ReadMeshIndices(TSrc& src, u32 data_len, EChunkId id)
	{
		Mesh::ICont cont;
		switch (id)
		{
		case EChunkId::MeshIndices:
			{
				// Read the index count
				auto count = Read<u32>(src);
				data_len -= sizeof(u32);

				// Read the stride
				auto stride = Read<u32>(src);
				data_len -= sizeof(u32);

				if (count * stride != data_len)
					throw std::runtime_error(Fmt("Index list count is invalid. Index count is %d, data available for %d indices.", count, data_len / stride));

				// Read the index data into memory
				cont.resize(data_len);
				cont.m_stride = stride;
				Read(src, cont.data(), data_len);
				break;
			}
		case EChunkId::MeshIndicesV:
			{
				// Read the index count
				auto count = Read<u32>(src);
				data_len -= sizeof(u32);

				// Read the stride
				auto stride = Read<u32>(src);
				data_len -= sizeof(u32);

				// Create a temporary buffer for holding the compressed indices
				std::vector<u8> buf(data_len);
				Read(src, buf.data(), buf.size());

				int64_t prev = 0;
				for (u8 const* p = buf.data(), *pend = p + buf.size(); p != pend; ++p)
				{
					int s = 0;
					uint64_t zz = 0;
					for (; p != pend && (*p & 0x80); ++p, s += 7)
						zz |= static_cast<uint64_t>(*p & 0x7F) << s;
					if (p != pend)
						zz |= static_cast<uint64_t>(*p & 0x7F) << s;

					// ZigZag decode
					auto delta = static_cast<int64_t>((zz & 1) ? (zz >> 1) ^ -1 : (zz >> 1));

					// Get the index value from the delta
					auto idx = prev + delta;
					cont.push_back(reinterpret_cast<u8 const*>(&idx), stride);

					prev += delta;
				}

				if (cont.size() != count * stride)
					throw std::runtime_error(Fmt("Index list count is invalid. Index count is %d, %d indices provided.", count, static_cast<u32>(cont.size() / stride)));

				cont.m_stride = stride;
				break;
			}
		case EChunkId::MeshIndices16: // deprecated
			{
				using Idx = u16;
				size_t count = Read<u32>(src);
				data_len -= sizeof(u32);
				if (count * sizeof(Idx) != data_len)
					throw std::runtime_error(Fmt("Index list count is invalid. Index count is %d, data available for %d indices.", count, data_len / sizeof(Idx)));

				cont.resize<Idx>(count);
				cont.m_stride = sizeof(Idx);
				Read(src, cont.data<Idx>(), count);
				break;
			}
		case EChunkId::MeshIndices32: // deprecated
			{
				using Idx = u32;
				size_t count = Read<u32>(src);
				data_len -= sizeof(u32);
				if (count * sizeof(Idx) != data_len)
					throw std::runtime_error(Fmt("Index list count is invalid. Index count is %d, data available for %d indices.", count, data_len / sizeof(Idx)));

				cont.resize<Idx>(count);
				cont.m_stride = sizeof(Idx);
				Read(src, cont.data<Idx>(), count);
				break;
			}
		default:
			{
				throw std::runtime_error("Unsupported Mesh Index format");
			}
		}
		return std::move(cont);
	}

	// Fill a container of nuggets. 'src' is assumed to point to the start of EChunkId::MeshNuggets chunk data
	template <typename TSrc> Mesh::NCont ReadMeshNuggets(TSrc& src, u32 data_len)
	{
		Mesh::NCont cont;

		size_t count = Read<u32>(src);
		data_len -= sizeof(u32);

		if (count * sizeof(Nugget) != data_len)
			throw std::runtime_error(Fmt("Nugget list count is invalid. Nugget count is %d, data available for %d nuggets.", count, data_len/sizeof(Nugget)));

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
			case EChunkId::MeshTransform:
				{
					mesh.m_o2p = Read<m4x4>(src);
					break;
				}
			case EChunkId::MeshVertices:
			case EChunkId::MeshVerticesV:
				{
					mesh.m_verts = ReadMeshVertices(src, data_len, hdr.m_id);
					break;
				}
			case EChunkId::MeshIndices:
			case EChunkId::MeshIndicesV:
			case EChunkId::MeshIndices16:
			case EChunkId::MeshIndices32:
				{
					mesh.m_idx = ReadMeshIndices(src, data_len, hdr.m_id);
					break;
				}
			case EChunkId::MeshNuggets:
				{
					mesh.m_nugget = ReadMeshNuggets(src, data_len);
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
							scene.m_materials.emplace_back(ReadMaterial(src, data_len));
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
							scene.m_meshes.emplace_back(ReadMesh(src, data_len));
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
			throw std::runtime_error("Source is not a p3d stream");

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
		auto reset_stream = pr::CreateStateScope(
			[&]{ return traits<TSrc>::tellg(src); },
			[&](u64 start){ traits<TSrc>::seekg(src, start); });

		// Check that this is actually a P3D stream
		auto main = Read<ChunkHeader>(src);
		if (main.m_id != EChunkId::Main)
			throw std::runtime_error("Source is not a p3d stream");

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
			[&]{ return traits<TSrc>::tellg(src); },
			[&](u64 start){ traits<TSrc>::seekg(src, start); }
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

	// Notes:
	//  - Each Write function returns the size (in bytes) added to 'out'
	//  - To write out only part of a File, delete the parts in a temporary copy of the file.

	// Write 'hdr' at 'offset', preserving the current output position in 'src'
	template <typename TOut> void UpdateHeader(TOut& out, u64 offset, ChunkHeader hdr)
	{
		auto pos = traits<TOut>::tellp(out);
		traits<TOut>::seekp(out, offset);
		Write<ChunkHeader>(out, hdr);
		traits<TOut>::seekp(out, pos);
	}

	// Write an array
	template <typename TIn, typename TOut> inline u32 Write(TOut& out, TIn const* in, size_t count)
	{
		traits<TOut>::write(out, in, count);
		return s_cast<u32>(count * sizeof(*in));
	}

	// Write a single type
	template <typename TIn, typename TOut> inline u32 Write(TOut& out, TIn const& in)
	{
		return Write(out, &in, 1);
	}

	// Write a null terminated string to 'out'
	template <typename TOut> u32 WriteCStr(TOut& out, EChunkId chunk_id, std::string const& str)
	{
		ChunkHeader hdr(chunk_id, str.size() + 1);
		Write<ChunkHeader>(out, hdr);
		Write(out, str.c_str(), DataLength(hdr));
		return hdr.m_length;
	}

	// Write a texture tiling chunk to 'out'
	template <typename TOut> u32 WriteTexTiling(TOut& out, u32 tiling)
	{
		ChunkHeader hdr(EChunkId::TexTiling, sizeof(u32));
		Write<ChunkHeader>(out, hdr);
		Write<u32>(out, tiling);
		return hdr.m_length;
	}

	// Write a texture to 'out'
	template <typename TOut> u32 WriteTexture(TOut& out, Texture const& tex)
	{
		auto offset = traits<TOut>::tellp(out);

		// Texture chunk header
		ChunkHeader hdr(EChunkId::DiffuseTexture, 0U);
		Write<ChunkHeader>(out, hdr);

		// Texture filepath
		if (!tex.m_filepath.empty())
			hdr.m_length += WriteCStr(out, EChunkId::TexFilepath, tex.m_filepath);

		// Texture tiling
		hdr.m_length += WriteTexTiling(out, tex.m_tiling);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write a diffuse colour chunk to 'out'
	template <typename TOut> u32 WriteDiffuseColour(TOut& out, pr::Colour const& colour)
	{
		ChunkHeader hdr(EChunkId::DiffuseColour, sizeof(Vec4));
		Write<ChunkHeader>(out, hdr);
		Write<Vec4>(out, {colour.r, colour.g, colour.b, colour.a});
		return hdr.m_length;
	}

	// Write a material to 'out'
	template <typename TOut> u32 WriteMaterial(TOut& out, Material const& mat)
	{
		auto offset = traits<TOut>::tellp(out);

		// Material chunk header
		ChunkHeader hdr(EChunkId::Material, 0U);
		Write<ChunkHeader>(out, hdr);

		// Material name
		hdr.m_length += Write(out, mat.m_id.str, sizeof(Str16));

		// Diffuse colour
		hdr.m_length += WriteDiffuseColour(out, mat.m_diffuse);

		// Textures
		for (auto& tex : mat.m_tex_diffuse)
			hdr.m_length += WriteTexture(out, tex);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write a collection of materials to 'out'
	template <typename TOut> u32 WriteMaterials(TOut& out, std::span<Material const> mats)
	{
		auto offset = traits<TOut>::tellp(out);

		// Materials chunk header
		ChunkHeader hdr(EChunkId::Materials, 0U);
		Write<ChunkHeader>(out, hdr);

		// Material data
		for (auto& mat : mats)
			hdr.m_length += WriteMaterial(out, mat);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write a bounding box to 'out'
	template <typename TOut> u32 WriteMeshBBox(TOut& out, BBox const& bbox)
	{
		if (bbox.radius.x < 0 ||
			bbox.radius.y < 0 ||
			bbox.radius.z < 0)
			throw std::runtime_error("Writing an invalid bounding box into p3d");

		ChunkHeader hdr(EChunkId::MeshBBox, sizeof(BBox));
		Write<ChunkHeader>(out, hdr);
		Write<BBox>(out, bbox);
		return hdr.m_length;
	}

	// Write a mesh to parent transform to 'out'
	template <typename TOut> u32 WriteMeshTransform(TOut& out, m4x4 const& o2p)
	{
		if (o2p == m4x4Identity)
			return 0;

		ChunkHeader hdr(EChunkId::MeshTransform, sizeof(m4x4));
		Write<ChunkHeader>(out, hdr);
		Write<m4x4>(out, o2p);
		return hdr.m_length;
	}

	// Write vertices to 'out'
	template <typename TOut> u32 WriteVertices(TOut& out, std::span<Vert const> verts, EFlags flags, EGeom geom)
	{
		auto offset = traits<TOut>::tellp(out);

		ChunkHeader hdr(EChunkId::MeshVertices, 0U);
		Write<ChunkHeader>(out, hdr);

		// Vertex count
		hdr.m_length += Write<u32>(out, s_cast<u32>(verts.size()));

		// Vertex data
		if (AllSet(flags, EFlags::Compress))
		{
			ByteData<> buf;
			buf.reserve(verts.size() * sizeof(float) * 3);

			// Record what geometry data is added
			buf.push_back(static_cast<u32>(geom));

			if (AllSet(geom, EGeom::Vert))
			{
				for (auto& v : verts)
				{
					buf.push_back(v.pos.x);
					buf.push_back(v.pos.y);
					buf.push_back(v.pos.z);
				}
			}
			if (AllSet(geom, EGeom::Norm))
			{
				for (auto& v : verts)
					buf.push_back(Norm32bit::Compress(v.norm));
			}
			if (AllSet(geom, EGeom::Colr))
			{
				for (auto& v : verts)
					buf.push_back(Colour32(v.col.r, v.col.g, v.col.b, v.col.a));
			}
			if (AllSet(geom, EGeom::Tex0))
			{
				for (auto& v : verts)
				{
					buf.push_back(v.uv.x);
					buf.push_back(v.uv.y);
				}
			}

			hdr.m_id = EChunkId::MeshVerticesV;
			hdr.m_length += Write<u8>(out, buf.data(), buf.size());
		}
		else
		{
			hdr.m_id = EChunkId::MeshVertices;
			hdr.m_length += Write<Vert>(out, verts.data(), verts.size());
		}

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write indices to 'out'
	template <typename TOut, typename Idx> u32 WriteIndices(TOut& out, std::span<Idx const> indices, EFlags flags)
	{
		auto offset = traits<TOut>::tellp(out);

		// Indices chunk header
		ChunkHeader hdr(EChunkId::MeshIndices, 0U);
		Write<ChunkHeader>(out, hdr);

		// Index count
		hdr.m_length += Write<u32>(out, s_cast<u32>(indices.size()));

		// Index stride
		hdr.m_length += Write<u32>(out, s_cast<u32>(sizeof(Idx)));

		// Compress the indices using variable length ints
		if (AllSet(flags, EFlags::Compress))
		{
			constexpr int BitWidth = sizeof(Idx) * 8;

			// Preallocate a buffer to hold the compressed indices
			std::vector<u8> buf;
			buf.reserve(indices.size() * 3 / 2);

			// Write the indices as zig-zag encoded variable length ints (like protobuf)
			int64_t prev = 0;
			for (auto& idx : indices)
			{
				// Get the delta from the previous index
				auto delta = idx - prev;

				// ZigZag encode to prevent negative 2s compliment numbers using lots of space
				auto zz = static_cast<uint64_t>((delta << 1) ^ (delta >> (BitWidth - 1)));

				// Variable length int encode
				for (; zz > 127; zz >>= 7)
					buf.push_back(s_cast<u8>(0x80 | (zz & 0x7F)));
				buf.push_back(s_cast<u8>(zz));

				// Update the new previous value
				prev += delta;
			}

			// Write the header, stride, count, and compressed index data
			hdr.m_id = EChunkId::MeshIndicesV;
			hdr.m_length += Write<u8>(out, buf.data(), buf.size());
		}
		else
		{
			// Write the indices as a contiguous block
			hdr.m_id = EChunkId::MeshIndices;
			hdr.m_length += Write<Idx>(out, indices.data(), indices.size());
		}

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write nuggets to 'out'
	template <typename TOut> u32 WriteNuggets(TOut& out, std::span<Nugget const> nuggets, EFlags flags)
	{
		auto offset = traits<TOut>::tellp(out);

		ChunkHeader hdr(EChunkId::MeshNuggets, 0U);
		Write<ChunkHeader>(out, hdr);

		// Nugget count
		hdr.m_length += Write<u32>(out, s_cast<u32>(nuggets.size()));

		// Nugget data
		if (int(flags & EFlags::Compress) != 0)
		{
			// todo
			hdr.m_length += Write<Nugget>(out, nuggets.data(), nuggets.size());
		}
		else
		{
			hdr.m_length += Write<Nugget>(out, nuggets.data(), nuggets.size());
		}

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write a mesh to 'out'
	template <typename TOut> u32 WriteMesh(TOut& out, Mesh const& mesh, EFlags flags)
	{
		auto offset = traits<TOut>::tellp(out);

		// Determine the union of geometry from the nuggets
		auto geom = EGeom::Vert;
		for (auto const& nug : mesh.m_nugget)
			geom |= nug.m_geom;

		// TriMesh chunk header
		ChunkHeader hdr(EChunkId::Mesh, 0U);
		Write<ChunkHeader>(out, hdr);

		// Mesh name
		hdr.m_length += WriteCStr(out, EChunkId::MeshName, mesh.m_name);

		// Mesh bounding box
		hdr.m_length += WriteMeshBBox(out, mesh.m_bbox);

		// Mesh to parent transform
		hdr.m_length += WriteMeshTransform(out, mesh.m_o2p);

		// Mesh vertex data
		hdr.m_length += WriteVertices(out, mesh.m_verts, flags, geom);

		// Mesh index data
		switch (mesh.m_idx.m_stride)
		{
		default: throw std::runtime_error(Fmt("Index stride value %d is not supported", mesh.m_idx.m_stride));
		case 1: hdr.m_length += WriteIndices(out, mesh.m_idx.span<u8>(), flags); break;
		case 2: hdr.m_length += WriteIndices(out, mesh.m_idx.span<u16>(), flags); break;
		case 4: hdr.m_length += WriteIndices(out, mesh.m_idx.span<u32>(), flags); break;
		}

		// Mesh nugget data
		hdr.m_length += WriteNuggets(out, mesh.m_nugget, flags);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write a collection of meshes to 'out'
	template <typename TOut> u32 WriteMeshes(TOut& out, std::span<Mesh const> meshs, EFlags flags)
	{
		auto offset = traits<TOut>::tellp(out);

		// Meshes chunk header
		ChunkHeader hdr(EChunkId::Meshes, 0U);
		Write<ChunkHeader>(out, hdr);

		// Mesh data
		for (auto& mesh : meshs)
			hdr.m_length += WriteMesh(out, mesh, flags);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write a scene to 'out'
	template <typename TOut> u32 WriteScene(TOut& out, Scene const& scene, EFlags flags)
	{
		auto offset = traits<TOut>::tellp(out);

		ChunkHeader hdr(EChunkId::Scene, 0U);
		Write<ChunkHeader>(out, hdr);

		// Scene materials
		hdr.m_length += WriteMaterials(out, scene.m_materials);

		// Scene meshes
		hdr.m_length += WriteMeshes(out, scene.m_meshes, flags);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write the file version chunk
	template <typename TOut> u32 WriteVersion(TOut& out, u32 version)
	{
		ChunkHeader hdr(EChunkId::FileVersion, sizeof(u32));
		Write<ChunkHeader>(out, hdr);
		Write<u32>(out, version);
		return hdr.m_length;
	}

	// Write the p3d file to an ostream-like output.
	template <typename TOut> u32 Write(TOut& out, File const& file, EFlags flags = EFlags::None)
	{
		// Notes:
		//  - Cannot use forward iteration only because some chunk sizes are not known ahead of time.
		//  - The chunk sizes in 'index' are ignored/overwritten, compressed chunks will have smaller sizes.

		auto offset = traits<TOut>::tellp(out);

		// Write a proxy file chunk header. The length will be filled in at the end once known.
		ChunkHeader hdr(EChunkId::Main, 0U);
		Write<ChunkHeader>(out, hdr);

		// Write the file version
		hdr.m_length += WriteVersion(out, file.m_version);

		// Write the scene
		hdr.m_length += WriteScene(out, file.m_scene, flags);

		UpdateHeader(out, offset, hdr);
		return hdr.m_length;
	}

	// Write the p3d file as code
	template <typename TOut> void WriteAsCode(TOut& out, File const& file, char const* indent = "")
	{
		for (auto& mesh : file.m_scene.m_meshes)
		{
			switch (mesh.m_idx.m_stride)
			{
			case 1: pr::geometry::GenerateModelCode(mesh.m_name, mesh.m_verts.size(), mesh.m_idx.size<u8>(), mesh.m_verts.begin(), mesh.m_idx.begin<u8>(), out, indent); break;
			case 2: pr::geometry::GenerateModelCode(mesh.m_name, mesh.m_verts.size(), mesh.m_idx.size<u16>(), mesh.m_verts.begin(), mesh.m_idx.begin<u16>(), out, indent); break;
			case 4: pr::geometry::GenerateModelCode(mesh.m_name, mesh.m_verts.size(), mesh.m_idx.size<u32>(), mesh.m_verts.begin(), mesh.m_idx.begin<u32>(), out, indent); break;
			default: throw std::runtime_error(Fmt("Index stride value %d is not supported", mesh.m_idx.m_stride));
			}

			out << indent << "#pragma region BoundingBox\n";
			out << indent << "static pr::BBox const bbox = {";
			out << pr::Fmt("{%+ff, %+ff, %+ff, 1.0f}, "   , mesh.m_bbox.centre.x, mesh.m_bbox.centre.y, mesh.m_bbox.centre.z);
			out << pr::Fmt("{%+ff, %+ff, %+ff, 0.0f}};\n" , mesh.m_bbox.radius.x, mesh.m_bbox.radius.y, mesh.m_bbox.radius.z);
			out << indent << "#pragma endregion\n";
		}
	}

	// Write the p3d file as ldr script
	template <typename TOut> void WriteAsScript(TOut& out, File const& file, char const* indent = "")
	{
		out << indent << "*Group {\n";
		for (auto& mesh : file.m_scene.m_meshes)
		{
			if (mesh.m_nugget.empty())
				continue;

			out << indent << "\t*Mesh " << mesh.m_name << " {\n";

			// Verts
			out << indent << "\t\t*Verts {\n";
			for (auto& vert : mesh.m_verts)
				out << indent << "\t\t\t" << vert.pos.x << " " << vert.pos.y << " " << vert.pos.z << "\n";
			out << indent << "\t\t}\n";

			// Normals
			out << indent << "\t\t*Normals {\n";
			for (auto& vert : mesh.m_verts)
				out << indent << "\t\t\t" << vert.norm.x << " " << vert.norm.y << " " << vert.norm.z << "\n";
			out << indent << "\t\t}\n";

			// Colours
			out << indent << "\t\t*Colours {\n";
			for (auto& vert : mesh.m_verts)
				out << indent << "\t\t\t" << Fmt("%8.8X", Colour32(vert.col.r, vert.col.g, vert.col.b, vert.col.a).argb) << "\n";
			out << indent << "\t\t}\n";

			// Faces
			auto WriteFaces = [&](p3d::Mesh const& mesh, auto* indices)
			{
				for (auto& nug : mesh.m_nugget)
				{
					switch (nug.m_topo)
					{
					case EPrim::LineList:  out << indent << "\t\t*LineList {\n"; break;
					case EPrim::LineStrip: out << indent << "\t\t*LineStrip {\n"; break;
					case EPrim::TriList:   out << indent << "\t\t*TriList {\n"; break;
					case EPrim::TriStrip:  out << indent << "\t\t*TriStrip {\n"; break;
					default: throw std::runtime_error("Unsupported topology type");
					}

					int icount = 0;
					out << indent << "\t\t\t";
					for (u32 i = begin(nug.m_irange), iend = end(nug.m_irange); i != iend; ++i)
					{
						out << indices[i] << " ";
						if (++icount != 16) continue;
						out << "\n" << indent << "\t\t\t";
						icount = 0;
					}
					if (icount != 0)
						out << "\n";

					// End faces
					out << indent << "\t\t}\n";
				}
			};
			switch (mesh.m_idx.m_stride)
			{
			case 1: WriteFaces(mesh, mesh.m_idx.data<u8>()); break;
			case 2: WriteFaces(mesh, mesh.m_idx.data<u16>()); break;
			case 4: WriteFaces(mesh, mesh.m_idx.data<u32>()); break;
			default: throw std::runtime_error(Fmt("Index stride value %d is not supported", mesh.m_idx.m_stride));
			}

			// End mesh
			out << indent << "\t}\n";
		}

		// End group
		out << indent << "}\n";
	}
	
	#pragma endregion
}
namespace pr::maths
{
//	template <> struct is_vec<pr::geometry::p3d::Vec4> :std::true_type
//	{
//		using elem_type = float;
//		using cp_type = float;
//		static int const dim = 4;
//	};
//	template <> struct is_vec<pr::geometry::p3d::Vec2> :std::true_type
//	{
//		using elem_type = float;
//		using cp_type = float;
//		static int const dim = 2;
//	};
}

#if PR_UNITTESTS
#include <fstream>
#include "pr/common/unittests.h"
#include "pr/view3d/renderer.h"
namespace pr::geometry
{
	PRUnitTest(P3dTests)
	{
		//{
		//	std::ifstream ifile("S:\\software\\PC\\vrex\\res\\lod1\\lh_foot.p3d", std::ios::binary);
		//	std::ofstream ofile("P:\\dump\\lh_foot.p3d", std::ios::binary);
		//	auto m = p3d::Read(ifile);
		//	p3d::Write(ofile, m, p3d::EFlags::Compress);
		//}

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

		p3d::Vert vert = {};
		vert.pos = v4(1,2,3,1);
		vert.norm = v4(0,0,1,0);
		vert.col = v4(1,1,1,1);
		vert.uv = v2(1,1);

		p3d::Mesh mesh;
		mesh.m_name = "mesh";
		mesh.m_bbox = pr::BBox(v4Origin, v4(1,2,3,0));
		mesh.m_verts.push_back(vert);
		mesh.m_verts.push_back(vert);
		mesh.m_verts.push_back(vert);
		mesh.m_verts.push_back(vert);
		mesh.m_idx.m_stride = sizeof(uint16_t);
		mesh.m_idx.push_back<uint16_t>(0);
		mesh.m_idx.push_back<uint16_t>(1);
		mesh.m_idx.push_back<uint16_t>(2);
		mesh.m_idx.push_back<uint16_t>(0);
		mesh.m_idx.push_back<uint16_t>(2);
		mesh.m_idx.push_back<uint16_t>(3);
		mesh.m_nugget.push_back(nug);
		file.m_scene.m_meshes.push_back(mesh);

		//std::ofstream buf("\\dump\\test.p3d", std::ofstream:: binary);
		//p3d::Write(buf, file, p3d::EFlags::Compress);

		//*
		std::stringstream buf(std::ios_base::in|std::ios_base::out|std::ios_base::binary);
		auto size = p3d::Write(buf, file, p3d::EFlags::Compress);
		PR_CHECK(size_t(buf.tellp()), size_t(size));

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
			PR_CHECK(m0.m_idx.size()    , m1.m_idx.size());
			PR_CHECK(m0.m_idx.m_stride  , m1.m_idx.m_stride);
			PR_CHECK(m0.m_idx.m_stride == sizeof(uint16_t), true);
			PR_CHECK(m0.m_nugget.size() , m1.m_nugget.size());
			for (size_t j = 0; j != m1.m_verts.size(); ++j)
			{
				auto& v0 = m0.m_verts[j];
				auto& v1 = m1.m_verts[j];
				PR_CHECK(v0.pos == v1.pos, true);
				PR_CHECK(v0.norm == v1.norm, true);
				PR_CHECK(v0.col == v1.col, true);
				PR_CHECK(v0.uv == v1.uv, true);
				PR_CHECK(v0.pad == v1.pad, true);
			}
			for (size_t j = 0; j != m1.m_idx.size<uint16_t>(); ++j)
			{
				auto& i0 = m0.m_idx.at<uint16_t>(j);
				auto& i1 = m1.m_idx.at<uint16_t>(j);
				PR_CHECK(i0 == i1, true);
			}
			for (size_t j = 0; j != m1.m_nugget.size(); ++j)
			{
				auto& n0 = m0.m_nugget[j];
				auto& n1 = m1.m_nugget[j];
				PR_CHECK(n0.m_geom == n1.m_geom, true);
				PR_CHECK(n0.m_topo == n1.m_topo, true);
				PR_CHECK(n0.m_vrange == n1.m_vrange, true);
				PR_CHECK(n0.m_irange == n1.m_irange, true);
				PR_CHECK(n0.m_mat == n1.m_mat, true);
			}
		}
		//*/
		//std::ifstream ifile("\\dump\\test2.3ds", std::ifstream::binary);
		//Read3DSMaterials(ifile, [](max_3ds::Material&&){});
		//Read3DSModels(ifile, [](max_3ds::Object&&){});
	}
}
#endif