//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#ifndef LDR_USER_SETTINGS_H
#define LDR_USER_SETTINGS_H
#pragma once

#include <list>
#include <iostream>
#include "LineDrawer/Source/Forward.h"

struct UserSettings
{
	typedef std::list<std::string> TStringList;
	enum { MaxRecentFiles = 20 };

	std::string		m_settings_filename;

	// Recent files
	TStringList		m_recent_files;

	// GUI
	CRect			m_window_pos;
	std::string		m_new_object_string;
	bool			m_show_origin;
	bool			m_show_axis;
	bool			m_show_focus_point;
	bool			m_show_selection_box;
	float			m_asterix_scale;
	bool			m_reset_camera_on_load;
	bool			m_persist_object_state;

	// Renderer
	std::string		m_shader_version;
	rdr::EQuality::Type	m_geometry_quality;
	rdr::EQuality::Type	m_texture_quality;
	bool			m_enable_resource_monitor;

	// Light
	rdr::Light		m_light;
	bool			m_light_is_camera_relative;

	// Error Output
	bool			m_ignore_missing_includes;
	bool			m_error_output_msgbox;
	bool			m_error_output_to_file;
	std::string		m_error_output_log_filename;

	UserSettings()
	:m_settings_filename()
	,m_recent_files()
	,m_window_pos(0,0,0,0)
	,m_new_object_string("")
	,m_show_origin(false)
	,m_show_axis(false)
	,m_show_focus_point(true)
	,m_show_selection_box(false)
	,m_asterix_scale(0.015f)
	,m_reset_camera_on_load(true)
	,m_persist_object_state(true)
	,m_shader_version("v3_0")
	,m_geometry_quality(rdr::EQuality::High)
	,m_texture_quality(rdr::EQuality::High)
	,m_enable_resource_monitor(false)
	,m_light()
	,m_light_is_camera_relative(true)
	,m_ignore_missing_includes(true)
	,m_error_output_msgbox(true)
	,m_error_output_to_file(false)
	,m_error_output_log_filename("")
	{}

	// Load/Save the user settings from/to file
	// Throw LdrExceptions
	void Load();
	void Save();
};

#endif//LDR_USER_SETTINGS_H