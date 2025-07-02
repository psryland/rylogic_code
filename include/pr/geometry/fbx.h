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

	// Mix-in for range and iteration
	template <typename TDerived, typename TData> struct Iterable
	{
		struct Iter
		{
			TData const* m_ptr;
			Iter& operator++() { ++m_ptr; return *this; }
			TDerived operator*() const { return { m_ptr }; }
			friend bool operator == (Iter const& lhs, Iter const& rhs) { return lhs.m_ptr == rhs.m_ptr; }
		};
		struct Range
		{
			std::span<TData const* const> m_span;
			Iter begin() const { return Iter{ *m_span.begin() }; }
			Iter end() const { return Iter{ *m_span.end() }; }
		};
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

	// Dynamically loaded FBX dll
	class Fbx
	{
		// Fbx is private only, only friends can access the API functions.
		friend struct Scene;
		friend struct Mesh;

		// Dll module handle
		HMODULE m_module;

		#define PR_FBX_API(x)\
		x(Initialise, Context* (__stdcall*)(ErrorHandler error_cb))\
		x(Release, void (__stdcall*)(Context* ctx))
		#define PR_FBX_FUNCTION_MEMBERS(name, function_type) using name##Fn = function_type; name##Fn name = {};
		PR_FBX_API(PR_FBX_FUNCTION_MEMBERS)
		#undef PR_FBX_FUNCTION_MEMBERS

		Fbx()
			: m_module(win32::LoadDll<struct FbxDll>("fbx.dll"))
			, Scene(m_module)
			, Mesh(m_module)
		{
			#define PR_FBX_GET_PROC_ADDRESS(name, function_type) name = ((name##Fn)GetProcAddress(m_module, "Fbx_"#name));
			PR_FBX_API(PR_FBX_GET_PROC_ADDRESS)
			#undef PR_FBX_GET_PROC_ADDRESS
		}

		// Singleton Instance
		static Fbx& get()
		{
			static Fbx s_this;
			return s_this;
		}

		// Scene API
		struct SceneApi
		{
			#define PR_FBX_SCENE_API(x)\
			x(Load, SceneData* (__stdcall *)(Context& ctx, std::istream& src))\
			x(Save, void (__stdcall *)(Context& ctx, SceneData const& scene, std::ostream& out, char const* format))\
			x(ReadProps, SceneProps (__stdcall*)(Context& ctx, SceneData& scene))\
			x(Read, void (__stdcall *)(Context& ctx, SceneData& scene, ReadOptions const& options))\
			x(Dump, void(__stdcall*)(Context& ctx, SceneData const& scene, DumpOptions const& options, std::ostream& out))\
			x(MeshesGet, std::span<MeshData const* const> (__stdcall*)(Context& ctx, SceneData const& scene))
			#define PR_FBX_FUNCTION_MEMBERS(name, function_type) using name##Fn = function_type; name##Fn name = {};
			PR_FBX_SCENE_API(PR_FBX_FUNCTION_MEMBERS)
			#undef PR_FBX_FUNCTION_MEMBERS

			SceneApi(HMODULE hmodule)
			{
				#define PR_FBX_GET_PROC_ADDRESS(name, function_type) name = ((name##Fn)GetProcAddress(hmodule, "Fbx_Scene_"#name));
				PR_FBX_SCENE_API(PR_FBX_GET_PROC_ADDRESS)
				#undef PR_FBX_GET_PROC_ADDRESS
			}
		} Scene;

		// Mesh API
		struct MeshApi
		{
			#define PR_FBX_MESH_API(x)\
			x(IdGet, uint64_t (__stdcall*)(MeshData const& mesh))\
			x(NameGet, char const* (__stdcall*)(MeshData const& mesh))\
			x(VBufferGet, std::span<Vert const> (__stdcall*)(MeshData const& mesh))\
			x(IBufferGet, std::span<int const> (__stdcall*)(MeshData const& mesh))\
			x(NBufferGet, std::span<Nugget const> (__stdcall*)(MeshData const& mesh))\
			x(BBoxGet, BBox const& (__stdcall*)(MeshData const& mesh))\
			x(O2PGet, m4x4 const& (__stdcall*)(MeshData const& mesh))\
			x(LevelGet, int (__stdcall*)(MeshData const& mesh))
			#define PR_FBX_FUNCTION_MEMBERS(name, function_type) using name##Fn = function_type; name##Fn name = {};
			PR_FBX_MESH_API(PR_FBX_FUNCTION_MEMBERS)
			#undef PR_FBX_FUNCTION_MEMBERS

			MeshApi(HMODULE hmodule)
			{
				#define PR_FBX_GET_PROC_ADDRESS(name, function_type) name = ((name##Fn)GetProcAddress(hmodule, "Fbx_Mesh_"#name));
				PR_FBX_MESH_API(PR_FBX_GET_PROC_ADDRESS)
				#undef PR_FBX_GET_PROC_ADDRESS
			}
		} Mesh;
	};

	// Model accessors
	struct Material : Iterable<Material, MaterialData>
	{
		MaterialData const* m_matdata;
	};

	struct Mesh : Iterable<Mesh, MeshData>
	{
		MeshData const* m_meshdata;

		uint64_t id() const
		{
			return Fbx::get().Mesh.IdGet(*m_meshdata);
		}
		std::string_view name() const
		{
			return Fbx::get().Mesh.NameGet(*m_meshdata);
		}
		std::span<Vert const> vbuf() const
		{
			return Fbx::get().Mesh.VBufferGet(*m_meshdata);
		}
		std::span<int const> ibuf() const
		{
			return Fbx::get().Mesh.IBufferGet(*m_meshdata);
		}
		std::span<Nugget const> nbuf() const
		{
			return Fbx::get().Mesh.NBufferGet(*m_meshdata);
		}
		//Skin m_skin;
		BBox const& bbox() const
		{
			return Fbx::get().Mesh.BBoxGet(*m_meshdata);
		}
		m4x4 const& o2p() const
		{
			return Fbx::get().Mesh.O2PGet(*m_meshdata);
		}
		int level() const
		{
			return Fbx::get().Mesh.LevelGet(*m_meshdata);
		}
	};

	// FBX Scene
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
			, m_scene(Fbx::get().Scene.Load(*m_ctx, src))
			, m_props(Fbx::get().Scene.ReadProps(*m_ctx, *m_scene))
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

		Material::Range materials() const
		{
		}

		// Access the meshes in the scene
		Mesh::Range meshes() const
		{
			return { Fbx::get().Scene.MeshesGet(*m_ctx, *m_scene) };
		}

		// Read the full scene into memory
		void Read(ReadOptions const& options)
		{
			// Notes:
			//  - If this is slow, it's probably spending most of the time trianguling the
			//    meshes. Try getting the fbx export tool (e.g. blender) to triangulate on export
			Fbx::get().Scene.Read(*m_ctx, *m_scene, options);
		}

		// Read the full scene into memory
		void Dump(DumpOptions const& options, std::ostream& out)
		{
			// You probably want to read first
			Fbx::get().Scene.Dump(*m_ctx, *m_scene, options, out);
		}
	};
}
