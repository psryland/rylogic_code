//*******************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2002
//*******************************************************************************************

#ifndef LDR_API
#define LDR_API(src, rtype, fname, params)
#endif

// Optional functions that the plugin can implement ****************************************
// Called on startup/shutdown of a plugin
LDR_API(LDR_IMPORT, void                  ,Initialise           ,(ldrapi::PluginHandle handle, wchar_t const* args))
LDR_API(LDR_IMPORT, void                  ,Uninitialise         ,())

// Implementing this will cause the plugin to be stepped periodically (on a windows timer)
LDR_API(LDR_IMPORT, void                  ,Step                 ,(double elapsed_s))

//LDR_EXPORT pr::ldr::EPlugInResult	ldrNotifyKeyDown			(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags);
//LDR_EXPORT pr::ldr::EPlugInResult	ldrNotifyKeyUp				(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags);
//LDR_EXPORT pr::ldr::EPlugInResult 	ldrNotifyMouseDown			(unsigned int button, pr::v2 position);
//LDR_EXPORT pr::ldr::EPlugInResult 	ldrNotifyMouseMove			(pr::v2 position);
//LDR_EXPORT pr::ldr::EPlugInResult 	ldrNotifyMouseWheel			(unsigned int nFlags, short zDelta, pr::v2 position);
//LDR_EXPORT pr::ldr::EPlugInResult 	ldrNotifyMouseUp			(unsigned int button, pr::v2 position);
//LDR_EXPORT pr::ldr::EPlugInResult 	ldrNotifyMouseClk			(unsigned int button, pr::v2 position);
//LDR_EXPORT pr::ldr::EPlugInResult 	ldrNotifyMouseDblClk		(unsigned int button, pr::v2 position);
//LDR_EXPORT void					ldrNotifyDeleteObject		(pr::ldr::ObjectHandle object);
//LDR_EXPORT void					ldrNofifyRefresh			();
// Functions that the plugin must/can implement ****************************************

// Functions implemented by linedrawer *************************************************
LDR_API(LDR_EXPORT, ldrapi::ObjectHandle  ,RegisterObject       ,(ldrapi::PluginHandle handle, char const* object_description, wchar_t const* include_paths, pr::Guid const* ctx_id, bool async))
LDR_API(LDR_EXPORT, void                  ,UnregisterObject     ,(ldrapi::PluginHandle handle, ldrapi::ObjectHandle object))
LDR_API(LDR_EXPORT, void                  ,UnregisterAllObjects ,(ldrapi::PluginHandle handle))
LDR_API(LDR_EXPORT, void                  ,Render               ,(ldrapi::PluginHandle handle))
LDR_API(LDR_EXPORT, HWND                  ,MainWindowHandle     ,(ldrapi::PluginHandle handle))
LDR_API(LDR_EXPORT, void                  ,Error                ,(ldrapi::PluginHandle handle, char const* err_msg))
LDR_API(LDR_EXPORT, void                  ,Status               ,(ldrapi::PluginHandle handle, char const* msg, bool bold, int priority, DWORD min_display_time_ms))
LDR_API(LDR_EXPORT, void                  ,MouseStatusUpdates   ,(ldrapi::PluginHandle handle, bool enable))
//Import	bool					ldrSource					(const char* src, std::size_t len, bool clear_data, bool recentre);
//Import	pr::ldr::ObjectHandle	ldrRegisterCustomObject		(pr::ldr::CustomObjectData const& settings);
//Import	unsigned int			ldrGetNumPluginObjects		();
//Import	void					ldrEditObject				(pr::ldr::ObjectHandle object, pr::ldr::EditObjectFunc func, void* user_data);
LDR_API(LDR_EXPORT, pr::m4x4              ,ObjectO2W            ,(ldrapi::ObjectHandle object))
LDR_API(LDR_EXPORT, void                  ,ObjectSetO2W         ,(ldrapi::ObjectHandle object, pr::m4x4 const& o2w))
LDR_API(LDR_EXPORT, bool                  ,ObjectVisible        ,(ldrapi::ObjectHandle object))
LDR_API(LDR_EXPORT, void                  ,ObjectSetVisible     ,(ldrapi::ObjectHandle object, bool visible, char const* name))
LDR_API(LDR_EXPORT, bool                  ,ObjectWireframe      ,(ldrapi::ObjectHandle object))
LDR_API(LDR_EXPORT, void                  ,ObjectSetWireframe   ,(ldrapi::ObjectHandle object, bool wireframe, char const* name))
//Import	void					ldrSetObjectColour			(pr::ldr::ObjectHandle object, pr::Colour32 colour);
//Import	void					ldrSetObjectSemiTransparent	(pr::ldr::ObjectHandle object, bool on);
//Import	void					ldrSetObjectTransform   	(pr::ldr::ObjectHandle object, const pr::m4x4& object_to_world);
//Import	void					ldrSetObjectPosition    	(pr::ldr::ObjectHandle object, const pr::v4& position);
//Import	void					ldrSetObjectUserData		(pr::ldr::ObjectHandle object, void* user_data);
//Import	pr::Colour32			ldrGetObjectColour			(pr::ldr::ObjectHandle object);
//Import	pr::v4					ldrGetFocusPoint        	();
////Import	const char*				ldrGetSourceString			(pr::ldr::ObjectHandle object);
//Import	void*					ldrGetObjectUserData		(pr::ldr::ObjectHandle object);
//Import	pr::m4x4				ldrGetCameraToWorld			();
//Import	pr::ldr::CameraData		ldrGetCameraData			();
//Import	pr::v4					ldrScreenToWorld			(pr::v4 ss_position);
//Import	void					ldrView						(const pr::BBox& bbox);
//Import	void					ldrViewAll					();
// Functions implemented by linedrawer *************************************************

#undef LDR_API

#ifndef LDR_PLUGIN_INTERFACE_H
#define LDR_PLUGIN_INTERFACE_H

#include <windows.h>
#include "pr/maths/maths.h"

// Note: Don't include "ldr_object.h" in here. Clients must choose to use this file
// explicitly. If they include "ldr_object.h" but not "ldr_object.cpp" then they can
// use the public members of a 'pr::ldr::LdrObject' but not the methods defined in
// the cpp file. Doing this is risky tho, the members of LdrObject could be different
// between LineDrawer and the plugin due to different compiler settings.

#if LDR_EXPORTS // Defined when this header is being built in the LineDrawer project
#define LDR_EXPORT  __declspec(dllexport)
#define LDR_IMPORT  __declspec(dllimport)
#else
#define LDR_EXPORT  __declspec(dllimport)
#define LDR_IMPORT  __declspec(dllexport)
#endif

// Forward declarations
namespace ldr { struct Plugin; }
namespace pr { namespace ldr { struct LdrObject; } }

namespace ldrapi
{
	// A handle for each plugin to identify itself
	typedef ::ldr::Plugin* PluginHandle;

	// A handle to each created ldr object
	typedef ::pr::ldr::LdrObject* ObjectHandle;

	// Callback function for when a menu item is clicked
	typedef void (__stdcall *OnMenuClickCB)(void* ctx, int id);

	// Generate function typedefs
	#define LDR_API(src, rtype, fname, params) typedef rtype (*Plugin_##fname) params;
	#include "ldr_plugin_interface.h"

	// Generate function pointers to the api functions for plugin projects only
	// Linedrawer cannot use these function pointers as it loads multiple plugins
	#if !LDR_EXPORTS

	// Generate function pointers to the api functions
	#define LDR_API(src, rtype, fname, params) static Plugin_##fname fname;
	#include "ldr_plugin_interface.h"

	// Setup the function pointers
	inline void InitAPI()
	{
		HMODULE ldr_exe = ::GetModuleHandleA(0);
		#define LDR_API(src, rtype, fname, params) fname = (Plugin_##fname)::GetProcAddress(ldr_exe, "ldr"#fname);
		#include "ldr_plugin_interface.h"
	}

	// A helper wrapper around LdrObjects
	struct Object
	{
		ObjectHandle m_obj;

		Object() :m_obj()                           {}
		Object(ObjectHandle obj) :m_obj(obj)        {}
		void operator =(ObjectHandle obj)           { m_obj = obj; }
		operator ObjectHandle() const               { return m_obj; }
		pr::m4x4 O2W() const                        { return ObjectO2W(m_obj); }
		void O2W(pr::m4x4 const& o2w)               { return ObjectSetO2W(m_obj, o2w); }
		bool Visible() const                        { return ObjectVisible(m_obj); }
		void Visible(bool vis, char const* name)    { return ObjectSetVisible(m_obj, vis, name); }
		bool Wireframe() const                      { return ObjectWireframe(m_obj); }
		void Wireframe(bool wire, char const* name) { return ObjectSetWireframe(m_obj, wire, name); }
	};
	#endif
}

// Declare imported/exported functions with "C" linkage
// These have the form: extern "C" __declspec(dllexport) void ldrFunction(int);
// Plugins should use functions of the form: ldrapi::Function(int);
#define LDR_API(src, rtype, fname, params) src rtype ldr##fname params;
#include "ldr_plugin_interface.h"

#endif
