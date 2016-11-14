//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************

#pragma once

#include "linedrawer/main/forward.h"

namespace pr
{
	namespace settings
	{
		// Export/Import function overloads - overload as necessary
		inline std::string Write(pr::rdr::Light const& t)
		{
			return t.Settings();
		}
		inline bool Read(pr::script::Reader& reader, pr::rdr::Light& t)
		{
			std::string s;
			bool res = reader.Section(s, false);
			t.Settings(s.c_str());
			return res;
		}
	}
}
namespace ldr
{
	//(type, name, default_value, description)
	#define LDR_SETTING(x)\
		x(std::string             ,LdrVersion             ,ldr::AppStringLine()        ,"Application version number")\
		x(bool                    ,WatchForChangedFiles   ,false                       ,"Set to true to poll for file changes")\
		x(std::wstring            ,TextEditorCmd          ,L"C:\\Windows\\notepad.exe" ,"The text editor to use")\
		x(bool                    ,AlwaysOnTop            ,false                       ,"Set to true to keep the application above all others")\
		x(size_t                  ,MaxRecentFiles         ,10                          ,"The maximum length of the recent files history")\
		x(size_t                  ,MaxSavedViews          ,10                          ,"The maximum number of saved camera views")\
		x(std::wstring            ,RecentFiles            ,L""                         ,"The recent files list")\
		x(std::wstring            ,NewObjectString        ,L""                         ,"The string last entered in the new object window")\
		x(std::string             ,ObjectManagerSettings  ,""                          ,"Settings data for the object manager")\
		x(bool                    ,ShowOrigin             ,false                       ,"Set to true to show the point (0,0,0)")\
		x(bool                    ,ShowAxis               ,false                       ,"Set to true to show a reference X,Y,Z axis set")\
		x(bool                    ,ShowFocusPoint         ,true                        ,"Set to true to show the focus point of the camera")\
		x(bool                    ,ShowSelectionBox       ,false                       ,"Set to true to display a bounding box of the current selection")\
		x(bool                    ,ShowObjectBBoxes       ,false                       ,"Set to true to show bounding boxes around objects")\
		x(float                   ,FocusPointScale        ,0.015f                      ,"Scaler for the size of the camera focus point axes")\
		x(bool                    ,ResetCameraOnLoad      ,true                        ,"Set to true to reset the camera position whenever a file is loaded")\
		x(bool                    ,PersistObjectState     ,false                       ,)\
		x(pr::v4                  ,CameraAlignAxis        ,pr::v4Zero                  ,)\
		x(bool                    ,CameraOrbit            ,false                       ,)\
		x(float                   ,CameraOrbitSpeed       ,0.3f                        ,)\
		x(pr::v4                  ,CameraResetForward     ,-pr::v4ZAxis                ,"The direction the camera faces when reset")\
		x(pr::v4                  ,CameraResetUp          ,pr::v4YAxis                 ,"The up direction for the camera when reset")\
		x(bool                    ,EnableResourceMonitor  ,false                       ,)\
		x(bool                    ,RenderingEnabled       ,true                        ,)\
		x(pr::Colour32            ,BackgroundColour       ,pr::Colour32Gray            ,"The background colour")\
		x(EFillMode               ,GlobalFillMode         ,EFillMode::Solid            ,"Render all objects in the scene as solid, wireframe, or both")\
		x(pr::rdr::Light          ,Light                  ,                            ,"Global lighting properties")\
		x(bool                    ,LightIsCameraRelative  ,true                        ,)\
		x(bool                    ,IgnoreMissingIncludes  ,true                        ,)\
		x(bool                    ,ErrorOutputMsgBox      ,true                        ,)\
		x(bool                    ,ErrorOutputToFile      ,false                       ,)\
		x(std::string             ,ErrorOutputLogFilename ,""                          ,)
	PR_DEFINE_SETTINGS(UserSettings, LDR_SETTING);
	#undef LDR_SETTING
}
