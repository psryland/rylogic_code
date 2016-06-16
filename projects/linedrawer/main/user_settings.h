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
	//(type, name, default_value, hashvalue, description)
	#define LDR_SETTING(x)\
		x(std::string             ,LdrVersion             ,ldr::AppStringLine()           ,0xED58EA61        ,"Application version number")\
		x(bool                    ,WatchForChangedFiles   ,false                          ,0xCC24D35A        ,"Set to true to poll for file changes")\
		x(std::wstring            ,TextEditorCmd          ,L"C:\\Windows\\notepad.exe"    ,0x0A19EEEF        ,"The text editor to use")\
		x(bool                    ,AlwaysOnTop            ,false                          ,0x57D3FCE6        ,"Set to true to keep the application above all others")\
		x(size_t                  ,MaxRecentFiles         ,10                             ,0xBE9EE923        ,"The maximum length of the recent files history")\
		x(size_t                  ,MaxSavedViews          ,10                             ,0xAE3BCC78        ,"The maximum number of saved camera views")\
		x(std::wstring            ,RecentFiles            ,L""                            ,0x0E4E516B        ,"The recent files list")\
		x(std::wstring            ,NewObjectString        ,L""                            ,0xE2EE73FD        ,"The string last entered in the new object window")\
		x(std::string             ,ObjectManagerSettings  ,""                             ,0xE98CDECE        ,"Settings data for the object manager")\
		x(bool                    ,ShowOrigin             ,false                          ,0x8F719534        ,"Set to true to show the point (0,0,0)")\
		x(bool                    ,ShowAxis               ,false                          ,0x5C782037        ,"Set to true to show a reference X,Y,Z axis set")\
		x(bool                    ,ShowFocusPoint         ,true                           ,0xA6E97EF8        ,"Set to true to show the focus point of the camera")\
		x(bool                    ,ShowSelectionBox       ,false                          ,0x1592F7CD        ,"Set to true to display a bounding box of the current selection")\
		x(bool                    ,ShowObjectBBoxes       ,false                          ,0x3F0A3E36        ,"Set to true to show bounding boxes around objects")\
		x(float                   ,FocusPointScale        ,0.015f                         ,0x7D1795A9        ,"Scaler for the size of the camera focus point axes")\
		x(bool                    ,ResetCameraOnLoad      ,true                           ,0x767EAEC4        ,"Set to true to reset the camera position whenever a file is loaded")\
		x(bool                    ,PersistObjectState     ,false                          ,0x1535B477        ,)\
		x(pr::v4                  ,CameraAlignAxis        ,pr::v4Zero                     ,0x0AC1A26C        ,)\
		x(bool                    ,CameraOrbit            ,false                          ,0x01C17998        ,)\
		x(float                   ,CameraOrbitSpeed       ,0.3f                           ,0x751158B3        ,)\
		x(bool                    ,EnableResourceMonitor  ,false                          ,0x8F07EA22        ,)\
		x(bool                    ,RenderingEnabled       ,true                           ,0x9FB24AFE        ,)\
		x(pr::Colour32            ,BackgroundColour       ,pr::Colour32Gray               ,0x1DE2B413        ,"The background colour")\
		x(EFillMode               ,GlobalFillMode         ,EFillMode::Solid               ,0x0F6C392A        ,"Render all objects in the scene as solid, wireframe, or both")\
		x(pr::rdr::Light          ,Light                  ,                               ,0xC7672DD9        ,"Global lighting properties")\
		x(bool                    ,LightIsCameraRelative  ,true                           ,0x93235BA2        ,)\
		x(bool                    ,IgnoreMissingIncludes  ,true                           ,0x0E13FAF2        ,)\
		x(bool                    ,ErrorOutputMsgBox      ,true                           ,0xB33D0E08        ,)\
		x(bool                    ,ErrorOutputToFile      ,false                          ,0x2FA54769        ,)\
		x(std::string             ,ErrorOutputLogFilename ,""                             ,0x8985E07D        ,)
	PR_DEFINE_SETTINGS(UserSettings, LDR_SETTING);
	#undef LDR_SETTING
}
