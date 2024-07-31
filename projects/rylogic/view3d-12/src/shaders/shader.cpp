//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12
{
	Shader::Shader()
		:RefCounted<Shader>()
		,m_code()
		,m_signature()
	{}
	
	// Sort id for the shader
	SortKeyId Shader::SortId() const
	{
		// Hash all of the ByteCode pointers together for the sort id.
		return SortKeyId(pr::hash::HashBytes32(&m_code, &m_code + 1) % SortKey::MaxShaderId);
	}
	// Config the shader.
	void Shader::Setup(ID3D12GraphicsCommandList*, GpuUploadBuffer&, Scene const&, DrawListElement const*)
	{
		// This method may be called with:
		//  'dle == null' => Setup constants for the frame
		//  'dle != null' => Setup constants per nugget
	}

	// Ref counting clean up function
	void Shader::RefCountZero(RefCounted<Shader>* doomed)
	{
		auto shdr = static_cast<Shader*>(doomed);
		shdr->Delete();
	}
	void Shader::Delete()
	{
		rdr12::Delete<Shader>(this);
	}

	// Compile a shader at run time
	// 'entry_point' is the kernel function name
	// 'code' is the source file as a string
	// 'args' is an array of pointers to arguments
	// 'shader_model' is the shader model to compile to
	std::vector<uint8_t> CompileShader(std::string_view code, std::span<wchar_t const*> args)
	{
		DxcBuffer source = {
			.Ptr = code.data(),
			.Size = code.size(),
			.Encoding = DXC_CP_UTF8,
		};
		
		D3DPtr<IDxcUtils> utils;
		Check(DxcCreateInstance(CLSID_DxcUtils, __uuidof(IDxcUtils), (void**)&utils.m_ptr));

		D3DPtr<IDxcIncludeHandler> include_handler;
		Check(utils->CreateDefaultIncludeHandler(&include_handler.m_ptr));

		D3DPtr<IDxcCompiler3> compiler;
		Check(DxcCreateInstance(CLSID_DxcCompiler, __uuidof(IDxcCompiler3), (void**)&compiler.m_ptr));

		D3DPtr<IDxcResult> result;
		auto hr = compiler->Compile(&source, args.data(), s_cast<uint32_t>(args.size()), include_handler.get(), __uuidof(IDxcResult), (void**)&result.m_ptr);
		if (SUCCEEDED(hr))
		{
			Check(result->GetStatus(&hr));
		}
		if (FAILED(hr))
		{
			std::string message = "Compile Failed";
			if (result)
			{
				D3DPtr<IDxcBlobEncoding> errors_blob;
				if (SUCCEEDED(result->GetErrorBuffer(&errors_blob.m_ptr)))
					message.append(": ").append(static_cast<char const*>(errors_blob->GetBufferPointer()));
			}
			Check(hr, message.c_str());
		}

		D3DPtr<IDxcBlob> shader;
		Check(result->GetResult(&shader.m_ptr));

		std::vector<uint8_t> byte_code(shader->GetBufferSize());
		memcpy(byte_code.data(), shader->GetBufferPointer(), shader->GetBufferSize());
		return byte_code;
	}
	std::vector<uint8_t> CompileShader(std::filesystem::path const& shader_path, std::span<wchar_t const*> args)
	{
		uint32_t code_page = DXC_CP_UTF8;
		
		D3DPtr<IDxcUtils> utils;
		Check(DxcCreateInstance(CLSID_DxcUtils, __uuidof(IDxcUtils), (void**)&utils.m_ptr));

		D3DPtr<IDxcBlobEncoding> source_blob;
		Check(utils->LoadFile(shader_path.wstring().c_str(), &code_page, &source_blob.m_ptr));

		//D3DPtr<IDxcCompilerArgs> compiler_args;
		//Check(utils->BuildArguments(shader_path.wstring().c_str(), entry_point, shader_model, args.data(), s_cast<uint32_t>(args.size()), nullptr, 0, &compiler_args.m_ptr));

		auto source = std::string_view{ static_cast<char const*>(source_blob->GetBufferPointer()), source_blob->GetBufferSize() };
		return CompileShader(source, args);
	}

	// Compiled shader byte code
	namespace shader_code
	{
		// Not a shader
		ByteCode const none;

		// Forward rendering shaders
		namespace compiled
		{
			#include PR_RDR_SHADER_COMPILED_DIR(forward_vs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(forward_ps.h)
			#include PR_RDR_SHADER_COMPILED_DIR(forward_radial_fade_ps.h)
		}
		ByteCode const forward_vs(compiled::forward_vs);
		ByteCode const forward_ps(compiled::forward_ps);
		ByteCode const forward_radial_fade_ps(compiled::forward_radial_fade_ps);

		// Deferred rendering
		namespace compiled
		{
			#include PR_RDR_SHADER_COMPILED_DIR(gbuffer_vs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(gbuffer_ps.h)
			#include PR_RDR_SHADER_COMPILED_DIR(dslighting_vs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(dslighting_ps.h)
		}
		ByteCode const gbuffer_vs(compiled::gbuffer_vs);
		ByteCode const gbuffer_ps(compiled::gbuffer_ps);
		ByteCode const dslighting_vs(compiled::dslighting_vs);
		ByteCode const dslighting_ps(compiled::dslighting_ps);

		// Shadows
		namespace compiled
		{
			#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_vs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_ps.h)
		}
		ByteCode const shadow_map_vs(compiled::shadow_map_vs);
		ByteCode const shadow_map_ps(compiled::shadow_map_ps);

		// Screen Space
		namespace compiled
		{
			#include PR_RDR_SHADER_COMPILED_DIR(point_sprites_gs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(thick_line_list_gs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(thick_line_strip_gs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(arrow_head_gs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(show_normals_gs.h)
		}
		ByteCode const point_sprites_gs(compiled::point_sprites_gs);
		ByteCode const thick_line_list_gs(compiled::thick_line_list_gs);
		ByteCode const thick_line_strip_gs(compiled::thick_line_strip_gs);
		ByteCode const arrow_head_gs(compiled::arrow_head_gs);
		ByteCode const show_normals_gs(compiled::show_normals_gs);

		// Ray cast
		namespace compiled
		{
			#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_vs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_vert_gs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_edge_gs.h)
			#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_face_gs.h)
		}
		ByteCode const ray_cast_vs(compiled::ray_cast_vs);
		ByteCode const ray_cast_vert_gs(compiled::ray_cast_vert_gs);
		ByteCode const ray_cast_edge_gs(compiled::ray_cast_edge_gs);
		ByteCode const ray_cast_face_gs(compiled::ray_cast_face_gs);

		// MipMap generation
		namespace compiled
		{
			#include PR_RDR_SHADER_COMPILED_DIR(mipmap_generator_cs.h)
		}
		ByteCode const mipmap_generator_cs(compiled::mipmap_generator_cs);
	}
}
