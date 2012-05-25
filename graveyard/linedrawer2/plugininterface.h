//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
//	Usage:
//		The user dll must implement "Initialise", "StepPlugIn", and "UnInitialise"
//		"StepPlugIn" is called periodically until "eTerminate" is returned or the
//		plugin is stopped in LineDrawer
//
//
#ifndef LINEDRAWER_PLUGININTERFACE_H
#define LINEDRAWER_PLUGININTERFACE_H

#include <string>
#include <vector>
#include "pr/maths/maths.h"
#include "pr/geometry/colour.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Source/LineDrawerAssertEnable.h"
#include "Linedrawer/Source/CameraData.h"
#include "pr/linedrawer/customobjectdata.h"

// If this file is being built as part of the line drawer project
#ifdef BUILT_WITH_LINEDRAWER
	#define Export __declspec(dllimport)
	#define Import __declspec(dllexport)
#else
	#define Export __declspec(dllexport)
	#define Import __declspec(dllimport)

	// Remember to link to these
	//#pragma comment(lib, "d3d9.lib")
	//#pragma message(PR_LINK "Linking to d3d9.lib")
	//#pragma comment(lib, "d3dx9.lib")
	//#pragma message(PR_LINK "Linking to d3dx9.lib")
	//#ifndef NDEBUG
	//#pragma comment(lib, "LineDrawerD.lib")
	//#pragma message(PR_LINK "Linking to LineDrawerD.lib")
	//#else//NDEBUG
	//#pragma comment(lib, "LineDrawer.lib")
	//#pragma message(PR_LINK "Linking to LineDrawer.lib")
	//#endif//NDEBUG
#endif//BUILT_WITH_LINEDRAWER

namespace pr
{
	namespace ldr
	{
		//*****
		// Results returned from the Plugin
		enum EPlugInResult
		{
			EPlugInResult_Success,			
			EPlugInResult_Handled,
			EPlugInResult_NotHandled,
			EPlugInResult_Continue,			// Used in StepPlugIn
			EPlugInResult_Terminate,		// Used in StepPlugIn
		};

		typedef void* ObjectHandle;
		static const ObjectHandle InvalidObjectHandle = 0;

		// The settings for a plugin
		struct PlugInSettings
		{
			pr::uint	m_step_rate_hz;
		};
		PlugInSettings const DefaultPlugInSettings = { 30 };

		typedef std::vector<std::string> TArgs;
	}//namespace ldr
}//namespace pr

extern "C" 
{
	// Note: These cannot be in the ldr namespace because
	// that's C++ linkage and VS will not produce a lib.
	// Also, don't have any memory allocation happening between dll and Ldr
	
	//****************************************
	// Functions to be implemented by the plugin
	Export	pr::ldr::PlugInSettings	ldrInitialise(pr::ldr::TArgs const& args);
	Export	pr::ldr::EPlugInResult	ldrStepPlugIn();
	Export	void					ldrUnInitialise();

	//****************************************
	// Optional functions to be implemented by the plugin
	Export	pr::ldr::EPlugInResult	ldrNotifyKeyDown			(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags);
	Export	pr::ldr::EPlugInResult	ldrNotifyKeyUp				(unsigned int nChar, unsigned int nRepCnt, unsigned int nFlags);
	Export	pr::ldr::EPlugInResult 	ldrNotifyMouseDown			(unsigned int button, pr::v2 position);
	Export	pr::ldr::EPlugInResult 	ldrNotifyMouseMove			(pr::v2 position);
	Export	pr::ldr::EPlugInResult 	ldrNotifyMouseWheel			(unsigned int nFlags, short zDelta, pr::v2 position);
	Export	pr::ldr::EPlugInResult 	ldrNotifyMouseUp			(unsigned int button, pr::v2 position);
	Export	pr::ldr::EPlugInResult 	ldrNotifyMouseClk			(unsigned int button, pr::v2 position);
	Export	pr::ldr::EPlugInResult 	ldrNotifyMouseDblClk		(unsigned int button, pr::v2 position);
	Export	void					ldrNotifyDeleteObject		(pr::ldr::ObjectHandle object);
	Export	void					ldrNofifyRefresh			();

	//****************************************
	// Functions implemented by LineDrawer
	Import	bool					ldrSource					(const char* src, std::size_t len, bool clear_data, bool recentre);
	Import	pr::ldr::ObjectHandle	ldrRegisterObject			(const char* object_description, std::size_t length);
	Import	pr::ldr::ObjectHandle	ldrRegisterCustomObject		(pr::ldr::CustomObjectData const& settings); 
	Import	void					ldrUnRegisterObject			(pr::ldr::ObjectHandle object);
	Import	void					ldrUnRegisterAllObjects		();
	Import	unsigned int			ldrGetNumPluginObjects		();
	Import	void					ldrEditObject				(pr::ldr::ObjectHandle object, pr::ldr::EditObjectFunc func, void* user_data);
	Import	void					ldrSetLDWindowText			(const char* str);
	Import	void					ldrSetPollingFreq			(float step_rate_hz);
	Import	void					ldrSetObjectColour			(pr::ldr::ObjectHandle object, pr::Colour32 colour);
	Import	void					ldrSetObjectSemiTransparent	(pr::ldr::ObjectHandle object, bool on);
	Import	void					ldrSetObjectTransform   	(pr::ldr::ObjectHandle object, const pr::m4x4& object_to_world);
	Import	void					ldrSetObjectPosition    	(pr::ldr::ObjectHandle object, const pr::v4& position);
	Import	void					ldrSetObjectUserData		(pr::ldr::ObjectHandle object, void* user_data);
	Import	pr::Colour32			ldrGetObjectColour			(pr::ldr::ObjectHandle object);
	Import	pr::IRect				ldrGetMainWindowRect		();
	Import	pr::IRect				ldrGetMainClientRect		();
	Import	pr::v4					ldrGetFocusPoint        	();
	//Import	const char*				ldrGetSourceString			(pr::ldr::ObjectHandle object);
	Import	void*					ldrGetObjectUserData		(pr::ldr::ObjectHandle object);
	Import	pr::m4x4				ldrGetCameraToWorld			();
	Import	pr::ldr::CameraData		ldrGetCameraData			();
	Import	pr::v4					ldrScreenToWorld			(pr::v4 ss_position);
	Import	void					ldrView						(const pr::BoundingBox& bbox);
	Import	void					ldrViewAll					();
	Import	void					ldrRender();
	Import	void					ldrErrorReport				(const char* err_msg);
};	// extern "C++"

#endif//LINEDRAWER_PLUGININTERFACE_H


