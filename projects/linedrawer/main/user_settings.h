//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************

#pragma once
#ifndef LINEDRAWER_MAIN_USER_SETTINGS_H
#define LINEDRAWER_MAIN_USER_SETTINGS_H

#include "linedrawer/main/forward.h"
#include "pr/storage/settings.h"

namespace pr
{
	namespace settings
	{
		// Export/Import function overloads - overload as necessary
		inline std::string Write(pr::rdr::Light const& t)                         { return t.Settings(); }
		inline bool        Read(pr::script::Reader& reader, pr::rdr::Light& t)    { std::string s; bool res = reader.ExtractSection(s, false); t.Settings(s.c_str()); return res; }
	}
}

namespace ldr
{
	//(type, name, default_value, hashvalue, description)
	#define LDR_SETTING(x)\
		x(std::string             ,LdrVersion             ,ldr::AppStringLine()           ,0x16152F0E        ,"Application version number")\
		x(bool                    ,WatchForChangedFiles   ,false                          ,0x18a3e067        ,"Set to true to poll for file changes")\
		x(std::string             ,TextEditorCmd          ,"C:\\Windows\\notepad.exe"     ,0x1d17d0a3        ,"The text editor to use")\
		x(bool                    ,AlwaysOnTop            ,false                          ,0x0aa9a55a        ,"Set to true to keep the application above all others")\
		x(size_t                  ,MaxRecentFiles         ,10                             ,0x143730ad        ,"The maximum length of the recent files history")\
		x(size_t                  ,MaxSavedViews          ,10                             ,0x14179485        ,"The maximum number of saved camera views")\
		x(std::string             ,RecentFiles            ,""                             ,0x07beccd6        ,"The recent files list")\
		x(std::string             ,NewObjectString        ,""                             ,0x1f25de04        ,"The string last entered in the new object window")\
		x(std::string             ,ObjectManagerSettings  ,""                             ,0x114bb3ad        ,"Settings data for the object manager")\
		x(bool                    ,ShowOrigin             ,false                          ,0x0530f813        ,"Set to true to show the point (0,0,0)")\
		x(bool                    ,ShowAxis               ,false                          ,0x13ed30d0        ,"Set to true to show a reference X,Y,Z axis set")\
		x(bool                    ,ShowFocusPoint         ,true                           ,0x114d5c18        ,"Set to true to show the focus point of the camera")\
		x(bool                    ,ShowSelectionBox       ,false                          ,0x0c1ae3f8        ,"Set to true to display a bounding box of the current selection")\
		x(bool                    ,ShowObjectBBoxes       ,false                          ,0x02e80459        ,"Set to true to show bounding boxes around objects")\
		x(float                   ,FocusPointScale        ,0.015f                         ,0x13e3066f        ,"Scaler for the size of the camera focus point axes")\
		x(bool                    ,ResetCameraOnLoad      ,true                           ,0x04e0448a        ,"Set to true to reset the camera position whenever a file is loaded")\
		x(bool                    ,PersistObjectState     ,false                          ,0x0f494a1e        ,)\
		x(pr::v4                  ,CameraAlignAxis        ,pr::v4Zero                     ,0x1e332604        ,)\
		x(bool                    ,CameraOrbit            ,false                          ,0x1d242e05        ,)\
		x(float                   ,CameraOrbitSpeed       ,0.3f                           ,0x05a1619d        ,)\
		x(bool                    ,EnableResourceMonitor  ,false                          ,0x0924652f        ,)\
		x(bool                    ,RenderingEnabled       ,true                           ,0x12a0793e        ,)\
		x(pr::Colour32            ,BackgroundColour       ,pr::Colour32Gray               ,0x13f2d4d2        ,"The background colour")\
		x(EGlobalRenderMode       ,GlobalRenderMode       ,EGlobalRenderMode::Solid       ,0x106641db        ,)\
		x(pr::rdr::Light          ,Light                  ,                               ,0x08eeae72        ,"Global lighting properties")\
		x(bool                    ,LightIsCameraRelative  ,true                           ,0x0e1123a0        ,)\
		x(bool                    ,IgnoreMissingIncludes  ,true                           ,0x13eca235        ,)\
		x(bool                    ,ErrorOutputMsgBox      ,true                           ,0x10c8bbd5        ,)\
		x(bool                    ,ErrorOutputToFile      ,false                          ,0x13637f31        ,)\
		x(std::string             ,ErrorOutputLogFilename ,""                             ,0x10b0ffa8        ,)
	PR_DEFINE_SETTINGS(UserSettings, LDR_SETTING);
	#undef LDR_SETTING
}

#endif
