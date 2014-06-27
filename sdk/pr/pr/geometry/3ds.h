//********************************
// 3DS file data
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include <iostream>
#include <memory>
#include <exception>
#include <unordered_map>
#include <cassert>
#include "pr/common/cast.h"
#include "pr/macros/enum.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace geometry
	{
		namespace impl
		{
			inline unsigned int tell_pos(std::istream& src)                   { return static_cast<unsigned int>(src.tellg()); }
			inline bool         seek_abs(std::istream& src, unsigned int pos) { return static_cast<bool>(src.seekg(pos)); }
			inline bool         seek_rel(std::istream& src, unsigned int ofs) { return static_cast<bool>(src.seekg(ofs, src.cur)); }

			// Read a type
			template <typename TOut> TOut read(std::istream& src)
			{
				TOut out;
				if (src.read(char_ptr(&out), sizeof(out))) return out;
				throw std::exception("incomplete chunk");
			}
			
			// Peek at a type
			template <typename TOut> TOut peek(std::istream& src)
			{
				TOut out = read<TOut>(src);
				if (src.seekg(int(0 - sizeof(TOut)), src.cur)) return out;
				throw std::exception("seek failed on input stream");
			}
		}

		struct Max3DS
		{
			#pragma region Chunk Ids
			#define PR_ENUM(x)                           /*
				*/x(Null                      ,= 0x0000) /* 0x0000 // Null chunk id
				*/x(Main                      ,= 0x4d4d) /* 0x4D4D // Main Chunk
				*/x(M3DVersion                ,= 0x0002) /* ├─ 0x0002 // M3D Version
				*/x(M3DEditor                 ,= 0x3D3D) /* ├─ 0x3D3D // 3D Editor Chunk
				*/x(MeshVersion               ,= 0x3D3E) /* │  ├─ 0x3D3E // Mesh Version
				*/x(ObjectBlock               ,= 0x4000) /* │  ├─ 0x4000 // Object Block
				*/x(TriangularMesh            ,= 0x4100) /* │  │  ├─ 0x4100 // Triangular Mesh
				*/x(VerticesList              ,= 0x4110) /* │  │  │  ├─ 0x4110 // Vertices List
				*/x(FacesDescription          ,= 0x4120) /* │  │  │  ├─ 0x4120 // Faces Description
				*/x(FacesMaterial             ,= 0x4130) /* │  │  │  │  ├─ 0x4130 // Faces Material
				*/x(SmoothingGroupList        ,= 0x4150) /* │  │  │  │  └─ 0x4150 // Smoothing Group List
				*/x(MappingCoordinatesList    ,= 0x4140) /* │  │  │  ├─ 0x4140 // Mapping Coordinates List
				*/x(LocalCoordinatesSystem    ,= 0x4160) /* │  │  │  └─ 0x4160 // Local Coordinates System
				*/x(Light                     ,= 0x4600) /* │  │  ├─ 0x4600 // Light
				*/x(Spotlight                 ,= 0x4610) /* │  │  │  └─ 0x4610 // Spotlight
				*/x(Camera                    ,= 0x4700) /* │  │  └─ 0x4700 // Camera
				*/x(MaterialBlock             ,= 0xAFFF) /* │  └─ 0xAFFF // Material Block
				*/x(MaterialName              ,= 0xA000) /* │     ├─ 0xA000 // Material Name
				*/x(AmbientColor              ,= 0xA010) /* │     ├─ 0xA010 // Ambient Color
				*/x(DiffuseColor              ,= 0xA020) /* │     ├─ 0xA020 // Diffuse Color
				*/x(SpecularColor             ,= 0xA030) /* │     ├─ 0xA030 // Specular Color
				*/x(TextureMap1               ,= 0xA200) /* │     ├─ 0xA200 // Texture Map 1
				*/x(BumpMap                   ,= 0xA230) /* │     ├─ 0xA230 // Bump Map
				*/x(ReflectionMap             ,= 0xA220) /* │     └─ 0xA220 // Reflection Map
				*/x(MappingFilename           ,= 0xA300) /* │        ├─ 0xA300 // Mapping Filename    (Sub chunks for each map type)
				*/x(MappingParameters         ,= 0xA351) /* │        └─ 0xA351 // Mapping Parameters
				*/x(KeyframerChunk            ,= 0xB000) /* └─ 0xB000 // Keyframer Chunk
				*/x(MeshInformationBlock      ,= 0xB002) /*    ├─ 0xB002 // Mesh Information Block
				*/x(SpotLightInformationBlock ,= 0xB007) /*    ├─ 0xB007 // Spot Light Information Block
				*/x(Frames                    ,= 0xB008) /*    └─ 0xB008 // Frames (Start and End)
				*/x(ObjectName                ,= 0xB010) /* 	  ├─ 0xB010 // Object Name
				*/x(ObjectPivotPoint          ,= 0xB013) /* 	  ├─ 0xB013 // Object Pivot Point
				*/x(PositionTrack             ,= 0xB020) /* 	  ├─ 0xB020 // Position Track
				*/x(RotationTrack             ,= 0xB021) /* 	  ├─ 0xB021 // Rotation Track
				*/x(ScaleTrack                ,= 0xB022) /* 	  ├─ 0xB022 // Scale Track
				*/x(HierarchyPosition         ,= 0xB030) /* 	  └─ 0xB030 // Hierarchy Position */
			PR_DEFINE_ENUM2(EChunkId, PR_ENUM);
			#undef PR_ENUM
			#pragma endregion

			typedef unsigned short ushort;
			typedef unsigned int ulong;
			static_assert(sizeof(ushort) == 2, "3DS format specifies short as 2 bytes");
			static_assert(sizeof(ulong) == 4, "3DS format specifies long as 4 bytes");
			static unsigned int const ChunkHeaderSize = 6;

			struct Chunk;
			typedef std::vector<Chunk*> ChunkCont;

			// Base class for 3ds file chunks
			struct Chunk
			{
				ulong     m_offset; // byte offset to the start of this chunk
				EChunkId  m_id;     // Chunk id
				ulong     m_length; // The length of the chunk (including the chunk header)
				ChunkCont m_child;  // Nested chunks

				template <typename TSrc> Chunk(TSrc& src, EChunkId id)
					:m_offset(impl::tell_pos(src))
					,m_id    (static_cast<EChunkId>(impl::read<ushort>(src)))
					,m_length(impl::read<ulong>(src))
					,m_child ()
				{
					if (id != EChunkId::Null && id != m_id)
						throw std::exception("chunk type mismatch");
				}
				virtual ~Chunk()
				{
					for (auto c : m_child)
						delete c;
				}

			protected:

				template <typename TSrc> void seek_start(TSrc& src)
				{
					if (!impl::seek_abs(src, m_offset))
						throw std::exception("seek chunk start failed on input stream");
				}
				template <typename TSrc> void seek_end(TSrc& src)
				{
					if (!impl::seek_abs(src, m_offset + m_length))
						throw std::exception("seek chunk end failed on input stream");
				}
				template <typename TSrc> std::string read_cstr(TSrc& src)
				{
					std::string str; char c;
					int max_length = int(m_offset + m_length - impl::tell_pos(src));
					for (int i = 0; i != max_length && (c = impl::read<char>(src)) != 0; ++i)
						str.push_back(c);
					return str;
				}
				template <typename TSrc> void read_nested(TSrc& src)
				{
					while (impl::tell_pos(src) - m_offset != m_length)
					{
						auto id = static_cast<EChunkId>(impl::peek<ushort>(src));
						switch (id)
						{
						default:                               add(std::make_unique<Unknown                >(src)); break;
						case EChunkId::M3DVersion:             add(std::make_unique<M3DVersion             >(src)); break;
						case EChunkId::M3DEditor:              add(std::make_unique<M3DEditor              >(src)); break;
						case EChunkId::MeshVersion:            add(std::make_unique<MeshVersion            >(src)); break;
						case EChunkId::ObjectBlock:            add(std::make_unique<ObjectBlock            >(src)); break;
						case EChunkId::MaterialBlock:          add(std::make_unique<MaterialBlock          >(src)); break;
						case EChunkId::TriangularMesh:         add(std::make_unique<TriangularMesh         >(src)); break;
						case EChunkId::VerticesList:           add(std::make_unique<VerticesList           >(src)); break;
						case EChunkId::FacesDescription:       add(std::make_unique<FacesDescription       >(src)); break;
						case EChunkId::MappingCoordinatesList: add(std::make_unique<MappingCoordinatesList >(src)); break;
						case EChunkId::MaterialName:           add(std::make_unique<MaterialName           >(src)); break;
						}
					}
				}

				void add(std::unique_ptr<Chunk> chunk)
				{
					m_child.push_back(chunk.get());
					chunk.release();
				}
			};

			// Chunks
			struct Unknown :Chunk
			{
				template <typename TSrc> Unknown(TSrc& src) :Chunk(src, EChunkId::Null)
				{
					seek_end(src);
				}
			};
			struct Main :Chunk
			{
				template <typename TSrc> Main(TSrc& src) :Chunk(src, EChunkId::Main)
				{
					read_nested(src);
				}
			};
			struct M3DVersion :Chunk
			{
				ushort m_version;
				template <typename TSrc> M3DVersion(TSrc& src) :Chunk(src, EChunkId::M3DVersion)
					,m_version(impl::read<ushort>(src))
				{
					seek_end(src);
				}
			};
			struct M3DEditor :Chunk
			{
				template <typename TSrc> M3DEditor(TSrc& src) :Chunk(src, EChunkId::M3DEditor)
				{
					read_nested(src);
				}
			};
			struct MeshVersion :Chunk
			{
				ushort m_version;
				template <typename TSrc> MeshVersion(TSrc& src) :Chunk(src, EChunkId::MeshVersion)
					,m_version(impl::read<ushort>(src))
				{
					seek_end(src);
				}
			};
			struct ObjectBlock :Chunk
			{
				std::string m_name;
				template <typename TSrc> ObjectBlock(TSrc& src) :Chunk(src, EChunkId::ObjectBlock) ,m_name(read_cstr(src))
				{
					read_nested(src);
				}
			};
			struct MaterialBlock :Chunk
			{
				template <typename TSrc> MaterialBlock(TSrc& src) :Chunk(src, EChunkId::MaterialBlock)
				{
					read_nested(src);
				}
			};
			struct TriangularMesh :Chunk
			{
				template <typename TSrc> TriangularMesh(TSrc& src) :Chunk(src, EChunkId::TriangularMesh)
				{
					read_nested(src);
				}
			};
			struct VerticesList :Chunk
			{
				std::vector<pr::v4> m_verts;
				template <typename TSrc> VerticesList(TSrc& src) :Chunk(src, EChunkId::VerticesList)
				{
					int count = impl::read<ushort>(src);
					m_verts.reserve(count);
					for (int i = 0; i != count; ++i)
					{
						auto x = impl::read<float>(src);
						auto y = impl::read<float>(src);
						auto z = impl::read<float>(src);
						m_verts.push_back(pr::v4::make(x,y,z,1.0f));
					}
					read_nested(src);
				}
			};
			struct FacesDescription :Chunk
			{
				std::vector<ushort> m_faces; // three indices per face
				std::vector<ushort> m_flags; // one value per face (3 m_faces indices)
				template <typename TSrc> FacesDescription(TSrc& src) :Chunk(src, EChunkId::FacesDescription)
				{
					int count = impl::read<ushort>(src);
					m_faces.reserve(count * 3);
					m_flags.reserve(count);
					for (int i = 0; i != count; ++i)
					{
						auto i0    = impl::read<ushort>(src);
						auto i1    = impl::read<ushort>(src);
						auto i2    = impl::read<ushort>(src);
						auto flags = impl::read<ushort>(src);
						m_faces.push_back(i0);
						m_faces.push_back(i1);
						m_faces.push_back(i2);
						m_flags.push_back(flags);
					}
					read_nested(src);
				}
			};
			struct MappingCoordinatesList :Chunk
			{
				std::vector<pr::v2> m_uv;
				template <typename TSrc> MappingCoordinatesList(TSrc& src) :Chunk(src, EChunkId::MappingCoordinatesList)
				{
					int count = impl::read<ushort>(src);
					m_uv.reserve(count);
					for (int i = 0; i != count; ++i)
					{
						auto u = impl::read<float>(src);
						auto v = impl::read<float>(src);
						m_uv.push_back(pr::v2::make(u,v));
					}
				}
			};
			struct MaterialName :Chunk
			{
				std::string m_name;
				template <typename TSrc> MaterialName(TSrc& src) :Chunk(src, EChunkId::MaterialName) ,m_name(read_cstr(src))
				{
					read_nested(src);
				}
			};

			// Root of the 3ds tree
			Main* m_main;

			Max3DS() :m_main() {}
			~Max3DS() { delete m_main; }
			template <typename TSrc> Max3DS(TSrc& src) { Load(src); }
			template <typename TSrc> void Load(TSrc& src)
			{
				m_main = new Main(src);
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <fstream>
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_3ds)
		{
			//std::ifstream ifile("D:\\dump\\test.3ds", std::ifstream::binary);
			//pr::geometry::Max3DS _3ds(ifile);
		}
	}
}
#endif
