//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/gpu_upload_buffer.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	// The compiled byte code for the shader stages
	struct ShaderCode
	{
		ByteCode VS;
		ByteCode PS;
		ByteCode GS;
		ByteCode CS;
		ByteCode DS;
		ByteCode HS;
	};

	// Statically declared shader byte code
	namespace shader_code
	{
		// Not a shader
		extern ByteCode const none;

		// Forward rendering shaders
		extern ByteCode const forward_vs;
		extern ByteCode const forward_ps;
		extern ByteCode const forward_radial_fade_ps;

		// Deferred rendering
		extern ByteCode const gbuffer_vs;
		extern ByteCode const gbuffer_ps;
		extern ByteCode const dslighting_vs;
		extern ByteCode const dslighting_ps;

		// Shadows
		extern ByteCode const shadow_map_vs;
		extern ByteCode const shadow_map_ps;

		// Screen Space
		extern ByteCode const point_sprites_gs;
		extern ByteCode const thick_line_list_gs;
		extern ByteCode const thick_line_strip_gs;
		extern ByteCode const arrow_head_gs;
		extern ByteCode const show_normals_gs;

		// Ray cast
		extern ByteCode const ray_cast_vs;
		extern ByteCode const ray_cast_vert_gs;
		extern ByteCode const ray_cast_edge_gs;
		extern ByteCode const ray_cast_face_gs;
	}

	// Shader base class
	struct Shader :RefCounted<Shader>
	{
		// Notes:
		//  - Base class for all shaders.
		//  - Shaders are allocated types because they can be different sizes.
		//  - A "shader" means the full set of VS,PS,GS,DS,HS,etc because constant buffers etc
		//    apply to all stages now. If a shader only has a GS, it can be applied on top of another
		//    shader. I.e. Shaders can be layered.
		//  - Shader instances should be lightweight, we may want multiple instances with
		//    different parameters... should the parameters be separate then?
		ResourceManager* m_mgr;
		GpuUploadBuffer m_cbuf;

		Shader(ResourceManager& mgr, GpuSync& gsync, int64_t blk_size, ShaderCode code);
		virtual ~Shader() {}

		// The compiled byte code for the shader stages
		ShaderCode Code;

		// Ref counting clean up
		static void RefCountZero(RefCounted<Shader>* doomed);
		protected: virtual void Delete();
	};
}
