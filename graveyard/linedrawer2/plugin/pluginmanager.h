//*********************************************
// Plugin Manager
//	(C)opyright Rylogic Limited 2007
//*********************************************
#ifndef PLUGINMANAGER_H
#define PLUGINMANAGER_H

#include <set>
#include "pr/common/PollingToEvent.h"
#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/Objects/ObjectTypes.h"
#define BUILT_WITH_LINEDRAWER
#include "pr/linedrawer/plugininterface.h"
#undef	BUILT_WITH_LINEDRAWER

class PlugInManager
{
public:
	PlugInManager(LineDrawer& linedrawer);
	~PlugInManager();

	void	Clear();
	bool	IsPlugInLoaded() const	{ return m_plugin != 0; }
	bool	StartPlugIn(const char* plugin_name, pr::ldr::TArgs const& args);
	bool	RestartPlugIn();
	void	StepPlugIn();
	void	StopPlugIn();
	void	DeleteObject(LdrObject* object);
	
	// Dll interface implementation in PlugInManagerDLLImplementation
	pr::ldr::ObjectHandle	RegisterObject(const char* object_description, std::size_t length);
	pr::ldr::ObjectHandle	RegisterCustomObject(pr::ldr::CustomObjectData const& settings);
	void					UnRegisterObject(pr::ldr::ObjectHandle object);
	void					UnRegisterAllObjects();
	unsigned int			GetNumPluginObjects();
	void					EditObject(pr::ldr::ObjectHandle object, pr::ldr::EditObjectFunc func, void* user_data);
	void					SetLDWindowText(const char* str);
	void					SetPollingFreq(float step_rate_hz);
	void					SetObjectColour(pr::ldr::ObjectHandle object, Colour32 colour);
	void					SetObjectSemiTransparent(pr::ldr::ObjectHandle object, bool on);
	void					SetObjectPosition(pr::ldr::ObjectHandle object, const v4& position);
	void					SetObjectTransform(pr::ldr::ObjectHandle object, const m4x4& object_to_world);
	void					SetObjectUserData(pr::ldr::ObjectHandle object, void* user_data);
	pr::Colour32			GetObjectColour(pr::ldr::ObjectHandle object);
	void					GetFocusPoint(v4& focus_point);
//	const char*				GetSourceString(pr::ldr::ObjectHandle object);
	void*					GetObjectUserData(pr::ldr::ObjectHandle object);

	// Hook functions
	pr::ldr::EPlugInResult	Hook_OnKeyDown(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags);
	pr::ldr::EPlugInResult	Hook_OnKeyUp(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags);
	pr::ldr::EPlugInResult 	Hook_OnMouseDown(unsigned int button, v2 position);
	pr::ldr::EPlugInResult 	Hook_OnMouseMove(v2 position);
	pr::ldr::EPlugInResult 	Hook_OnMouseWheel(unsigned int nFlags, short zDelta, v2 position);
	pr::ldr::EPlugInResult 	Hook_OnMouseUp(unsigned int button, v2 position);
	pr::ldr::EPlugInResult 	Hook_OnMouseClk(unsigned int button, v2 position);
	pr::ldr::EPlugInResult 	Hook_OnMouseDblClk(unsigned int button, v2 position);
	void					Hook_OnDeleteObject(LdrObject* object);
	void					Hook_OnRefresh();

	volatile bool m_step_plugin_pending;

private:
	static PollingToEventSettings PluginPollerSettings(void* user_data);
	static bool PollPlugIn(void* user);
	typedef std::set<LdrObject*> TObjectSet;

private:
	LineDrawer*			m_linedrawer;
	PollingToEvent		m_plugin_poller;
	std::string			m_plugin_name;
	HMODULE				m_plugin;			// Handle to the loaded dll
	TObjectSet			m_plugin_objects;
	pr::ldr::TArgs		m_plugin_args;

	// Required function pointers
	typedef pr::ldr::PlugInSettings	(*PlugInInitialise)(pr::ldr::TArgs const& args);
	typedef pr::ldr::EPlugInResult	(*PlugInStepPlugIn)(void);
	typedef void					(*PlugInUnInitialise)(void);
	PlugInInitialise	m_PlugInInitialise;
	PlugInStepPlugIn	m_PlugInStepPlugIn;
	PlugInUnInitialise	m_PlugInUnInitialise;

	// Optional function pointers
	typedef pr::ldr::EPlugInResult	(*NotifyKeyDown)		(unsigned int, unsigned int, unsigned int);
	typedef pr::ldr::EPlugInResult	(*NotifyKeyUp)			(unsigned int, unsigned int, unsigned int);
	typedef pr::ldr::EPlugInResult	(*NotifyOnMouseDown)	(unsigned int, v2);
	typedef pr::ldr::EPlugInResult	(*NotifyOnMouseMove)	(v2);
	typedef pr::ldr::EPlugInResult	(*NotifyOnMouseWheel)	(unsigned int, short, v2);
	typedef pr::ldr::EPlugInResult	(*NotifyOnMouseUp)		(unsigned int, v2);
	typedef pr::ldr::EPlugInResult	(*NotifyOnMouseClk)		(unsigned int, v2);
	typedef pr::ldr::EPlugInResult	(*NotifyOnMouseDblClk)	(unsigned int, v2);
	typedef void					(*NotifyDeleteObject)	(pr::ldr::ObjectHandle);
	typedef void					(*NotifyRefresh)		();
	NotifyKeyDown		m_NotifyKeyDown;
	NotifyKeyUp			m_NotifyKeyUp;
	NotifyOnMouseDown	m_NotifyOnMouseDown;
	NotifyOnMouseMove	m_NotifyOnMouseMove;
	NotifyOnMouseWheel	m_NotifyOnMouseWheel;
	NotifyOnMouseUp		m_NotifyOnMouseUp;
	NotifyOnMouseClk	m_NotifyOnMouseClk;
	NotifyOnMouseDblClk	m_NotifyOnMouseDblClk;
	NotifyDeleteObject	m_NotifyDeleteObject;
	NotifyRefresh		m_NotifyRefresh;
};

#endif//PLUGINMANAGER_H
