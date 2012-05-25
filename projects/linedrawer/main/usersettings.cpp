//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/usersettings.h"
#include "linedrawer/types/ldrevent.h"
#include "linedrawer/types/ldrexception.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"
#include "pr/common/events.h"
#include "pr/common/fmt.h"
#include "pr/common/hash.h"
#include "pr/filesys/filesys.h"
#include "pr/script/reader.h"
	
// Construct with defaults
UserSettings::UserSettings(std::string const& filename, bool load)
:m_filename(filename)
#define LDR_SETTING(type, member, name, hashvalue, default_value) ,member(default_value)
#include "usersettings.h"
{
	if (load && !m_filename.empty())
		Load(m_filename);
}
	
// Return true if the settings have changed since last save
bool UserSettings::SaveRequired() const
{
	std::string settings = Export();
	return m_hash != pr::hash::FastHash(settings.c_str(), (pr::uint)settings.size(), 0);
}
	
// Return a string containing the settings data
std::string UserSettings::Export() const
{
	std::stringstream out;
	std::string tmp;

	out << "//==================================\n";
	out << "// User options file for LineDrawer\n";
	out << "//==================================\n";

	// General
	out << "\n";
	out << "*Version \"" << ldr::AppStringLine() << "\"\n";
	out << "\n";
	out << "*WatchForChangedFiles " << m_watch_for_changed_files << "\n";
	out << "*TextEditorCmd {#lit " << m_text_editor_cmd << "#end}\n";
	out << "*AlwaysOnTop " << m_always_on_top << "\n";
	out << "*MaxRecentFiles " << m_max_recent_files << "\n";
	out << "*MaxSavedViews " << m_max_saved_views << "\n";

	// GUI
	out << "\n";
	out << "*RecentFiles \"" << m_recent_files << "\"\n";
	out << "*ObjectManagerSettings {" << m_objmgr_settings << "}\n";
	out << "*ShowOrigin " << m_show_origin << "\n";
	out << "*ShowAxis " << m_show_axis << "\n";
	out << "*ShowFocusPoint " << m_show_focus_point << "\n";
	out << "*ShowSelectionBox " << m_show_selection_box << "\n";
	out << "*ShowObjectBBoxes " << m_show_object_bboxes << "\n";
	out << "*FocusPointScale " << m_focus_point_scale << "\n";
	out << "*ResetCameraOnLoad " << m_reset_camera_on_load << "\n";
	out << "*PersistObjectState " << m_persist_object_state << "\n";

	// Navigation
	out << "\n";
	out << "*CameraAlignAxis {" << m_camera_align << "}\n";
	out << "*CameraOrbit " << m_camera_orbit << "\n";
	out << "*CameraOrbitSpeed " << m_camera_orbit_speed << "\n";

	// Renderer
	out << "\n";
	out << "*ShaderVersion \"" << pr::rdr::EShaderVersion::ToString(m_shader_version) << "\"\n";
	out << "*GeometryQuality " << m_geometry_quality << "\n";
	out << "*TextureQuality " << m_texture_quality << "\n";
	out << "*EnableResourceMonitor " << m_enable_resource_monitor << "\n";

	// Light
	out << "\n";
	out << "*LightIsCameraRelative " << m_light_is_camera_relative << "\n";
	out << "*Light {\n" << m_light.Settings() << "}\n";

	// Error Output
	out << "\n";
	out << "*IgnoreMissingIncludes " << m_ignore_missing_includes << "\n";
	out << "*ErrorOutputMsgBox " << m_msgbox_error_msgs << "\n";
	out << "*ErrorOutputToFile " << m_error_output_to_file << "\n";
	out << "*ErrorOutputLogFilename \"" << m_error_output_log_filename << "\"\n";

	// New object text
	out << "\n";
	out << "*NewObjectString {#lit " << m_new_object_string << "#end}\n";

	return out.str();
}
	
// Load settings from a string of settings data
bool UserSettings::Import(std::string settings)
{
	try
	{
		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings.c_str());
		reader.AddSource(src);
		
		// Verify the hash values are correct
#		if PR_DBG_LDR
#		define LDR_SETTING(type, member, name, hashvalue, default_value) PR_ASSERT(PR_DBG_LDR, reader.HashKeyword(#name) == hashvalue, pr::FmtS("Hash value for %s incorrect. Should be 0x%08x\n", #name, reader.HashKeyword(#name)));
#		include "usersettings.h"
#		endif

		for (EUserSetting::Type setting; reader.NextKeywordH(setting);)
		{
			switch (setting)
			{
			default:
				PR_ASSERT(PR_DBG_LDR, false, "Unknown user setting");
				break; // Ignore unknown settings
			
			// General
			case EUserSetting::Version:
				{
					std::string version;
					reader.ExtractString(version);
					if (!pr::str::Equal(version, ldr::AppStringLine())) { throw LdrException(ELdrException::IncorrectVersion); }
				}break;
			case EUserSetting::WatchForChangedFiles:  reader.ExtractBool(m_watch_for_changed_files);    break;
			case EUserSetting::TextEditorCmd:         reader.ExtractSection(m_text_editor_cmd, false);  break;
			case EUserSetting::AlwaysOnTop:           reader.ExtractBool(m_always_on_top);              break;
			case EUserSetting::MaxRecentFiles:        reader.ExtractInt(m_max_recent_files, 10);        break;
			case EUserSetting::MaxSavedViews:         reader.ExtractInt(m_max_saved_views, 10);         break;

			// GUI
			case EUserSetting::RecentFiles:           reader.ExtractString(m_recent_files);             break;
			case EUserSetting::ObjectManagerSettings: reader.ExtractSection(m_objmgr_settings, false);  break;
			case EUserSetting::ShowOrigin:            reader.ExtractBool(m_show_origin);                break;
			case EUserSetting::ShowAxis:              reader.ExtractBool(m_show_axis);                  break;
			case EUserSetting::ShowFocusPoint:        reader.ExtractBool(m_show_focus_point);           break;
			case EUserSetting::ShowSelectionBox:      reader.ExtractBool(m_show_selection_box);         break;
			case EUserSetting::ShowObjectBBoxes:      reader.ExtractBool(m_show_object_bboxes);         break;
			case EUserSetting::FocusPointScale:       reader.ExtractReal(m_focus_point_scale);          break;
			case EUserSetting::ResetCameraOnLoad:     reader.ExtractBool(m_reset_camera_on_load);       break;
			case EUserSetting::PersistObjectState:    reader.ExtractBool(m_persist_object_state);       break;

			// Navigation
			case EUserSetting::CameraAlignAxis:       reader.ExtractVector3S(m_camera_align, 0.0f);     break;
			case EUserSetting::CameraOrbit:           reader.ExtractBool(m_camera_orbit);               break;
			case EUserSetting::CameraOrbitSpeed:      reader.ExtractReal(m_camera_orbit_speed);         break;

			// Renderer
			case EUserSetting::ShaderVersion:
				{
					std::string version;
					reader.ExtractString(version);
					m_shader_version = pr::rdr::EShaderVersion::Parse(version.c_str());
				}break;
			case EUserSetting::GeometryQuality:
				{
					int quality;
					reader.ExtractInt(quality, 10);
					m_geometry_quality = static_cast<pr::rdr::EQuality::Type>(pr::Clamp<int>(quality, pr::rdr::EQuality::Low, pr::rdr::EQuality::High));
				}break;
			case EUserSetting::TextureQuality:
				{
					int quality;
					reader.ExtractInt(quality, 10);
					m_texture_quality = static_cast<pr::rdr::EQuality::Type>(pr::Clamp<int>(quality, pr::rdr::EQuality::Low, pr::rdr::EQuality::High));
				}break;
			case EUserSetting::EnableResourceMonitor:
				{
					reader.ExtractBool(m_enable_resource_monitor);
				}break;

			// Light
			case EUserSetting::Light:
				{
					std::string desc;
					reader.ExtractSection(desc, false);
					m_light.Settings(desc.c_str());
				}break;
			case EUserSetting::LightIsCameraRelative:
				{
					reader.ExtractBool(m_light_is_camera_relative);
				}break;
		
			// Error Output
			case EUserSetting::IgnoreMissingIncludes:  reader.ExtractBool(m_ignore_missing_includes);     break;
			case EUserSetting::ErrorOutputMsgBox:      reader.ExtractBool(m_msgbox_error_msgs);           break;
			case EUserSetting::ErrorOutputToFile:      reader.ExtractBool(m_error_output_to_file);        break;
			case EUserSetting::ErrorOutputLogFilename: reader.ExtractString(m_error_output_log_filename); break;

			// New object text
			case EUserSetting::NewObjectString:        reader.ExtractSection(m_new_object_string, false); break;
			}
		}
		return true;
	}
	catch (LdrException const& e)
	{
		pr::script::string msg = "Error found while parsing user settings.\n"; msg += e.what();
		if (e.code() == ELdrException::IncorrectVersion) pr::events::Send(ldr::Event_Warn("User settings file is out of date. Default settings used."));
		else pr::events::Send(ldr::Event_Error(msg.c_str()));
	}
	catch (pr::script::Exception const& e)
	{
		pr::script::string msg = "Error found while parsing user settings.\n" + e.msg();
		pr::events::Send(ldr::Event_Error(msg.c_str()));
	}
	catch (std::exception const& e)
	{
		std::string msg = "Error found while parsing user settings.\nError details: "; msg += e.what();
		pr::events::Send(ldr::Event_Error(msg.c_str()));
	}
	*this = UserSettings(m_filename, false);
	return false;
}
	
// Fill out the user settings from a file
bool UserSettings::Load(std::string const& file)
{
	for (;;)
	{
		// Read the user settings file into a string
		std::string settings;
		if (!pr::filesys::FileExists(file))            { pr::events::Send(ldr::Event_Info(pr::FmtS("User settings file '%s' not found", file.c_str()))); break; }
		if (!pr::FileToBuffer(file.c_str(), settings)) { pr::events::Send(ldr::Event_Error(pr::FmtS("User settings file '%s' could not be read", file.c_str()))); break; }
		return Import(settings);
	}
	*this = UserSettings(file, false);
	return false;
}
	
// Save user preferences
bool UserSettings::Save(std::string const& file)
{
	std::string settings = Export();
	m_hash = pr::hash::FastHash(settings.c_str(), (pr::uint)settings.size(), 0);
	return pr::BufferToFile(settings, file.c_str());
}
