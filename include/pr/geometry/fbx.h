//********************************
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
	//  - FBX scenes are a hierarchy of FbxNodes.
	//  - Meshes, skeletons, etc are output using the ModelTree serialisation of the hierarchy.
	//    The nodes are output in depth-first order with the hierarchy level.
	//    e.g.,
	//             A
	//           /   \
	//          B     C
	//        / | \   |
	//       D  E  F  G
	//    Serialised as: A0 B1 D2 E2 F2 C1 G2
	//    Children are all nodes to the right with level > the current.
	//  - The 'fbxsdk' library is *NOT* thread-safe

	enum class EParts
	{
		None = 0,
		Meshes = 1 << 0,
		Materials = 1 << 1,
		Skeleton = 1 << 2,
		Skinning = 1 << 3,
		AnimCurves = 1 << 4,

		All = Meshes | Materials | Skeleton | Skinning | AnimCurves,
		ModelOnly = Meshes | Materials,
		_flags_enum = 0,
	};
	enum class EBoneType
	{
		Root,
		Limb,
		Effector,
	};

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
		BBox m_bbox;

		void reset(uint64_t id)
		{
			m_id = id;
			m_name.resize(0);
			m_vbuf.resize(0);
			m_ibuf.resize(0);
			m_nbuf.resize(0);
			m_bbox = BBox::Reset();
		}
	};
	struct Skeleton
	{
		using IdCont = std::vector<uint64_t>;
		using NameCont = std::vector<std::string>;
		using TypeCont = std::vector<EBoneType>;
		using BoneCont = std::vector<m4x4>;
		using LvlCont = std::vector<int>;

		IdCont m_ids;        // Bone unique ids (first is the root bone)
		NameCont m_names;    // Bone names
		BoneCont m_b2p;      // Bone to parent transform hierarchy in the skeleton rest position
		TypeCont m_types;    // Bone types
		LvlCont m_hierarchy; // Hierarchy levels

		void reset()
		{
			m_ids.resize(0);
			m_names.resize(0);
			m_b2p.resize(0);
			m_types.resize(0);
			m_hierarchy.resize(0);
		}
	};
	struct Skin
	{
		struct Influence
		{
			iv4 m_bones; // Indices of the bones that influence a vertex 'v' (i.e. m_bones[v])
			v4 m_weights; // Weights of each bone's influence a vertex 'v' (i.e. m_weights[v])
		};
		using InfluenceCont = std::vector<Influence>;
		
		uint64_t m_mesh_id;
		uint64_t m_skel_id;
		InfluenceCont m_verts;

		void reset(uint64_t mesh_id, uint64_t skel_id)
		{
			m_mesh_id = mesh_id;
			m_skel_id = skel_id;
			m_verts.resize(0);
		}
	};
	struct BoneKey
	{
		// Notes:
		//  - Keys are just the stored snapshot points in the animation
		//  - Frames occur at the frame rate. All keys occur on frames, but not all frames are keys
		quat m_rotation;
		v4 m_translation;
		v4 m_scale;
		double m_time;
	};
	struct BoneTracks
	{
		using Track = std::vector<BoneKey>; 
		using Tracks = std::unordered_map<uint64_t, Track>;

		uint64_t m_skel_id; // The skeleton that these tracks should match
		Tracks m_tracks;    // A bone track for each bone (by id)
	
		void reset(uint64_t skel_id)
		{
			m_skel_id = skel_id;
			m_tracks.clear();
		}
	};

	// Interfaces for emitting scene parts during 'Read'
	struct ISceneOut
	{
		virtual ~ISceneOut() = default;

		// Add a material to the output
		virtual void AddMaterial(uint64_t unique_id, Material const& mat) { (void)unique_id, mat; }

		// Add a mesh to the output
		virtual void AddMesh(Mesh const& mesh, m4x4 const& o2p, int level) { (void)mesh, o2p, level; }

		// Add a skeleton to the output
		virtual void AddSkeleton(Skeleton const& skeleton) { (void)skeleton; }

		// Add Skin data for an existing mesh
		virtual void AddSkin(Skin const& skin) { (void)skin; }

		// Add an animation sequence
		virtual void AddAnimation(uint64_t skel_id, BoneTracks const& tracks) { (void)skel_id, tracks; }
	};

	// Options for parsing FBXfiles
	struct ReadOptions
	{
		// Parts of the scene to read
		EParts m_parts = EParts::All;
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
		using Fbx_ReadSceneFn = void (__stdcall *)(fbxsdk::FbxScene& scene, ISceneOut& out, ReadOptions const& options);
		Fbx_ReadSceneFn ReadScene;

		// Dump info about the scene to 'out'
		using Fbx_DumpSceneFn = void(__stdcall*)(fbxsdk::FbxScene& scene, std::ostream& out);
		Fbx_DumpSceneFn DumpScene;

		// Round trip test an fbx scene
		using Fbx_RoundTripTestFn = void (*)(std::istream& src, std::ostream& out);
		Fbx_RoundTripTestFn RoundTripTest;
	};

	// FBX Scene
	struct Scene
	{
		// Notes:
		//  - Remember to open streams in binary mode!

		fbxsdk::FbxScene* m_scene;
		SceneProps m_props;

		Scene(std::istream& src)
			: m_scene(Fbx::get().LoadScene(src))
			, m_props(Fbx::get().ReadSceneProps(*m_scene))
		{
		}
		Scene(Scene&& rhs) noexcept
			: m_scene()
			, m_props()
		{
			std::swap(m_scene, rhs.m_scene);
			std::swap(m_props, rhs.m_props);
		}
		Scene(Scene const&) = delete;
		Scene& operator=(Scene&& rhs) noexcept
		{
			std::swap(m_scene, rhs.m_scene);
			std::swap(m_props, rhs.m_props);
			return *this;
		}
		Scene& operator=(Scene const&) = delete;
		~Scene()
		{
			if (m_scene != nullptr)
				Fbx::get().ReleaseScene(m_scene);
		}

		// Read the hierarchy from the FBX scene
		void ReadScene(ISceneOut& out, ReadOptions const& options)
		{
			// Notes:
			//  - If this is slow, it's probably spending most of the time trianguling the
			//    meshes. Try getting the fbx export tool (e.g. blender) to triangulate on export
			Fbx::get().ReadScene(*m_scene, out, options);
		}
	};
}
