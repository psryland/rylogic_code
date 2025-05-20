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
	using ErrorList = std::vector<std::string>;

	struct Vert
	{
		v4 m_vert = {};
		Colour m_colr = {};
		v4 m_norm = {};
		v2 m_tex0 = {};
		int pad[2] = {};

		friend bool operator == (Vert const& lhs, Vert const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(Vert)) == 0;
		}
	};
	struct Bone
	{
		enum class EType { Root, Limb, Effector };
		v4 m_pos;       // Position relative to the parent
		quat m_rot;     // Bone rotation
		EType m_type;   // Bone type
		int m_level;    // Hierarchy level
	};
	struct Nugget
	{
		ETopo m_topo = ETopo::TriList;
		EGeom m_geom = EGeom::Vert;
		Range<int64_t> m_vrange = Range<int64_t>::Reset();
		Range<int64_t> m_irange = Range<int64_t>::Reset();
		uint64_t m_mat_id = 0;
	};
	struct Material
	{
		Colour m_ambient = ColourBlack;
		Colour m_diffuse = ColourWhite;
		Colour m_specular = ColourZero;
		std::filesystem::path m_tex_diff;
	};
	struct Skeleton
	{
		std::vector<std::string> m_names;
		std::vector<Bone> m_bones;

		void reset()
		{
			m_names.resize(0);
			m_bones.resize(0);
		}
	};
	struct Mesh
	{
		using VBuffer = std::vector<Vert>;
		using IBuffer = std::vector<int>;
		using NBuffer = std::vector<Nugget>;

		std::string_view m_name;
		VBuffer m_vbuf;
		IBuffer m_ibuf;
		NBuffer m_nbuf;
		BBox m_bbox;
		int m_level;

		void reset()
		{
			m_name = "";
			m_vbuf.resize(0);
			m_ibuf.resize(0);
			m_nbuf.resize(0);
			m_bbox = BBox::Reset();
			m_level = 0;
		}
	};

	// Interfaces for emitting model parts during 'Read'
	struct IModelOut
	{
		virtual ~IModelOut() = default;

		// Add a material to the output
		virtual void AddMaterial(uint64_t unique_id, Material const& mat) = 0;

		// Add a mesh to the output
		virtual void AddMesh(Mesh const& mesh, m4x4 const& o2p) = 0;
	};
	struct ISkeletonOut
	{
		virtual ~ISkeletonOut() = default;

		// Add a skeleton to the output
		virtual void AddSkeleton(Skeleton const& skeleton) = 0;

		// Outputs a frame of animation
		virtual void AddAnimFrame() = 0;
	};

	// Options for parsing FBXfiles
	struct ReadModelOptions
	{
		// Read all materials from the model, not just the used ones
		bool m_all_materials = false;

		// The animation stack to use
		int m_anim = 0;

		// The time at which to read the transforms
		double m_time_in_seconds = 0;
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
			, ReadSkeleton((Fbx_ReadSkeletonFn)GetProcAddress(m_module, "Fbx_ReadSkeleton"))
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
		using Fbx_ReadScenePropsFn = void(__stdcall*)(fbxsdk::FbxScene const* scene);
		Fbx_ReadScenePropsFn ReadSceneProps;

		// Read the model hierarchy from the scene
		using Fbx_ReadModelFn = void (__stdcall *)(fbxsdk::FbxScene& scene, IModelOut& out, ReadModelOptions const& options);
		Fbx_ReadModelFn ReadModel;
		
		// Read the skeleton from the scene
		using Fbx_ReadSkeletonFn = void(__stdcall*)(fbxsdk::FbxScene& scene, ISkeletonOut& out);
		Fbx_ReadSkeletonFn ReadSkeleton;

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
		struct Props
		{
			int m_animation_stack_count;
		};

		fbxsdk::FbxScene* m_scene;
		Props m_props;

		Scene(std::istream& src)
			: m_scene(Fbx::get().LoadScene(src))
		{
		}
		Scene(Scene&& rhs) noexcept
			: m_scene()
		{
			std::swap(m_scene, rhs.m_scene);
		}
		Scene(Scene const&) = delete;
		Scene& operator=(Scene&& rhs) noexcept
		{
			std::swap(m_scene, rhs.m_scene);
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
			Fbx::get().ReadModel(*m_scene, out, options);
		}
	};
}
