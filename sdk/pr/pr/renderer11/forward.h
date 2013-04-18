//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_FORWARD_H
#define PR_RDR_FORWARD_H

#include "pr/common/min_max_fix.h"

#include <initguid.h>
#include <malloc.h>
#include <sdkddkver.h>
#include <vector>
#include <string>
#include <list>
#include <new>
#include <algorithm>
#include <functional>
#include <intrin.h>
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

//namespace pr
//{
//	// Example code:
//	#define PR_REFPTR_TRACE 1
//	template <typename T> inline long PtrRefCount(T*);
//	template <typename T> inline void RefPtrTrace(bool, T*){}
//	template <> inline void RefPtrTrace<ID3D11DeviceContext>(bool add, ID3D11DeviceContext* ptr)
//	{
//		OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
//		pr::StackDump(3,5,[](std::string const& file, int line)
//		{
//			OutputDebugStringA(pr::FmtS("%s(%d):\n", file.c_str(), line));
//		});
//	}
//}
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
#include "pr/storage/nugget_file/nuggetfile.h"
#include "pr/maths/maths.h"
#include "pr/script/reader.h"
#include "pr/linedrawer/ldr_helper.h"
#include "pr/camera/camera.h"

#define PR_DBG_RDR PR_DBG

namespace pr
{
	class Renderer;

	namespace rdr
	{
		typedef pr::uint8  byte;
//		typedef pr::uint8  ViewportId;
//		typedef pr::uint16 Index;
		typedef uintptr_t  RdrId;
		typedef pr::uint32 SortKey;
		typedef pr::uint16 SortKeyId;
		RdrId const AutoId = ~0U; // A special value for automatically generating an Id
		
		typedef pr::string<char, 32>     string32;
		typedef pr::string<wchar_t, 32>  wstring32;
		typedef pr::string<wchar_t, 256> wstring256;
//		typedef pr::string<char, 1024> string1024;
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
//		class  VertexFormatManager;
//		namespace vf
//		{
//			typedef pr::uint32 Type;
//			typedef pr::uint32 Format;
//			struct MemberOffsets;
//			class  Iter;
//		}

//		// Lighting
//		struct Light;
//		class  LightingManager;
		
		// Shaders
		class  ShaderManager;
		struct ShaderDesc;
		struct BaseShader;   typedef pr::RefPtr<BaseShader> ShaderPtr;
		
		// Textures
		class  TextureManager;
		struct TextureDesc;
		struct Texture2D;     typedef pr::RefPtr<Texture2D> Texture2DPtr;
//		struct TextureFilter;
//		struct TextureAddrMode;
//		namespace effect
//		{
//			struct Desc;
//			namespace frag
//			{
//				struct Header;
//				struct Txfm;
//				struct Tinting;
//				struct PVC;
//				struct Texture2D;
//				struct EnvMap;
//				struct Lighting;
//				struct Terminate;
//			}
//		}
//		
//		// Video
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
		struct DrawMethod;
		struct Drawlist;
		struct DrawListElement;

		// Enums
		namespace ERenderMethod
		{
			enum Type { None, Forward, Deferred };
		}
		namespace EDbgRdrFlags
		{
			enum Type { WarnedNoRenderNuggets = 1 << 0 };
		}

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
//		namespace EShaderVersion
//		{
//			enum Type { v0_0, v1_1, v1_4, v2_0, v3_0, NumberOf };
//			inline char const* ToString(Type type)
//			{
//				switch (type) {
//				default: return "";
//				case v0_0: return "v0_0";
//				case v1_1: return "v1_1";
//				case v1_4: return "v1_4";
//				case v2_0: return "v2_0";
//				case v3_0: return "v3_0";
//				}
//			}
//			inline Type Parse(char const* str)
//			{
//				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
//				return static_cast<Type>(i);
//			}
//		}

//		namespace EStockEffect
//		{
//			enum Type
//			{
//				TxTint = 1,
//				TxTintPvc,
//				TxTintTex,
//				TxTintPvcTex,
//				TxTintLitEnv,
//				TxTintPvcLitEnv,
//				TxTintTexLitEnv,
//				TxTintPvcTexLitEnv,
//			};
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

#endif
