//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// This file contains a set of helper wrappers for initialising some d3d11 structures
#pragma once
#include "pr/view3d/forward.h"
#include "pr/view3d/util/util.h"
#include "pr/view3d/textures/image.h"

namespace pr::rdr
{
	// Notes on buffer usage:
	//  Here are some ways to initialize a vertex buffer that changes over time:
	//   1) Create a default usage buffer. Create a 2nd buffer with D3D10_USAGE_STAGING;
	//      fill the second buffer using ID3D11DeviceContext::Map, ID3D11DeviceContext::Unmap;
	//      use ID3D11DeviceContext::CopyResource to copy from the staging buffer to the default buffer.
	//   2) Use ID3D11DeviceContext::UpdateSubresource to copy data from memory.
	//   3) Create a buffer with EUsage::DYNAMIC, and fill it with ID3D11DeviceContext::Map,
	//      ID3D11DeviceContext::Unmap (using the Discard and NoOverwrite flags appropriately).
	// #1 and #2 are useful for content that changes less than once per frame. In general, GPU
	//  reads will be fast and CPU updates will be slower.
	// #3 is useful for content that changes more than once per frame. In general, GPU reads will
	//  be slower, but CPU updates will be faster.

	#pragma region Wrapped Enums
	enum class EUsage :std::underlying_type_t<D3D11_USAGE>
	{
		Default   = D3D11_USAGE_DEFAULT,
		Immutable = D3D11_USAGE_IMMUTABLE,
		Dynamic   = D3D11_USAGE_DYNAMIC,
		Staging   = D3D11_USAGE_STAGING,
	};
	enum class EBind :std::underlying_type_t<D3D11_BIND_FLAG>
	{
		None = 0,
		VertexBuffer    = D3D11_BIND_VERTEX_BUFFER,
		IndexBuffer     = D3D11_BIND_INDEX_BUFFER,
		ConstantBuffer  = D3D11_BIND_CONSTANT_BUFFER,
		ShaderResource  = D3D11_BIND_SHADER_RESOURCE,
		StreamOutput    = D3D11_BIND_STREAM_OUTPUT,
		RenderTarget    = D3D11_BIND_RENDER_TARGET,
		DepthStencil    = D3D11_BIND_DEPTH_STENCIL,
		UnorderedAccess = D3D11_BIND_UNORDERED_ACCESS,
		Decoder         = D3D11_BIND_DECODER,
		VideoEncoder    = D3D11_BIND_VIDEO_ENCODER,

		_bitwise_operators_allowed,
	};
	enum class ECPUAccess :std::underlying_type_t<D3D11_CPU_ACCESS_FLAG>
	{
		None = 0,
		Write = D3D11_CPU_ACCESS_WRITE,
		Read  = D3D11_CPU_ACCESS_READ,

		_bitwise_operators_allowed,
	};
	enum class EResMisc :std::underlying_type_t<D3D11_RESOURCE_MISC_FLAG>
	{
		None = 0,
		GenerateMips                 = D3D11_RESOURCE_MISC_GENERATE_MIPS,
		Shared                       = D3D11_RESOURCE_MISC_SHARED,
		TextureCube                  = D3D11_RESOURCE_MISC_TEXTURECUBE,
		DrawIndirectArgs             = D3D11_RESOURCE_MISC_DRAWINDIRECT_ARGS,
		BufferAllowRawViews          = D3D11_RESOURCE_MISC_BUFFER_ALLOW_RAW_VIEWS,
		BufferStructured             = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED,
		ResourceClamp                = D3D11_RESOURCE_MISC_RESOURCE_CLAMP,
		SharedKeyedMutex             = D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX,
		GdiCompatible                = D3D11_RESOURCE_MISC_GDI_COMPATIBLE,
		SharedNTHandle               = D3D11_RESOURCE_MISC_SHARED_NTHANDLE,
		RestrictedContent            = D3D11_RESOURCE_MISC_RESTRICTED_CONTENT,
		RestrictSharedResource       = D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE,
		RestrictSharedResourceDriver = D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE_DRIVER,
		Guarded                      = D3D11_RESOURCE_MISC_GUARDED,
		TilePool                     = D3D11_RESOURCE_MISC_TILE_POOL,
		Tiled                        = D3D11_RESOURCE_MISC_TILED,
		//HWProtected                  = D3D11_RESOURCE_MISC_HW_PROTECTED, // requires WinSDK 10

		_bitwise_operators_allowed,
	};
	#pragma endregion

	// Standard buffer description
	struct BufferDesc :D3D11_BUFFER_DESC
	{
		void const* Data;      // ByteWidth is the size of the data
		size_t      ElemCount; // The number of elements in this buffer (verts, indices, whatever)

		BufferDesc()
			:D3D11_BUFFER_DESC()
			,Data(0)
			,ElemCount(0)
		{}
		BufferDesc(D3DPtr<ID3D11Buffer> const& buf)
			:BufferDesc()
		{
			// Would need to 'Map' to get the 'Data' in 'buf'. Just get the description.
			buf->GetDesc(this);
		}
		BufferDesc(size_t count, int element_size_in_bytes, EUsage usage = EUsage::Default, EBind bind_flags = EBind::UnorderedAccess, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc()
		{
			Init(count, element_size_in_bytes, nullptr, usage, bind_flags, cpu_access, res_flag);
		}
		BufferDesc(size_t count, void const* data, int element_size_in_bytes, EUsage usage = EUsage::Default, EBind bind_flags = EBind::UnorderedAccess, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc()
		{
			Init(count, element_size_in_bytes, data, usage, bind_flags, cpu_access, res_flag);
		}
		void Init(size_t count, int element_size_in_bytes, void const* data, EUsage usage, EBind bind_flags, ECPUAccess cpu_access, EResMisc res_flag)
		{
			Data                = data;                                // The initialisation data (or null)
			ElemCount           = count;                               // The number of elements in the buffer
			ByteWidth           = UINT(element_size_in_bytes * count); // Size of the buffer in bytes
			Usage               = D3D11_USAGE(usage);                  // How the buffer will be used
			BindFlags           = D3D11_BIND_FLAG(bind_flags);         // How the buffer will be bound (i.e. can it be a render target too?)
			CPUAccessFlags      = D3D11_CPU_ACCESS_FLAG(cpu_access);   // What access the CPU needs. (if data provided, assume none)
			MiscFlags           = D3D11_RESOURCE_MISC_FLAG(res_flag);  // General flags for the resource
			StructureByteStride = UINT(element_size_in_bytes);         // For structured buffers
		}
		size_t SizeInBytes() const
		{
			return ElemCount * StructureByteStride;
		}
	};

	// Vertex buffer flavour of a buffer description
	struct VBufferDesc :BufferDesc
	{
		// Want a dynamic buffer? read the notes above
		VBufferDesc()
			:BufferDesc()
		{}
		VBufferDesc(size_t count, int element_size_in_bytes, EUsage usage = EUsage::Default, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc(count, element_size_in_bytes, usage, EBind::VertexBuffer, cpu_access, res_flag)
		{}
		template <typename Elem> VBufferDesc(size_t count, Elem const* data, EUsage usage = EUsage::Default, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc(count, data, sizeof(Elem), usage, EBind::VertexBuffer, cpu_access, res_flag)
		{}
		template <typename Elem, size_t Sz> VBufferDesc(Elem const (&data)[Sz], EUsage usage = EUsage::Default, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc(Sz, &data[0], sizeof(Elem), usage, EBind::VertexBuffer, cpu_access, res_flag)
		{}
		template <typename Elem> static VBufferDesc Of(size_t count)
		{
			return VBufferDesc(count, static_cast<Elem const*>(nullptr));
		}
	};

	// Index buffer flavour of a buffer description
	struct IBufferDesc :BufferDesc
	{
		DXGI_FORMAT Format;    // The buffer format

		IBufferDesc()
			:BufferDesc()
			,Format(DXGI_FORMAT_UNKNOWN)
		{}
		IBufferDesc(size_t count, DXGI_FORMAT format, EUsage usage = EUsage::Default, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc(count, BytesPerPixel(format), usage, EBind::IndexBuffer, cpu_access, res_flag)
			,Format(format)
		{}
		IBufferDesc(size_t count, void const* data, DXGI_FORMAT format, EUsage usage = EUsage::Default, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc(count, data, BytesPerPixel(format), usage, EBind::IndexBuffer, cpu_access, res_flag)
			,Format(format)
		{}
		template <typename Elem, size_t Sz> IBufferDesc(Elem const (&data)[Sz], DXGI_FORMAT format = dx_format_v<Elem>, EUsage usage = EUsage::Default, ECPUAccess cpu_access = ECPUAccess::None, EResMisc res_flag = EResMisc::None)
			:BufferDesc(Sz, &data[0], BytesPerPixel(format), usage, EBind::IndexBuffer, cpu_access, res_flag)
			,Format(format)
		{}
		template <typename Elem> static IBufferDesc Of(size_t count)
		{
			return IBufferDesc(count, static_cast<Elem const*>(0));
		}
	};

	// Constants buffer flavour of a buffer description
	struct CBufferDesc :BufferDesc
	{
		CBufferDesc()
			:BufferDesc()
		{}
		CBufferDesc(size_t size, EUsage usage = EUsage::Dynamic, ECPUAccess cpu_access = ECPUAccess::Write, EResMisc res_flag = EResMisc::None)
			:BufferDesc(size, sizeof(byte), usage, EBind::ConstantBuffer, cpu_access, res_flag)
		{
			if ((size % 16) != 0)
				throw std::exception("Constant buffers must be a multiple of 16 bytes");
		}
	};

	// Multi sampling description
	struct MultiSamp :DXGI_SAMPLE_DESC
	{
		MultiSamp()
			:MultiSamp(1, 0)
		{}
		MultiSamp(UINT count, UINT quality = ~UINT())
			:DXGI_SAMPLE_DESC()
		{
			Count   = count;
			Quality = quality;
		}
		MultiSamp& Validate(ID3D11Device* device, DXGI_FORMAT format)
		{
			UINT quality = 0;
			for (; Count > 1 && (quality = MultisampleQualityLevels(device, format, Count)) == 0; Count >>= 1) {}
			if (quality != 0 && Quality >= quality) Quality = quality - 1;
			return *this;
		}
	};
	inline bool operator == (DXGI_SAMPLE_DESC const& lhs, DXGI_SAMPLE_DESC const& rhs) { return lhs.Count == rhs.Count && lhs.Quality == rhs.Quality; }

	// Texture buffer description
	struct Texture1DDesc :D3D11_TEXTURE1D_DESC
	{
		Texture1DDesc()
			:D3D11_TEXTURE1D_DESC()
		{
			InitDefaults();
		}
		Texture1DDesc(size_t width, size_t mips = 0U, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM, EUsage usage = EUsage::Default)
			:D3D11_TEXTURE1D_DESC(Texture1DDesc())
		{
			InitDefaults();
			Width          = static_cast<UINT>(width);
			MipLevels      = static_cast<UINT>(mips); // 0 means use all mips down to 1x1
			Format         = format;
			Usage          = D3D11_USAGE(usage);
		}
		Texture1DDesc(Image const& src, size_t mips = 0U, EUsage usage = EUsage::Default)
			:D3D11_TEXTURE1D_DESC(Texture1DDesc())
		{
			InitDefaults();
			Width          = static_cast<UINT>(src.m_dim.x);
			MipLevels      = static_cast<UINT>(mips); // 0 means use all mips down to 1x1
			Format         = src.m_format;
			Usage          = D3D11_USAGE(usage);
		}
		void InitDefaults()
		{
			// Notes about mips: if you use 'MipLevels' other than 1, you need to provide
			// initialisation data for all of the generated mip levels as well
			Width          = 0U;
			MipLevels      = 1U;
			ArraySize      = 1U;
			Format         = DXGI_FORMAT_R8G8B8A8_UNORM;
			Usage          = D3D11_USAGE(EUsage::Default);// Other options: EUsage::Immutable, EUsage::Dynamic
			BindFlags      = D3D11_BIND_FLAG(EBind::ShaderResource);
			CPUAccessFlags = 0U;
			MiscFlags      = 0U;
		}
	};
	struct Texture2DDesc :D3D11_TEXTURE2D_DESC
	{
		Texture2DDesc()
			:D3D11_TEXTURE2D_DESC()
		{
			InitDefaults();
		}
		Texture2DDesc(size_t width, size_t height, size_t mips = 0U, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM, EUsage usage = EUsage::Default)
			:D3D11_TEXTURE2D_DESC(Texture2DDesc())
		{
			InitDefaults();
			Width          = static_cast<UINT>(width);
			Height         = static_cast<UINT>(height);
			MipLevels      = static_cast<UINT>(mips); // 0 means use all mips down to 1x1
			Format         = format;
			Usage          = D3D11_USAGE(usage);
		}
		Texture2DDesc(Image const& src, size_t mips = 0U, EUsage usage = EUsage::Default)
			:D3D11_TEXTURE2D_DESC(Texture2DDesc())
		{
			InitDefaults();
			Width          = static_cast<UINT>(src.m_dim.x);
			Height         = static_cast<UINT>(src.m_dim.y);
			MipLevels      = static_cast<UINT>(mips); // 0 means use all mips down to 1x1
			Format         = src.m_format;
			Usage          = D3D11_USAGE(usage);
		}
		void InitDefaults()
		{
			// Notes about mips: if you use 'MipLevels' other than 1, you need to provide
			// initialisation data for all of the generated mip levels as well
			Width          = 0U;
			Height         = 0U;
			MipLevels      = 1U;
			ArraySize      = 1U;
			Format         = DXGI_FORMAT_R8G8B8A8_UNORM;
			SampleDesc     = MultiSamp();
			Usage          = D3D11_USAGE(EUsage::Default);// Other options: EUsage::Immutable, EUsage::Dynamic
			BindFlags      = D3D11_BIND_FLAG(EBind::ShaderResource);
			CPUAccessFlags = 0U;
			MiscFlags      = 0U;
		}
	};
	struct Texture3DDesc :D3D11_TEXTURE3D_DESC
	{
		Texture3DDesc()
			:D3D11_TEXTURE3D_DESC()
		{
			InitDefaults();
		}
		Texture3DDesc(size_t width, size_t height, size_t depth, size_t mips = 0U, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM, EUsage usage = EUsage::Default)
			:D3D11_TEXTURE3D_DESC(Texture3DDesc())
		{
			InitDefaults();
			Width          = static_cast<UINT>(width);
			Height         = static_cast<UINT>(height);
			Depth          = static_cast<UINT>(depth);
			MipLevels      = static_cast<UINT>(mips); // 0 means use all mips down to 1x1
			Format         = format;
			Usage          = D3D11_USAGE(usage);
		}
		void InitDefaults()
		{
			// Notes about mips: if you use 'MipLevels' other than 1, you need to provide
			// initialisation data for all of the generated mip levels as well
			Width          = 0U;
			Height         = 0U;
			Depth          = 0U;
			MipLevels      = 1U;
			Format         = DXGI_FORMAT_R8G8B8A8_UNORM;
			Usage          = D3D11_USAGE(EUsage::Default);// Other options: EUsage::Immutable, EUsage::Dynamic
			BindFlags      = D3D11_BIND_FLAG(EBind::ShaderResource);
			CPUAccessFlags = 0U;
			MiscFlags      = 0U;
		}
	};
	struct TextureDesc
	{
		D3D11_RESOURCE_DIMENSION dim;
		union
		{
			Texture1DDesc Tex1D;
			Texture2DDesc Tex2D;
			Texture3DDesc Tex3D;
		};

		TextureDesc()
			:dim(D3D11_RESOURCE_DIMENSION_UNKNOWN)
		{}
	};

	// Texture sampler description
	struct SamplerDesc :D3D11_SAMPLER_DESC
	{
		static SamplerDesc PointClamp()  { return SamplerDesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_POINT); }
		static SamplerDesc PointWrap()   { return SamplerDesc(D3D11_TEXTURE_ADDRESS_WRAP , D3D11_FILTER_MIN_MAG_MIP_POINT); }
		static SamplerDesc LinearClamp() { return SamplerDesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_LINEAR); }
		static SamplerDesc LinearWrap()  { return SamplerDesc(D3D11_TEXTURE_ADDRESS_WRAP , D3D11_FILTER_MIN_MAG_MIP_LINEAR); }
		static SamplerDesc AnisotropicClamp() { return SamplerDesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_ANISOTROPIC); }
			
		SamplerDesc()
			:SamplerDesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_LINEAR)
		{}
		SamplerDesc(D3D11_TEXTURE_ADDRESS_MODE addr, D3D11_FILTER filter)
			:SamplerDesc(addr, addr, addr, filter)
		{}
		SamplerDesc(
			D3D11_TEXTURE_ADDRESS_MODE addrU,
			D3D11_TEXTURE_ADDRESS_MODE addrV,
			D3D11_TEXTURE_ADDRESS_MODE addrW,
			D3D11_FILTER filter)
			:D3D11_SAMPLER_DESC()
		{
			InitDefaults();
			Filter         = filter;
			AddressU       = addrU;
			AddressV       = addrV;
			AddressW       = addrW;
		}
		void InitDefaults()
		{
			Filter         = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
			AddressU       = D3D11_TEXTURE_ADDRESS_CLAMP;
			AddressV       = D3D11_TEXTURE_ADDRESS_CLAMP;
			AddressW       = D3D11_TEXTURE_ADDRESS_CLAMP;
			MipLODBias     = 0.0f;
			MaxAnisotropy  = 1;
			ComparisonFunc = D3D11_COMPARISON_ALWAYS;
			BorderColor[0] = 0.0f;
			BorderColor[1] = 0.0f;
			BorderColor[2] = 0.0f;
			BorderColor[3] = 0.0f;
			MinLOD         = 0.0f;
			MaxLOD         = D3D11_FLOAT32_MAX;
		}
	};

	// Initialisation data
	struct SubResourceData :D3D11_SUBRESOURCE_DATA
	{
		SubResourceData()
			:D3D11_SUBRESOURCE_DATA()
		{}
		SubResourceData(void const* init_data, UINT pitch, UINT pitch_per_slice)
			:D3D11_SUBRESOURCE_DATA()
		{
			pSysMem          = init_data;       // Initialisation data for a resource
			SysMemPitch      = pitch;           // used for 2D texture initialisation
			SysMemSlicePitch = pitch_per_slice; // used for 3D texture initialisation
		}
		template <typename InitType> SubResourceData(InitType const& init)
			:D3D11_SUBRESOURCE_DATA()
		{
			pSysMem          = &init;
			SysMemPitch      = 0;
			SysMemSlicePitch = sizeof(InitType);
		}
	};

	// Rasterer state description
	struct RasterStateDesc :D3D11_RASTERIZER_DESC
	{
		RasterStateDesc(
			D3D11_FILL_MODE fill          = D3D11_FILL_SOLID,
			D3D11_CULL_MODE cull          = D3D11_CULL_BACK,
			bool depth_clip_enable        = true,
			bool front_ccw                = true,
			bool multisample_enable       = false,
			bool antialiased_line_enable  = false,
			bool scissor_enable           = false,
			int depth_bias                = 0,
			float depth_bias_clamp        = 0.0f,
			float slope_scaled_depth_bias = 0.0f)
			:D3D11_RASTERIZER_DESC()
		{
			FillMode              = fill;
			CullMode              = cull;
			FrontCounterClockwise = front_ccw;
			DepthBias             = depth_bias;
			MultisampleEnable     = multisample_enable;
			SlopeScaledDepthBias  = slope_scaled_depth_bias;
			DepthClipEnable       = depth_clip_enable;
			ScissorEnable         = scissor_enable;
			DepthBiasClamp        = depth_bias_clamp;
			AntialiasedLineEnable = antialiased_line_enable;
		}
	};

	// Blend state description
	struct BlendStateDesc :D3D11_BLEND_DESC
	{
		BlendStateDesc()
			:D3D11_BLEND_DESC()
		{
			AlphaToCoverageEnable                 = FALSE;
			IndependentBlendEnable                = FALSE;
			RenderTarget[0].BlendEnable           = FALSE;
			RenderTarget[0].SrcBlend              = D3D11_BLEND_ONE;
			RenderTarget[0].DestBlend             = D3D11_BLEND_ZERO;
			RenderTarget[0].BlendOp               = D3D11_BLEND_OP_ADD;
			RenderTarget[0].SrcBlendAlpha         = D3D11_BLEND_ONE;
			RenderTarget[0].DestBlendAlpha        = D3D11_BLEND_ZERO;
			RenderTarget[0].BlendOpAlpha          = D3D11_BLEND_OP_ADD;
			RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
		}
	};

	// DepthStencil state description
	struct DepthStateDesc :D3D11_DEPTH_STENCIL_DESC
	{
		DepthStateDesc()
			:D3D11_DEPTH_STENCIL_DESC()
		{
			DepthEnable                  = TRUE;
			DepthWriteMask               = D3D11_DEPTH_WRITE_MASK_ALL;
			DepthFunc                    = D3D11_COMPARISON_LESS;
			StencilEnable                = FALSE;
			StencilReadMask              = D3D11_DEFAULT_STENCIL_READ_MASK;
			StencilWriteMask             = D3D11_DEFAULT_STENCIL_WRITE_MASK;
			FrontFace.StencilFunc        = D3D11_COMPARISON_ALWAYS;
			FrontFace.StencilDepthFailOp = D3D11_STENCIL_OP_KEEP;
			FrontFace.StencilPassOp      = D3D11_STENCIL_OP_KEEP;
			FrontFace.StencilFailOp      = D3D11_STENCIL_OP_KEEP;
			BackFace.StencilFunc         = D3D11_COMPARISON_ALWAYS;
			BackFace.StencilDepthFailOp  = D3D11_STENCIL_OP_KEEP;
			BackFace.StencilPassOp       = D3D11_STENCIL_OP_KEEP;
			BackFace.StencilFailOp       = D3D11_STENCIL_OP_KEEP;
		}
	};

	// Shader resource view description
	struct ShaderResourceViewDesc :D3D11_SHADER_RESOURCE_VIEW_DESC
	{
		ShaderResourceViewDesc()
			:D3D11_SHADER_RESOURCE_VIEW_DESC()
		{}
		explicit ShaderResourceViewDesc(DXGI_FORMAT format, D3D11_SRV_DIMENSION view_dim = D3D_SRV_DIMENSION_UNKNOWN)
			:ShaderResourceViewDesc()
		{
			Format = format;
			ViewDimension = view_dim;
		}
	};

	// Render target view description
	struct RenderTargetViewDesc :D3D11_RENDER_TARGET_VIEW_DESC
	{
		RenderTargetViewDesc()
			:D3D11_RENDER_TARGET_VIEW_DESC()
		{}
		explicit RenderTargetViewDesc(DXGI_FORMAT format, D3D11_RTV_DIMENSION view_dim = D3D11_RTV_DIMENSION_TEXTURE2D)
			:RenderTargetViewDesc()
		{
			Format = format;
			ViewDimension = view_dim;
		}
	};

	// Depth stencil view description
	struct DepthStencilViewDesc :D3D11_DEPTH_STENCIL_VIEW_DESC
	{
		DepthStencilViewDesc()
			:D3D11_DEPTH_STENCIL_VIEW_DESC()
		{}
		explicit DepthStencilViewDesc(DXGI_FORMAT format, D3D11_DSV_DIMENSION view_dim = D3D11_DSV_DIMENSION_TEXTURE2D)
			:DepthStencilViewDesc()
		{
			Format = format;
			ViewDimension = view_dim;
		}
	};

	// Unordered access view description
	struct UnorderedAccessViewDesc :D3D11_UNORDERED_ACCESS_VIEW_DESC
	{
		UnorderedAccessViewDesc()
			:D3D11_UNORDERED_ACCESS_VIEW_DESC()
		{}
		explicit UnorderedAccessViewDesc(DXGI_FORMAT format, D3D11_UAV_DIMENSION view_dim = D3D11_UAV_DIMENSION_BUFFER)
			:UnorderedAccessViewDesc()
		{
			Format = format;
			ViewDimension = view_dim;
		}
	};

	// Display mode description
	struct DisplayMode :DXGI_MODE_DESC
	{
		DisplayMode(UINT width = 1024, UINT height = 768, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:DXGI_MODE_DESC()
		{
			Width                   = width  ? width  : 8;
			Height                  = height ? height : 8;
			Format                  = format;
			RefreshRate.Numerator   = 0; // let dx choose
			RefreshRate.Denominator = 0;
			ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
			Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
		}
		DisplayMode(pr::iv2 const& area, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:DXGI_MODE_DESC()
		{
			Width                   = area.x ? area.x : 8;
			Height                  = area.y ? area.y : 8;
			Format                  = format;
			RefreshRate.Numerator   = 0; // let dx choose
			RefreshRate.Denominator = 0;
			ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
			Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
		}
	};

	// Viewport description
	struct Viewport :D3D11_VIEWPORT
	{
		// Viewports represent an area on the backbuffer, *not* the target HWND.
		// Viewports are in render target space
		// e.g.
		//  x,y          = 0,0 (not -0.5f,-0.5f)
		//  width,height = 800,600 (not 1.0f,1.0f)
		//  depth is normalised from 0.0f -> 1.0f
		Viewport& set(float x, float y, float width, float height, float min_depth = 0.0f, float max_depth = 1.0f)
		{
			PR_ASSERT(PR_DBG_RDR, x >= D3D11_VIEWPORT_BOUNDS_MIN && x <= D3D11_VIEWPORT_BOUNDS_MAX, "X value out of range");
			PR_ASSERT(PR_DBG_RDR, y >= D3D11_VIEWPORT_BOUNDS_MIN && y <= D3D11_VIEWPORT_BOUNDS_MAX, "Y value out of range");
			PR_ASSERT(PR_DBG_RDR, width >= 0.0f , "Width value invalid");
			PR_ASSERT(PR_DBG_RDR, height >= 0.0f, "Height value invalid");
			PR_ASSERT(PR_DBG_RDR, x + width  <= D3D11_VIEWPORT_BOUNDS_MAX, "Width value out of range");
			PR_ASSERT(PR_DBG_RDR, y + height <= D3D11_VIEWPORT_BOUNDS_MAX, "Height value out of range");
			PR_ASSERT(PR_DBG_RDR, min_depth >= 0.0f && min_depth <= 1.0f, "Min depth value out of range");
			PR_ASSERT(PR_DBG_RDR, max_depth >= 0.0f && max_depth <= 1.0f, "Max depth value out of range");
			PR_ASSERT(PR_DBG_RDR, min_depth <= max_depth, "Min and max depth values invalid");

			TopLeftX = x;
			TopLeftY = y;
			Width    = width;
			Height   = height;
			MinDepth = min_depth;
			MaxDepth = max_depth;
			return *this;
		}

		Viewport(float width, float height)
			:D3D11_VIEWPORT()
		{
			set(0.0f, 0.0f, width, height);
		}
		Viewport(UINT width, UINT height)
			:D3D11_VIEWPORT()
		{
			set(0.0f, 0.0f, static_cast<float>(width), static_cast<float>(height));
		}
		Viewport(float x, float y, float width, float height)
			:D3D11_VIEWPORT()
		{
			set(x, y, width, height);
		}
		Viewport(float x, float y, float width, float height, float min_depth, float max_depth)
			:D3D11_VIEWPORT()
		{
			set(x, y, width, height, min_depth, max_depth);
		}
		Viewport(pr::iv2 const& area)
			:D3D11_VIEWPORT()
		{
			set(0.0f, 0.0f, float(area.x), float(area.y));
		}
		Viewport(IRect const& rect)
			:D3D11_VIEWPORT()
		{
			auto r = FRect(rect);
			set(r.X(), r.Y(), r.SizeX(), r.SizeY());
		}

		size_t WidthUI() const  { return static_cast<size_t>(Width); }
		size_t HeightUI() const { return static_cast<size_t>(Height); }

		FRect AsFRect() const { return FRect(TopLeftX, TopLeftY, TopLeftX + Width, TopLeftY + Height); }
		IRect AsIRect() const { return IRect(int(TopLeftX), int(TopLeftY), int(TopLeftX + Width), int(TopLeftY + Height)); }
		RECT  AsRECT() const  { return RECT{LONG(TopLeftX), LONG(TopLeftY), LONG(TopLeftX + Width), LONG(TopLeftY + Height)}; }

		// Convert a screen space point to normalised screen space
		pr::v2 SSPointToNSSPoint(pr::v2 const& ss_point) const
		{
			return pr::NormalisePoint(AsIRect(), ss_point, 1.0f, -1.0f);
		}
		pr::v2 NSSPointToSSPoint(pr::v2 const& nss_point) const
		{
			return pr::ScalePoint(AsIRect(), nss_point, 1.0f, -1.0f);
		}
	};
}

