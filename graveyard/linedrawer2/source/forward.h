//***************************************************************************
//
//	LineDrawer Forward Declarations
//
//***************************************************************************
#ifndef LINE_DRAWER_FORWARD_H
#define LINE_DRAWER_FORWARD_H

#include "pr/common/PRArray.h"
#include "pr/renderer/types/Forward.h"

enum EGlobalWireframeMode
{
	EGlobalWireframeMode_NotSet = -1,
	EGlobalWireframeMode_Solid = 0,
	EGlobalWireframeMode_Wire,
	EGlobalWireframeMode_SolidAndWire,
	EGlobalWireframeMode_NumberOf
};

// App types
class  LineDrawer;
class  DataManager;
class  StringParser;
struct FileLoader;
class  LineDrawerGUI;

// Object types
struct LdrObject;
#define LDR_OBJECT(identifier, hash) struct identifier;
#include "LineDrawer/Objects/LdrObjects.inc"

// Event types
struct GUIUpdate;

// Interface types
namespace pr
{
	namespace ldr
	{
		extern const char g_version_string[];
		extern const uint g_version_string_length;

		typedef void (*EditObjectFunc)(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager2& mat_mgr);
		struct CustomObjectData;
	}
}

#if USE_NEW_PARSER
// Object array
typedef pr::Array<LdrObject*, 8> TLdrObjectPtrVec;
#endif

#endif//LINE_DRAWER_FORWARD_H