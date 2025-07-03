//********************************
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

	// Metadata in the scene
	struct SceneProps
	{
		// The number of animations in the scene
		int m_animation_stack_count = 0;

		// The animation frame rate
		double m_frame_rate = 0;

		// Scene object counts
		int m_mesh_count = 0;
		int m_material_count = 0;
		int m_skeleton_count = 0;
		int m_animation_count = 0;
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
		x(Scene_   , MaterialGet, Material (__stdcall *)(Context& ctx, SceneData const& scene, uint64_t mat_id))\

		#define PR_FBX_FUNCTION_MEMBERS(prefix, name, function_type) using prefix##name##Fn = function_type; prefix##name##Fn prefix##name = {};
		PR_FBX_API(PR_FBX_FUNCTION_MEMBERS)
		#undef PR_FBX_FUNCTION_MEMBERS

		Fbx()
			: m_module(win32::LoadDll<struct FbxDll>("fbx.dll"))
		{
			#define PR_FBX_GET_PROC_ADDRESS(prefix, name, function_type) prefix##name = ((prefix##name##Fn)GetProcAddress(m_module, "Fbx_"#prefix###name));
			PR_FBX_API(PR_FBX_GET_PROC_ADDRESS)
			#undef PR_FBX_GET_PROC_ADDRESS
		}

		// Singleton Instance
		static Fbx& get()
		{
			static Fbx s_this;
			return s_this;
		}
		static void StaticChecks();
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
		SkinData const* m_skindata;
		uint64_t m_skel_id;
		int m_influence_count;
		int m_max_bones;

		explicit operator bool() const
		{
			return m_skindata != nullptr && m_influence_count != 0;
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
	struct Scene
	{
		// The dll context
		Context* m_ctx;

		// The loaded scene
		SceneData* m_scene;

		// Scene global properties
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
			Fbx::get().Release(m_ctx);
		}

		// The number of meshes that have been loaded
		int material_count() const
		{
			return m_props.m_material_count;
		}

		// Get a material in the scene
		Material material(uint64_t mat_id) const
		{
			return { Fbx::get().Scene_MaterialGet(*m_ctx, *m_scene, mat_id) };
		}

		// The number of meshes that have been loaded
		int mesh_count() const
		{
			return m_props.m_mesh_count;
		}

		// Access a mesh in the scene
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
