//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/stock_resources.h"
//#include "pr/view3d-12/resource/descriptor_store.h"
//#include "pr/view3d-12/resource/resource_state_store.h"
//#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/resource/gpu_transfer_buffer.h"
#include "pr/view3d-12/resource/mipmap_generator.h"
#include "pr/view3d-12/shaders/stock_shaders.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/utility/keep_alive.h"
//#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	struct ResourceFactory
	{
		// Notes:
		// - The resource factory is an instance-able object used to create resources.
		// - Multiple resource factories can exist at any one time.
		// - The resource factory is expected to be used on one thread only.

	private:

		Renderer&       m_rdr;                // The owning renderer instance
		GpuSync         m_gsync;              // Sync with GPU
		KeepAlive       m_keep_alive;         // Keep alive for the resource manager
		GfxCmdAllocPool m_gfx_cmd_alloc_pool; // A pool of command allocators.
		GfxCmdList      m_gfx_cmd_list;       // Command list for resource manager operations.
		GpuUploadBuffer m_upload_buffer;      // Upload memory buffer for initialising resources
		MipMapGenerator m_mipmap_gen;         // Utility class for generating mip maps for a texture
		bool            m_flush_required;     // True if commands have been added to the command list and need sending to the GPU

	public:

		ResourceFactory(Renderer& rdr);
		ResourceFactory(ResourceFactory const&) = delete;
		ResourceFactory& operator = (ResourceFactory const&) = delete;
		~ResourceFactory();

		// Renderer access
		ID3D12Device4* d3d() const;
		Renderer& rdr() const;

		// Flush creation commands to the GPU. Returns the sync point for when they've been executed
		uint64_t FlushToGpu(EGpuFlush flush);
		void Wait(uint64_t sync_point) const;
		
		// Create and initialise a resource
		D3DPtr<ID3D12Resource> CreateResource(ResDesc const& desc, char const* name);

		// Create a model.
		ModelPtr CreateModel(ModelDesc const& mdesc, D3DPtr<ID3D12Resource> vb, D3DPtr<ID3D12Resource> ib);
		ModelPtr CreateModel(ModelDesc const& desc);
		ModelPtr CreateModel(EStockModel id);

		// Create a new nugget
		Nugget* CreateNugget(NuggetDesc const& ndata, Model* model);

		// Create a new texture instance.
		Texture2DPtr CreateTexture2D(TextureDesc const& desc);
		Texture2DPtr CreateTexture2D(std::filesystem::path const& resource_path, TextureDesc const& desc);
		TextureCubePtr CreateTextureCube(std::filesystem::path const& resource_path, TextureDesc const& desc);
		Texture2DPtr CreateTexture(EStockTexture id);

		// Create a new sampler instance
		SamplerPtr GetSampler(SamplerDesc const& desc);
		SamplerPtr GetSampler(EStockSampler id);

		// Create a texture that references a shared resource
		Texture2DPtr OpenSharedTexture2D(HANDLE shared_handle, TextureDesc const& desc);

		// Create a shader
		ShaderPtr CreateShader(EStockShader id, char const* config);

	private:

		friend struct UpdateSubresourceScope;
	};

#if 0
	struct ResourceManager
	{


		friend struct Shader;
		// Notes:
		//  - The resource manager is used by all windows and scenes and therefore has it's own cmd allocator and list.
		//  - When resources are created, the commands are added to the internal command list. Callers need to 
		//  - Maintains resource heaps and allocation of resources (i.e. vertex buffers, index buffers, textures, etc)
		//  - Use 'GetPrivateData(WKPDID_D3DDebugObjectNameW,..)' to get names of resources.
		//  - In Dx12, samplers are separate from textures. Callers should create and store them separately.
		//  - Compiler complaints about Model not being defined here means you have forgotten to include model.h in some cpp.
	private:

		using SignaturePtr       = D3DPtr<ID3D12RootSignature>;
		using PipelineStatePtr   = D3DPtr<ID3D12PipelineState>;
		using TextureLookup      = Lookup<RdrId, TextureBase*>;
		using SamplerLookup      = Lookup<RdrId, Sampler*>;
		using DxResLookup        = Lookup<RdrId, ID3D12Resource*>;
		using GpuViewHeap        = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		using GpuSamplerHeap     = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER>;
		using ModelCont          = pr::vector<ModelPtr>;
		using TextureCont        = pr::vector<Texture2DPtr>;
		using SamplerCont        = pr::vector<SamplerPtr>;
		using GdiPlus            = pr::GdiPlus;

		GpuViewHeap        m_heap_view;          // GPU visible descriptor heap for CBV/SRV/UAV
		GpuSamplerHeap     m_heap_sampler;       // GPU visible descriptor heap for samplers
		DxResLookup        m_lookup_res;         // A map from hash of resource URI to existing Dx12 resource pointer.
		TextureLookup      m_lookup_tex;         // A map from texture id to existing texture instances.
		SamplerLookup      m_lookup_sam;         // A map from sampler id to existing sampler instances.
		DescriptorStore    m_descriptor_store;   // Manager of resource descriptors
		TextureCont        m_stock_textures;     // Stock textures
		SamplerCont        m_stock_samplers;     // Stock samplers
		GdiPlus            m_gdiplus;            // Context scope for GDI
		AutoSub            m_eh_resize;          // Event handler subscription for the RT resize event
		int                m_gdi_dc_ref_count;   // Used to detect outstanding DC references


	};
#endif
}
