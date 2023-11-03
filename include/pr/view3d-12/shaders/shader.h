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

	// A shader base class
	struct Shader :RefCounted<Shader>
	{
		// Notes:
		//  - A "shader" means the full set of VS,PS,GS,DS,HS,etc because constant buffers etc apply to all stages now.
		//  - A shader without a Signature is an 'overlay' shader, intended to replace parts of a full shader. Overlay shaders
		//    must use constant buffers that don't conflict with the base shader, and the base shader must have a signature that
		//    handles all possible overlays.
		//  - A shader does not contain a reference to a render step or window (i.e. without a GpuSync).
		//    When the shader is needed, it is "realised" in a given pool that is owned by the window/render step, etc.
		//  - The size of a shader depends on the shader type, so this type must be allocated.
		//  - The shader contains the shader specific parameters.
		//  - The realised shader is reused by the window/render step.
		//  - All shaders can share one GpuUploadBuffer
		ShaderCode Code;                       // Byte code for the shader parts
		D3DPtr<ID3D12RootSignature> Signature; // Signature for shader, null if an overlay
		
		Shader();
		virtual ~Shader() {}

		// Config the shader.
		// This method may be called with:
		//  'dle == null' => Setup constants for the frame
		//  'dle != null' => Setup constants per nugget
		virtual void Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const& scene, DrawListElement const* dle);

		// Create a shader
		template <typename TShader, typename... Args> requires (std::is_base_of_v<Shader, TShader>)
		static RefPtr<TShader> Create(Args... args)
		{
			RefPtr<TShader> shdr(rdr12::New<TShader>(std::forward<Args>(args)...), true);
			return shdr;
		}

		// Ref counting clean up
		static void RefCountZero(RefCounted<Shader>* doomed);
		protected: virtual void Delete();
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

		// MipMap generation
		extern ByteCode const mipmap_generator_cs;
	}
}
