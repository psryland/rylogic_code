﻿//********************************
// FBX Model file format
//  Copyright (c) Rylogic Ltd 2018
//********************************
// Using *.fbx files requires the AutoDesk FBX SDK.
// To avoid making this a build dependency, this header will dynamically load 'fbx.dll' as needed.
#pragma once
#include <string>
#include <vector>
#include <iostream>
#include <functional>
#include <filesystem>
#include "pr/common/range.h"
#include "pr/common/flags_enum.h"
#include "pr/container/vector.h"
#include "pr/maths/bbox.h"
#include "pr/gfx/colour.h"
#include "pr/geometry/common.h"
#include "pr/win32/win32.h"

namespace fbxsdk
{
	class FbxScene;
}
namespace pr::geometry::fbx
{
	// Notes:
	//  - FBX scenes are a hierarchy of FbxNodes. Under the Root node are trees of nodes representing
	//    meshes, skeletons, etc. These trees are serialised depth-first into arrays.
	//    e.g.,
	//             A
	//           /   \
	//          B     C
	//        / | \   |
	//       D  E  F  G
	//    Serialised as: A0 B1 D2 E2 F2 C1 G2
	//    Children are all nodes to the right with level > the current.
	//  - The 'fbxsdk' library is *NOT* thread-safe

	struct Vert
	{
		v4 m_vert = {};
		Colour m_colr = {};
		v4 m_norm = {};
		v2 m_tex0 = {};
		iv2 m_idx0 = {};

		friend bool operator == (Vert const& lhs, Vert const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(Vert)) == 0;
		}
	};
	struct Material
	{
		Colour m_ambient = ColourBlack;
		Colour m_diffuse = ColourWhite;
		Colour m_specular = ColourZero;
		std::filesystem::path m_tex_diff;
	};
	struct Nugget
	{
		uint64_t m_mat_id = 0;
		ETopo m_topo = ETopo::TriList;
		EGeom m_geom = EGeom::Vert;
		Range<int64_t> m_vrange = Range<int64_t>::Reset();
		Range<int64_t> m_irange = Range<int64_t>::Reset();
	};
	struct Skeleton
	{
		// Notes:
		//  - Skeletons can have multiple root bones. Check for a 'm_hierarchy[i] == 0' values

		using IdCont = std::vector<uint64_t>;
		using NameCont = std::vector<std::string>;
		using BoneCont = std::vector<m4x4>;
		using LvlCont = std::vector<int>;

		IdCont m_ids;        // Bone unique ids (first is the root bone)
		NameCont m_names;    // Bone names
		BoneCont m_o2bp;     // Inverse of the bind-pose to root-object-space transform for each bone
		LvlCont m_hierarchy; // Hierarchy levels. lvl == 0 are root bones.

		// The root bone id is the skeleton id
		uint64_t Id() const { return m_ids[0]; }

		// The number of bones in this skeleton
		int size() const
		{
			assert(isize(m_ids) == isize(m_names) && isize(m_names) == isize(m_o2bp) && isize(m_o2bp) == isize(m_hierarchy));
			return isize(m_ids);
		}

		// Create a lookup table from bone id to bone index
		std::unordered_map<uint64_t, int> BoneIndexMap() const
		{
			std::unordered_map<uint64_t, int> map;
			map.reserve(m_ids.size());
			for (auto const& id : m_ids) map[id] = s_cast<int>(&id - m_ids.data());
			return map;
		}
	};
	struct Skin
	{
		struct Influence
		{
			vector<uint64_t, 8> m_bones; // The ID of the bones that influence a vertex 'v' (i.e. m_bones[v])
			vector<double, 8> m_weights; // Weights of each bone's influence a vertex 'v' (i.e. m_weights[v])
			
			int size() const
			{
				assert(isize(m_bones) == isize(m_weights));
				return isize(m_bones);
			}
		};
		using InfluenceCont = std::vector<Influence>;

		uint64_t m_skel_id = ~0ULL;
		InfluenceCont m_influences; // An influence per vertex
		explicit operator bool() const
		{
			return !m_influences.empty();
		}
	};
	struct BoneKey
	{
		// Notes:
		//  - Keys are just the stored snapshot points in the animation
		//  - Frames occur at the frame rate. All keys occur on frames, but not all frames are keys

		enum class EInterpolation
		{
			Constant = 0,
			Linear = 1,
			Cubic = 2,
		};

		quat m_rotation;
		v4 m_translation;
		v4 m_scale;
		double m_time;
		uint64_t m_flags; // [0,2) = EInterpolation flags
	};
	struct Animation
	{
		using Track = std::vector<BoneKey>; 
		using Tracks = std::unordered_map<uint64_t, Track>;
		using TimeRange = Range<double>;

		uint64_t m_skel_id; // The skeleton that these tracks should match
		Tracks m_tracks; // A bone track for each bone (by id)
		TimeRange m_time_range; // The time span of the animation

		explicit operator bool() const
		{
			for (auto& [id, track] : m_tracks)
			{
				if (track.empty()) continue;
				return true;
			}
			return false;
		}
	};
	struct Mesh
	{
		using Name = std::string;
		using VBuffer = std::vector<Vert>;
		using IBuffer = std::vector<int>;
		using NBuffer = std::vector<Nugget>;

		uint64_t m_id;
		Name m_name;
		VBuffer m_vbuf;
		IBuffer m_ibuf;
		NBuffer m_nbuf;
		Skin m_skin;
		BBox m_bbox;
		m4x4 m_o2p;
		int m_level;
	};

	// Options for parsing FBXfiles
	struct ReadOptions
	{
		enum class EParts
		{
			None = 0,
			Meshes = 1 << 0,
			Materials = 1 << 1,
			Skeletons = 1 << 2,
			Skinning = 1 << 3 | Meshes | Skeletons,
			Animation = 1 << 4,

			All = Meshes | Materials | Skeletons | Skinning | Animation,
			ModelOnly = Meshes | Materials,
			_flags_enum = 0,
		};

		// Parts of the scene to read
		EParts m_parts = EParts::All;

		// Progress callback
		using ProgressCB = struct { void* ctx; bool (*cb)(void* ctx, int64_t step, int64_t total, char const* message, int nest); };
		ProgressCB m_progress = { nullptr, nullptr };
	};

	// Options for outputting the FBX scene dump
	struct DumpOptions
	{
		enum class EParts
		{
			None           = 0,
			GlobalSettings = 1 << 0,
			NodeHierarchy  = 1 << 1,
			Materials      = 1 << 2,
			Meshes         = 1 << 3,
			Skeletons      = 1 << 4,
			Skinning       = 1 << 5 | Meshes | Skeletons,
			All            = ~0,
			_flags_enum    = 0,
		};

		// Parts of the scene to dump
		EParts m_parts = EParts::All;

		// The number to cap output of arrays at
		int m_summary_length = 10;

		// Transform the scene to 'Y=up, -Z=forward"
		bool m_convert_axis_system = true;

		// Run triangulation on meshes before outputting them
		bool m_triangulate_meshes = false;
	};

	// Fbx File Formats
	struct Formats
	{
		static constexpr char const* FbxBinary = "FBX (*.fbx)";
		static constexpr char const* FbxAscii = "FBX ascii (*.fbx)";
	};

	// Metadata in the scene
	struct SceneProps
	{
		// The number of animations in the scene
		int m_animation_stack_count = 0;

		// The animation frame rate
		double m_frame_rate = 0;
	};

	// Dynamically loaded FBX dll
	class Fbx
	{
		// Notes:
		//  - All API functions must be found using GetProcAddress.
		//  - Fbx is private only, only friends can access the API functions.
		friend struct Scene;

		static Fbx& get()
		{
			static Fbx s_this;
			return s_this;
		}

		HMODULE m_module;
		Fbx()
			: m_module(win32::LoadDll<struct FbxDll>("fbx.dll"))
			, LoadScene((Fbx_LoadSceneFn)GetProcAddress(m_module, "Fbx_LoadScene"))
			, ReleaseScene((Fbx_ReleaseSceneFn)GetProcAddress(m_module, "Fbx_ReleaseScene"))
			, ReadSceneProps((Fbx_ReadScenePropsFn)GetProcAddress(m_module, "Fbx_ReadSceneProps"))
			, ReadScene((Fbx_ReadSceneFn)GetProcAddress(m_module, "Fbx_ReadScene"))
			, DumpScene((Fbx_DumpSceneFn)GetProcAddress(m_module, "Fbx_DumpScene"))
			, RoundTripTest((Fbx_RoundTripTestFn)GetProcAddress(m_module, "Fbx_RoundTripTest"))
		{}

		// Load an fbx scene
		using Fbx_LoadSceneFn = fbxsdk::FbxScene* (__stdcall *)(std::istream& src);
		Fbx_LoadSceneFn LoadScene;

		// Release an fbx scene
		using Fbx_ReleaseSceneFn = void (__stdcall *)(fbxsdk::FbxScene* scene);
		Fbx_ReleaseSceneFn ReleaseScene;

		// Read meta data about the scene
		using Fbx_ReadScenePropsFn = SceneProps (__stdcall*)(fbxsdk::FbxScene const& scene);
		Fbx_ReadScenePropsFn ReadSceneProps;

		// Read the scene hierarchy
		using Fbx_ReadSceneFn = void (__stdcall *)(Scene& scene, ReadOptions const& options);
		Fbx_ReadSceneFn ReadScene;

		// Dump info about the scene to 'out'
		using Fbx_DumpSceneFn = void(__stdcall*)(pr::geometry::fbx::Scene const& scene, pr::geometry::fbx::DumpOptions const& options, std::ostream& out);
		Fbx_DumpSceneFn DumpScene;

		// Round trip test an fbx scene
		using Fbx_RoundTripTestFn = void (*)(std::istream& src, std::ostream& out);
		Fbx_RoundTripTestFn RoundTripTest;
	};

	// FBX Scene
	struct Scene
	{
		using MeshCont = std::vector<Mesh>;
		using MaterialCont = std::unordered_map<uint64_t, Material>;
		using SkeletonCont = std::vector<Skeleton>;
		using AnimationCont = std::vector<Animation>;

		// The FBX scene instance
		fbxsdk::FbxScene* m_fbxscene;

		// Scene global properties
		SceneProps m_props;

		// One or more model hierarchies.
		// Meshes with 'm_level == 0' are the roots of a model tree
		// Meshes are in depth-first order.
		MeshCont m_meshes;

		// Material definitions
		MaterialCont m_materials;

		// One or more skeleton hierarchies.
		SkeletonCont m_skeletons;

		// Animation data
		AnimationCont m_animations;

		// Remember to open streams in binary mode!
		Scene(std::istream& src)
			: m_fbxscene(Fbx::get().LoadScene(src))
			, m_props(Fbx::get().ReadSceneProps(*m_fbxscene))
			, m_meshes()
			, m_materials()
			, m_skeletons()
			, m_animations()
		{}
		Scene(Scene&& rhs) noexcept
			: m_fbxscene()
			, m_props()
		{
			std::swap(m_fbxscene, rhs.m_fbxscene);
			std::swap(m_props, rhs.m_props);
		}
		Scene(Scene const&) = delete;
		Scene& operator=(Scene&& rhs) noexcept
		{
			std::swap(m_fbxscene, rhs.m_fbxscene);
			std::swap(m_props, rhs.m_props);
			return *this;
		}
		Scene& operator=(Scene const&) = delete;
		~Scene()
		{
			if (m_fbxscene != nullptr)
				Fbx::get().ReleaseScene(m_fbxscene);
		}

		// Read the full scene into memory
		void Read(ReadOptions const& options)
		{
			// Notes:
			//  - If this is slow, it's probably spending most of the time trianguling the
			//    meshes. Try getting the fbx export tool (e.g. blender) to triangulate on export
			Fbx::get().ReadScene(*this, options);
		}

		// Read the full scene into memory
		void Dump(DumpOptions const& options, std::ostream& out)
		{
			// You probably want to read first
			Fbx::get().DumpScene(*this, options, out);
		}
	};
}
