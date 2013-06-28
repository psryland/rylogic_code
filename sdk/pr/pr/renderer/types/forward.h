//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_FORWARD_H
#define PR_RDR_FORWARD_H

//#pragma warning(disable: 4995) // use of depricated code in stl

#include "pr/common/min_max_fix.h"
#include <vector>
#include <string>
#include <list>
#include <algorithm>
#include <intrin.h>
#include <windows.h>
#include <malloc.h> // for _alloca

//#pragma warning(default: 4995) // use of depricated code in stl

#include "pr/macros/link.h"
#include "pr/macros/count_of.h"
#include "pr/common/prtypes.h"
#include "pr/common/assert.h"
#include "pr/common/hresult.h"
#include "pr/common/prtypes.h"
#include "pr/common/chain.h"
#include "pr/common/d3dptr.h"
#include "pr/common/refcount.h"
#include "pr/common/fmt.h"
#include "pr/common/crc.h"
#include "pr/common/fixedpodarray.h"
#include "pr/common/valuecast.h"
#include "pr/common/alloca.h"
#include "pr/common/hash.h"
#include "pr/common/range.h"
#include "pr/common/array.h"
#include "pr/common/imposter.h"
#include "pr/common/events.h"
#include "pr/common/colour.h"
#include "pr/str/prstring.h"
#include "pr/str/tostring.h"
#include "pr/str/prstdstring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/storage/nugget_file/nuggetfile.h"
#include "pr/maths/maths.h"
#include "pr/geometry/geometry.h"
#include "pr/script/reader.h"
#include "pr/linedrawer/ldr_helper.h"
#include "pr/camera/camera.h"
#include "pr/renderer/utility/assert.h"
#include "pr/renderer/utility/errors.h"

namespace pr
{
	class Renderer;

	namespace rdr
	{
		typedef pr::uint8  ViewportId;
		typedef pr::uint16 Index;
		typedef pr::uint32 RdrId;
		typedef pr::uint32 SortKey;
		
		// A special value for automatically generating an Id
		RdrId const AutoId = ~0U;
		
		// Configuration
		struct DeviceConfig;
		struct System;
		struct Adapter;
		struct DisplayModeIter;
		struct RdrSettings;
		struct VPSettings;
		struct IAllocator;

		typedef pr::string<char, 32>   string32;
		typedef pr::string<char, 256>  string256;
		typedef pr::string<char, 1024> string1024;

		// Vertex formats
		class  VertexFormatManager;
		namespace vf
		{
			typedef pr::uint32 Type;
			typedef pr::uint32 Format;
			struct MemberOffsets;
			class  Iter;
		}

		// Render states
		class RenderStateManager;
		namespace rs
		{
			struct State;
			struct Block;
			struct StateEx;
			struct DeviceState;
			namespace stack_frame
			{
				struct Viewport;
				struct DLE;
				struct DLEShadows;
				struct RSB;
			}
		}

		// Lighting
		struct Light;
		class  LightingManager;

		// Textures
		struct TexInfo;
		struct Texture;
		struct TextureFilter;
		struct TextureAddrMode;
		typedef pr::RefPtr<Texture> TexturePtr;
		
		// Effects
		struct Effect;
		typedef pr::RefPtr<Effect> EffectPtr;
		namespace effect
		{
			struct Desc;
			namespace frag
			{
				struct Header;
				struct Txfm;
				struct Tinting;
				struct PVC;
				struct Texture2D;
				struct EnvMap;
				struct Lighting;
				struct Terminate;
			}
		}
		
		// Video
		struct Video;
		struct AllocPres;
		typedef pr::RefPtr<Video> VideoPtr;
		typedef pr::RefPtr<AllocPres> AllocPresPtr;
		
		// Materials
		struct Material;
		class  MaterialManager;
		
		// Models
		class  ModelManager;
		struct ModelBuffer;
		struct Model;
		typedef pr::RefPtr<Model> ModelPtr;
		typedef pr::RefPtr<ModelBuffer> ModelBufferPtr;
		struct RenderNugget;
		namespace model
		{
			namespace EPrimitive { enum Type; }
			namespace EUsage { enum Type; }
			enum EMemPool;
			struct MBSettings;
			struct MSettings;
			class Lock;
			struct RdrRenderNuggetChain;
			typedef pr::Range<size_t> Range;
		}

		// Instances
		namespace instance
		{
			struct ComponentDesc;
			struct Base;
			struct DrawlistData;
		}
		struct BasicInstance;
		
		// Viewports
		class  Viewport;
		class  Drawlist;
		struct DrawListElement;
		namespace viewport
		{
			struct RdrViewportChain;
		}
		
		// Enums
		namespace EState
		{
			enum Type { Idle, BuildingScene, PresentPending };
		}
		namespace EQuality
		{
			enum Type { Low,  Medium, High, NumberOf };
			inline char const* ToString(Type type)
			{
				switch (type) {
				default: return "";
				case Low: return "Low";
				case Medium: return "Medium";
				case High: return "High";
				}
			}
			inline Type Parse(char const* str)
			{
				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
				return static_cast<Type>(i);
			}
		}
		namespace EShaderVersion
		{
			enum Type { v0_0, v1_1, v1_4, v2_0, v3_0, NumberOf };
			inline char const* ToString(Type type)
			{
				switch (type) {
				default: return "";
				case v0_0: return "v0_0";
				case v1_1: return "v1_1";
				case v1_4: return "v1_4";
				case v2_0: return "v2_0";
				case v3_0: return "v3_0";
				}
			}
			inline Type Parse(char const* str)
			{
				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
				return static_cast<Type>(i);
			}
		}
		namespace ELight
		{
			enum Type { Ambient, Directional, Point, Spot, NumberOf };
			inline char const* ToString(Type type)
			{
				switch (type) {
				default:          return "";
				case Ambient:     return "Ambient";
				case Directional: return "Directional";
				case Point:       return "Point";
				case Spot:        return "Spot";
				}
			}
			inline Type Parse(char const* str)
			{
				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
				return static_cast<Type>(i);
			}
		}
		namespace EStockEffect
		{
			enum Type
			{
				TxTint = 1,
				TxTintPvc,
				TxTintTex,
				TxTintPvcTex,
				TxTintLitEnv,
				TxTintPvcLitEnv,
				TxTintTexLitEnv,
				TxTintPvcTexLitEnv,
			};
		}
		namespace EStockTexture
		{
			enum Type
			{
				Black,
				White,
				Checker,
				NumberOf
			};
			inline char const* ToString(Type type)
			{
				switch (type)
				{
				default:      return "";
				case Black:   return "black";
				case White:   return "white";
				case Checker: return "checker";
				}
			}
			inline Type Parse(char const* str)
			{
				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
				return static_cast<Type>(i);
			}
		}
		enum
		{
			MaxLights = 8,
			MaxShadowCasters = 4
		};
		namespace EDeviceResetPriority
		{
			enum Type
			{
				Normal = 0,
				LightingManager,
				ModelManager,
				MaterialManager,
				RenderStateManager,
				VertexFormatManager,
				Renderer,
			};
		}
	}
}

#endif
