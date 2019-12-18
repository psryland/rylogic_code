//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/render/renderer.h"
#include "renderer11/shaders/shader_forward.h"

namespace pr::rdr
{
	ShaderManager::ShaderManager(MemFuncs& mem, Renderer& rdr)
		:m_mem(mem)
		,m_dbg_mem()
		,m_rdr(rdr)
		,m_lookup_ip(mem)
		,m_lookup_vs(mem)
		,m_lookup_ps(mem)
		,m_lookup_gs(mem)
		,m_lookup_cs(mem)
		,m_lookup_shader(mem)
		,m_lookup_cbuf(mem)
		,m_stock_shaders()
		,m_mutex()
	{
		CreateStockShaders();
	}
	ShaderManager::~ShaderManager()
	{
		Renderer::Lock lock(m_rdr);
		auto dc = lock.ImmediateDC();

		// Clear all shaders
		dc->VSSetShader(0, 0, 0);
		dc->PSSetShader(0, 0, 0);
		dc->GSSetShader(0, 0, 0);
		dc->CSSetShader(0, 0, 0);
		dc->HSSetShader(0, 0, 0);
		dc->DSSetShader(0, 0, 0);

		m_stock_shaders.resize(0);
		assert(m_lookup_shader.empty() && "There are shader instances still in use");
	}

	// Create the built-in shaders
	void ShaderManager::CreateStockShaders()
	{
		// Forward shaders
		CreateShader<FwdShaderVS>();
		CreateShader<FwdShaderPS>();
		CreateShader<FwdRadialFadePS>();

		// GBuffer shaders
		CreateShader<GBufferVS>();
		CreateShader<GBufferPS>();
		CreateShader<DSLightingVS>();
		CreateShader<DSLightingPS>();

		// Shadow map shaders
		CreateShader<ShadowMapVS>();
		CreateShader<ShadowMapFaceGS>();
		CreateShader<ShadowMapLineGS>();
		CreateShader<ShadowMapPS>();

		// Other shaders
		CreateShader<PointSpritesGS>();
		CreateShader<ThickLineListGS>();
		CreateShader<ThickLineStripGS>();
		CreateShader<ArrowHeadGS>();
	}

	// Get/Create a d3d resource of type 'TRes'.
	template <typename TRes, typename TCreate, typename TLookup = Lookup<RdrId, D3DPtr<TRes>>>
	D3DPtr<TRes> Get(RdrId id, TLookup& lookup, TCreate create)
	{
		// If 'id' is AutoId, the caller wants a new instance.
		if (id != AutoId)
		{
			// Check the lookup table as it may already exist
			auto iter = lookup.find(id);
			if (iter != std::end(lookup))
				return iter->second;
		}

		// Doesn't already exist, or the caller wants a new instance
		auto res = create();

		// Add it to the lookup
		id = id == AutoId ? MakeId(res.m_ptr) : id;
		AddLookup(lookup, id, res);
		return std::move(res);
	}

	// Get (or create) an input layout
	D3DPtr<ID3D11InputLayout> ShaderManager::GetIP(RdrId id, VShaderDesc const* desc)
	{
		std::lock_guard<std::recursive_mutex> lock0(m_mutex);

		// Note: we need an input layout per vertex shader because the CreateInputLayout function
		// validates the input layout for the vertex shader and can make changes if there is a difference.
		return Get<ID3D11InputLayout>(id, m_lookup_ip, [&]
		{
			if (desc == nullptr)
				throw pr::Exception<HRESULT>(E_FAIL, "Input layout description not provided");
				
			Renderer::Lock lock1(m_rdr);
			D3DPtr<ID3D11InputLayout> ip;
			pr::Throw(lock1.D3DDevice()->CreateInputLayout(desc->m_iplayout, UINT(desc->m_iplayout_count), desc->m_data, desc->m_size, &ip.m_ptr));
			return std::move(ip);
		});
	}

	// Get (or create) a vertex shader.
	D3DPtr<ID3D11VertexShader> ShaderManager::GetVS(RdrId id, VShaderDesc const* desc)
	{
		std::lock_guard<std::recursive_mutex> lock0(m_mutex);

		return Get<ID3D11VertexShader>(id, m_lookup_vs, [&]
		{
			if (desc == nullptr)
				throw pr::Exception<HRESULT>(E_FAIL, "Vertex shader description not provided");

			// Ensure the associated input layout exists
			GetIP(id, desc);
				
			// Attach the input layout as private data to the vertex shader
			Renderer::Lock lock1(m_rdr);
			D3DPtr<ID3D11VertexShader> vs;
			pr::Throw(lock1.D3DDevice()->CreateVertexShader(desc->m_data, desc->m_size, 0, &vs.m_ptr));
			return std::move(vs);
		});
	}

	// Get (or create) a pixel shader.
	D3DPtr<ID3D11PixelShader> ShaderManager::GetPS(RdrId id, PShaderDesc const* desc)
	{
		std::lock_guard<std::recursive_mutex> lock0(m_mutex);

		return Get<ID3D11PixelShader>(id, m_lookup_ps, [&]
		{
			if (desc == nullptr)
				throw pr::Exception<HRESULT>(E_FAIL, "Pixel shader description not provided");

			// Create the pixel shader
			Renderer::Lock lock1(m_rdr);
			D3DPtr<ID3D11PixelShader> ps;
			pr::Throw(lock1.D3DDevice()->CreatePixelShader(desc->m_data, desc->m_size, 0, &ps.m_ptr));
			return std::move(ps);
		});
	}

	// Get (or create) a geometry shader.
	D3DPtr<ID3D11GeometryShader> ShaderManager::GetGS(RdrId id, GShaderDesc const* desc)
	{
		std::lock_guard<std::recursive_mutex> lock0(m_mutex);

		return Get<ID3D11GeometryShader>(id, m_lookup_gs, [&]
		{
			if (desc == nullptr)
				throw pr::Exception<HRESULT>(E_FAIL, "Geometry shader description not provided");

			// Create the geometry shader
			Renderer::Lock lock1(m_rdr);
			D3DPtr<ID3D11GeometryShader> gs;
			pr::Throw(lock1.D3DDevice()->CreateGeometryShader(desc->m_data, desc->m_size, 0, &gs.m_ptr));
			return std::move(gs);
		});
	}
	D3DPtr<ID3D11GeometryShader> ShaderManager::GetGS(RdrId id, GShaderDesc const* desc, StreamOutDesc const& so_desc)
	{
		std::lock_guard<std::recursive_mutex> lock0(m_mutex);

		return Get<ID3D11GeometryShader>(id, m_lookup_gs, [&]
		{
			if (desc == nullptr || so_desc.num_entries() == 0)
				throw pr::Exception<HRESULT>(E_FAIL, "Geometry shader description not provided");

			// Create the geometry shader with stream out
			Renderer::Lock lock1(m_rdr);
			D3DPtr<ID3D11GeometryShader> gs;
			pr::Throw(lock1.D3DDevice()->CreateGeometryShaderWithStreamOutput(desc->m_data, desc->m_size, so_desc.decl(), so_desc.num_entries(), so_desc.strides(), so_desc.num_strides(), so_desc.raster_stream(), so_desc.class_linkage(), &gs.m_ptr));
			return std::move(gs);
		});
	}

	// Get (or create) a compute shader.
	D3DPtr<ID3D11ComputeShader> ShaderManager::GetCS(RdrId id, CShaderDesc const* desc)
	{
		std::lock_guard<std::recursive_mutex> lock0(m_mutex);

		return Get<ID3D11ComputeShader>(id, m_lookup_cs, [&]
		{
			if (desc == nullptr)
				throw pr::Exception<HRESULT>(E_FAIL, "Compute shader description not provided");

			// Create the pixel shader
			Renderer::Lock lock1(m_rdr);
			D3DPtr<ID3D11ComputeShader> cs;
			pr::Throw(lock1.D3DDevice()->CreateComputeShader(desc->m_data, desc->m_size, 0, &cs.m_ptr));
			return std::move(cs);
		});
	}

	// Get or create a 'cbuffer' object for given type 'TCBuf'
	D3DPtr<ID3D11Buffer> ShaderManager::GetCBuf(char const* name, RdrId id, size_t sz)
	{
		auto iter = m_lookup_cbuf.find(id);
		if (iter != std::end(m_lookup_cbuf))
			return iter->second;

		Renderer::Lock lock(m_rdr);

		// Create the 'cbuffer', add it to the lookup, and return it
		D3DPtr<ID3D11Buffer> cbuf;
		CBufferDesc cbdesc(sz);
		pr::Throw(lock.D3DDevice()->CreateBuffer(&cbdesc, 0, &cbuf.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(cbuf.get(), name)); (void)name;
		m_lookup_cbuf[id] = cbuf;
		return std::move(cbuf);
	}
}
