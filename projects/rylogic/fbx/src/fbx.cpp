﻿//********************************
// FBX Model loader
//  Copyright (c) Rylogic Ltd 2014
//********************************
#include <concepts>
#include <memory>
#include <string>
#include <vector>
#include <unordered_map>
#include <string_view>
#include <functional>
#include <execution>
#include <atomic>
#include <mutex>

#include <Windows.h>

#include "ufbx/ufbx.h"
#include "ufbx/extra/ufbx_os.h"

#include "pr/common/to.h"
#include "pr/common/cast.h"
#include "pr/geometry/fbx.h"
#include "pr/container/vector.h"

using namespace pr;
using namespace pr::geometry::fbx;

namespace pr
{
	template <typename T> requires (requires (T t) { t == nullptr; })
	static constexpr T NullCheck(T ptr, std::string_view msg)
	{
		if (!(ptr == nullptr)) return ptr;
		throw std::runtime_error(std::string(msg));
	}

	// Convert to ufbx
	template <> struct Convert<ufbx_vec2, v2>
	{
		constexpr static ufbx_vec2 To_(v2_cref v)
		{
			ufbx_vec2 r = {};
			r.x = s_cast<ufbx_real>(v.x);
			r.y = s_cast<ufbx_real>(v.y);
			return r;
		}
	};
	template <> struct Convert<ufbx_vec3, v3>
	{
		constexpr static ufbx_vec3 To_(v3_cref v)
		{
			ufbx_vec3 r = {};
			r.x = s_cast<ufbx_real>(v.x);
			r.y = s_cast<ufbx_real>(v.y);
			r.z = s_cast<ufbx_real>(v.z);
			return r;
		}
	};
	template <> struct Convert<ufbx_vec3, v4>
	{
		constexpr static ufbx_vec3 To_(v4_cref v)
		{
			ufbx_vec3 r = {};
			r.x = s_cast<ufbx_real>(v.x);
			r.y = s_cast<ufbx_real>(v.y);
			r.z = s_cast<ufbx_real>(v.z);
			return r;
		}
	};
	template <> struct Convert<ufbx_quat, quat>
	{
		constexpr static ufbx_quat To_(quat_cref v)
		{
			ufbx_quat r = {};
			r.x = s_cast<ufbx_real>(v.x);
			r.y = s_cast<ufbx_real>(v.y);
			r.z = s_cast<ufbx_real>(v.z);
			r.w = s_cast<ufbx_real>(v.w);
			return r;
		}
	};
	template <> struct Convert<ufbx_matrix, m4x4>
	{
		constexpr static ufbx_matrix To_(m4x4 const& v)
		{
			ufbx_matrix r = {};
			r.cols[0] = To<ufbx_vec3>(v.x);
			r.cols[1] = To<ufbx_vec3>(v.y);
			r.cols[2] = To<ufbx_vec3>(v.z);
			r.cols[3] = To<ufbx_vec3>(v.w);
			return r;
		}
	};
	template <> struct Convert<ufbx_transform, Transform>
	{
		constexpr static ufbx_transform To_(Transform const& x)
		{
			return ufbx_transform{
				.translation = To<ufbx_vec3>(x.translation),
				.rotation = To<ufbx_quat>(x.rotation),
				.scale = To<ufbx_vec3>(x.scale),
			};
		}
	};
	template <> struct Convert<ufbx_string, std::string_view>
	{
		constexpr static ufbx_string To_(std::string_view sv)
		{
			return ufbx_string{ .data = sv.data(), .length = sv.size() };
		}
	};
	template <> struct Convert<ufbx_coordinate_axis, ECoordAxis>
	{
		constexpr static ufbx_coordinate_axis To_(ECoordAxis x)
		{
			switch (x)
			{
				case ECoordAxis::PosX: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_POSITIVE_X;
				case ECoordAxis::NegX: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_NEGATIVE_X;
				case ECoordAxis::PosY: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_POSITIVE_Y;
				case ECoordAxis::NegY: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_NEGATIVE_Y;
				case ECoordAxis::PosZ: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_POSITIVE_Z;
				case ECoordAxis::NegZ: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_NEGATIVE_Z;
				case ECoordAxis::Unknown: return ufbx_coordinate_axis::UFBX_COORDINATE_AXIS_UNKNOWN;
				default: throw std::runtime_error("Unknown enum value");
			}
		}
	};
	template <> struct Convert<ufbx_space_conversion, ESpaceConversion>
	{
		constexpr static ufbx_space_conversion To_(ESpaceConversion x)
		{
			switch (x)
			{
				case ESpaceConversion::TransformRoot: return ufbx_space_conversion::UFBX_SPACE_CONVERSION_TRANSFORM_ROOT;
				case ESpaceConversion::AdjustTransforms: return ufbx_space_conversion::UFBX_SPACE_CONVERSION_ADJUST_TRANSFORMS;
				case ESpaceConversion::ModifyGeometry: return ufbx_space_conversion::UFBX_SPACE_CONVERSION_MODIFY_GEOMETRY;
				default: throw std::runtime_error("Unknown enum value");
			}
		}
	};
	template <> struct Convert<ufbx_pivot_handling, EPivotHandling>
	{
		constexpr static ufbx_pivot_handling To_(EPivotHandling x)
		{
			switch (x)
			{
				case EPivotHandling::Retain: return ufbx_pivot_handling::UFBX_PIVOT_HANDLING_RETAIN;
				case EPivotHandling::AdjustToPivot: return ufbx_pivot_handling::UFBX_PIVOT_HANDLING_ADJUST_TO_PIVOT;
				case EPivotHandling::AdjustToRotationPivot: return ufbx_pivot_handling::UFBX_PIVOT_HANDLING_ADJUST_TO_ROTATION_PIVOT;
				default: throw std::runtime_error("Unknown enum value");
			}
		}
	};
	template <> struct Convert<ufbx_mirror_axis, EMirrorAxis>
	{
		constexpr static ufbx_mirror_axis To_(EMirrorAxis x)
		{
			switch (x)
			{
				case EMirrorAxis::None: return ufbx_mirror_axis::UFBX_MIRROR_AXIS_NONE;
				case EMirrorAxis::X: return ufbx_mirror_axis::UFBX_MIRROR_AXIS_X;
				case EMirrorAxis::Y: return ufbx_mirror_axis::UFBX_MIRROR_AXIS_Y;
				case EMirrorAxis::Z: return ufbx_mirror_axis::UFBX_MIRROR_AXIS_Z;
				default: throw std::runtime_error("Unknown enum value");
			}
		}
	};
	template <> struct Convert<ufbx_coordinate_axes, CoordAxes>
	{
		constexpr static ufbx_coordinate_axes To_(CoordAxes x)
		{
			return ufbx_coordinate_axes{
				.right = To<ufbx_coordinate_axis>(x.right),
				.up = To<ufbx_coordinate_axis>(x.up),
				.front = To<ufbx_coordinate_axis>(x.front),
			};
		}
	};
	template <> struct Convert<ufbx_load_opts, LoadOptions>
	{
		static ufbx_load_opts To_(LoadOptions const& x)
		{
			return ufbx_load_opts {
				._begin_zero = 0,

				// Preferences
				.ignore_geometry = x.ignore_geometry,
				.ignore_animation = x.ignore_animation,
				.ignore_embedded = x.ignore_embedded,
				.ignore_all_content = x.ignore_all_content,

				.evaluate_skinning = x.evaluate_skinning,
				.evaluate_caches = x.evaluate_caches,

				.load_external_files = x.load_external_files,
				.ignore_missing_external_files = x.ignore_missing_external_files,
				.skip_skin_vertices = x.skip_skin_vertices,
				.skip_mesh_parts = x.skip_mesh_parts,
				.clean_skin_weights = x.clean_skin_weights,
				.use_blender_pbr_material = x.use_blender_pbr_material,
				.disable_quirks = x.disable_quirks,
				.strict = x.strict,
				.force_single_thread_ascii_parsing = x.force_single_thread_ascii_parsing,

				.connect_broken_elements = x.connect_broken_elements,
				.allow_nodes_out_of_root = x.allow_nodes_out_of_root,
				.allow_missing_vertex_position = x.allow_missing_vertex_position,
				.allow_empty_faces = x.allow_empty_faces,
				.generate_missing_normals = x.generate_missing_normals,
				.open_main_file_with_default = x.open_main_file_with_default,
				.path_separator = x.path_separator,

				.node_depth_limit = x.node_depth_limit,
				.file_size_estimate = x.file_size_estimate,
				.read_buffer_size = x.read_buffer_size,

				.filename = To<ufbx_string>(x.filename),

				.space_conversion = To<ufbx_space_conversion>(x.space_conversion),
				.pivot_handling = To<ufbx_pivot_handling>(x.pivot_handling),
				.pivot_handling_retain_empties = x.pivot_handling_retain_empties,
				.handedness_conversion_axis = To<ufbx_mirror_axis>(x.handedness_conversion_axis),
				.handedness_conversion_retain_winding = x.handedness_conversion_retain_winding,
				.reverse_winding = x.reverse_winding,
				.target_axes = To<ufbx_coordinate_axes>(x.target_axes),
				.target_unit_meters = s_cast<ufbx_real>(x.target_unit_meters),
				.target_camera_axes = To<ufbx_coordinate_axes>(x.target_camera_axes),
				.target_light_axes = To<ufbx_coordinate_axes>(x.target_light_axes),

				.normalize_normals = x.normalize_normals,
				.normalize_tangents = x.normalize_tangents,
				.use_root_transform = x.use_root_transform,
				.root_transform = To<ufbx_transform>(x.root_transform),

				.key_clamp_threshold = x.key_clamp_threshold,
				._end_zero = 0,
			};
		}
	};
	template <> struct Convert<std::string, ufbx_error>
	{
		static std::string To_(ufbx_error const& error)
		{
			std::string err(UFBX_ERROR_INFO_LENGTH, '\0');
			err.resize(ufbx_format_error(err.data(), err.size(), &error));
			return err;
		}
		static std::string To_(ufbx_error const& error, std::string_view msg)
		{
			std::string err;
			err.reserve(msg.size() + 1 + UFBX_ERROR_INFO_LENGTH);
			err.append(msg).append(" ");
			
			auto ofs = err.size();
			err.append(UFBX_ERROR_INFO_LENGTH, '\0');
			err.resize(ofs + ufbx_format_error(err.data() + ofs, err.size() - ofs, &error));
			return err;
		}
	};

	// Convert from ufbx
	template <> struct Convert<v2, ufbx_vec2>
	{
		constexpr static v2 To_(ufbx_vec2 const& v)
		{
			return v2{
				s_cast<float>(v.x),
				s_cast<float>(v.y),
			};
		}
	};
	template <> struct Convert<v3, ufbx_vec3>
	{
		constexpr static v3 To_(ufbx_vec3 const& v)
		{
			return v3{
				s_cast<float>(v.x),
				s_cast<float>(v.y),
				s_cast<float>(v.z),
			};
		}
	};
	template <> struct Convert<v4, ufbx_vec3>
	{
		constexpr static v4 To_(ufbx_vec3 const& v, float w)
		{
			return v4{
				s_cast<float>(v.x),
				s_cast<float>(v.y),
				s_cast<float>(v.z),
				w,
			};
		}
	};
	template <> struct Convert<v4, ufbx_vec4>
	{
		constexpr static v4 To_(ufbx_vec4 const& v)
		{
			return v4{
				s_cast<float>(v.x),
				s_cast<float>(v.y),
				s_cast<float>(v.z),
				s_cast<float>(v.w),
			};
		}
	};
	template <> struct Convert<quat, ufbx_quat>
	{
		constexpr static quat To_(ufbx_quat const& v)
		{
			return quat{
				s_cast<float>(v.x),
				s_cast<float>(v.y),
				s_cast<float>(v.z),
				s_cast<float>(v.w),
			};
		}
	};
	template <> struct Convert<m4x4, ufbx_matrix>
	{
		constexpr static m4x4 To_(ufbx_matrix const& v)
		{
			return m4x4{
				To<v4>(v.cols[0], 0.f),
				To<v4>(v.cols[1], 0.f),
				To<v4>(v.cols[2], 0.f),
				To<v4>(v.cols[3], 1.f),
			};
		}
	};
	template <> struct Convert<std::string_view, ufbx_string>
	{
		constexpr static std::string_view To_(ufbx_string sv)
		{
			return std::string_view(sv.data, sv.length);
		}
	};
	template <> struct Convert<Colour, ufbx_vec4>
	{
		constexpr static Colour To_(ufbx_vec4 const& v)
		{
			return Colour{
				s_cast<float>(v.x),
				s_cast<float>(v.y),
				s_cast<float>(v.z),
				s_cast<float>(v.w),
			};
		}
	};
}
namespace ufbx
{
	// Initialize ufbx thread pool from OS thread pool
	struct thread_pool
	{
		ufbx_os_thread_pool_opts m_opts;
		std::shared_ptr<ufbx_os_thread_pool> m_os_pool;
		ufbx_thread_pool m_pool;

		thread_pool(int max_threads = 0) // 0 means auto detect
			: m_opts({ 0, static_cast<size_t>(max_threads), 0 })
			, m_os_pool(NullCheck(ufbx_os_create_thread_pool(&m_opts), "Failed to create thread pool"), ufbx_os_free_thread_pool)
			, m_pool()
		{
			ufbx_os_init_ufbx_thread_pool(&m_pool, m_os_pool.get());
		}
		operator ufbx_thread_pool() const
		{
			return m_pool;
		}
	};
}
namespace pr::geometry::fbx
{
	// Notes:
	//  - 'element_id' in ufbx is the index of the element in the list of all elements of all types.
	//  - 'typed_id' in ufbx is the index of the element in the list of elements of that type

	struct MeshNode
	{
		ufbx_mesh* mesh;
		ufbx_mesh const* root;
		int level;
		int index;
	};
	struct BoneNode
	{
		ufbx_bone* bone;
		ufbx_bone const* root;
		int level;
		int index;
	};
	static constexpr int NoIndex = -1;
	static constexpr Vert NoVert = { .m_idx0 = {NoIndex, 0} };
	using SceneDataPtr = std::unique_ptr<SceneData>;
	using MeshNodeMap = std::unordered_map<uint64_t, MeshNode>; // Map from mesh id to mesh
	using BoneNodeMap = std::unordered_map<uint64_t, BoneNode>; // Map from bone id to bone

	// FBX file input stream
	struct IStream : ufbx_stream
	{
		std::istream& m_src;

		IStream(std::istream& src)
			: m_src(src)
		{
			if (!src.good())
				throw std::runtime_error("FBX input stream is unhealthy");

			user = this;
			read_fn = &Read;
			skip_fn = &Skip;
			size_fn = &Size;
			close_fn = &Close;
		}
		IStream(IStream&&) = delete;
		IStream(IStream const&) = delete;
		IStream& operator =(IStream&&) = delete;
		IStream& operator =(IStream const&) = delete;

		// Try to read up to `size` bytes to `data`, return the amount of read bytes. Return `SIZE_MAX` to indicate an IO error.
		static size_t Read(void* ctx, void* data, size_t size)
		{
			auto& src = static_cast<IStream*>(ctx)->m_src;
			auto bytes_read = src.read(char_ptr(data), size).gcount();
			return src.bad() ? SIZE_MAX : bytes_read;
		}

		// Skip `size` bytes in the file.
		static bool Skip(void* ctx, size_t size)
		{
			auto& src = static_cast<IStream*>(ctx)->m_src;
			src.seekg(size, std::ios::cur);
			return src.good();
		}

		// Get the size of the file. Return `0` if unknown, `UINT64_MAX` if error.
		static uint64_t Size(void* ctx)
		{
			// 'src' might be a network stream
			auto& src = static_cast<IStream*>(ctx)->m_src;
			return src.good() ? 0 : UINT64_MAX;
		}

		// Close the file
		static void Close(void*)
		{
		}
	};

	// Model data types
	struct MaterialData
	{
		Colour m_ambient = ColourBlack;
		Colour m_diffuse = ColourWhite;
		Colour m_specular = ColourZero;
		std::string m_tex_diff;
	};
	struct SkinData
	{
		uint32_t m_skel_id = NoId;
		vector<int> m_offsets;    // Index offset to the first bone for each vertex
		vector<uint32_t> m_bones; // The Ids of the bones that influence a vertex
		vector<float> m_weights;  // The influence weights

		void Reset()
		{
			m_skel_id = NoId;
			m_offsets.resize(0);
			m_bones.resize(0);
			m_weights.resize(0);
		}
		operator Skin() const
		{
			return Skin{
				.m_skel_id = m_skel_id,
				.m_offsets = m_offsets,
				.m_bones = m_bones,
				.m_weights = m_weights,
			};
		}
	};
	struct SkeletonData
	{
		// Notes:
		//  - Skeletons can have multiple root bones. Check for a 'm_hierarchy[i] == 0' values
		uint32_t m_skel_id = NoId;   // Skeleton Id (= the node id that contains the root bone, because skeletons can instance bones)
		vector<uint32_t> m_bone_ids; // Bone unique ids
		vector<std::string> m_names; // Bone names
		vector<m4x4> m_o2bp;         // Inverse of the bind-pose to root-object-space transform for each bone
		vector<int> m_hierarchy;     // Hierarchy levels. level == 0 are root bones.

		void Reset()
		{
			m_skel_id = NoId;
			m_bone_ids.resize(0);
			m_names.resize(0);
			m_o2bp.resize(0);
			m_hierarchy.resize(0);
		}
		operator Skeleton() const
		{
			assert(
				isize(m_bone_ids) == isize(m_names) &&
				isize(m_names) == isize(m_o2bp) &&
				isize(m_o2bp) == isize(m_hierarchy)
			);

			return Skeleton{
				.m_skel_id = m_skel_id,
				.m_bone_ids = m_bone_ids,
				.m_names = m_names,
				.m_o2bp = m_o2bp,
				.m_hierarchy = m_hierarchy,
			};
		}
	};
	struct AnimationData
	{
		uint32_t m_skel_id = NoId;      // The skeleton that this animation should be used with
		double m_duration = 0;          // The length (in seconds) of the animation
		double m_frame_rate = 24.0;     // The native frame rate of the animation
		vector<uint32_t, 0> m_bone_map; // The bone id for each track. Length = bone count.
		vector<quat, 0> m_rotation;     // Frames of bone rotations
		vector<v3, 0> m_position;       // Frames of bone positions
		vector<v3, 0> m_scale;          // Frames of bone scales

		void Reset()
		{
			m_skel_id = NoId;
			m_duration = 0.0;
			m_frame_rate = 24.0;
			m_bone_map.resize(0);
			m_rotation.resize(0);
			m_position.resize(0);
			m_scale.resize(0);
		}
		operator Animation() const
		{
			return Animation{
				.m_skel_id = m_skel_id,
				.m_duration = m_duration,
				.m_frame_rate = m_frame_rate,
				.m_bone_map = m_bone_map,
				.m_rotation = m_rotation,
				.m_position = m_position,
				.m_scale = m_scale,
			};
		}
	};
	struct MeshData
	{
		uint32_t m_mesh_id = NoId;
		std::string m_name;
		vector<Vert> m_vbuf;
		vector<int> m_ibuf;
		vector<Nugget> m_nbuf;
		SkinData m_skin = {};
		BBox m_bbox = {};

		void Reset()
		{
			m_mesh_id = NoId;
			m_name.resize(0);
			m_vbuf.resize(0);
			m_ibuf.resize(0);
			m_nbuf.resize(0);
			m_skin.Reset();
			m_bbox = BBox::Reset();
		}
		operator Mesh() const
		{
			return Mesh{
				.m_mesh_id = m_mesh_id,
				.m_name = m_name,
				.m_vbuf = m_vbuf,
				.m_ibuf = m_ibuf,
				.m_nbuf = m_nbuf,
				.m_skin = Skin{
					.m_skel_id = m_skin.m_skel_id,
					.m_offsets = m_skin.m_offsets,
					.m_bones = m_skin.m_bones,
					.m_weights = m_skin.m_weights,
				},
				.m_bbox = m_bbox,
			};
		}
	};

	// Loaded scene data
	struct SceneData
	{
		// Notes:
		//  - Root nodes for meshes, skeletons can occur at any level.
		//  - Any mesh/skeleton node whose parent is not a mesh/skeleton
		//    node is the start of a new mesh/skeleton hierarchy.
		std::shared_ptr<ufbx_scene> m_fbxscene;
		SceneData(ufbx_scene* scene)
			: m_fbxscene(scene, ufbx_free_scene)
		{
			//TraverseHierarchy(*m_fbxscene, EParts::All, m_mesh_node_map, m_bone_node_map);
			/*
			// Create a list of the root mesh names
			for (auto const& [id, meshnode] : m_mesh_node_map)
			{
				if (meshnode.mesh != meshnode.root) continue;
				m_mesh_names.push_back(meshnode.root->GetNode()->GetName());
			}

			// Create a list of the root bone names
			for (auto const& [id, bonenode] : m_bone_node_map)
			{
				if (bonenode.bone != bonenode.root) continue;
				m_skel_names.push_back(bonenode.root->GetNode()->GetName());
			}

			// Read properties from the scene
			m_props = SceneProps{
				.m_animation_stack_count = m_fbxscene->GetSrcObjectCount<FbxAnimStack>(),
				.m_frame_rate = FbxTime::GetFrameRate(m_fbxscene->GetGlobalSettings().GetTimeMode()),
				.m_material_available = m_fbxscene->GetMaterialCount(),
				.m_meshes_available = s_cast<int>(std::ranges::count_if(m_mesh_node_map, [](auto const& x) { return x.second.mesh == x.second.root; })),
				.m_skeletons_available = s_cast<int>(std::ranges::count_if(m_bone_node_map, [](auto const& x) { return x.second.bone == x.second.root; })),
				.m_animations_available = m_fbxscene->GetSrcObjectCount<FbxAnimStack>(),
				.m_material_count = 0,
				.m_mesh_count = 0,
				.m_skeleton_count = 0,
				.m_animation_count = 0,
				.m_mesh_names = m_mesh_names,
				.m_skel_names = m_skel_names,
			};
			*/
		}
	};

	// Read data from a scene and output it to the caller
	struct Reader
	{
		struct Influence
		{
			vector<uint32_t, 8> m_bones; // Ids
			vector<float, 8> m_weights;
		};
		using Materials = vector<Material, 2>;
		using Skeletons = vector<SkeletonData, 1>;

		ufbx_scene const& m_fbxscene;
		ReadOptions const& m_opts;
		IReadOutput& m_out;

		// Cache
		MeshData m_mesh;
		Materials m_materials;
		Skeletons m_skeletons;
		vector<int> m_vlookup;
		vector<uint32_t> m_tri_indices;
		vector<Influence> m_influences;

		Reader(SceneData const& scene, ReadOptions const& opts, IReadOutput& out)
			: m_fbxscene(*scene.m_fbxscene)
			, m_opts(opts)
			, m_out(out)
			, m_mesh()
			, m_materials()
			, m_skeletons()
			, m_vlookup()
			, m_tri_indices()
			, m_influences()
		{
			// Add a default material
			m_materials.push_back({});
		}

		// Read the scene
		void Do()
		{
			if (AllSet(m_opts.m_parts, EParts::Materials))
				ReadMaterials();
			if (AllSet(m_opts.m_parts, EParts::Skeletons))
				ReadSkeletons();
			if (AllSet(m_opts.m_parts, EParts::Meshes))
				ReadGeometry();
			if (AllSet(m_opts.m_parts, EParts::Animation))
				ReadAnimation();
		}

		// Read the materials
		void ReadMaterials()
		{
			// If the scene doesn't contain materials, just add a default one
			if (m_fbxscene.materials.count == 0)
			{
				m_materials.resize(1, {});
				return;
			}

			// Materials require a lot more work. For now, just use diffuse colour.
			// Textures have wrapping modes and transforms etc...

			// Parse the scene materials
			m_materials.resize(0);
			m_materials.reserve(m_fbxscene.materials.count);
			for (auto const& m : m_fbxscene.materials)
			{
				Progress(1LL + (&m - m_fbxscene.materials.begin()), m_fbxscene.materials.count, "Reading materials...");

				Material mat = {};
				switch (m->shader_type)
				{
					// Unknown shading model
					case ufbx_shader_type::UFBX_SHADER_UNKNOWN:
					{
						break;
					}
					// FBX builtin diffuse material
					case ufbx_shader_type::UFBX_SHADER_FBX_LAMBERT:
					{
						mat.m_ambient = To<Colour>(m->fbx.ambient_color.value_vec4);
						mat.m_diffuse = To<Colour>(m->fbx.diffuse_color.value_vec4);
						mat.m_specular = To<Colour>(m->fbx.specular_color.value_vec4);
						break;
					}
					// FBX builtin diffuse+specular material
					case ufbx_shader_type::UFBX_SHADER_FBX_PHONG:
					{
						mat.m_ambient = To<Colour>(m->fbx.ambient_color.value_vec4);
						mat.m_diffuse = To<Colour>(m->fbx.diffuse_color.value_vec4);
						mat.m_specular = To<Colour>(m->fbx.specular_color.value_vec4);
						break;
					}
					// Open Shading Language standard surface
					// https://github.com/Autodesk/standard-surface
					case ufbx_shader_type::UFBX_SHADER_OSL_STANDARD_SURFACE:
					{
						break;
					}
					// Arnold standard surface
					// https://docs.arnoldrenderer.com/display/A5AFMUG/Standard+Surface
					case ufbx_shader_type::UFBX_SHADER_ARNOLD_STANDARD_SURFACE:
					{
						break;
					}
					// 3ds Max Physical Material
					// https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2022/ENU/3DSMax-Lighting-Shading/files/GUID-C1328905-7783-4917-AB86-FC3CC19E8972-htm.html
					case ufbx_shader_type::UFBX_SHADER_3DS_MAX_PHYSICAL_MATERIAL:
					{
						break;
					}
					// 3ds Max PBR (Metal/Rough) material
					// https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2021/ENU/3DSMax-Lighting-Shading/files/GUID-A16234A5-6500-4662-8B20-A5EC9FE1B255-htm.html
					case ufbx_shader_type::UFBX_SHADER_3DS_MAX_PBR_METAL_ROUGH:
					{
						break;
					}
					// 3ds Max PBR (Spec/Gloss) material
					// https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2021/ENU/3DSMax-Lighting-Shading/files/GUID-18087194-B2A6-43EF-9B80-8FD1736FAE52-htm.html
					case ufbx_shader_type::UFBX_SHADER_3DS_MAX_PBR_SPEC_GLOSS:
					{
						break;
					}
					// 3ds glTF Material
					// https://help.autodesk.com/view/3DSMAX/2023/ENU/?guid=GUID-7ABFB805-1D9F-417E-9C22-704BFDF160FA
					case ufbx_shader_type::UFBX_SHADER_GLTF_MATERIAL:
					{
						break;
					}
					// 3ds OpenPBR Material
					// https://help.autodesk.com/view/3DSMAX/2025/ENU/?guid=GUID-CD90329C-1E2B-4BBA-9285-3BB46253B9C2
					case ufbx_shader_type::UFBX_SHADER_OPENPBR_MATERIAL:
					{
						break;
					}
					// Stingray ShaderFX shader graph.
					// Contains a serialized `"ShaderGraph"` in `ufbx_props`.
					case ufbx_shader_type::UFBX_SHADER_SHADERFX_GRAPH:
					{
						break;
					}
					// Variation of the FBX phong shader that can recover PBR properties like
					// `metalness` or `roughness` from the FBX non-physical values.
					// NOTE: Enable `ufbx_load_opts.use_blender_pbr_material`.
					case ufbx_shader_type::UFBX_SHADER_BLENDER_PHONG:
					{
						break;
					}
					// Wavefront .mtl format shader (used by .obj files)
					case ufbx_shader_type::UFBX_SHADER_WAVEFRONT_MTL:
					{
						break;
					}
					default:
					{
						break;
					}
				}
				m_materials.push_back(mat);
			}
		}

		// Read meshes from the FBX scene
		void ReadGeometry()
		{
			size_t mesh_nodes = 0;

			// Meshes are in a separate list in the fbx scene. The nodes contain instances of the meshes.
			// Output each mesh to the caller, then output a tree with references to the meshes plus a transform.
			for (auto const* fbxmesh : m_fbxscene.meshes)
			{
				ReadMesh(*fbxmesh);
				m_out.CreateMesh(m_mesh, m_materials);
				mesh_nodes += fbxmesh->instances.count;
			}

			vector<MeshTree> mesh_tree;
			mesh_tree.reserve(mesh_nodes);

			// Build a mesh tree for each mesh root
			auto roots = FindRoots(m_fbxscene.meshes, IsMeshRoot);
			for (auto const*& root : roots)
			{
				Progress(1 + (&root - roots.data()), ssize(roots), "Reading models...");

				// Walk the node hierarchy and build the mesh tree
				WalkHierarchy(root, [&mesh_tree](ufbx_node const* node) -> bool
				{
					if (node->mesh == nullptr)
						return false;

					auto level = s_cast<int>(node->node_depth - MeshRoot(node)->node_depth);
					mesh_tree.push_back(MeshTree{
						.m_o2p = level == 0 ? To<m4x4>(node->node_to_world) : To<m4x4>(node->node_to_parent),
						.m_name = To<std::string_view>(node->name),
						.m_mesh_id = node->mesh->typed_id,
						.m_level = level,
					});
					return true;
				});
			}

			// Output the full model hierarchy
			m_out.CreateModel(mesh_tree);
		}

		// Read ufbx mesh data
		void ReadMesh(ufbx_mesh const& fbxmesh)
		{
			// Notes:
			//  - "ufbx_part" ~= Nugget
			auto& mesh = m_mesh;
			auto& vlookup = m_vlookup;
			auto& tri_indices = m_tri_indices;

			// Count the size of the buffers needed
			size_t icount = 0;
			size_t ncount = 0;
			for (size_t pi = 0; pi != fbxmesh.material_parts.count; ++pi)
			{
				ufbx_mesh_part const& mesh_part = fbxmesh.material_parts[pi];
				if (mesh_part.num_triangles == 0)
					continue;

				ncount += 1;
				icount += mesh_part.num_triangles * 3;
			}

			// Reserve space in the mesh data
			mesh.Reset();
			mesh.m_mesh_id = fbxmesh.typed_id;
			mesh.m_name = To<std::string_view>(fbxmesh.name);
			mesh.m_vbuf.reserve(icount / 2); // Just a guess
			mesh.m_ibuf.reserve(icount);
			mesh.m_nbuf.reserve(ncount);
			vlookup.resize(0);
			vlookup.reserve(icount);
			tri_indices.resize(fbxmesh.max_face_triangles * 3);

			// Add a vertex to 'm_vbuf' and return its index.
			auto AddVert = [&mesh, &vlookup](int src_vidx, v4 const& pos, Colour const& col, v4 const& norm, v2 const& uv) -> int
			{
				Vert v = {
					.m_vert = pos,
					.m_colr = col,
					.m_norm = norm,
					.m_tex0 = uv,
					.m_idx0 = {src_vidx, 0},
				};

				// 'vlookup' is a linked list (within an array) of vertices that are permutations of 'src_idx'
				for (int vidx = src_vidx;;)
				{
					// If 'vidx' is outside the buffer, add it
					auto vbuf_count = isize(mesh.m_vbuf);
					if (vidx >= vbuf_count)
					{
						// Note: This can leave "dead" verts in the buffer, but typically
						// there shouldn't be many of these, and no indices should reference them.
						mesh.m_vbuf.resize(std::max(vbuf_count, vidx + 1), NoVert);
						vlookup.resize(std::max(vbuf_count, vidx + 1), NoIndex);
						mesh.m_vbuf[vidx] = v;
						vlookup[vidx] = NoIndex;
						return vidx;
					}

					// If 'v' is already in the buffer, use it's index
					if (mesh.m_vbuf[vidx] == v)
					{
						return vidx;
					}

					// If the position 'vidx' is an unused slot, use it
					if (mesh.m_vbuf[vidx] == NoVert)
					{
						mesh.m_vbuf[vidx] = v;
						return vidx;
					}

					// If there is no "next", prepare to insert it at the end
					if (vlookup[vidx] == NoIndex)
					{
						vlookup[vidx] = vbuf_count;
					}

					// Go to the next vertex to check
					vidx = vlookup[vidx];
				}
			};

			// Get or add a nugget
			auto GetOrAddNugget = [&mesh](uint32_t mat_id) -> Nugget&
			{
				for (auto& n : mesh.m_nbuf)
				{
					if (n.m_mat_id != mat_id) continue;
					return n;
				}
				mesh.m_nbuf.push_back(Nugget{ .m_mat_id = mat_id });
				return mesh.m_nbuf.back();
			};

			// Create a nugget per material.
			for (size_t pi = 0; pi != fbxmesh.material_parts.count; ++pi)
			{
				// `ufbx_mesh_part` contains a handy compact list of faces that use the material which we use here.
				ufbx_mesh_part const& mesh_part = fbxmesh.material_parts[pi];
				if (mesh_part.num_triangles == 0)
					continue;

				assert(m_materials.size() != 0 && "There should be a default material if no materials have been loaded");
				auto mat_id = Clamp<uint32_t>(mesh_part.index, 0, s_cast<uint32_t>(m_materials.size() - 1));

				Nugget nugget = {};
				nugget.m_mat_id = mat_id;
				nugget.m_topo = ETopo::TriList;
				nugget.m_geom = EGeom::Vert |
					(fbxmesh.vertex_color.exists ? EGeom::Colr : EGeom::None) |
					(fbxmesh.vertex_normal.exists ? EGeom::Norm : EGeom::None) |
					(fbxmesh.vertex_uv.exists ? EGeom::Tex0 : EGeom::None);

				// "Inflate" the verts into a unique list of each required combination
				for (size_t fi = 0; fi < mesh_part.num_faces; fi++)
				{
					ufbx_face const& face = fbxmesh.faces[mesh_part.face_indices[fi]];
					auto num_tris = ufbx_triangulate_face(tri_indices.data(), tri_indices.size(), &fbxmesh, face);

					// Iterate through every vertex of every triangle in the triangulated result
					for (size_t vi = 0; vi != num_tris * 3; ++vi)
					{
						auto ix = tri_indices[vi];
						auto vidx = fbxmesh.vertex_indices[ix];
						auto vert = To<v4>(ufbx_get_vertex_vec3(&fbxmesh.vertex_position, ix), 1.0f);
						auto colr = fbxmesh.vertex_color.exists ? To<Colour>(ufbx_get_vertex_vec4(&fbxmesh.vertex_color, ix)) : ColourWhite;
						auto norm = fbxmesh.vertex_normal.exists ? To<v4>(ufbx_get_vertex_vec3(&fbxmesh.vertex_normal, ix), 0.0f) : v4::Zero();
						auto tex0 = fbxmesh.vertex_uv.exists ? To<v2>(ufbx_get_vertex_vec2(&fbxmesh.vertex_uv, ix)) : v2::Zero();
						auto idx0 = iv2{ s_cast<int>(vidx), 0 };

						vidx = AddVert(vidx, vert, colr, norm, tex0);
						mesh.m_ibuf.push_back(vidx);

						nugget.m_vrange.grow(vidx);
						nugget.m_irange.grow(isize(mesh.m_ibuf) - 1);
					}
				}

				// Add the nugget
				mesh.m_nbuf.push_back(nugget);
			}

			// Compute the bounding box
			for (auto const& v : mesh.m_vbuf)
			{
				if (v == NoVert) continue;
				mesh.m_bbox.Grow(v.m_vert);
			}

			// Read the skinning data for this mesh
			if (AllSet(m_opts.m_parts, EParts::Skins))
				ReadSkin(fbxmesh);
		}

		// Read the skin data for 'fbxmesh'
		void ReadSkin(ufbx_mesh const& fbxmesh)
		{
			auto& skin = m_mesh.m_skin;
			auto& influences = m_influences;
			influences.resize(fbxmesh.num_vertices);
			skin.Reset();

			ufbx_node const* root = nullptr;

			// Get the skinning data for this mesh
			for (size_t  d = 0; d != fbxmesh.skin_deformers.count; ++d)
			{
				auto const& fbxskin = *fbxmesh.skin_deformers[d];
				for (size_t c = 0; c != fbxskin.clusters.count; ++c)
				{
					auto const& cluster = *fbxskin.clusters[c];
					if (cluster.num_weights == 0)
						continue;

					// Get the bone that influences this cluster
					auto const* fbxbone = cluster.bone_node;
					root = root ? root : BoneRoot(fbxbone);

					// Get the span of vert indices and weights
					for (int w = 0; w != cluster.num_weights; ++w)
					{
						auto vidx = cluster.vertices[w];
						auto weight = s_cast<float>(cluster.weights[w]);

						influences[vidx].m_bones.push_back(fbxbone->typed_id);
						influences[vidx].m_weights.push_back(weight);
					}
				}
			}

			// Populate the skinning data
			skin.m_skel_id = root ? root->typed_id : NoId; // The skeleton id is the id of the node containing the root bone (see ReadSkeleton)
			skin.m_offsets.reserve(fbxmesh.num_vertices + 1);
			skin.m_bones.reserve(skin.m_offsets.capacity() * 8);
			skin.m_weights.reserve(skin.m_bones.capacity());

			int count = 0;
			for (auto const& influence : influences)
			{
				// Record the offset to this influence
				skin.m_offsets.push_back(count);
				count += isize(influence.m_bones);

				// Append the weights
				for (int i = 0, iend = isize(influence.m_bones); i != iend; ++i)
				{
					skin.m_bones.push_back(influence.m_bones[i]);
					skin.m_weights.push_back(influence.m_weights[i]);
				}
			}
			skin.m_offsets.push_back(count);
		}

		// Read skeletons from the FBX scene
		void ReadSkeletons()
		{
			// Notes:
			//  - Fbx doesn't really have skeletons. Define a skeleton as a hierarchically connected tree of bones.
			//  - Bones are in a separate list in the fbx scene. Nodes contain instances of the bones
			//    where the node transform describes the relationship between bone instances.
			//  - Mesh hierarchies can reference multiple disconnected skeletons, but also,
			//    single skeletons (bone hierarchies) can influence multiple disconnected mesh hierarchies.
			//  - To find the unique skeletons, scan all meshes in the scene and record
			//    which roots each mesh-tree is associated with. Separate skeletons are those
			//    that don't share mesh-trees.
			//  - The reader has the option of only loading Skeleton data, so don't rely
			//    on parsed meshes when determining skeletons.
			//
			// All of above is true, but it's too complicated. Just create skeletons from connected bone hierarchies.

			SkeletonData skel;
			skel.m_bone_ids.reserve(m_fbxscene.bones.count);
			skel.m_names.reserve(m_fbxscene.bones.count);
			skel.m_o2bp.reserve(m_fbxscene.bones.count);
			skel.m_hierarchy.reserve(m_fbxscene.bones.count);

			std::unordered_map<ufbx_node const*, ufbx_bone_pose const*> bind_pose;
			bind_pose.reserve(m_fbxscene.bones.count);

			// Build a skeleton from each root bone
			auto roots = FindRoots(m_fbxscene.bones, IsBoneRoot);
			for (auto const*& root : roots)
			{
				Progress(1 + (&root - roots.data()), ssize(roots), "Reading skeletons...");
				
				// Skeleton Id is the id of the node that contains the root bone,
				// because the same bone could be instanced in multiple nodes/skeletons.
				skel.m_skel_id = root->typed_id;
				
				// Create a lookup for bone node to pose data
				// The bind pose is a snapshot of the global transforms of the bones
				// at the time skinning was authored in the DCC tool.
				bind_pose.clear();
				if (root->bind_pose != nullptr && root->bind_pose->is_bind_pose)
				{
					for (auto const& pose : root->bind_pose->bone_poses)
						bind_pose[pose.bone_node] = &pose;
				}

				// Walk the bone hierarchy creating the skeleton
				WalkHierarchy(root, [&skel, &bind_pose](ufbx_node const* node)
				{
					if (node->bone == nullptr)
						return false;

					auto const& bone = *node->bone;

					// Object space to bind pose.
					auto bp2o = bind_pose.contains(node)
						? To<m4x4>(bind_pose[node]->bone_to_world)
						: To<m4x4>(node->node_to_world);
					auto o2bp = IsOrthonormal(bp2o)
						? InvertFast(bp2o)
						: Invert(bp2o);
					auto level = s_cast<int>(node->node_depth - BoneRoot(node)->node_depth);
					
					skel.m_bone_ids.push_back(bone.typed_id);
					skel.m_names.push_back(std::string(To<std::string_view>(node->name)));
					skel.m_o2bp.push_back(o2bp);
					skel.m_hierarchy.push_back(level);
					return true;
				});

				m_out.CreateSkeleton(skel);
				skel.Reset();
			}
		}

		// Read the animation data from the scene
		void ReadAnimation()
		{
			// Notes:
			//  - The anim stack can affect any node in the scene so it's possible for one animation to affect multiple skeletons.
			//  - Fbx files store complex curves with different types of interpolation. Every sane bit of software deals with fixed
			//    frame rates and numbers of frames. Use ufbx to resample the animation into a fixed frame rate.
			AnimationData anim;

			// Set the animation to use
			for (int i = 0; i != m_fbxscene.anim_stacks.count; ++i)
			{
				Progress(1LL + i, m_fbxscene.anim_stacks.count, "Reading animation...");

				auto const& fbxstack = *m_fbxscene.anim_stacks[i];
				auto const& fbxanim = *fbxstack.anim;
				if (fbxanim.layers.count == 0 || fbxanim.time_begin == fbxanim.time_end || m_fbxscene.settings.frames_per_second == 0)
					continue;

				// Native frame rate
				anim.m_frame_rate = m_fbxscene.settings.frames_per_second;

				// Limit the time span based on the options. Round to whole multiples of frames
				auto frame_range = Intersect(m_opts.m_frame_range, Range<int>(
					static_cast<int>(std::ceil(fbxanim.time_begin * anim.m_frame_rate)),
					static_cast<int>(std::floor(fbxanim.time_end * anim.m_frame_rate))
				));
				auto num_keys = frame_range.size() + 1;
				if (num_keys <= 1)
					continue;

				// Set the duration of the animation
				anim.m_duration = (num_keys - 1) / anim.m_frame_rate;
				auto time_offset = frame_range.begin() / anim.m_frame_rate;
				assert(FEql((num_keys - 1) / anim.m_duration, anim.m_frame_rate));

				// Evaluate the animation for each skeleton
				auto roots = FindRoots(m_fbxscene.bones, IsBoneRoot);
				for (auto const*& skel : roots)
				{
					// Skeleton Id that this animation is for
					anim.m_skel_id = skel->typed_id;

					// Build the bone map for 'skel'
					anim.m_bone_map.reserve(m_fbxscene.bones.count);
					WalkHierarchy(skel, [&anim](ufbx_node const* node)
					{
						if (node->bone == nullptr)
							return false;

						// Store the 'node_id' in the bone map initially.
						// This is replaced later with the actual bone id.
						anim.m_bone_map.push_back(node->typed_id);
						return true;
					});

					// Pre-allocate space for M bones x N frames
					auto num = anim.m_bone_map.size() * num_keys;
					anim.m_rotation.resize(num);
					anim.m_position.resize(num);
					anim.m_scale.resize(num);

					// Watch for inactive channels
					std::atomic_bool active[3] = { false, false, false };

					// For each bone in the skeleton, sample the transforms
					auto idx = std::views::iota(0, isize(anim.m_bone_map));
					std::for_each(std::execution::par, idx.begin(), idx.end(), [this, &fbxanim, &anim, num_keys, time_offset, &active](int bone_idx)
					{
						// Note, the bone map contains node ids initially.
						auto node_id = anim.m_bone_map[bone_idx];
						auto const* node = m_fbxscene.nodes[node_id];
						auto bone_count = ssize(anim.m_bone_map);

						// Replace the node id with the actual bone id
						anim.m_bone_map[bone_idx] = node->bone->typed_id;

						quat prev = quat::Identity();
						bool actv[3] = { false, false, false };

						// Sample data for each frame
						for (int k = 0; k != num_keys; ++k)
						{
							auto time = time_offset + k / anim.m_frame_rate;

							ufbx_transform transform = ufbx_evaluate_transform(&fbxanim, node, time);
							auto rot = To<quat>(transform.rotation);
							auto pos = To<v3>(transform.translation);
							auto scl = To<v3>(transform.scale);

							// Ensure shortest path between adjacent quaternions
							if (k != 0 && Dot(rot, prev) < 0)
								rot = -rot;

							auto idx = k * bone_count + bone_idx;
							anim.m_rotation[idx] = rot;
							anim.m_position[idx] = pos;
							anim.m_scale[idx] = scl;

							prev = rot;
							actv[0] |= !FEql(rot, quat::Identity());
							actv[1] |= !FEql(pos, v3::Zero());
							actv[2] |= !FEql(scl, v3::One());
						}

						// Track default channels
						for (int i = 0; i != 3; ++i)
						{
							if (!actv[i]) continue;
							active[i] = true;
						}
					});

					// Any tracks that are all default can be resized to empty
					if (!active[0]) anim.m_rotation.resize(0);
					if (!active[1]) anim.m_position.resize(0);
					if (!active[2]) anim.m_scale.resize(0);

					// Output the animation for this skeleton
					if (!m_out.CreateAnimation(anim))
						return;

					anim.m_bone_map.resize(0);
					anim.m_rotation.resize(0);
					anim.m_position.resize(0);
					anim.m_scale.resize(0);
				}

				anim.Reset();
			}
		}

		// Traverse the scene hierarchy building up lookup tables from unique IDs to nodes (mesh, skeleton)
		template <std::invocable<ufbx_node const*> Callback> requires std::convertible_to<std::invoke_result_t<Callback, ufbx_node const*>, bool>
		static void WalkHierarchy(ufbx_node const* root, Callback cb)
		{
			vector<ufbx_node const*> stack; stack.reserve(64);
			stack.push_back(root);

			for (; !stack.empty(); )
			{
				auto* node = stack.back();
				stack.pop_back();

				// Return true to recurse into the node. n.b. 'node->node_depth'
				if (!cb(node))
					continue;

				// Recurse in depth first order
				for (auto i = node->children.count; i-- != 0; )
					stack.push_back(node->children[i]);
			}
		}

		// Report progress
		void Progress(int64_t step, int64_t total, char const* message, int nest = 0) const
		{
			if (m_opts.m_progress == nullptr) return;
			if (m_opts.m_progress(step, total, message, nest)) return;
			throw std::runtime_error("user cancelled");
		}

		// Find the root nodes in the list of elements
		template <typename Elements, typename IsRootFn>
		static auto FindRoots(Elements const& elements, IsRootFn is_root)
		{
			vector<ufbx_node const*, 4> roots;
			for (auto const* element : elements)
			{
				for (auto const* node : element->instances)
				{
					if (!is_root(node)) continue;
					roots.push_back(node);
				}
			}
			roots.resize(std::unique(begin(roots), end(roots)) - std::begin(roots));
			return roots;
		}

		// True if 'node' is a mesh root node
		static bool IsMeshRoot(ufbx_node const* node)
		{
			return node->mesh != nullptr && (node->parent == nullptr || node->parent->mesh == nullptr);
		}

		// True if 'node' is a bone root node
		static bool IsBoneRoot(ufbx_node const* node)
		{
			return node->bone != nullptr && (node->parent == nullptr || node->parent->bone == nullptr);
		}

		// Return the ancestor of 'node' that is not a mesh node
		static ufbx_node const* MeshRoot(ufbx_node const* node)
		{
			for (; !IsMeshRoot(node); node = node->parent) {}
			return node;
		}

		// Return the ancestor of 'node' that is not a bone node
		static ufbx_node const* BoneRoot(ufbx_node const* node)
		{
			for (; !IsBoneRoot(node); node = node->parent) {}
			return node;
		}
	};

	// Dump the structure of an FBX file to a stream
	struct Dumper
	{
		//using NodeAndLevel = struct NodeAndLevel { FbxNode* node; int level; };

		std::ostream& m_out;
		ufbx_scene& m_fbxscene;
		//MeshNodeMap m_meshes;
		//BoneNodeMap m_bones;
		DumpOptions const& m_opts;
		//double m_frame_rate;

		Dumper(ufbx_scene& fbxscene, DumpOptions const& opts, std::ostream& out)
			: m_out(out)
			, m_fbxscene(fbxscene)
			//, m_meshes()
			//, m_bones()
			, m_opts(opts)
			//, m_frame_rate(FbxTime::GetFrameRate(m_fbxscene.GetGlobalSettings().GetTimeMode()))
		{
			// Bake transforms into the nodes
			//m_fbxscene.GetRootNode()->ConvertPivotAnimationRecursive(nullptr, FbxNode::eDestinationPivot, m_frame_rate);
			//ConvertScene(m_fbxscene, m_opts.m_coord_system);
			out << std::showpos;
		}

		void Do() const
		{
			/*
			if (AllSet(m_opts.m_parts, EParts::MainObjects))
				DumpMainObjects();
			if (AllSet(m_opts.m_parts, EParts::GlobalSettings))
				DumpGlobalSettings();
			if (AllSet(m_opts.m_parts, EParts::NodeHierarchy))
				DumpHierarchy();
			if (AllSet(m_opts.m_parts, EParts::Materials))
				DumpMaterials();
			*/
			if (AllSet(m_opts.m_parts, EParts::Meshes))
				DumpGeometry();
			/*
			if (AllSet(m_opts.m_parts, EParts::Skeletons))
				DumpSkeletons();
			if (AllSet(m_opts.m_parts, EParts::Animation))
				DumpAnimations();
			*/
		}
		void DumpGeometry() const
		{
			m_out << " GEOMETRY =====================================================================================================\n";

			for (auto const* fbxmesh : m_fbxscene.meshes)
			{
				//auto& mesh = *meshnode.mesh;
				//auto& node = *mesh.GetNode();
				auto level = 0;
				m_out << Indent(level) << "Mesh (ID: " << fbxmesh->element_id << "):\n";
				{
					++level;
					m_out << Indent(level) << "Name: " << To<std::string_view>(fbxmesh->name) << "\n";
					m_out << Indent(level) << "Instances:\n";
					for (auto const* inst : fbxmesh->instances)
					{
						++level;
						m_out << Indent(level) << "Name: " << To<std::string_view>(inst->name) << "\n";
						--level;
					}
					m_out << Indent(level) << "Vert Count: " << fbxmesh->num_vertices << "\n";
					m_out << Indent(level) << "Index Count: " << fbxmesh->num_indices << "\n";
					m_out << Indent(level) << "Face Count: " << fbxmesh->num_faces << "\n";
					m_out << Indent(level) << "Tri Count: " << fbxmesh->num_triangles << "\n";
					m_out << Indent(level) << "Edge Count: " << fbxmesh->num_edges << "\n";
					m_out << Indent(level) << "Max Face Tri Count: " << fbxmesh->max_face_triangles << "\n";
					m_out << Indent(level) << "Empty Face Count: " << fbxmesh->num_empty_faces << "\n";
					m_out << Indent(level) << "Point Face Count: " << fbxmesh->num_point_faces << "\n";
					m_out << Indent(level) << "Line Face Count: " << fbxmesh->num_line_faces << "\n";
					--level;
				}
			}
		}

		// Indent helper
		static std::string_view Indent(int amount)
		{
			constexpr static char const space[] = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
			constexpr static int len = (int)_countof(space);
			return std::string_view(space, amount < len ? amount : len);
		}
	};

	// An RAII dll reference
	struct Context
	{
	private:
		using SceneCont = std::vector<SceneDataPtr>;

		ErrorHandler m_error_cb;
		mutable std::mutex m_mutex;
		//FbxCustomAllocator m_alloc;
		//ManagerPtr m_manager;
		//FbxIOSettings* m_settings;
		uint32_t m_version;
		SceneCont m_scenes;

	public:

		Context(ErrorHandler error_cb)
			: m_error_cb(error_cb)
			, m_mutex()
			//, m_alloc()
			//, m_manager(Check(FbxManager::Create(), "Error: Unable to create FBX Manager"))
			//, m_settings(Check(FbxIOSettings::Create(m_manager.get(), IOSROOT), "Error: Unable to create settings"))
			, m_version(UFBX_VERSION)
			, m_scenes()
		{
		}
		Context(Context&&) = delete;
		Context(Context const&) = delete;
		Context& operator=(Context&&) = delete;
		Context& operator=(Context const&) = delete;

		// Report errors
		void ReportError(std::string_view msg) const
		{
			std::lock_guard<std::mutex> lock(m_mutex);
			m_error_cb(msg);
		}

		// Add 'fbxscene' to this context
		SceneData* AddScene(ufbx_scene* fbxscene)
		{
			std::lock_guard<std::mutex> lock(m_mutex);
			m_scenes.push_back(SceneDataPtr{ new SceneData(fbxscene) });
			return m_scenes.back().get();
		}
	};
}

extern "C"
{
	std::mutex g_mutex;
	std::vector<std::unique_ptr<Context>> g_contexts;
	HINSTANCE g_instance;

	// DLL entry point
	BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
	{
		g_instance = hInstance;
		switch (ul_reason_for_call)
		{
			case DLL_PROCESS_ATTACH: break;
			case DLL_PROCESS_DETACH: break;
			case DLL_THREAD_ATTACH: break;
			case DLL_THREAD_DETACH: break;
		}
		return TRUE;
	}

	// Create a dll context
	__declspec(dllexport) Context* __stdcall Fbx_Initialise(ErrorHandler error_cb)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_mutex);
			g_contexts.push_back(std::unique_ptr<Context>(new Context(error_cb)));
			return g_contexts.back().get();
		}
		catch (std::exception const& ex)
		{
			error_cb(ex.what());
			return nullptr;
		}
	}

	// Release a dll context
	__declspec(dllexport) void __stdcall Fbx_Release(Context* ctx)
	{
		try
		{
			if (ctx == nullptr) return;
			std::lock_guard<std::mutex> lock(g_mutex);
			g_contexts.erase(std::remove_if(begin(g_contexts), end(g_contexts), [ctx](auto& p) { return p.get() == ctx; }), end(g_contexts));
		}
		catch (std::exception const& ex)
		{
			ctx->ReportError(ex.what());
		}
	}

	// Load an fbx scene
	__declspec(dllexport) SceneData* __stdcall Fbx_Scene_Load(Context& ctx, std::istream& src, LoadOptions const& opts) // threadsafe
	{
		try
		{
			// Convert user options
			auto ufbx_opts = To<ufbx_load_opts>(opts);

			// Use a threadpool
			ufbx::thread_pool thread_pool = {};
			ufbx_opts.thread_opts = { .pool = thread_pool };

			// Create a stream adapter
			pr::geometry::fbx::IStream stream(src);

			// Load the scene
			ufbx_error error = {};
			auto fbxscene = ufbx_load_stream(&stream, &ufbx_opts, &error);
			if (error.type != UFBX_ERROR_NONE)
				throw std::runtime_error(To<std::string>(error));

			return ctx.AddScene(fbxscene);
		}
		catch (std::exception const& ex)
		{
			ctx.ReportError(ex.what());
			return nullptr;
		}
	}

	// Read meta data about the scene
	__declspec(dllexport) SceneProps __stdcall Fbx_Scene_ReadProps(Context& ctx, SceneData const& scene)
	{
		try
		{
			if (scene.m_fbxscene == nullptr)
				throw std::runtime_error("Scene is null");

			return SceneProps{
				.m_animation_stack_count = s_cast<int>(scene.m_fbxscene->anim_stacks.count),
				.m_frame_rate = scene.m_fbxscene->settings.frames_per_second,
				.m_material_available = s_cast<int>(scene.m_fbxscene->materials.count),
				.m_meshes_available = s_cast<int>(scene.m_fbxscene->meshes.count),
				.m_skeletons_available = s_cast<int>(0),
				.m_animations_available = s_cast<int>(0),

				// Scene object counts (loaded scene objects)
				.m_material_count = 0,
				.m_mesh_count = 0,
				.m_skeleton_count = 0,
				.m_animation_count = 0,
				.m_mesh_names = {},
				.m_skel_names = {},
			};
		}
		catch (std::exception const& ex)
		{
			ctx.ReportError(ex.what());
			return {};
		}
	}

	// Read the hierarchy from the scene
	__declspec(dllexport) void __stdcall Fbx_Scene_Read(Context& ctx, SceneData& scene, ReadOptions const& options, IReadOutput& out)
	{
		try
		{
			NullCheck(scene.m_fbxscene, "Scene is null");
			Reader reader(scene, options, out);
			reader.Do();
		}
		catch (std::exception const& ex)
		{
			ctx.ReportError(ex.what());
		}
	}

	// Dump info about the scene to 'out'
	__declspec(dllexport) void __stdcall Fbx_Scene_Dump(Context& ctx, SceneData const& scene, DumpOptions const& options, std::ostream& out)
	{
		try
		{
			NullCheck(scene.m_fbxscene, "Scene is null");
			Dumper dumper(*scene.m_fbxscene, options, out);
			dumper.Do();
		}
		catch (std::exception const& ex)
		{
			ctx.ReportError(ex.what());
		}
	}

	// Static function signature checks
	void Fbx::StaticChecks()
	{
		#define PR_FBX_API_CHECK(prefix, name, function_type)\
		static_assert(std::is_same_v<Fbx::prefix##name##Fn, decltype(&Fbx_##prefix##name)>, "Function signature mismatch for Fbx_"#prefix#name);
		PR_FBX_API(PR_FBX_API_CHECK);
		#undef PR_FBX_API_CHECK
	}
}
