//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/image.h"
#include "pr/view3d-12/utility/utility.h"
#include "pr/view3d-12/shaders/shader_registers.h"

namespace pr::rdr12
{
	// Resource usage flags
	enum class EUsage :std::underlying_type_t<D3D12_RESOURCE_FLAGS>
	{
		None = D3D12_RESOURCE_FLAG_NONE,
		RenderTarget = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,
		DepthStencil = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL,
		UnorderedAccess = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
		NotShaderResource = D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE,
		CrossAdapter = D3D12_RESOURCE_FLAG_ALLOW_CROSS_ADAPTER,
		SimultaneousAccess = D3D12_RESOURCE_FLAG_ALLOW_SIMULTANEOUS_ACCESS,
		VideoDecodeRefOnly = D3D12_RESOURCE_FLAG_VIDEO_DECODE_REFERENCE_ONLY,
		VideoEncodeRefOnly = D3D12_RESOURCE_FLAG_VIDEO_ENCODE_REFERENCE_ONLY,
		_flags_enum,
	};

	// Display mode description
	struct DisplayMode :DXGI_MODE_DESC
	{
		// Notes:
		//  - Credit: https://www.rastertek.com/dx12tut03.html
		//    Before we can initialize the swap chain we have to get the refresh rate from the video card/monitor.
		//    Each computer may be slightly different so we will need to query for that information. We query for
		//    the numerator and denominator values and then pass them to DirectX during the setup and it will calculate
		//    the proper refresh rate. If we don't do this and just set the refresh rate to a default value which may
		//    not exist on all computers then DirectX will respond by performing a buffer copy instead of a buffer flip
		//    which will degrade performance and give us annoying errors in the debug output.
		//  - For gamma-correct rendering to standard 8-bit per channel UNORM formats, you’ll want to create the Render
		//    Target using an sRGB format. The new flip modes, however, do not allow you to create a swap chain back buffer
		//    using an sRGB format. In this case, you create one using the non-sRGB format (i.e. DXGI_SWAP_CHAIN_DESC1.Format
		//    = DXGI_FORMAT_B8G8R8A8_UNORM) and use sRGB for the Render Target View (i.e. D3D12_RENDER_TARGET_VIEW_DESC.Format
		//    = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB).
		DisplayMode(int width = 1024, int height = 768, DXGI_FORMAT format = DXGI_FORMAT_B8G8R8A8_UNORM)
			:DXGI_MODE_DESC()
		{
			Width                   = width  ? s_cast<uint32_t>(width) : 16;
			Height                  = height ? s_cast<uint32_t>(height) : 16;
			Format                  = format;
			RefreshRate.Numerator   = 0;
			RefreshRate.Denominator = 1;
			ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
			Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
		}
		DisplayMode(iv2 const& area, DXGI_FORMAT format = DXGI_FORMAT_B8G8R8A8_UNORM)
			:DisplayMode(area.x, area.y, format)
		{}
	};

	// Multi sampling description
	struct MultiSamp :DXGI_SAMPLE_DESC
	{
		constexpr MultiSamp()
			:DXGI_SAMPLE_DESC()
		{
			Count   = 1;
			Quality = 0;
		}
		constexpr MultiSamp(uint32_t count, uint32_t quality = ~uint32_t())
			:DXGI_SAMPLE_DESC()
		{
			Count = count;
			Quality = quality;
		}
		MultiSamp& Validate(ID3D12Device* device, DXGI_FORMAT format)
		{
			uint32_t quality = 0;
			for (; Count > 1 && (quality = MultisampleQualityLevels(device, format, Count)) == 0; Count >>= 1) {}
			if (quality != 0 && Quality >= quality) Quality = quality - 1;
			return *this;
		}
		friend bool operator == (MultiSamp const& lhs, MultiSamp const& rhs)
		{
			return lhs.Count == rhs.Count && lhs.Quality == rhs.Quality;
		}
	};

	// Viewport description
	struct Viewport :D3D12_VIEWPORT
	{
		// Notes:
		//  - Viewports represent an area on the backbuffer, *not* the target HWND.
		//  - Viewports are in render target space.
		//    e.g.
		//     x,y          = 0,0 (not -0.5f,-0.5f)
		//     width,height = 800,600 (not 1.0f,1.0f)
		//     depth is normalised from 0.0f -> 1.0f
		//  - Viewports are measured in render target pixels not DIP or window pixels.
		//    i.e. generally, the viewport is not in client space coordinates.
		//  - ScreenW/H should be in DIP
		//  - Dx12 requires scissor rectangles for all viewports so they're combined here
		using ScissorRects = pr::vector<RECT, 4, false>;

		int ScreenW; // The screen width (in DIP) that the render target will be mapped to.
		int ScreenH; // The screen height (in DIP) that the render target will be mapped to.
		ScissorRects m_clip;

		Viewport()
			:Viewport(0.0f, 0.0f, 16.0f, 16.0f, 16, 16, 0.0f, 1.0f)
		{}
		Viewport(pr::iv2 const& area)
			:Viewport(0.0f, 0.0f, float(area.x), float(area.y), area.x, area.y, 0.0f, 1.0f)
		{}
		Viewport(float x, float y, float width, float height, int screen_w, int screen_h, float min_depth, float max_depth)
			:D3D12_VIEWPORT()
			,ScreenW()
			,ScreenH()
			,m_clip()
		{
			Set(x, y, width, height, screen_w, screen_h, min_depth, max_depth);
		}

		// Set the viewport area and clip rectangle
		Viewport& Set(float x, float y, float width, float height)
		{
			return Set(x, y, width, height, (int)width, (int)height, 0.0f, 1.0f);
		}
		Viewport& Set(float x, float y, float width, float height, int screen_w, int screen_h, float min_depth, float max_depth)
		{
			#if PR_DBG_RDR
			Throw(x >= D3D12_VIEWPORT_BOUNDS_MIN && x <= D3D12_VIEWPORT_BOUNDS_MAX , "X value out of range");
			Throw(y >= D3D12_VIEWPORT_BOUNDS_MIN && y <= D3D12_VIEWPORT_BOUNDS_MAX , "Y value out of range");
			Throw(width >= 0.0f                                                    , "Width value invalid");
			Throw(height >= 0.0f                                                   , "Height value invalid");
			Throw(x + width  <= D3D12_VIEWPORT_BOUNDS_MAX                          , "Width value out of range");
			Throw(y + height <= D3D12_VIEWPORT_BOUNDS_MAX                          , "Height value out of range");
			Throw(min_depth >= 0.0f && min_depth <= 1.0f                           , "Min depth value out of range");
			Throw(max_depth >= 0.0f && max_depth <= 1.0f                           , "Max depth value out of range");
			Throw(min_depth <= max_depth                                           , "Min and max depth values invalid");
			Throw(screen_w >= 0                                                    , "Screen Width value invalid");
			Throw(screen_h >= 0                                                    , "Screen Height value invalid");
			#endif

			TopLeftX = x;
			TopLeftY = y;
			Width    = width;
			Height   = height;
			MinDepth = min_depth;
			MaxDepth = max_depth;
			ScreenW  = screen_w;
			ScreenH  = screen_h;
			Clip(static_cast<int>(TopLeftX), static_cast<int>(TopLeftY), static_cast<int>(Width), static_cast<int>(Height));
			return *this;
		}

		// Reset the clip rectangle collection
		Viewport& ClearClips()
		{
			m_clip.resize(0);
			return *this;
		}

		// Add a clip rectangle
		Viewport& Clip(int x, int y, int width, int height)
		{
			return Clip(RECT{x, y, x+width, y+height});
		}
		Viewport& Clip(RECT const& rect)
		{
			m_clip.push_back(rect);
			return *this;
		}

		// The viewport rectangle, in render target pixels
		FRect AsFRect() const
		{
			return FRect(TopLeftX, TopLeftY, TopLeftX + Width, TopLeftY + Height);
		}
		IRect AsIRect() const
		{
			return IRect(int(TopLeftX), int(TopLeftY), int(TopLeftX + Width), int(TopLeftY + Height));
		}
		RECT AsRECT() const
		{
			return RECT{LONG(TopLeftX), LONG(TopLeftY), LONG(TopLeftX + Width), LONG(TopLeftY + Height)};
		}

		// Convert a screen space point to normalised screen space
		// 'ss_point' must be in screen pixels, not logical pixels (DIP).
		v2 SSPointToNSSPoint(v2 const& ss_point) const
		{
			return NormalisePoint(IRect(0, 0, ScreenW, ScreenH), ss_point, 1.0f, -1.0f);
		}
		v2 NSSPointToSSPoint(v2 const& nss_point) const
		{
			return ScalePoint(IRect(0, 0, ScreenW, ScreenH), nss_point, 1.0f, -1.0f);
		}
	};

	// Heap properties
	struct HeapProps :D3D12_HEAP_PROPERTIES
	{
		HeapProps() = default;
		HeapProps(D3D12_HEAP_PROPERTIES const& rhs)
			:D3D12_HEAP_PROPERTIES(rhs)
		{}
		
		explicit HeapProps(D3D12_HEAP_TYPE heap_type, uint32_t creation_node_mask = 1, uint32_t visible_node_mask = 1)
			:HeapProps()
		{
			Type = heap_type;
			CPUPageProperty = D3D12_CPU_PAGE_PROPERTY::D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
			MemoryPoolPreference = D3D12_MEMORY_POOL::D3D12_MEMORY_POOL_UNKNOWN;
			CreationNodeMask = creation_node_mask;
			VisibleNodeMask = visible_node_mask;
		}
		bool IsCPUAccessible() const
		{
			return
				Type == D3D12_HEAP_TYPE_UPLOAD ||
				Type == D3D12_HEAP_TYPE_READBACK ||
				(Type == D3D12_HEAP_TYPE_CUSTOM && (
					CPUPageProperty == D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE ||
					CPUPageProperty == D3D12_CPU_PAGE_PROPERTY_WRITE_BACK));
		}
		static HeapProps const& Default()
		{
			static HeapProps props(D3D12_HEAP_TYPE_DEFAULT);
			return props;
		}
		static HeapProps const& Upload()
		{
			static HeapProps props(D3D12_HEAP_TYPE_UPLOAD);
			return props;
		}
	};

	// Texture resource description
	struct TexDesc :D3D12_RESOURCE_DESC
	{
		void const* Data;  // The initialisation data for the texture
		uint32_t RowPitch; // The row pitch in 'Data'

		TexDesc() = default;
		TexDesc(D3D12_RESOURCE_DIMENSION dimension, DXGI_FORMAT format, void const* data, uint64_t width, uint32_t height, uint16_t depth, uint32_t pitch, uint16_t mips, uint64_t alignment, EUsage flags, MultiSamp multisamp)
			:D3D12_RESOURCE_DESC()
			,Data(data)
			,RowPitch(pitch)
		{
			Dimension        = dimension;
			Alignment        = alignment;
			Width            = width;
			Height           = height;
			DepthOrArraySize = depth;
			MipLevels        = mips;
			Format           = format;
			SampleDesc       = multisamp;
			Layout           = D3D12_TEXTURE_LAYOUT_UNKNOWN;
			Flags            = s_cast<D3D12_RESOURCE_FLAGS>(flags);
		}
		
		// Constructors
		static TexDesc Tex1D(Image data, uint16_t mips = 0, EUsage flags = EUsage::None)
		{
			return TexDesc(D3D12_RESOURCE_DIMENSION_TEXTURE1D, data.m_format, data.m_pixels, s_cast<uint64_t>(data.m_dim.x), 1, 1, s_cast<uint32_t>(data.m_pitch.x), mips, 0ULL, flags, MultiSamp{});
		}
		static TexDesc Tex2D(Image data, uint16_t mips = 0, EUsage flags = EUsage::None, MultiSamp multisamp = MultiSamp{})
		{
			return TexDesc(D3D12_RESOURCE_DIMENSION_TEXTURE2D, data.m_format, data.m_pixels, s_cast<uint64_t>(data.m_dim.x), s_cast<uint32_t>(data.m_dim.y), 1, s_cast<uint32_t>(data.m_pitch.x), mips, 0ULL, flags, multisamp);
		}
		static TexDesc Tex3D(DXGI_FORMAT format, void const* data, uint64_t width, uint32_t height, uint16_t depth, uint32_t pitch, uint16_t mips = 0, EUsage flags = EUsage::None)
		{
			return TexDesc(D3D12_RESOURCE_DIMENSION_TEXTURE3D, format, data, width, height, depth, pitch, mips, 0ULL, flags, MultiSamp{});
		}

		// Return the contained data as sub-resource data
		operator D3D12_SUBRESOURCE_DATA() const
		{
			D3D12_SUBRESOURCE_DATA d = {};
			d.pData = Data;
			d.RowPitch = RowPitch;
			d.SlicePitch = RowPitch * Height;
			return d;
		}

		//// Depth
		//uint16_t Depth() const
		//{
		//	return Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D ? DepthOrArraySize : 1;
		//}
		//uint16_t ArraySize() const
		//{
		//	return Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE3D ? DepthOrArraySize : 1;
		//}
		//UINT8 PlaneCount(ID3D12Device* device) const
		//{
		//	D3D12_FEATURE_DATA_FORMAT_INFO info = {Format, 0};
		//	return Succeeded(device->CheckFeatureSupport(D3D12_FEATURE_FORMAT_INFO, &info, sizeof(info))) ? info.PlaneCount : 0;
		//}
		//uint32_t Subresources(ID3D12Device* device) const
		//{
		//	return MipLevels * ArraySize() * PlaneCount(device);
		//}
		//uint32_t CalcSubresource(uint32_t MipSlice, uint32_t ArraySlice, uint32_t PlaneSlice)
		//{
		//	return MipSlice + ArraySlice * MipLevels + PlaneSlice * MipLevels * ArraySize();
		//}
		//friend bool operator == (ResourceDesc const& lhs, ResourceDesc const& rhs)
		//{
		//	return
		//		lhs.Dimension          == rhs.Dimension &&
		//		lhs.Alignment          == rhs.Alignment &&
		//		lhs.Width              == rhs.Width &&
		//		lhs.Height             == rhs.Height &&
		//		lhs.DepthOrArraySize   == rhs.DepthOrArraySize &&
		//		lhs.MipLevels          == rhs.MipLevels &&
		//		lhs.Format             == rhs.Format &&
		//		lhs.SampleDesc.Count   == rhs.SampleDesc.Count &&
		//		lhs.SampleDesc.Quality == rhs.SampleDesc.Quality &&
		//		lhs.Layout             == rhs.Layout &&
		//		lhs.Flags              == rhs.Flags;
		//}
		//friend bool operator != (ResourceDesc const& lhs, ResourceDesc const& rhs)
		//{
		//	return !(lhs == rhs);
		//}
	};

	// Buffer description
	struct BufferDesc :D3D12_RESOURCE_DESC
	{
		void const* Data;      // Width is the size of the data in bytes
		size_t      ElemCount; // The number of elements in this buffer (verts, indices, whatever)

		BufferDesc() = default;
		BufferDesc(size_t count, int element_size_in_bytes, void const* data, uint64_t alignment, EUsage usage)
			:D3D12_RESOURCE_DESC()
			,Data(data)
			,ElemCount(count)
		{
			Dimension        = D3D12_RESOURCE_DIMENSION_BUFFER;
			Alignment        = alignment;
			Width            = element_size_in_bytes * count;
			Height           = 1;
			DepthOrArraySize = 1;
			MipLevels        = 1;
			Format           = DXGI_FORMAT_UNKNOWN;
			SampleDesc       = MultiSamp{};
			Layout           = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
			Flags            = s_cast<D3D12_RESOURCE_FLAGS>(usage);
		}
		int Stride() const
		{
			PR_ASSERT(PR_DBG_RDR, (Width % ElemCount) == 0, "Size is not a multiple of the stride");
			return s_cast<int>(Width / ElemCount);
		}

		// Constructors
		static BufferDesc Buffer(size_t size, void const* data, EUsage usage = EUsage::None)
		{
			return BufferDesc(size, 1, data, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT, usage);
		}
		static BufferDesc CBuf(size_t size)
		{
			// Size of resource heap must be 64K for single-textures and constant buffers
			size = PadTo<size_t>(size, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT);
			return BufferDesc(1, s_cast<int>(size), nullptr, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT, EUsage::None);
		}
		template <typename Elem> static BufferDesc VBuf(size_t count, Elem const* data, EUsage usage = EUsage::None)
		{
			return BufferDesc(count, s_cast<int>(sizeof(Elem)), data, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT, usage);
		}
		template <typename Elem> static BufferDesc IBuf(size_t count, Elem const* data, EUsage usage = EUsage::None)
		{
			return BufferDesc(count, s_cast<int>(sizeof(Elem)), data, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT, usage);
		}

		// Return the contained data as sub-resource data
		operator D3D12_SUBRESOURCE_DATA() const
		{
			D3D12_SUBRESOURCE_DATA d = {};
			d.pData = Data;
			d.RowPitch = Width;
			d.SlicePitch = Width;
			return d;
		}
	};

	// Resource barrier
	struct ResourceBarrier : D3D12_RESOURCE_BARRIER
	{
		ResourceBarrier() = default;
		ResourceBarrier(D3D12_RESOURCE_BARRIER const& rhs)
			:D3D12_RESOURCE_BARRIER(rhs)
		{}
		static ResourceBarrier Transition(ID3D12Resource* pResource, D3D12_RESOURCE_STATES stateBefore, D3D12_RESOURCE_STATES stateAfter, uint32_t subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, D3D12_RESOURCE_BARRIER_FLAGS flags = D3D12_RESOURCE_BARRIER_FLAG_NONE)
		{
			ResourceBarrier result = {};
			D3D12_RESOURCE_BARRIER& barrier = result;
			barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
			barrier.Flags = flags;
			barrier.Transition.pResource = pResource;
			barrier.Transition.StateBefore = stateBefore;
			barrier.Transition.StateAfter = stateAfter;
			barrier.Transition.Subresource = subresource;
			return result;
		}
		static ResourceBarrier Aliasing(ID3D12Resource* pResourceBefore, ID3D12Resource* pResourceAfter)
		{
			ResourceBarrier result = {};
			D3D12_RESOURCE_BARRIER& barrier = result;
			barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_ALIASING;
			barrier.Aliasing.pResourceBefore = pResourceBefore;
			barrier.Aliasing.pResourceAfter = pResourceAfter;
			return result;
		}
		static ResourceBarrier UAV(ID3D12Resource* pResource)
		{
			ResourceBarrier result = {};
			D3D12_RESOURCE_BARRIER& barrier = result;
			barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_UAV;
			barrier.UAV.pResource = pResource;
			return result;
		}
	};

	// Blend state description
	struct BlendStateDesc :D3D12_RENDER_TARGET_BLEND_DESC
	{
		BlendStateDesc()
			:D3D12_RENDER_TARGET_BLEND_DESC()
		{
			BlendEnable = FALSE;
			LogicOpEnable = FALSE;
			SrcBlend = D3D12_BLEND_ONE;
			DestBlend = D3D12_BLEND_ZERO;
			BlendOp = D3D12_BLEND_OP_ADD;
			SrcBlendAlpha = D3D12_BLEND_ONE;
			DestBlendAlpha = D3D12_BLEND_ZERO;
			BlendOpAlpha = D3D12_BLEND_OP_ADD;
			LogicOp = D3D12_LOGIC_OP_NOOP;
			RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;
		}
	};

	// Raster state description
	struct RasterStateDesc :D3D12_RASTERIZER_DESC
	{
		RasterStateDesc()
			:D3D12_RASTERIZER_DESC()
		{
			FillMode = D3D12_FILL_MODE_SOLID;
			CullMode = D3D12_CULL_MODE_BACK;
			FrontCounterClockwise = TRUE;
			DepthBias = 0;
			DepthBiasClamp = 0.0f;
			SlopeScaledDepthBias = 0.0f;
			DepthClipEnable = TRUE;
			MultisampleEnable = FALSE;
			AntialiasedLineEnable = FALSE;
			ForcedSampleCount = 0U;
			ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;
		}
	};

	// Depth state description
	struct DepthStateDesc :D3D12_DEPTH_STENCIL_DESC
	{
		DepthStateDesc()
			:D3D12_DEPTH_STENCIL_DESC()
		{
			DepthEnable = TRUE;
			DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;
			DepthFunc = D3D12_COMPARISON_FUNC_LESS;
			StencilEnable = FALSE;
			StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK;
			StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK;
			FrontFace = {
				.StencilFailOp = D3D12_STENCIL_OP_KEEP,
				.StencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
				.StencilPassOp = D3D12_STENCIL_OP_KEEP,
				.StencilFunc = D3D12_COMPARISON_FUNC_ALWAYS,
			};
			BackFace = {
				.StencilFailOp = D3D12_STENCIL_OP_KEEP,
				.StencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
				.StencilPassOp = D3D12_STENCIL_OP_KEEP,
				.StencilFunc = D3D12_COMPARISON_FUNC_ALWAYS,
			};
		}
	};

	// Stream output description
	struct StreamOutputDesc :D3D12_STREAM_OUTPUT_DESC
	{
		StreamOutputDesc()
			:D3D12_STREAM_OUTPUT_DESC()
		{
			pSODeclaration = nullptr;
			NumEntries = 0U;
			pBufferStrides = nullptr;
			NumStrides = 0U;
			RasterizedStream = 0U;
		}
	};

	// Texture sampler description
	struct SamDesc :D3D12_SAMPLER_DESC
	{
		SamDesc()
			:SamDesc(D3D12_TEXTURE_ADDRESS_MODE_BORDER, D3D12_FILTER_MIN_MAG_MIP_LINEAR)
		{}
		SamDesc(D3D12_TEXTURE_ADDRESS_MODE addr, D3D12_FILTER filter)
			:SamDesc(addr, addr, addr, filter)
		{}
		SamDesc(D3D12_TEXTURE_ADDRESS_MODE addrU, D3D12_TEXTURE_ADDRESS_MODE addrV, D3D12_TEXTURE_ADDRESS_MODE addrW, D3D12_FILTER filter)
			:D3D12_SAMPLER_DESC()
		{
			Filter         = filter;
			AddressU       = addrU;
			AddressV       = addrV;
			AddressW       = addrW;
			MipLODBias     = 0.0f;
			MaxAnisotropy  = 1;
			ComparisonFunc = D3D12_COMPARISON_FUNC_ALWAYS;
			BorderColor[0] = 0.0f;
			BorderColor[1] = 0.0f;
			BorderColor[2] = 0.0f;
			BorderColor[3] = 0.0f;
			MinLOD         = 0.0f;
			MaxLOD         = D3D12_FLOAT32_MAX;
		}

		// Standard samplers
		static SamDesc const& PointClamp()       { static SamDesc sam(D3D12_TEXTURE_ADDRESS_MODE_CLAMP, D3D12_FILTER_MIN_MAG_MIP_POINT); return sam; }
		static SamDesc const& PointWrap()        { static SamDesc sam(D3D12_TEXTURE_ADDRESS_MODE_WRAP , D3D12_FILTER_MIN_MAG_MIP_POINT); return sam; }
		static SamDesc const& LinearClamp()      { static SamDesc sam(D3D12_TEXTURE_ADDRESS_MODE_CLAMP, D3D12_FILTER_MIN_MAG_MIP_LINEAR); return sam; }
		static SamDesc const& LinearWrap()       { static SamDesc sam(D3D12_TEXTURE_ADDRESS_MODE_WRAP , D3D12_FILTER_MIN_MAG_MIP_LINEAR); return sam; }
		static SamDesc const& AnisotropicClamp() { static SamDesc sam(D3D12_TEXTURE_ADDRESS_MODE_CLAMP, D3D12_FILTER_ANISOTROPIC); return sam; }
	};

	// Static sampler description
	struct SamDescStatic :D3D12_STATIC_SAMPLER_DESC
	{
		SamDescStatic(ESamReg shader_register, int register_space, D3D12_SHADER_VISIBILITY vis = D3D12_SHADER_VISIBILITY_PIXEL)
			:SamDescStatic(shader_register, register_space, D3D12_TEXTURE_ADDRESS_MODE_BORDER, D3D12_FILTER_MIN_MAG_MIP_LINEAR, vis)
		{}
		SamDescStatic(ESamReg shader_register, int register_space, D3D12_TEXTURE_ADDRESS_MODE addr, D3D12_FILTER filter, D3D12_SHADER_VISIBILITY vis = D3D12_SHADER_VISIBILITY_PIXEL)
			:SamDescStatic(shader_register, register_space, addr, addr, addr, filter, vis)
		{}
		SamDescStatic(ESamReg shader_register, int register_space, D3D12_TEXTURE_ADDRESS_MODE addrU, D3D12_TEXTURE_ADDRESS_MODE addrV, D3D12_TEXTURE_ADDRESS_MODE addrW, D3D12_FILTER filter, D3D12_SHADER_VISIBILITY vis = D3D12_SHADER_VISIBILITY_PIXEL)
			:D3D12_STATIC_SAMPLER_DESC()
		{
			Filter           = filter;
			AddressU         = addrU;
			AddressV         = addrV;
			AddressW         = addrW;
			MipLODBias       = 0.0f;
			MaxAnisotropy    = 1;
			ComparisonFunc   = D3D12_COMPARISON_FUNC_ALWAYS;
			BorderColor      = D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK;
			MinLOD           = 0.0f;
			MaxLOD           = D3D12_FLOAT32_MAX;
			ShaderRegister   = s_cast<UINT>(shader_register);
			RegisterSpace    = s_cast<UINT>(register_space);
			ShaderVisibility = vis;
		}
	};
}
