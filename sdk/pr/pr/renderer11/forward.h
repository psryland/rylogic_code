//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_FORWARD_H
#define PR_RDR_FORWARD_H

#include "pr/common/min_max_fix.h"

#include <vector>
#include <string>
#include <list>
#include <new>
#include <algorithm>
#include <functional>
#include <intrin.h>
#include <malloc.h>
#include <sdkddkver.h>
#include <windows.h>
#include <d3d11.h>
#include <d3d11sdklayers.h>

#include "pr/macros/link.h"
#include "pr/macros/count_of.h"
#include "pr/macros/repeat.h"
#include "pr/macros/enum.h"
#include "pr/common/prtypes.h"
#include "pr/common/assert.h"
#include "pr/common/hresult.h"
#include "pr/common/prtypes.h"
#include "pr/common/fmt.h"
#include "pr/common/stackdump.h"
#include "pr/common/refcount.h"
#include "pr/common/log.h"
#include "pr/common/refptr.h"
#include "pr/common/d3dptr.h"
#include "pr/common/chain.h"
#include "pr/common/crc.h"
#include "pr/common/fixedpodarray.h"
#include "pr/common/valuecast.h"
#include "pr/common/byte_ptr_cast.h"
#include "pr/common/alloca.h"
#include "pr/common/hash.h"
#include "pr/common/range.h"
#include "pr/common/array.h"
#include "pr/common/imposter.h"
#include "pr/common/events.h"
#include "pr/common/colour.h"
#include "pr/str/prstring.h"
#include "pr/str/prstdstring.h"
#include "pr/str/tostring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/camera/camera.h"
#include "pr/storage/nugget_file/nuggetfile.h"
#include "pr/maths/maths.h"
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
		RdrId const AutoId = ~0U; // A special value for automatically generating an Id
		
		typedef pr::string<char, 32>     string32;
		typedef pr::string<char, 512>    string512;
		typedef pr::string<wchar_t, 32>  wstring32;
		typedef pr::string<wchar_t, 256> wstring256;
		typedef pr::Range<size_t> Range;
		const Range RangeZero = {0,0};

		// Configuration
//		struct DeviceConfig;
//		struct System;
//		struct Adapter;
//		struct DisplayModeIter;
//		struct RdrSettings;
//		struct VPSettings;
//		struct IAllocator;

		// Util
		struct Lock;
		struct MLock;
		template <class T> struct Allocator;

		// Input layouts
		class InputLayoutManager;

		// Lighting
		struct Light;

		// Shaders
		class  ShaderManager;
		struct ShaderDesc;
		struct BaseShader;
		typedef pr::RefPtr<BaseShader> ShaderPtr;
		
		// Textures
		class  TextureManager;
		struct TextureDesc;
		struct Texture2D;
		struct Image;
		struct AllocPres;
		//struct Video;
		typedef pr::RefPtr<Texture2D> Texture2DPtr;
		//typedef pr::RefPtr<AllocPres> AllocPresPtr;
		//typedef pr::RefPtr<Video>     VideoPtr;

		// Video
//		struct Video;
//		struct AllocPres;
//		typedef pr::RefPtr<Video> VideoPtr;
//		typedef pr::RefPtr<AllocPres> AllocPresPtr;
		
		// Models
		class  ModelManager;
		struct ModelBuffer;    typedef pr::RefPtr<ModelBuffer> ModelBufferPtr;
		struct Model;          typedef pr::RefPtr<Model>       ModelPtr;
		struct Nugget;         typedef pr::chain::head<Nugget, struct ChainGroupNugget> TNuggetChain;
		struct VertP;
		struct VertPC;
		struct VertPCT;
		struct VertPCNT;
		struct MdlSettings;
		
		// Instances
		struct BaseInstance;
		
		// Scenes
		struct SceneView;
		struct Scene;
		struct Stereo;
		struct DrawMethod;
		struct Drawlist;
		struct DrawListElement;

		// Rendering
		struct BSBlock;
		struct DSBlock;
		struct RSBlock;

		// Enums
		namespace ERenderMethod
		{
			enum Type { None, Forward, Deferred };
		}
		namespace EDbgRdrFlags
		{
			enum Type { WarnedNoRenderNuggets = 1 << 0 };
		}

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

		// EGeom
		#define PR_ENUM(x) /*
			*/x(Unknown ,= 0     ) /*
			*/x(Vert    ,= 1 << 0) /* Object space 3D position
			*/x(Colr    ,= 1 << 1) /* Diffuse base colour
			*/x(Norm    ,= 1 << 2) /* Object space 3D normal
			*/x(Tex0    ,= 1 << 3) // Diffuse texture
		PR_DEFINE_ENUM2_FLAGS(EGeom, PR_ENUM);
		#undef PR_ENUM

		// EConstBuf
		#define PR_ENUM(x)\
			x(FrameConstants)\
			x(ModelConstants)
		PR_DEFINE_ENUM1(EConstBuf, PR_ENUM);
		#undef PR_ENUM

		// EShader
		#define PR_ENUM(x)\
			x(TxTint         )\
			x(TxTintPvc      )\
			x(TxTintTex      )\
			x(TxTintPvcLitTex)
		PR_DEFINE_ENUM1(EShader, PR_ENUM);
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

		//		namespace EQuality
//		{
//			enum Type { Low,  Medium, High, NumberOf };
//			inline char const* ToString(Type type)
//			{
//				switch (type) {
//				default: return "";
//				case Low: return "Low";
//				case Medium: return "Medium";
//				case High: return "High";
//				}
//			}
//			inline Type Parse(char const* str)
//			{
//				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
//				return static_cast<Type>(i);
//			}
//		}
//		enum
//		{
//			MaxLights = 8,
//			MaxShadowCasters = 4
//		};
//		namespace EDeviceResetPriority
//		{
//			enum Type
//			{
//				Normal = 0,
//				LightingManager,
//				ModelManager,
//				MaterialManager,
//				RenderStateManager,
//				VertexFormatManager,
//				Renderer,
//			};
//		}

		// Events
		struct Evt_Resize
		{
			bool    m_done;  // True when the swap chain has resized it's buffers
			pr::iv2 m_area;  // The new size that the swap chain buffers (will) have
			Evt_Resize(bool done, pr::iv2 const& area) :m_done(done) ,m_area(area) {}
		};
		struct Evt_SceneRender
		{
			pr::rdr::Scene* m_scene; // The scene that's about to render
			explicit Evt_SceneRender(pr::rdr::Scene* scene) :m_scene(scene) {}
		};
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

#endif
