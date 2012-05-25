//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#include "Stdafx.h"
#include "LineDrawer/PlugIn/PlugInManager.h"
#include "LineDrawer/Source/LineDrawer.h"

extern PlugInManager* g_PlugInManager;

using namespace pr;
using namespace pr::ldr;

extern "C"
{
	bool				ldrSource(const char* src, std::size_t len, bool clear_data, bool recentre)			{ return LineDrawer::Get().RefreshFromString(src, len, clear_data, recentre); }
	ldr::ObjectHandle	ldrRegisterObject(const char* object_description, std::size_t length)				{ return g_PlugInManager->RegisterObject(object_description, length); }
	ldr::ObjectHandle	ldrRegisterCustomObject(pr::ldr::CustomObjectData const& settings)					{ return g_PlugInManager->RegisterCustomObject(settings); }
	void				ldrUnRegisterObject(ldr::ObjectHandle object)										{ g_PlugInManager->UnRegisterObject(object); }
	void				ldrUnRegisterAllObjects()															{ g_PlugInManager->UnRegisterAllObjects(); }
	unsigned int		ldrGetNumPluginObjects()															{ return g_PlugInManager->GetNumPluginObjects(); }
	void				ldrEditObject(ldr::ObjectHandle object, ldr::EditObjectFunc func, void* user_data)	{ return g_PlugInManager->EditObject(object, func, user_data); }
	void				ldrSetLDWindowText(const char* str)													{ g_PlugInManager->SetLDWindowText(str); }
	void				ldrSetPollingFreq(float step_rate_hz)												{ g_PlugInManager->SetPollingFreq(step_rate_hz); }
	void				ldrSetObjectSemiTransparent(ldr::ObjectHandle object, bool on)						{ g_PlugInManager->SetObjectSemiTransparent(object, on); }
	void				ldrSetObjectColour(ldr::ObjectHandle object, Colour32 colour)						{ g_PlugInManager->SetObjectColour(object, colour); }
	void				ldrSetObjectPosition(ldr::ObjectHandle object, const v4& position)					{ g_PlugInManager->SetObjectPosition(object, position); }
	void				ldrSetObjectTransform(ldr::ObjectHandle object, const m4x4& object_to_world)		{ g_PlugInManager->SetObjectTransform(object, object_to_world); }
	void				ldrSetObjectUserData(pr::ldr::ObjectHandle object, void* user_data)					{ g_PlugInManager->SetObjectUserData(object, user_data); }
	pr::Colour32		ldrGetObjectColour(pr::ldr::ObjectHandle object)									{ return g_PlugInManager->GetObjectColour(object); }
	pr::IRect			ldrGetMainWindowRect()																{ pr::IRect rect; GetWindowRect(LineDrawer::Get().m_window_handle, reinterpret_cast<RECT*>(&rect)); return rect; }
	pr::IRect			ldrGetMainClientRect()																{ pr::IRect rect; GetClientRect(LineDrawer::Get().m_window_handle, reinterpret_cast<RECT*>(&rect)); return rect; }
	pr::v4				ldrGetFocusPoint()																	{ return LineDrawer::Get().m_navigation_manager.GetFocusPoint(); }
	//const char*			ldrGetSourceString(pr::ldr::ObjectHandle object)									{ return g_PlugInManager->GetSourceString(object); }
	void*				ldrGetObjectUserData(pr::ldr::ObjectHandle object)									{ return g_PlugInManager->GetObjectUserData(object); }
	pr::m4x4			ldrGetCameraToWorld()																{ return LineDrawer::Get().m_navigation_manager.GetCameraToWorld(); }
	CameraData			ldrGetCameraData()																	{ return LineDrawer::Get().m_navigation_manager.GetCameraData(); }
	pr::v4				ldrScreenToWorld(v4 ss_position)													{ return LineDrawer::Get().m_navigation_manager.m_camera.ScreenToWorld(ss_position); }
	void				ldrView(const pr::BoundingBox& bbox)												{ LineDrawer::Get().m_navigation_manager.SetView(bbox); LineDrawer::Get().m_navigation_manager.ApplyView(); }
	void				ldrViewAll()																		{ LineDrawer::Get().m_navigation_manager.SetView(LineDrawer::Get().m_data_manager.m_bbox); LineDrawer::Get().m_navigation_manager.ApplyView(); }
	void				ldrRender()																			{ LineDrawer::Get().Refresh(); }
	void				ldrErrorReport(char const* err_msg)													{ LineDrawer::Get().m_error_output.Error(err_msg); }
}// extern "C"

// Add an object to the plugin data list
ObjectHandle PlugInManager::RegisterObject(const char* object_description, std::size_t length)
{
#if USE_OLD_PARSER
	// Parse the object
	StringParser string_parser(m_linedrawer);
	if( !string_parser.Parse(object_description, length) ) return InvalidObjectHandle;
	if( string_parser.GetNumObjects() != 1 )
	{
		PR_ASSERT_STR(PR_DBG_LDR, string_parser.GetNumObjects() == 0, "Cannot register multiple objects to one handle");
		return InvalidObjectHandle;
	}
	LdrObject* object = string_parser.GetObject(0);
#else
	length;// delete this
	ParseResult data;
	if (!ParseSource(*m_linedrawer, object_description, data)) return InvalidObjectHandle;
	if (data.NumObjects() != 1)
	{
		PR_ASSERT_STR(PR_DBG_LDR, data.NumObjects() == 0, "Cannot register multiple objects to one handle");
		return InvalidObjectHandle;
	}
	LdrObject* object = data.GetObject(0);
#endif

	// Sanity check the object
	BoundingBox bbox = object->BBox(true);
	if( bbox != BBoxReset )
	{
		if( bbox.m_centre + v4ZAxis == bbox.m_centre )
		{
			m_linedrawer->m_error_output.Error("BoundingBox for registered object too distant from origin");
			return InvalidObjectHandle;
		}
		if( bbox.m_radius + (v4One - v4Origin) == bbox.m_radius )
		{
			m_linedrawer->m_error_output.Error("BoundingBox for registered object too large");
			return InvalidObjectHandle;
		}
	}

	// Register the object with the data manager
	m_linedrawer->m_data_manager.AddObject(object);

	PR_ASSERT(PR_DBG_LDR, m_plugin_objects.find(object) == m_plugin_objects.end());
	m_plugin_objects.insert(object);
	return object;
}

// Create a custom object.
ObjectHandle PlugInManager::RegisterCustomObject(pr::ldr::CustomObjectData const& settings)
{
	TCustom* object = new TCustom(*m_linedrawer, settings);
	if( object->m_instance.m_model == 0 )
	{
		delete object;	
		return InvalidObjectHandle;
	}

	// Register the object with the data manager
	m_linedrawer->m_data_manager.AddObject(object);

	PR_ASSERT(PR_DBG_LDR, m_plugin_objects.find(object) == m_plugin_objects.end());
	m_plugin_objects.insert(object);
	return object;
}

// Remove an object from the plugin data list
void PlugInManager::UnRegisterObject(ObjectHandle object)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		m_linedrawer->m_data_manager.DeleteObject(*obj);
	}
}

// Remove all objects from the plugin data list
void PlugInManager::UnRegisterAllObjects()
{
	Clear();
}

// Return the number of objects currently registered by the plugin
unsigned int PlugInManager::GetNumPluginObjects()
{
	return static_cast<unsigned int>(m_plugin_objects.size());
}

// Allow the geometry of a render object to be edited
void PlugInManager::EditObject(pr::ldr::ObjectHandle object, pr::ldr::EditObjectFunc func, void* user_data)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		LdrObject* object = *obj;
		func(object->m_instance.m_model, object->m_bbox, user_data, m_linedrawer->m_renderer->m_material_manager);
	}
}

// Set the window text for LineDrawer
void PlugInManager::SetLDWindowText(const char* str)
{
	std::string window_text = "LineDrawer Plugin: ";
	window_text += str;
	SetWindowText(m_linedrawer->m_window_handle, window_text.c_str());
}

// Set the polling frequency for the plugin
void PlugInManager::SetPollingFreq(float step_rate_hz)
{
	m_plugin_poller.SetFrequency(step_rate_hz);
}

// Set the colour of an object
void PlugInManager::SetObjectColour(ObjectHandle object, Colour32 colour)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		(*obj)->SetColour(colour, true, false);
	}
}

// Set the transparency of an object on or off
void PlugInManager::SetObjectSemiTransparent(ObjectHandle object, bool on)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		(*obj)->SetAlpha(on, true);
	}
}

// Set the position of an object
void PlugInManager::SetObjectPosition(ObjectHandle object, const v4& position)
{
	PR_ASSERT(PR_DBG_LDR, position.w == 1.0f);
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		(*obj)->m_object_to_parent[3] = position;
	}
}

// Set the transform for an object
void PlugInManager::SetObjectTransform(ObjectHandle object, const m4x4& object_to_world)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		(*obj)->m_object_to_parent = object_to_world;
	}
}

// Set the user data for an object
void PlugInManager::SetObjectUserData(ObjectHandle object, void* user_data)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		(*obj)->m_user_data = user_data;
	}
}

// Return the colour of an object
pr::Colour32 PlugInManager::GetObjectColour(pr::ldr::ObjectHandle object)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		return (*obj)->m_instance.m_colour;
	}
	return pr::Colour32Black;
}

//// Return the source string used to create an object
//const char* PlugInManager::GetSourceString(pr::ldr::ObjectHandle object)
//{
//	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
//	if( obj != m_plugin_objects.end() )
//	{
//		return (*obj)->m_source_string.c_str();
//	}
//	return 0;
//}

// Set the user data for an object
void* PlugInManager::GetObjectUserData(ObjectHandle object)
{
	TObjectSet::iterator obj = m_plugin_objects.find((LdrObject*)object);
	if( obj != m_plugin_objects.end() )
	{
		return (*obj)->m_user_data;
	}
	return 0;
}

// Allow key presses to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnKeyDown(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags)
{
	if( m_NotifyKeyDown ) return m_NotifyKeyDown(nChar, nRepCnt, nFlags);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow key presses to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnKeyUp(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags)
{
	if( m_NotifyKeyUp ) return m_NotifyKeyUp(nChar, nRepCnt, nFlags);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow mouse button presses to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnMouseDown(unsigned int vk_button, v2 position)
{
	if( m_NotifyOnMouseDown ) return m_NotifyOnMouseDown(vk_button, position);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow mouse button presses to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnMouseMove(v2 position)
{
	if( m_NotifyOnMouseMove ) return m_NotifyOnMouseMove(position);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow mouse wheel movements to be handled by a plugin
pr::ldr::EPlugInResult PlugInManager::Hook_OnMouseWheel(unsigned int nFlags, short zDelta, v2 position)
{
	if( m_NotifyOnMouseWheel ) return m_NotifyOnMouseWheel(nFlags, zDelta, position);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow mouse button presses to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnMouseUp(unsigned int vk_button, v2 position)
{
	if( m_NotifyOnMouseUp ) return m_NotifyOnMouseUp(vk_button, position);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow mouse single click events to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnMouseClk(unsigned int button, v2 position)
{
	if( m_NotifyOnMouseClk ) return m_NotifyOnMouseClk(button, position);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow mouse double click events to be handled by a plugin before being passed to line drawer
pr::ldr::EPlugInResult PlugInManager::Hook_OnMouseDblClk(unsigned int button, v2 position)
{
	if( m_NotifyOnMouseDblClk ) return m_NotifyOnMouseDblClk(button, position);
	return pr::ldr::EPlugInResult_NotHandled;
}

// Allow plugins to detect when objects are deleted
void PlugInManager::Hook_OnDeleteObject(LdrObject* object)
{
	if( m_NotifyDeleteObject ) m_NotifyDeleteObject(object);
}
