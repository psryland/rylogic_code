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

	enum class EParts
	{
		None = 0,
		Meshes = 1 << 0,
		Materials = 1 << 1,
		Skeleton = 1 << 2,
		Skinning = 1 << 3,
		All = Meshes | Materials | Skeleton | Skinning,
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
		using VBuffer = std::vector<Vert>;
		using IBuffer = std::vector<int>;
		using NBuffer = std::vector<Nugget>;

		uint64_t m_id;
		std::string_view m_name;
		VBuffer m_vbuf;
		IBuffer m_ibuf;
		NBuffer m_nbuf;
		BBox m_bbox;

		void reset(uint64_t id)
		{
			m_id = id;
			m_name = "";
			m_vbuf.resize(0);
			m_ibuf.resize(0);
			m_nbuf.resize(0);
			m_bbox = BBox::Reset();
		}
	};
	struct Skeleton
	{
		using NameCont = std::vector<std::string>;
		using TypeCont = std::vector<EBoneType>;
		using BoneCont = std::vector<m4x4>;
		using LvlCont = std::vector<int>;

		uint64_t m_id;    // Unique id of the root bone node
		NameCont m_names; // Bone name
		TypeCont m_types; // Bone type
		BoneCont m_b2p;   // Bone to parent transform hierarchy in the skeleton rest position
		LvlCont m_levels; // Hierarchy levels

		void reset(uint64_t id)
		{
			m_id = id;
			m_names.resize(0);
			m_types.resize(0);
			m_b2p.resize(0);
			m_levels.resize(0);
		}
	};
	struct Skinning
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

	// Interfaces for emitting model parts during 'Read'
	struct IModelOut
	{
		virtual ~IModelOut() = default;

		// Add a material to the output
		virtual void AddMaterial(uint64_t unique_id, Material const& mat) { (void)unique_id, mat; }

		// Add a mesh to the output
		virtual void AddMesh(Mesh const& mesh, m4x4 const& o2p, int level) { (void)mesh, o2p, level; }

		// Add a skeleton to the output
		virtual void AddSkeleton(Skeleton const& skeleton) { (void)skeleton; }

		// Add Skinning data for a mesh to the output
		virtual void AddSkinning(Skinning const& skinning) { (void)skinning; }
	};

	// Options for parsing FBXfiles
	struct ReadModelOptions
	{
		// Parts of the model to read
		EParts m_parts = EParts::All;

		// The animation stack to use
		int m_anim = 0;

		// The time at which to read the transforms
		double m_time_in_seconds = 0;
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
		int m_animation_stack_count = 0;
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
			, ReadModel((Fbx_ReadModelFn)GetProcAddress(m_module, "Fbx_ReadModel"))
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

		// Read the model hierarchy from the scene
		using Fbx_ReadModelFn = void (__stdcall *)(fbxsdk::FbxScene& scene, IModelOut& out, ReadModelOptions const& options);
		Fbx_ReadModelFn ReadModel;

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

		// Read the model from the FBX scene
		void ReadModel(IModelOut& out, ReadModelOptions const& options)
		{
			// Notes:
			//  - If this is slow, it's probably spending most of the time trianguling the
			//    meshes. Try getting the fbx export tool (e.g. blender) to triangulate on export
			Fbx::get().ReadModel(*m_scene, out, options);
		}
	};
}
