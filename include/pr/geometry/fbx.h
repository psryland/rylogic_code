﻿//********************************
// FBX Model file format
//  Copyright (c) Rylogic Ltd 2018
//********************************
#pragma once
#include <memory>
#include <string_view>
#include "pr/common/range.h"
#include "pr/common/flags_enum.h"
#include "pr/maths/maths.h"
#include "pr/maths/bbox.h"
#include "pr/gfx/colour.h"
#include "pr/geometry/common.h"
#include "pr/win32/win32.h"

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
	//  - Hides all FBX Sdk types within the dll.
	//  - To avoid making this a build dependency, this header will dynamically load 'fbx.dll' as needed.
	//  - Using *.fbx files requires the AutoDesk FBX SDK.
	//  - *No* memory allocation expose across DLL boundary (i.e. no 'stl')
	//    Uses the pimpl pattern
	//
	// Blender Export Settings:
	//  - If exporting from blender use:
	//    include:
	//       Object Types: Mesh, Armature,
	//    Transform:
	//        Scale: 1.0
	//        Apply Settings: All Local
	//        Forward: -Z Forward
	//        Up: Y Up
	//        Apply Unit: Yes
	//        Use Space Transform: Yes
	//        Apply Transform: No
	//    Geometry:
	//        Whatever you like
	//    Armature:
	//        Primary: Y Axis
	//        Secondary: X Axis
	//        Armature FBXNode Type: Null
	//        Only Deform Bones: No
	//        Add Leaf Bones: No


	struct Context;
	struct SceneData;
	struct MeshData;
	struct MaterialData;
	struct SkinData;

	struct ErrorHandler
	{
		using FuncCB = void(*)(void*, char const* message);

		void* m_ctx;
		FuncCB m_cb;

		void operator()(char const* message)
		{
			if (m_cb) m_cb(m_ctx, message);
			else throw std::runtime_error(message);
		}
	};

	// Fbx File Formats
	struct Formats
	{
		static constexpr char const* FbxBinary = "FBX (*.fbx)";
		static constexpr char const* FbxAscii = "FBX ascii (*.fbx)";
	};

	// Parts of an FBX Scene
	using EParts = ESceneParts;

	// Axis systems the scene can be converted to
	enum class ECoordSystem
	{
		// Right, Up, Forward
		PosX_PosY_NegZ,
		PosX_PosZ_PosY,
	};

	// Animation channels
	enum class EAnimChannel
	{
		None = 0,
		Rotation = 1 << 0,
		Position = 1 << 1,
		Scale = 1 << 2,
		_flags_enum = 0,
	};

	// Interpolation modes
	enum class EInterpolation
	{
		Constant = 0,
		Linear = 1,
		Cubic = 2,
	};

	// Metadata in the scene
	struct SceneProps
	{
		// The number of animations in the scene
		int m_animation_stack_count = 0;

		// The animation frame rate
		double m_frame_rate = 0;

		// Scene objects available (i.e. in the scene, but not necessarily loaded)
		int m_material_available = 0;
		int m_meshes_available = 0;
		int m_skeletons_available = 0;
		int m_animations_available = 0;

		// Scene object counts (loaded scene objects)
		int m_material_count = 0;
		int m_mesh_count = 0;
		int m_skeleton_count = 0;
		int m_animation_count = 0;

		// Names of the root mesh nodes
		std::span<std::string const> m_mesh_names;

		// Names of the root bone nodes
		std::span<std::string const> m_skel_names;
	};

	// Options for parsing FBXfiles
	struct ReadOptions
	{
		// Parts of the scene to read
		EParts m_parts = EParts::All;
	
		// The coordinate system to convert the scene to
		ECoordSystem m_coord_system = ECoordSystem::PosX_PosY_NegZ;

		// The animation frame range to read
		pr::Range<int> m_frame_range = { 0, std::numeric_limits<int>::max() };

		// The subset of meshes to load. Empty means load all.
		std::function<bool(std::string_view)> m_mesh_filter = {};

		// The subset of skeletons to load. Empty means load all.
		std::function<bool(std::string_view)> m_skel_filter = {};

		// Progress callback
		std::function<bool(int64_t step, int64_t total, char const* message, int nest)> m_progress = {};
	};

	// Options for outputting the FBX scene dump
	struct DumpOptions
	{
		// Parts of the scene to dump
		EParts m_parts = EParts::All;

		// The coordinate system to convert the scene to
		ECoordSystem m_coord_system = ECoordSystem::PosX_PosY_NegZ;

		// The number to cap output of arrays at
		int m_summary_length = 10;

		// Run triangulation on meshes before outputting them
		bool m_triangulate_meshes = false;
	};

	// Model types
	struct Vert
	{
		v4 m_vert = {};
		Colour m_colr = {};
		v4 m_norm = {};
		v2 m_tex0 = {};
		iv2 m_idx0 = {};

		friend bool operator == (Vert const& lhs, Vert const& rhs)
		{
			return std::memcmp(&lhs, &rhs, sizeof(Vert)) == 0;
		}
	};
	struct TimeKey
	{
		double m_time;
		EInterpolation m_interp;
	};
	struct TimeRange : Range<double>
	{};
	struct Nugget
	{
		uint64_t m_mat_id = 0;
		ETopo m_topo = ETopo::TriList;
		EGeom m_geom = EGeom::Vert;
		Range<int64_t> m_vrange = Range<int64_t>::Reset();
		Range<int64_t> m_irange = Range<int64_t>::Reset();
	};
	struct Material
	{
		Colour m_ambient;
		Colour m_diffuse;
		Colour m_specular;
		std::string_view m_tex_diff;
	};
	struct Skin
	{
		uint64_t m_skel_id;                // The skeleton that this skin is based on
		std::span<int const> m_offsets;    // Index offset to the first influence for each vertex
		std::span<uint64_t const> m_bones; // The Ids of the bones that influence a vertex
		std::span<double const> m_weights; // The influence weights

		// The number of vertices influenced by this skin
		int vert_count() const
		{
			return static_cast<int>(ssize(m_offsets) - 1);
		}
		int influence_count(int vidx) const
		{
			return static_cast<int>(m_offsets[vidx+1] - m_offsets[vidx]);
		}
		explicit operator bool() const
		{
			return !m_offsets.empty() && m_offsets.back() != 0;
		}
	};
	struct Skeleton
	{
		uint64_t m_id = 0ULL;                 // Unique skeleton Id
		std::span<uint64_t const> m_bone_ids; // Bone unique ids (first is the root bone)
		std::span<std::string const> m_names; // Bone names
		std::span<m4x4 const> m_o2bp;         // Inverse of the bind-pose to root-object-space transform for each bone
		std::span<int const> m_hierarchy;     // Hierarchy levels. level == 0 are root bones.

		// The number of bones in this skeleton
		int size() const
		{
			assert(isize(m_bone_ids) == isize(m_names) && isize(m_names) == isize(m_o2bp) && isize(m_o2bp) == isize(m_hierarchy));
			return isize(m_bone_ids);
		}

		// Create a lookup table from bone id to bone index
		std::unordered_map<uint64_t, int> BoneIndexMap() const
		{
			std::unordered_map<uint64_t, int> map;
			map.reserve(s_cast<size_t>(size()));
			for (auto const& id : m_bone_ids) map[id] = s_cast<int>(&id - m_bone_ids.data());
			return map;
		}
	};
	struct Animation
	{
		// Notes:
		//  - Bone transform data are stored concatenated for each bone, e.g.,
		//    m_rotation: [(bone0,,,)(bone1,,,)(bone2,,)...]
		//    m_position: [(bone0,,,)(bone1,,,)(bone2,,)...]
		//  - Each (boneN...) is a "track", not all tracks are the same length.
		//    However the length of a bone's track is the same in each array
		//  - 'm_offsets[bone_index]' is the index offset into the arrays for a bone.

		uint64_t m_skel_id;               // The skeleton that these tracks should match
		TimeRange m_time_range;           // The time span of the animation
		double m_frame_rate;              // The native frame rate of the animation
		int m_bone_count;                 // The number of bones per key frame
		std::span<int const> m_offsets;   // Index offsets to the start of each bone track (one for each bone in the skeleton)
		std::span<TimeKey const> m_times; // The key frame time for each entry
		std::span<quat const> m_rotation;
		std::span<v3 const> m_position;
		std::span<v3 const> m_scale;
		
		// The number of tracks in this animation (one for each bone)
		int track_count() const
		{
			return isize(m_offsets) - 1;
		}

		// Return the index range for a bone in the data arrays
		std::tuple<int64_t, size_t> index_range(int bone_index) const
		{
			assert(bone_index >= 0 && bone_index < track_count());
			auto i0 = m_offsets[bone_index + 0];
			auto i1 = m_offsets[bone_index + 1];
			return { i0 , static_cast<size_t>(i1 - i0) };
		}

		// Return the key frame times for a bone
		std::span<TimeKey const> times(int bone_index) const
		{
			auto [ofs, len] = index_range(bone_index);
			return std::span{ m_times.data() + ofs, len };
		}

		// Return the rotation data for a bone
		std::span<quat const> rotation(int bone_index) const
		{
			auto [ofs, len] = index_range(bone_index);
			return !m_rotation.empty() ? m_rotation.subspan(ofs, len) : m_rotation;
		}

		// Return the position data for a bone
		std::span<v3 const> position(int bone_index) const
		{
			auto [ofs, len] = index_range(bone_index);
			return !m_position.empty() ? m_position.subspan(ofs, len) : m_position;
		}

		// Return the scale data for a bone
		std::span<v3 const> scale(int bone_index) const
		{
			auto [ofs, len] = index_range(bone_index);
			return !m_scale.empty() ? m_scale.subspan(ofs, len) : m_scale;
		}

		// True if the animation has data
		explicit operator bool() const
		{
			return !m_offsets.empty() && m_offsets.back() != 0;
		}
	};
	struct Mesh
	{
		uint64_t m_id;
		std::string_view m_name;
		std::span<Vert const> m_vbuf;
		std::span<int const> m_ibuf;
		std::span<Nugget const> m_nbuf;
		Skin m_skin;
		BBox m_bbox;
		m4x4 m_o2p;
		int m_level;
	};

	// Dynamically loaded FBX dll
	class Fbx
	{
		// Fbx is private only, only friends can access the API functions.
		friend struct Scene;
		friend struct Mesh;
		friend struct Material;
		friend struct Skin;

		// Dll module handle
		HMODULE m_module;

		#define PR_FBX_API(x)\
		x(         , Initialise, Context* (__stdcall*)(ErrorHandler error_cb))\
		x(         , Release, void (__stdcall*)(Context* ctx))\
		\
		x(Scene_   , Load, SceneData* (__stdcall *)(Context& ctx, std::istream& src))\
		x(Scene_   , Save, void (__stdcall *)(Context& ctx, SceneData const& scene, std::ostream& out, char const* format))\
		x(Scene_   , ReadProps, SceneProps (__stdcall *)(Context& ctx, SceneData const& scene))\
		x(Scene_   , Read, void (__stdcall *)(Context& ctx, SceneData& scene, ReadOptions const& options))\
		x(Scene_   , Dump, void (__stdcall *)(Context& ctx, SceneData const& scene, DumpOptions const& options, std::ostream& out))\
		x(Scene_   , MeshGet, Mesh (__stdcall *)(Context& ctx, SceneData const& scene, int i))\
		x(Scene_   , SkeletonGet, Skeleton (__stdcall *)(Context& ctx, SceneData const& scene, int i))\
		x(Scene_   , AnimationGet, Animation (__stdcall *)(Context& ctx, SceneData const& scene, int i))\
		x(Scene_   , MaterialGetById, Material (__stdcall *)(Context& ctx, SceneData const& scene, uint64_t mat_id))\
		x(Scene_   , SkeletonGetById, Skeleton (__stdcall *)(Context& ctx, SceneData const& scene, uint64_t skel_id))

		#define PR_FBX_FUNCTION_MEMBERS(prefix, name, function_type) using prefix##name##Fn = function_type; prefix##name##Fn prefix##name = {};
		PR_FBX_API(PR_FBX_FUNCTION_MEMBERS)
		#undef PR_FBX_FUNCTION_MEMBERS

		Fbx()
			: m_module(win32::LoadDll<struct FbxDll>("fbx.dll"))
		{
			#pragma warning(push)
			#pragma warning(disable: 4191) // 'reinterpret_cast': unsafe conversion from 'FARPROC' to function pointer
			#define PR_FBX_GET_PROC_ADDRESS(prefix, name, function_type) prefix##name = reinterpret_cast<prefix##name##Fn>(GetProcAddress(m_module, "Fbx_" #prefix #name));
			PR_FBX_API(PR_FBX_GET_PROC_ADDRESS)
			#undef PR_FBX_GET_PROC_ADDRESS
			#pragma warning(pop)
		}

		// Singleton Instance
		static Fbx& get()
		{
			static Fbx s_this;
			return s_this;
		}
		static void StaticChecks();
	};

	// A loaded FBX scene
	struct Scene
	{
		// The dll context
		Context* m_ctx;

		// The loaded scene
		SceneData* m_scene;

		// Scene props
		SceneProps m_props;

		// Remember to open streams in binary mode!
		Scene(std::istream& src, ErrorHandler error_cb = {})
			: m_ctx(Fbx::get().Initialise(error_cb))
			, m_scene(Fbx::get().Scene_Load(*m_ctx, src))
			, m_props(Fbx::get().Scene_ReadProps(*m_ctx, *m_scene))
		{}
		Scene(Scene&& rhs) noexcept
			: m_ctx()
			, m_scene()
			, m_props()
		{
			std::swap(m_ctx, rhs.m_ctx);
			std::swap(m_scene, rhs.m_scene);
			std::swap(m_props, rhs.m_props);
		}
		Scene(Scene const&) = delete;
		Scene& operator=(Scene&& rhs) noexcept
		{
			std::swap(m_ctx, rhs.m_ctx);
			std::swap(m_scene, rhs.m_scene);
			std::swap(m_props, rhs.m_props);
			return *this;
		}
		Scene& operator=(Scene const&) = delete;
		~Scene()
		{
			if (m_ctx)
				Fbx::get().Release(m_ctx);
		}

		// Scene global properties
		SceneProps const& props() const
		{
			return m_props;
		}

		// The number of meshes that have been loaded
		int material_count() const
		{
			return m_props.m_material_count;
		}

		// Get a material in the scene
		Material material(uint64_t mat_id) const
		{
			return { Fbx::get().Scene_MaterialGetById(*m_ctx, *m_scene, mat_id) };
		}

		// The number of meshes that have been loaded
		int mesh_count() const
		{
			return m_props.m_mesh_count;
		}

		// Access a mesh by index in the scene
		Mesh mesh(int i) const
		{
			assert(i >= 0 && i < mesh_count());
			return Fbx::get().Scene_MeshGet(*m_ctx, *m_scene, i);
		}

		// Allow range-based for over meshes
		auto meshes() const
		{
			struct Iterator
			{
				Scene const* scene;
				int index;
				void operator++() { ++index; }
				Mesh operator*() const { return scene->mesh(index); }
				bool operator!=(Iterator const& rhs) const { return index != rhs.index; }
			};
			struct MeshRange
			{
				Scene const* scene;
				Iterator begin() const { return { scene, 0 }; }
				Iterator end() const { return { scene, scene->mesh_count() }; }
			};
			return MeshRange{ this };
		}

		// The number of skeletons that have been loaded
		int skeleton_count() const
		{
			return m_props.m_skeleton_count;
		}

		// Access a skeleton by index in the scene
		Skeleton skeleton(int i) const
		{
			assert(i >= 0 && i < skeleton_count());
			return Fbx::get().Scene_SkeletonGet(*m_ctx, *m_scene, i);
		}

		// Allow range-based for over skeletons
		auto skeletons() const
		{
			struct Iterator
			{
				Scene const* scene;
				int index;
				void operator++() { ++index; }
				Skeleton operator*() const { return scene->skeleton(index); }
				bool operator!=(Iterator const& rhs) const { return index != rhs.index; }
			};
			struct SkelRange
			{
				Scene const* scene;
				Iterator begin() const { return { scene, 0 }; }
				Iterator end() const { return { scene, scene->skeleton_count() }; }
			};
			return SkelRange{ this };
		}

		// Access a skeleton in the scene by Id
		Skeleton skeleton(uint64_t skel_id) const
		{
			return { Fbx::get().Scene_SkeletonGetById(*m_ctx, *m_scene, skel_id) };
		}

		// The number of animations in the scene
		int animation_count() const
		{
			return m_props.m_animation_count;
		}

		// Access an Animation by index in the scene
		Animation animation(int i) const
		{
			assert(i >= 0 && i < animation_count());
			return Fbx::get().Scene_AnimationGet(*m_ctx, *m_scene, i);
		}

		// Allow range-based for over animations
		auto animations() const
		{
			struct Iterator
			{
				Scene const* scene;
				int index;
				void operator++() { ++index; }
				Animation operator*() const { return scene->animation(index); }
				bool operator!=(Iterator const& rhs) const { return index != rhs.index; }
			};
			struct AnimRange
			{
				Scene const* scene;
				Iterator begin() const { return { scene, 0 }; }
				Iterator end() const { return { scene, scene->animation_count() }; }
			};
			return AnimRange{ this };
		}

		// Read the full scene into memory
		void Read(ReadOptions const& options)
		{
			// Notes:
			//  - If this is slow, it's probably spending most of the time trianguling the
			//    meshes. Try getting the fbx export tool (e.g. blender) to triangulate on export
			Fbx::get().Scene_Read(*m_ctx, *m_scene, options);
			m_props = Fbx::get().Scene_ReadProps(*m_ctx, *m_scene);
		}

		// Read the full scene into memory
		void Dump(DumpOptions const& options, std::ostream& out)
		{
			// You probably want to read first
			Fbx::get().Scene_Dump(*m_ctx, *m_scene, options, out);
		}
	};
}
