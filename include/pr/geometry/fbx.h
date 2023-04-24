//********************************
// 3DS file data
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include <string>
#include <array>
#include <iostream>
#include <stdexcept>
#include <cassert>
#include "pr/macros/enum.h"
#include "pr/common/cast.h"
#include "pr/common/hash.h"
#include "pr/common/fmt.h"

namespace pr::geometry::fbx
{
	// Notes:
	//  - FBX files can be text or binary.
	//  - FBX are recursive nested lists of 'Nodes' where a node has the form:
	//       NodeType: Property0, Property1, ... , {
	//           NodeType: Prop0, Prop1, ... , {}
	//           NodeType: Prop0, Prop1, ... , {}
	//           NodeType: Prop0, Prop1, ... , {}
	//           ...
	//       }
	//  - FBX node lists are terminated with 'Null' nodes
	//  - Primitive properties are stored as little-endian values.
	//  - String properties are not null terminated and can contain \0 characters.
	//
	// 	Node structure
	//  struct Node
	//  {
	//      uint32_t  EndOffset;            // byte offset to the next node
	//      uint32_t  NumProperties;        // the number of properties in this node
	//      uint32_t  PropertyListLen;      // the size (in bytes) of all N properties
	//      uint8_t   NameLen;              // the size (in bytes) of the 'Name' field
	//      char      Name[NameLen];        // the name field. Not null terminated.
	//      Property  Prop[NumProperties];  // the list of properties. 'Property' is different sizes.
	//      -- Optional --
	//      Node      Children[...];        // null terminated list of nested child nodes
	//      NullNode  Null;                 // null indicates there is a list.
	//  };
	//
	//  Property structure
	//  struct Property
	//  {
	//      uint8_t TypeCode;   // Property type id
	//      uint8_t Data[n];    // 'n' bytes of data implied by property type
	//  };
	//
	//  Array properties have the structure
	//  struct ArrayProperty
	//  {
	//     uint32_t ArrayLength;      // 
	//     uint32_t Encoding;         // If 0 then 'n' = ArrayLength * ArrayType. If 1 then 'n' = CompressedLength and data is LZ compressed (use zip to inflate)
	//     uint32_t CompressedLength; //
	//     uint8_t  Data[n];          //
	//  };
	//
	//  String or RawBinary properties
	//  struct StringProperty
	//  {
	//     uint32_t Length;      // in bytes
	//     uint8_t Data[Length]; // String or binary data
	//  };

	// FBX node types
	#pragma region Node Type Ids
	#define PR_ENUM(x)\
		x(Null,               = pr::hash::HashCT("NULL"))               /* Null node id */\
		x(FBXHeaderExtension, = pr::hash::HashCT("FBXHeaderExtension")) /* */\
		x(FBXHeaderversion,   = pr::hash::HashCT("FBXHeaderVersion"))   /* */
	PR_DEFINE_ENUM2(ENodeId, PR_ENUM);
	#undef PR_ENUM
	#pragma endregion

	// FBX property types
	#pragma region Property Type Ids
	#define PR_ENUM(x)\
		x(Boolean,     = 'C') /* The LSB of a 1-byte */\
		x(Short,       = 'Y') /* 2-byte signed integer */\
		x(Integer,     = 'I') /* 4-byte signed integer */\
		x(Long,        = 'L') /* 8-byte signed integer */\
		x(Float,       = 'F') /* 4-byte IEEE 754 float */\
		x(Double,      = 'D') /* 8-byte IEEE 754 float */\
		x(BoolArray,   = 'b') /* Array of 1-byte booleans (0 or 1) */\
		x(IntArray,    = 'i') /* Array of 4-byte integers */\
		x(LongArray,   = 'l') /* Array of 8-byte integers */\
		x(FloatArray,  = 'f') /* Array of 4-byte IEEE 754 floats */\
		x(DoubleArray, = 'd') /* Array of 8-byte IEEE 754 floats */\
		x(String,      = 'S') /* String */\
		x(RawBinary,   = 'R') /* Raw binary data */
	PR_DEFINE_ENUM2_BASE(EPropId, PR_ENUM, char);
	#undef PR_ENUM
	static_assert(sizeof(EPropId) == sizeof(uint8_t));
	#pragma endregion

	// Magic marker at the start of an FBX file
	using Marker = char[23];

	// A node
	template <typename TSrc>
	struct Node
	{
		TSrc* m_src;              // The FBX source stream
		uint64_t m_ofs;           // Byte offset into 'src' for the start of this node
		uint32_t EndOffset;       // Byte offset to the next node
		uint32_t NumProperties;   // number of properties in this node
		uint32_t PropertyListLen; // size (in bytes) of the properties
		ENodeId  NodeId;          // Node type

		Node(TSrc& src, uint64_t ofs);
	};

	// Object(?)
	struct Object
	{
	};

	// Helpers for reading from a stream source
	// Specialise these for non 'std::istream's
	template <typename TSrc> struct Src
	{
		static uint64_t TellPos(TSrc& src)
		{
			return static_cast<uint64_t>(src.tellg());
		}
		static bool SeekAbs(TSrc& src, uint64_t pos)
		{
			return static_cast<bool>(src.seekg(pos));
		}

		// Read an array
		template <typename TOut> static void Read(TSrc& src, TOut* out, size_t count)
		{
			if (src.read(char_ptr(out), count * sizeof(TOut))) return;
			throw std::runtime_error("partial read of input stream");
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
			return std::move(out);
		}

		// Recursive node reading function
		// 'src' should point to the start of a node.
		// 'len' is the remaining bytes in the parent node from 'src' to the end of the parent node.
		// 'func' is called back with the chunk header, it should return true to stop reading.
		template <typename TSrc, typename Func>
		void ReadNodes(TSrc& src, Func func)
		{
			for (;;)
			{
				auto start = Src<TSrc>::TellPos(src);

				Node node(src, start);
				if (node.NodeId == ENodeId::Null)
					return;

				// Parse the node
				if (func(node))
					return;

				// Seek to the next node
				Src<TSrc>::SeekAbs(src, node.EndOffset);
			}
		}
	}

	template <typename TSrc>
	inline Node<TSrc>::Node(TSrc& src, uint64_t ofs)
		: m_src(&src)
		, m_ofs(ofs)
	{
		EndOffset = impl::Read<uint32_t>(src);
		NumProperties = impl::Read<uint32_t>(src);
		PropertyListLen = impl::Read<uint32_t>(src);

		// Read the node name and hash it to find the node type id
		char name[256];
		auto name_len = impl::Read<uint8_t>(src);
		impl::Read(src, &name[0], name_len);
		NodeId = static_cast<ENodeId>(pr::hash::Hash32CT(&name[0], &name[name_len]));

		//if (EndOffset <= len) len -= hdr.length; else throw std::runtime_error(FmtS("invalid chunk found at offset 0x%X", start));

	}

	// Extract the objects from a FBX stream.
	// 'obj_out' should return true to stop searching (i.e. object found)
	template <typename TSrc, typename TObjOut> void ReadObjects(TSrc& src, TObjOut obj_out)
	{
		// Restore the src position on return
		auto start = Src<TSrc>::TellPos(src);
		auto reset_stream = Scope<void>([] {}, [&] { Src<TSrc>::SeekAbs(src, start); });

		// Check that this is actually a FBX stream
		Marker magic_marker;
		impl::Read(src, &magic_marker[0], _countof(magic_marker));
		if (memcmp(&magic_marker[0], "Kaydara FBX Binary  \0\x1A\0", sizeof(magic_marker)) != 0)
			throw std::runtime_error("Source is not a binary *.fbx stream");

		// Read the version
		auto version = impl::Read<uint32_t>(src);
		if (version < 7300 || version > 7400)
			throw std::runtime_error("Source is an unsupported *.fbx version. It might work, you need to check this code");

		// Read the list of nodes
		impl::ReadNodes(src, [&](Node<TSrc> node)
		{
			switch (node.NodeId)
			{
				case ENodeId::FBXHeaderExtension:
				{
					return false;
				}
				default:
				{
					throw std::runtime_error(FmtS("Unknown FBX node type %d found at byte offset %lld", node.NodeId, node.m_ofs));
				}
			}
		});
		
		(void)obj_out;



		////// Find the M3DEditor sub chunk
		////auto editor = impl::Find(EChunkId::M3DEditor, src, main.length - sizeof(ChunkHeader));
		////if (editor.id != EChunkId::M3DEditor)
		////	return; // Source contains no editor data

		//// Read the nodes
		//impl::ReadNodes(src, editor.length - sizeof(ChunkHeader), [&](ChunkHeader hdr, TSrc& src, u32 data_len)
		//{
		//	switch (hdr.id)
		//	{
		//		case EChunkId::ObjectBlock:
		//			if (obj_out(impl::ReadObject(src, data_len)))
		//				return true;
		//			break;
		//	}
		//	return false;
		//});
	}



	
	#if 0
	// Given an FBX model object, generate verts/indices for a renderer model.
	// 'TOut' is an object with callback methods for received model data.
	template <typename TMatLookup, typename VOut, typename IOut, typename NOut>
	void CreateModel(max_3ds::Object const& obj, TMatLookup mats, VOut vout, IOut iout, NOut nout)
	{
		// Validate 'obj'
		if (!obj.m_mesh.m_uv.empty() && obj.m_mesh.m_vert.size() != obj.m_mesh.m_uv.size())
			throw std::runtime_error("invalid 3DS object. Number of UVs != number of verts");
		if (obj.m_mesh.m_face.size() != obj.m_mesh.m_smoothing_groups.size())
			throw std::runtime_error("invalid 3DS object. Number of faces != number of smoothing groups");

		// Can't just output the verts directly.
		// In a max model verts can have multiple normals.
		// Create one of these 'Verts' per unique model vert.
		struct Vert
		{
			v4       m_norm;       // The accumulated vertex normal
			Colour   m_col;        // The material colour for this vert
			uint32_t m_smooth;     // The smoothing group bits
			Vert* m_next;       // Another copy of this vert with different smoothing group
			uint16_t m_orig_index; // The index into the original obj.m_mesh.m_vert container
			uint16_t m_new_index;  // The index of this vert in the 'verts' container

			Vert(uint16_t orig_index, uint16_t new_index, v4 const& norm, Colour const& col, uint32_t sg)
				:m_norm(norm)
				, m_col(col)
				, m_smooth(sg)
				, m_next()
				, m_orig_index(orig_index)
				, m_new_index(new_index)
			{}
		};
		struct Verts
		{
			std::deque<Vert, pr::aligned_alloc<Vert>> m_data;

			auto begin() const
			{
				return m_data.begin();
			}
			auto end() const
			{
				return m_data.end();
			}
			void push_back(Vert&& vert)
			{
				m_data.emplace_back(std::move(vert));
			}

			// Returns the new index for the vert in 'cont'
			uint16_t add(uint16_t idx, v4 const& norm, Colour const& col, uint32_t sg)
			{
				auto& vert = m_data.at(idx);

				// If the smoothing group intersects, accumulate 'norm'
				// and return the vertex index of this vert
				if ((sg == 0 && vert.m_smooth == 0) || (sg & vert.m_smooth) != 0 || (vert.m_norm == v4Zero))
				{
					vert.m_norm += norm;
					vert.m_col = col;
					vert.m_smooth |= sg;
					return vert.m_new_index;
				}

				// Otherwise if we have a 'next' try that vert
				if (vert.m_next != nullptr)
					return add(vert.m_next->m_new_index, norm, col, sg);

				// Otherwise, create a new Vert and add it to the linked list
				auto new_index = s_cast<uint16_t>(m_data.size());
				m_data.emplace_back(vert.m_orig_index, new_index, norm, col, sg);
				vert.m_next = &m_data.back();
				return new_index;
			}
		};
		Verts verts;

		// Initialise 'verts'
		for (uint16_t i = 0, iend = s_cast<uint16_t>(obj.m_mesh.m_vert.size()); i != iend; ++i)
			verts.push_back(Vert(i, i, v4Zero, ColourWhite, 0));

		// Loop over material groups, each material group is a nugget
		auto vrange = Range<size_t>::Zero();
		auto irange = Range<size_t>::Zero();
		for (auto const& mgrp : obj.m_mesh.m_matgroup)
		{
			// Ignore material groups that aren't used in the model
			if (mgrp.m_face.empty())
				continue;

			// Find the material
			auto const& mat = mats(mgrp.m_name);
			auto topo = ETopo::TriList;
			auto geom = EGeom::Vert | EGeom::Colr | EGeom::Norm | (!mat.m_textures.empty() ? EGeom::Tex0 : EGeom::None);

			// Write out each face that belongs to this group
			irange.m_beg = irange.m_end;
			vrange = Range<size_t>::Reset();
			for (auto const& face_idx : mgrp.m_face)
			{
				// Get the face and it's smoothing group
				auto const& face = obj.m_mesh.m_face[face_idx];
				auto sg = obj.m_mesh.m_smoothing_groups[face_idx];

				// Calculate the weighted normal for this face at each vertex
				// Reason weights are needed: consider the (+x,+y,+z) corner of a box, the normal should point out along (1,1,1)
				// but if one box face has two triangles, while the others have one, this wouldn't be true without weight values.
				// Could use face area as the weight, it's cheaper but doesn't quite give the correct result.
				auto v0 = obj.m_mesh.m_vert[face.m_idx[0]].w1();
				auto v1 = obj.m_mesh.m_vert[face.m_idx[1]].w1();
				auto v2 = obj.m_mesh.m_vert[face.m_idx[2]].w1();
				auto e0 = v1 - v0;
				auto e1 = v2 - v1;
				auto cx = Cross3(e0, e1);
				auto norm = Normalise(cx, v4Zero);
				auto angles = TriangleAngles(v0, v1, v2);

				// Get the final vertex indices for the face
				auto i0 = verts.add(face.m_idx[0], angles.x * norm, mat.m_diffuse, sg);
				auto i1 = verts.add(face.m_idx[1], angles.y * norm, mat.m_diffuse, sg);
				auto i2 = verts.add(face.m_idx[2], angles.z * norm, mat.m_diffuse, sg);

				vrange.grow(i0);
				vrange.grow(i1);
				vrange.grow(i2);
				irange.m_end += 3;

				// Write out face indices
				iout(i0, i1, i2);
			}

			// Output a nugget for this material group
			nout(topo, geom, mat, vrange, irange);
		}

		// Write out the verts including their normals
		for (auto const& vert : verts)
		{
			auto p = obj.m_mesh.m_vert[vert.m_orig_index].w1();
			auto c = vert.m_col;
			auto n = Normalise(vert.m_norm, v4Zero);
			auto t = !obj.m_mesh.m_uv.empty() ? obj.m_mesh.m_uv[vert.m_orig_index] : v2Zero;
			vout(p, c, n, t);
		}
	}
	#endif
}

#if PR_UNITTESTS
#include <fstream>
#include "pr/common/unittests.h"
#include "pr/view3d/renderer.h"
namespace pr::geometry
{
	PRUnitTest(GeometryFbxTests)
	{
		std::ifstream ifile(unittests::RepoPath(L"dump\\model.fbx"), std::ifstream::binary);
		fbx::ReadObjects(ifile, [](fbx::Object&&){ return false; });
		//fbx::ReadMaterials(ifile, [](max_3ds::Material&&){ return false; });
	}
}
#endif