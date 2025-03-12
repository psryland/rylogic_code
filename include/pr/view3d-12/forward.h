//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once

#ifndef PR_DBG_RDR
#define PR_DBG_RDR PR_DBG
#endif

// Set this in the project settings, not here
#ifndef PR_RDR_RUNTIME_SHADERS
#define PR_RDR_RUNTIME_SHADERS 0
#endif


#include <vector>
#include <string>
#include <list>
#include <new>
#include <memory>
#include <sstream>
#include <fstream>
#include <unordered_map>
#include <algorithm>
#include <regex>
#include <optional>
#include <ranges>
#include <functional>
#include <filesystem>
#include <source_location>
#include <type_traits>
#include <mutex>
#include <condition_variable>
#include <thread>
#include <future>
#include <cwctype>

#include <intrin.h>
#include <malloc.h>
#include <sdkddkver.h>
#include <winsock2.h>
#include <windows.h>
#include <dxgi.h>
#include <dxgidebug.h>
#include <d3d12.h>
#include <d3d12sdklayers.h>
#include <d3d11on12.h>
#include <dxgi1_4.h>
#include <dxgitype.h>
#include <dxcapi.h>
#include <d2d1_2.h>
#include <dwrite_2.h>

#include "pr/camera/camera.h"
#include "pr/common/algorithm.h"
#include "pr/common/alloca.h"
#include "pr/common/allocator.h"
#include "pr/common/assert.h"
#include "pr/common/bstr_t.h"
#include "pr/common/build_options.h"
#include "pr/common/cast.h"
#include "pr/common/coalesce.h"
#include "pr/common/d3dptr.h"
#include "pr/common/event_handler.h"
#include "pr/common/flags_enum.h"
#include "pr/common/fmt.h"
#include "pr/common/guid.h"
#include "pr/common/hash.h"
#include "pr/common/hresult.h"
#include "pr/common/memstream.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/range.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/common/resource.h"
#include "pr/common/scope.h"
#include "pr/common/static_callback.h"
#include "pr/common/to.h"
#include "pr/common/user_data.h"
#include "pr/container/byte_data.h"
#include "pr/container/chain.h"
#include "pr/container/deque.h"
#include "pr/container/ring.h"
#include "pr/container/vector.h"
#include "pr/filesys/filewatch.h"
#include "pr/filesys/lock_file.h"
#include "pr/filesys/resolve_path.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/common.h"
#include "pr/geometry/distance.h"
#include "pr/geometry/index_buffer.h"
#include "pr/geometry/intersect.h"
#include "pr/geometry/model_file.h"
#include "pr/geometry/models_box.h"
#include "pr/geometry/models_cylinder.h"
#include "pr/geometry/models_extrude.h"
#include "pr/geometry/models_line.h"
#include "pr/geometry/models_mesh.h"
#include "pr/geometry/models_point.h"
#include "pr/geometry/models_quad.h"
#include "pr/geometry/models_shape2d.h"
#include "pr/geometry/models_skybox.h"
#include "pr/geometry/models_sphere.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/triangle.h"
#include "pr/geometry/utility.h"
#include "pr/gfx/colour.h"
#include "pr/gui/gdiplus.h"
#include "pr/macros/enum.h"
#include "pr/maths/bit_fields.h"
#include "pr/maths/maths.h"
#include "pr/meta/alignment_of.h"
#include "pr/network/winsock.h"
#include "pr/network/sockets.h"
#include "pr/str/char8.h"
#include "pr/str/string.h"
#include "pr/str/to_string.h"
#include "pr/threads/name_thread.h"
#include "pr/threads/synchronise.h"
#include "pr/win32/dummy_window.h"
#include "pr/win32/key_codes.h"
#include "pr/win32/win32.h"

namespace pr::rdr12
{
	// Types
	using byte = unsigned char;
	using float4_t = float[4];
	using RdrId = std::uintptr_t;
	using SortKeyId = uint16_t;
	using Range = pr::Range<int64_t>;
	using Handle = pr::win32::Handle;
	using Winsock = pr::network::Winsock;
	using HashValue32 = pr::hash::HashValue32;
	using seconds_t = std::chrono::duration<double, std::ratio<1, 1>>;
	using time_point_t = std::chrono::system_clock::time_point;
	using IPathResolver = pr::filesys::IPathResolver;
	using PathResolver = pr::filesys::PathResolver;
	using NoIncludes = pr::filesys::NoIncludes;
	template <typename T> using Scope = pr::Scope<T>;
	template <typename T> using Allocator = pr::aligned_alloc<T>;
	template <typename T> using alloc_traits = std::allocator_traits<Allocator<T>>;
	template <typename T> using RefCounted = pr::RefCount<T>;
	template <typename T> using RefPtr = pr::RefPtr<T>;

	// Fixed size strings
	using string32 = pr::string<char, 32>;
	using string512 = pr::string<char, 512>;
	using wstring32 = pr::string<wchar_t, 32>;
	using wstring256 = pr::string<wchar_t, 256>;

	// Constants
	static constexpr Range RangeZero = Range::Zero();
	static constexpr RdrId AutoId = ~RdrId(); // A special value for automatically generating an Id
	static constexpr RdrId InvalidId = RdrId();

	// Enumerations
	using EGeom = pr::geometry::EGeom;
	using ETopo = pr::geometry::ETopo;
	using ETopoGroup = pr::geometry::ETopoGroup;

	// Renderer
	struct Renderer;
	struct Window;
	struct Scene;
	struct Frame;
	struct SceneCamera;
	struct RdrSettings;
	struct WndSettings;

	// Rendering
	struct RenderStep;
	struct RenderForward;
	struct RenderSmap;
	struct RenderRayCast;
	struct DrawListElement;
	struct BackBuffer;
	struct PipeState;
	struct SortKey;

	// Resources
	struct ResourceFactory;
	struct ResourceStore;
	struct ResDesc;
	struct SamDesc;
	
	// Samplers
	struct SamplerDesc;
	struct Sampler;
	using SamplerPtr = RefPtr<Sampler>;

	// Textures
	struct TextureDesc;
	struct TextureBase;
	struct Texture2D;
	struct TextureCube;
	using Texture2DPtr = RefPtr<Texture2D>;
	using TextureCubePtr = RefPtr<TextureCube>;
	    struct AllocPres;
	    struct ProjectedTexture;

	// Video
	//struct Video;
	//struct AllocPres;
	//typedef RefPtr<Video> VideoPtr;
	//typedef RefPtr<AllocPres> AllocPresPtr;
	
	// Models
	struct ModelDesc;
	struct NuggetDesc;
	struct Model;
	struct Nugget;
	struct ModelTreeNode;
	struct MeshCreationData;
	using ModelPtr = RefPtr<Model>;
	using TNuggetChain = pr::chain::head<Nugget, struct ChainGroupNugget>;

	// Instances
	struct BaseInstance;

	// Shaders
	struct Vert;
	struct Shader;
	namespace shaders
	{
		struct Forward;
		struct PointSpriteGS;
		struct ShowNormalsGS;
		struct ThickLineStripGS;
		struct ThickLineListGS;
		struct ShadowMap;
	}
	using ShaderPtr = RefPtr<Shader>;
	struct ShadowMap;
	struct ShadowCaster;

	// Lighting
	struct Light;

	// Ray cast
	struct HitTestRay;
	struct HitTestResult;

	// Utility
	struct Lock;
	struct MLock;
	struct Image;
	struct ImageWithData;
	struct FeatureSupport;
	struct GpuSync;
	
	// LDraw
	namespace ldraw
	{
		struct LdrObject;
		struct LdrGizmo;
		using LdrObjectPtr = RefPtr<LdrObject>;
		using LdrGizmoPtr = RefPtr<LdrGizmo>;
		using ObjectCont = pr::vector<LdrObjectPtr, 8>;
		using GizmoCont = pr::vector<LdrGizmoPtr, 8>;
		enum class EGizmoMode :int;
	}
	using LdrObject = ldraw::LdrObject;
	using LdrGizmo = ldraw::LdrGizmo;
	using LdrObjectPtr = ldraw::LdrObjectPtr;
	using LdrGizmoPtr = ldraw::LdrGizmoPtr;
	using ObjectCont = ldraw::ObjectCont;
	using GizmoCont = ldraw::GizmoCont;

	// Dll
	struct Context;
	struct V3dWindow;
	struct ParseResult;
	struct ObjectAttributes;
	struct ScriptSources;
	enum class ECamField :int;

	// Event args
	struct ResolvePathArgs;
	struct BackBufferSizeChangedEventArgs;

	// Callbacks
	using InvokeFunc = void (__stdcall *)(void* ctx);

	// EResult
	#define PR_ENUM(x)\
		x(Success       ,= 0         )\
		x(Failed        ,= 0x80000000)\
		x(InvalidValue  ,)
	PR_DEFINE_ENUM2_BASE(EResult, PR_ENUM, uint32_t);
	
	// Render steps
	#undef PR_ENUM
		#define PR_ENUM(x)\
		x(Invalid        , = InvalidId)\
		x(RenderForward  ,)\
		x(GBuffer        ,)\
		x(DSLighting     ,)\
		x(ShadowMap      ,)\
		x(RayCast        ,)
	PR_DEFINE_ENUM2(ERenderStep, PR_ENUM);
	#undef PR_ENUM

	// EShaderType (in order of execution on the HW) http://msdn.microsoft.com/en-us/library/windows/desktop/ff476882(v=vs.85).aspx
	#define PR_ENUM(x)\
		x(Invalid ,= 0)\
		x(VS      ,= 1 << 0)\
		x(PS      ,= 1 << 1)\
		x(GS      ,= 1 << 2)\
		x(CS      ,= 1 << 3)\
		x(HS      ,= 1 << 4)\
		x(DS      ,= 1 << 5)\
		x(All     ,= ~0)\
		x(_flags_enum, = 0x7FFFFFFF)
	PR_DEFINE_ENUM2(EShaderType, PR_ENUM);
	#undef PR_ENUM

	// ETexAddrMode
	#define PR_ENUM(x)\
		x(Wrap       ,= D3D12_TEXTURE_ADDRESS_MODE_WRAP)\
		x(Mirror     ,= D3D12_TEXTURE_ADDRESS_MODE_MIRROR)\
		x(Clamp      ,= D3D12_TEXTURE_ADDRESS_MODE_CLAMP)\
		x(Border     ,= D3D12_TEXTURE_ADDRESS_MODE_BORDER)\
		x(MirrorOnce ,= D3D12_TEXTURE_ADDRESS_MODE_MIRROR_ONCE)
	PR_DEFINE_ENUM2(ETexAddrMode, PR_ENUM);
	#undef PR_ENUM

	// EFilter - MinMagMip
	#define PR_ENUM(x)\
		x(Point             ,= D3D12_FILTER_MIN_MAG_MIP_POINT)\
		x(PointPointLinear  ,= D3D12_FILTER_MIN_MAG_POINT_MIP_LINEAR)\
		x(PointLinearPoint  ,= D3D12_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT)\
		x(PointLinearLinear ,= D3D12_FILTER_MIN_POINT_MAG_MIP_LINEAR)\
		x(LinearPointPoint  ,= D3D12_FILTER_MIN_LINEAR_MAG_MIP_POINT)\
		x(LinearPointLinear ,= D3D12_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR)\
		x(LinearLinearPoint ,= D3D12_FILTER_MIN_MAG_LINEAR_MIP_POINT)\
		x(Linear            ,= D3D12_FILTER_MIN_MAG_MIP_LINEAR)\
		x(Anisotropic       ,= D3D12_FILTER_ANISOTROPIC)
	PR_DEFINE_ENUM2(EFilter, PR_ENUM);
	#undef PR_ENUM

	// EFillMode
	#define PR_ENUM(x)\
		x(Default   ,= 0)\
		x(Points    ,= 1)\
		x(Wireframe ,= D3D12_FILL_MODE::D3D12_FILL_MODE_WIREFRAME)\
		x(Solid     ,= D3D12_FILL_MODE::D3D12_FILL_MODE_SOLID)\
		x(SolidWire ,= 4)
	PR_DEFINE_ENUM2(EFillMode, PR_ENUM);
	#undef PR_ENUM

	// ECullMode
	#define PR_ENUM(x)\
		x(Default ,= 0)\
		x(None    ,= D3D12_CULL_MODE::D3D12_CULL_MODE_NONE)\
		x(Front   ,= D3D12_CULL_MODE::D3D12_CULL_MODE_FRONT)\
		x(Back    ,= D3D12_CULL_MODE::D3D12_CULL_MODE_BACK)
	PR_DEFINE_ENUM2(ECullMode , PR_ENUM);
	#undef PR_ENUM

	// ELight
	#define PR_ENUM(x)\
		x(Ambient    )\
		x(Directional)\
		x(Point      )\
		x(Spot       )
	PR_DEFINE_ENUM1(ELight, PR_ENUM);
	#undef PR_ENUM

	// EEye
	#define PR_ENUM(x)\
		x(Left )\
		x(Right)
	PR_DEFINE_ENUM1(EEye, PR_ENUM);
	#undef PR_ENUM

	// ERadial
	#define PR_ENUM(x)\
		x(Spherical)\
		x(Cylindrical)
	PR_DEFINE_ENUM1(ERadial, PR_ENUM);
	#undef PR_ENUM

	// EGpulush
	#define PR_ENUM(x)\
		x(DontFlush)\
		x(Async)\
		x(Block)
	PR_DEFINE_ENUM1(EGpuFlush, PR_ENUM);
	#undef PR_ENUM

	// Instances
	template <typename T>
	concept InstanceType = requires(T t)
	{
		{ std::is_same_v<decltype(t.m_base), BaseInstance> };                    // must have a member called 'm_base'
		{ static_cast<void const*>(&t.m_base) == static_cast<void const*>(&t) }; // it must be the first member
	};
	
	// Render steps
	template <typename T>
	concept RenderStepType = requires(T t)
	{
		{ std::is_base_of_v<RenderStep, T> };
		{ std::is_same_v<decltype(T::Id), ERenderStep> };
	};
}

// Enum flags
template <> struct is_flags_enum<DXGI_SWAP_CHAIN_FLAG> :std::true_type {};
