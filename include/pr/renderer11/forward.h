//*********************************************
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
#include <functional>
#include <type_traits>
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
#include "pr/meta/optional.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/build_options.h"
#include "pr/common/assert.h"
#include "pr/common/hresult.h"
#include "pr/common/prtypes.h"
#include "pr/common/fmt.h"
#include "pr/common/cast.h"
#include "pr/common/flags_enum.h"
#include "pr/common/stackdump.h"
#include "pr/common/refcount.h"
#include "pr/common/log.h"
#include "pr/common/refptr.h"
#include "pr/common/d3dptr.h"
#include "pr/common/chain.h"
#include "pr/common/crc.h"
#include "pr/common/alloca.h"
#include "pr/common/range.h"
#include "pr/common/events.h"
#include "pr/common/colour.h"
#include "pr/common/new.h"
#include "pr/common/to.h"
#include "pr/common/scope.h"
#include "pr/common/algorithm.h"
#include "pr/common/user_data.h"
#include "pr/common/static_callback.h"
#include "pr/container/array_view.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/crypt/hash.h"
#include "pr/camera/camera.h"
#include "pr/str/string.h"
#include "pr/str/to_string.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/maths/maths.h"
#include "pr/maths/bit_fields.h"
#include "pr/geometry/common.h"
#include "pr/geometry/models_point.h"
#include "pr/geometry/models_line.h"
#include "pr/geometry/models_quad.h"
#include "pr/geometry/models_shape2d.h"
#include "pr/geometry/models_box.h"
#include "pr/geometry/models_sphere.h"
#include "pr/geometry/models_cylinder.h"
#include "pr/geometry/models_extrude.h"
#include "pr/geometry/models_mesh.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/triangle.h"
#include "pr/geometry/model_file.h"
#include "pr/geometry/utility.h"
#include "pr/threads/synchronise.h"
#include "pr/gui/gdiplus.h"
#include "pr/win32/windows_com.h"
#include "pr/script/reader.h"
#include "pr/storage/nugget_file/nuggetfile.h"
#include "pr/linedrawer/ldr_helper.h"

#define PR_DBG_RDR PR_DBG

namespace pr
{
	class Renderer;

	namespace rdr
	{
		using byte      = unsigned char;
		using RdrId     = std::uintptr_t;
		using SortKeyId = pr::uint16;
		using Range     = pr::Range<size_t>;

		using string32   = pr::string<char, 32>;
		using string512  = pr::string<char, 512>;
		using wstring32  = pr::string<wchar_t, 32>;
		using wstring256 = pr::string<wchar_t, 256>;

		static Range const RangeZero = {0,0};
		static RdrId const AutoId = ~RdrId(); // A special value for automatically generating an Id
		static RdrId const InvalidId = RdrId();

		using EGeom = pr::geometry::EGeom;
		using EPrim = pr::geometry::EPrim;

		// Render
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
		using RenderStepPtr = std::shared_ptr<RenderStep>;

		// Models
		class  ModelManager;
		struct ModelBuffer;
		struct Model;
		struct NuggetProps;
		struct Nugget;
		struct MdlSettings;
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
		struct Texture2D;
		struct Image;
		struct AllocPres;
		struct ProjectedTexture;
		using Texture2DPtr = pr::RefPtr<Texture2D>;

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
		template <class T> struct Allocator;
		using InvokeFunc = void (__stdcall *)(void* ctx);

		// EResult
		#define PR_ENUM(x)\
			x(Success       ,= 0         )\
			x(Failed        ,= 0x80000000)\
			x(InvalidValue  ,)
		PR_DEFINE_ENUM2_BASE(EResult, PR_ENUM, uint);
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
			x(_bitwise_operators_allowed, = 0x7FFFFFFF)
		PR_DEFINE_ENUM2(EShaderType, PR_ENUM);
		#undef PR_ENUM

		// ETexAddrMode
		#define PR_ENUM(x)\
			x(Wrap       ,= D3D11_TEXTURE_ADDRESS_WRAP)\
			x(Mirror     ,= D3D11_TEXTURE_ADDRESS_MIRROR)\
			x(Clamp      ,= D3D11_TEXTURE_ADDRESS_CLAMP)\
			x(Border     ,= D3D11_TEXTURE_ADDRESS_BORDER)\
			x(MirrorOnce ,= D3D11_TEXTURE_ADDRESS_MIRROR_ONCE)
		PR_DEFINE_ENUM2(ETexAddrMode, PR_ENUM);
		#undef PR_ENUM

		// EFilter - MinMagMip
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
		PR_DEFINE_ENUM2(EFilter, PR_ENUM);
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
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::rdr
{
	PRUnitTest(ForwardTests)
	{
	}
}
#endif
