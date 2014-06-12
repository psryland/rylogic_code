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

#include <intrin.h>
#include <malloc.h>
#include <sdkddkver.h>
#include <windows.h>
#include <d3d11.h>
#include <d3d11_1.h>
#include <d3d11sdklayers.h>
#include <d2d1_2.h>

#include "pr/macros/link.h"
#include "pr/macros/count_of.h"
#include "pr/macros/repeat.h"
#include "pr/macros/enum.h"
#include "pr/macros/no_copy.h"
#include "pr/meta/alignment_of.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/prtypes.h"
#include "pr/common/assert.h"
#include "pr/common/hresult.h"
#include "pr/common/prtypes.h"
#include "pr/common/fmt.h"
#include "pr/common/cast.h"
#include "pr/common/stackdump.h"
#include "pr/common/refcount.h"
#include "pr/common/log.h"
#include "pr/common/refptr.h"
#include "pr/common/d3dptr.h"
#include "pr/common/chain.h"
#include "pr/common/crc.h"
#include "pr/common/valuecast.h"
#include "pr/common/cast.h"
#include "pr/common/alloca.h"
#include "pr/common/hash.h"
#include "pr/common/range.h"
#include "pr/common/array.h"
#include "pr/common/imposter.h"
#include "pr/common/events.h"
#include "pr/common/colour.h"
#include "pr/common/new.h"
#include "pr/common/to.h"
#include "pr/common/scope.h"
#include "pr/common/container_functions.h"
#include "pr/common/user_data.h"
#include "pr/str/prstring.h"
#include "pr/str/prstdstring.h"
#include "pr/str/tostring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/camera/camera.h"
#include "pr/storage/nugget_file/nuggetfile.h"
#include "pr/maths/maths.h"
#include "pr/gui/gdiplus.h"
#include "pr/script/reader.h"
#include "pr/linedrawer/ldr_helper.h"

#include "pr/geometry/line.h"
#include "pr/geometry/quad.h"
#include "pr/geometry/box.h"
#include "pr/geometry/sphere.h"
#include "pr/geometry/cylinder.h"
#include "pr/geometry/mesh.h"
#include "pr/geometry/utility.h"

#define PR_DBG_RDR PR_DBG

namespace pr
{
	class Renderer;

	namespace rdr
	{
		typedef pr::uint8  byte;
		typedef uintptr_t  RdrId;
		typedef pr::uint32 SortKey;
		typedef pr::uint16 SortKeyId;
		static RdrId const AutoId = ~0U; // A special value for automatically generating an Id
		static RdrId const InvalidId = 0U;

		typedef pr::string<char, 32>     string32;
		typedef pr::string<char, 512>    string512;
		typedef pr::string<wchar_t, 32>  wstring32;
		typedef pr::string<wchar_t, 256> wstring256;
		typedef pr::Range<size_t> Range;
		const Range RangeZero = {0,0};

		typedef pr::geometry::EGeom EGeom;

		// Render
		struct Window;
		struct Scene;
		struct SceneView;

		// Rendering
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
		typedef std::shared_ptr<RenderStep> RenderStepPtr;

		// Models
		class  ModelManager;
		struct ModelBuffer;
		struct Model;
		struct NuggetProps;
		struct Nugget;
		struct MdlSettings;
		typedef pr::RefPtr<ModelBuffer> ModelBufferPtr;
		typedef pr::RefPtr<Model> ModelPtr;
		typedef pr::chain::head<Nugget, struct ChainGroupNugget> TNuggetChain;

		// Instances
		struct BaseInstance;

		// Shaders
		struct Vert;
		class  ShaderManager;
		struct ShaderDesc;
		struct ShaderBase;
		struct ShaderSet;
		struct ShaderMap;
		typedef pr::RefPtr<ShaderBase> ShaderPtr;

		// Textures
		class  TextureManager;
		struct TextureDesc;
		struct Texture2D;
		struct TextureGdi;
		struct Image;
		struct AllocPres;
		struct ProjectedTexture;
		typedef pr::RefPtr<Texture2D> Texture2DPtr;
		typedef pr::RefPtr<TextureGdi> TextureGdiPtr;

		// Video
		//struct Video;
		//struct AllocPres;
		//typedef pr::RefPtr<Video> VideoPtr;
		//typedef pr::RefPtr<AllocPres> AllocPresPtr;

		// Lighting
		struct Light;

		// Util
		class BlendStateManager;
		class DepthStateManager;
		class RasterStateManager;
		struct Lock;
		struct MLock;
		template <class T> struct Allocator;

		// EResult
		#define PR_ENUM(x)/*
			*/x(Success       ,= 0         )/*
			*/x(Failed        ,= 0x80000000)/*
			*/x(InvalidValue  ,)
		PR_DEFINE_ENUM2(EResult, PR_ENUM);
		#undef PR_ENUM

		// EPrim
		#define PR_ENUM(x)\
			x(Invalid   ,= D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED)\
			x(PointList ,= D3D11_PRIMITIVE_TOPOLOGY_POINTLIST)\
			x(LineList  ,= D3D11_PRIMITIVE_TOPOLOGY_LINELIST)\
			x(LineStrip ,= D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP)\
			x(TriList   ,= D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST)\
			x(TriStrip  ,= D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP)
		PR_DEFINE_ENUM2(EPrim, PR_ENUM);
		#undef PR_ENUM

		// EShaderType (in order of execution on the HW) http://msdn.microsoft.com/en-us/library/windows/desktop/ff476882(v=vs.85).aspx
		#define PR_ENUM(x)\
			x(Invalid ,= 0)\
			x(VS      ,= 1 << 0)\
			x(HS      ,= 1 << 1)\
			x(DS      ,= 1 << 2)\
			x(GS      ,= 1 << 3)\
			x(PS      ,= 1 << 4)\
			x(All     ,= ~0)
		PR_DEFINE_ENUM2_FLAGS(EShaderType, PR_ENUM);
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
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_renderer11_forward)
		{
		}
	}
}
#endif
