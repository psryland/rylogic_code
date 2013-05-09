//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************

#ifndef LDR_SETTING
#define LDR_SETTING(type, member, name, hashvalue, default_value)
#endif

LDR_SETTING(char const*                   ,m_ldr_version                   ,Version                      ,0x1f8417ed        ,ldr::AppStringLine())
LDR_SETTING(bool                          ,m_watch_for_changed_files       ,WatchForChangedFiles         ,0x18a3e067        ,false)
LDR_SETTING(std::string                   ,m_text_editor_cmd               ,TextEditorCmd                ,0x1d17d0a3        ,"C:\\Program Files\\Notepad++\\notepad++.exe")
LDR_SETTING(bool                          ,m_always_on_top                 ,AlwaysOnTop                  ,0x0aa9a55a        ,false)
LDR_SETTING(pr::uint                      ,m_max_recent_files              ,MaxRecentFiles               ,0x143730ad        ,10)
LDR_SETTING(pr::uint                      ,m_max_saved_views               ,MaxSavedViews                ,0x14179485        ,10)
LDR_SETTING(std::string                   ,m_recent_files                  ,RecentFiles                  ,0x07beccd6        ,)
LDR_SETTING(std::string                   ,m_new_object_string             ,NewObjectString              ,0x1f25de04        ,)
LDR_SETTING(std::string                   ,m_objmgr_settings               ,ObjectManagerSettings        ,0x114bb3ad        ,)
LDR_SETTING(bool                          ,m_show_origin                   ,ShowOrigin                   ,0x0530f813        ,false)
LDR_SETTING(bool                          ,m_show_axis                     ,ShowAxis                     ,0x13ed30d0        ,false)
LDR_SETTING(bool                          ,m_show_focus_point              ,ShowFocusPoint               ,0x114d5c18        ,true)
LDR_SETTING(bool                          ,m_show_selection_box            ,ShowSelectionBox             ,0x0c1ae3f8        ,false)
LDR_SETTING(bool                          ,m_show_object_bboxes            ,ShowObjectBBoxes             ,0x02e80459        ,false)
LDR_SETTING(float                         ,m_focus_point_scale             ,FocusPointScale              ,0x13e3066f        ,0.015f)
LDR_SETTING(bool                          ,m_reset_camera_on_load          ,ResetCameraOnLoad            ,0x04e0448a        ,true)
LDR_SETTING(bool                          ,m_persist_object_state          ,PersistObjectState           ,0x0f494a1e        ,false)
LDR_SETTING(pr::v4                        ,m_camera_align                  ,CameraAlignAxis              ,0x1e332604        ,pr::v4Zero)
LDR_SETTING(bool                          ,m_camera_orbit                  ,CameraOrbit                  ,0x1d242e05        ,false)
LDR_SETTING(float                         ,m_camera_orbit_speed            ,CameraOrbitSpeed             ,0x05a1619d        ,0.3f)
//LDR_SETTING(pr::rdr::EShaderVersion::Type ,m_shader_version                ,ShaderVersion                ,0x0588f82b        ,pr::rdr::EShaderVersion::v3_0)
//LDR_SETTING(pr::rdr::EQuality::Type       ,m_geometry_quality              ,GeometryQuality              ,0x1c0bba13        ,pr::rdr::EQuality::High)
//LDR_SETTING(pr::rdr::EQuality::Type       ,m_texture_quality               ,TextureQuality               ,0x17a72093        ,pr::rdr::EQuality::High)
LDR_SETTING(bool                          ,m_enable_resource_monitor       ,EnableResourceMonitor        ,0x0924652f        ,false)
LDR_SETTING(bool                          ,m_rendering_enabled             ,RenderingEnabled             ,0x12a0793e        ,true)
LDR_SETTING(pr::Colour32                  ,m_background_colour             ,BackgroundColour             ,0x13f2d4d2        ,pr::Colour32::make(0xFF808080))
LDR_SETTING(EScreenView::Type             ,m_screen_view                   ,ScreenView                   ,0x10ce59f4        ,EScreenView::Default)
LDR_SETTING(float                         ,m_stereo_view_eye_separation    ,StereoEyeSeparation          ,0x1037777f        ,0.01f)
LDR_SETTING(EGlobalRenderMode::Type       ,m_global_render_mode            ,GlobalRenderMode             ,0x106641db        ,EGlobalRenderMode::Solid)
LDR_SETTING(pr::rdr::Light                ,m_light                         ,Light                        ,0x08eeae72        ,)
LDR_SETTING(bool                          ,m_light_is_camera_relative      ,LightIsCameraRelative        ,0x0e1123a0        ,true)
LDR_SETTING(bool                          ,m_ignore_missing_includes       ,IgnoreMissingIncludes        ,0x13eca235        ,true)
LDR_SETTING(bool                          ,m_msgbox_error_msgs             ,ErrorOutputMsgBox            ,0x10c8bbd5        ,true)
LDR_SETTING(bool                          ,m_error_output_to_file          ,ErrorOutputToFile            ,0x13637f31        ,false)
LDR_SETTING(std::string                   ,m_error_output_log_filename     ,ErrorOutputLogFilename       ,0x10b0ffa8        ,)
#undef LDR_SETTING

#ifndef LDR_USER_SETTINGS_H
#define LDR_USER_SETTINGS_H

#include "linedrawer/types/forward.h"

// Generate an enum of the hash values
namespace EUserSetting
{
	enum Type
	{
#		define LDR_SETTING(type, member, name, hashvalue, default_value) name = hashvalue,
#		include "usersettings.h"
	};
}

// A structure containing the user settings
struct UserSettings
{
	typedef std::list<std::string> TStringList;
	enum { MaxRecentFiles = 20 };
	std::string m_filename; // The file to save settings to
	pr::uint m_hash;        // The crc of this object last time it was saved

	// Declare settings
#	define LDR_SETTING(type, member, name, hashvalue, default_value) type member;
#	include "usersettings.h"

	// Construct with defaults
	UserSettings(std::string const& filename, bool load);

	// Load/Save the user settings from/to file
	bool SaveRequired() const;
	std::string Export() const;
	bool Import(std::string settings);
	bool Load(std::string const& file);
	bool Save(std::string const& file);
	bool Save() { return Save(m_filename); }
};

#endif
