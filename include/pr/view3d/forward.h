﻿//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <vector>
#include <string>
#include <list>
#include <new>
#include <memory>
#include <unordered_map>
#include <algorithm>
#include <regex>
#include <optional>
#include <functional>
#include <filesystem>
#include <type_traits>
#include <string_view>
#include <mutex>
#pragma warning(disable:4355)
#include <future>
#pragma warning(default:4355)

#include <intrin.h>
#include <malloc.h>
#include <sdkddkver.h>
#include <windows.h>
#include <d3d11.h>
#include <d3d11_1.h>
#include <d3d11sdklayers.h>
#include <d2d1_2.h>
#include <dwrite_2.h>

#include "pr/macros/link.h"
#include "pr/macros/count_of.h"
#include "pr/macros/repeat.h"
#include "pr/macros/enum.h"
#include "pr/macros/align.h"
#include "pr/meta/alignment_of.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/build_options.h"
#include "pr/common/assert.h"
#include "pr/common/hresult.h"
#include "pr/common/fmt.h"
#include "pr/common/cast.h"
#include "pr/common/flags_enum.h"
#include "pr/common/refcount.h"
#include "pr/common/log.h"
#include "pr/common/refptr.h"
#include "pr/common/d3dptr.h"
#include "pr/common/crc.h"
#include "pr/common/alloca.h"
#include "pr/common/allocator.h"
#include "pr/common/range.h"
#include "pr/common/hash.h"
#include "pr/common/to.h"
#include "pr/common/scope.h"
#include "pr/common/algorithm.h"
#include "pr/common/user_data.h"
#include "pr/common/event_handler.h"
#include "pr/common/static_callback.h"
#include "pr/container/span.h"
#include "pr/container/chain.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/container/byte_data.h"
#include "pr/camera/camera.h"
#include "pr/str/char8.h"
#include "pr/str/string.h"
#include "pr/str/to_string.h"
#include "pr/filesys/filesys.h"
#include "pr/maths/maths.h"
#include "pr/maths/bit_fields.h"
#include "pr/gfx/colour.h"
#include "pr/geometry/common.h"
#include "pr/geometry/distance.h"
#include "pr/geometry/index_buffer.h"
#include "pr/geometry/models_point.h"
#include "pr/geometry/models_line.h"
#include "pr/geometry/models_quad.h"
#include "pr/geometry/models_shape2d.h"
#include "pr/geometry/models_box.h"
#include "pr/geometry/models_sphere.h"
#include "pr/geometry/models_cylinder.h"
#include "pr/geometry/models_extrude.h"
#include "pr/geometry/models_mesh.h"
#include "pr/geometry/models_skybox.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/triangle.h"
#include "pr/geometry/model_file.h"
#include "pr/geometry/utility.h"
#include "pr/threads/synchronise.h"
#include "pr/gui/gdiplus.h"
#include "pr/win32/windows_com.h"
#include "pr/win32/stackdump.h"
#include "pr/script/reader.h"
#include "pr/ldraw/ldr_helper.h"

#ifndef PR_DBG_RDR
#define PR_DBG_RDR PR_DBG
#endif

// Set this in the project settings, not here
#ifndef PR_RDR_RUNTIME_SHADERS
#define PR_RDR_RUNTIME_SHADERS 0
#endif

namespace pr::rdr
{
	using byte = unsigned char;
	using RdrId = std::uintptr_t;
	using SortKeyId = uint16_t;
	using Range = pr::Range<size_t>;
	template <typename T> using RefCounted = pr::RefCount<T>;
	template <typename T> using Allocator = pr::aligned_alloc<T>;
	template <typename T> using alloc_traits = std::allocator_traits<Allocator<T>>;

	using string32 = pr::string<char, 32>;
	using string512 = pr::string<char, 512>;
	using wstring32 = pr::string<wchar_t, 32>;
	using wstring256 = pr::string<wchar_t, 256>;

	static Range const RangeZero = { 0, 0 };
	static RdrId const AutoId = ~RdrId(); // A special value for automatically generating an Id
	static RdrId const InvalidId = RdrId();

	using EGeom = pr::geometry::EGeom;
	using ETopo = pr::geometry::ETopo;

	// Render
	class Renderer;
	struct Window;
	struct Scene;
	struct SceneView;

	// Rendering
	struct SortKey;
	struct DrawListElement;
	struct BSBlock;
	struct DSBlock;
	struct RSBlock;
	struct StateStack;
	struct DeviceState;
	struct RenderStep;
	struct ForwardRender;
	struct GBuffer;
	struct DSLighting;
	struct ShadowMap;
	struct RayCast;
	using RenderStepPtr = std::unique_ptr<RenderStep>;

	// Models
	class  ModelManager;
	struct ModelBuffer;
	struct Model;
	struct NuggetProps;
	struct Nugget;
	struct MdlSettings;
	struct ModelTreeNode;
	using ModelBufferPtr = pr::RefPtr<ModelBuffer>;
	using ModelPtr = pr::RefPtr<Model>;
	using TNuggetChain = pr::chain::head<Nugget, struct ChainGroupNugget>;

	// Instances
	struct BaseInstance;

	// Shaders
	struct Vert;
	class  ShaderManager;
	struct ShaderDesc;
	struct Shader;
	struct ShaderSet0;
	struct ShaderSet1;
	struct ShaderMap;
	using ShaderPtr = pr::RefPtr<Shader>;

	// Textures
	class  TextureManager;
	struct TextureDesc;
	struct Texture1DDesc;
	struct Texture2DDesc;
	struct Texture3DDesc;
	struct TextureBase;
	struct Texture2D;
	struct TextureCube;
	struct Image;
	struct AllocPres;
	struct ProjectedTexture;
	using Texture2DPtr = pr::RefPtr<Texture2D>;
	using TextureCubePtr = pr::RefPtr<TextureCube>;

	// Video
	//struct Video;
	//struct AllocPres;
	//typedef pr::RefPtr<Video> VideoPtr;
	//typedef pr::RefPtr<AllocPres> AllocPresPtr;

	// Lighting
	struct Light;

	// Utility
	class BlendStateManager;
	class DepthStateManager;
	class RasterStateManager;
	struct Lock;
	struct MLock;
	using InvokeFunc = void(__stdcall*)(void* ctx);
	using ResolvePathArgs = struct { std::filesystem::path filepath; bool handled; };

	// EResult
	enum class EResult : uint32_t
	{
		#define PR_ENUM(x)\
		x(Success       ,= 0         )\
		x(Failed        ,= 0x80000000)\
		x(InvalidValue  ,)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EResult, PR_ENUM)
	#undef PR_ENUM

	// EShaderType (in order of execution on the HW) http://msdn.microsoft.com/en-us/library/windows/desktop/ff476882(v=vs.85).aspx
	enum class EShaderType
	{
		#define PR_ENUM(x)\
		x(Invalid ,= 0)\
		x(VS      ,= 1 << 0)\
		x(PS      ,= 1 << 1)\
		x(GS      ,= 1 << 2)\
		x(CS      ,= 1 << 3)\
		x(HS      ,= 1 << 4)\
		x(DS      ,= 1 << 5)\
		x(All     ,= ~0)\
		x(_flags_enum, = 0x7fffffff)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EShaderType, PR_ENUM);
	#undef PR_ENUM

	// ETexAddrMode
	enum class ETexAddrMode
	{
		#define PR_ENUM(x)\
		x(Wrap       ,= D3D11_TEXTURE_ADDRESS_WRAP)\
		x(Mirror     ,= D3D11_TEXTURE_ADDRESS_MIRROR)\
		x(Clamp      ,= D3D11_TEXTURE_ADDRESS_CLAMP)\
		x(Border     ,= D3D11_TEXTURE_ADDRESS_BORDER)\
		x(MirrorOnce ,= D3D11_TEXTURE_ADDRESS_MIRROR_ONCE)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(ETexAddrMode, PR_ENUM);
	#undef PR_ENUM

	// EFilter - MinMagMip
	enum class EFilter
	{
		#define PR_ENUM(x)\
		x(Point             ,= D3D11_FILTER_MIN_MAG_MIP_POINT)\
		x(PointPointLinear  ,= D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR)\
		x(PointLinearPoint  ,= D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT)\
		x(PointLinearLinear ,= D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR)\
		x(LinearPointPoint  ,= D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT)\
		x(LinearPointLinear ,= D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR)\
		x(LinearLinearPoint ,= D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT)\
		x(Linear            ,= D3D11_FILTER_MIN_MAG_MIP_LINEAR)\
		x(Anisotropic       ,= D3D11_FILTER_ANISOTROPIC)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EFilter, PR_ENUM);
	#undef PR_ENUM

	// EFillMode
	enum class EFillMode
	{
		#define PR_ENUM(x)\
		x(Default   ,= 0)\
		x(Points    ,= 1)\
		x(Wireframe ,= D3D11_FILL_WIREFRAME)\
		x(Solid     ,= D3D11_FILL_SOLID)\
		x(SolidWire ,= 4)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EFillMode , PR_ENUM);
	#undef PR_ENUM

	// ECullMode
	enum class ECullMode
	{
		#define PR_ENUM(x)\
		x(Default ,= 0)\
		x(None    ,= D3D11_CULL_NONE)\
		x(Front   ,= D3D11_CULL_FRONT)\
		x(Back    ,= D3D11_CULL_BACK)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(ECullMode, PR_ENUM);
	#undef PR_ENUM

	// ELight
	enum class ELight
	{
		#define PR_ENUM(x)\
		x(Ambient    )\
		x(Directional)\
		x(Point      )\
		x(Spot       )
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(ELight, PR_ENUM);
	#undef PR_ENUM

	// EEye
	enum class EEye
	{
		#define PR_ENUM(x)\
		x(Left )\
		x(Right)
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(EEye, PR_ENUM);
	#undef PR_ENUM

	// ERadial
	enum class ERadial
	{
		#define PR_ENUM(x)\
		x(Spherical)\
		x(Cylindrical)
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(ERadial, PR_ENUM);
	#undef PR_ENUM
}
