//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#include "stdafx.h"
#include <sstream>
#include "pr/maths/maths.h"
#include "pr/common/prstring.h"
#include "pr/common/scriptreader.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"
#include "linedrawer/source/usersettings.h"
#include "linedrawer/source/ldrexception.h"

// Fill out the user settings from a file
void UserSettings::Load()
{
	try
	{
		std::string user_settings;
		if (!pr::filesys::DoesFileExist(m_settings_filename))				{ return; }
		if (!pr::FileToBuffer(m_settings_filename.c_str(), user_settings))	{ throw LdrException(FmtS("Failed to load user settings file: '%s'", m_settings_filename.c_str())); }

		pr::script::Reader loader;
		loader.AddString(user_settings.c_str());
		while (loader.IsKeyword())
		{
			std::string keyword;
			loader.GetKeyword(keyword);
			
			// Version
			if (str::EqualNoCase(keyword, "Version"))
			{
				std::string version;
				loader.ExtractString(version);
				if (!str::Equal(version, &pr::ldr::g_version_string[0])) { throw LdrException("User settings not for this version of LineDrawer"); }
			}

			// Recent files
			else if (str::EqualNoCase(keyword, "RecentFiles"))
			{
				loader.SectionStart();
				m_recent_files.clear();
				std::size_t const MaxRecentFiles = 10;
				while (!loader.IsSectionEnd() && m_recent_files.size() < MaxRecentFiles)
				{
					std::string file;
					loader.ExtractString(file);
					m_recent_files.push_back(file);
				}
				loader.SectionEnd();
			}

			// GUI
			else if (str::EqualNoCase(keyword, "WindowPos"))
			{
				loader.SectionStart();
				loader.ExtractInt(m_window_pos.left  , 10);
				loader.ExtractInt(m_window_pos.top   , 10);
				loader.ExtractInt(m_window_pos.right , 10);
				loader.ExtractInt(m_window_pos.bottom, 10);
				loader.SectionEnd();
			}
			else if (str::EqualNoCase(keyword, "NewObjectString"))
			{
				loader.ExtractString(m_new_object_string);
			}
			else if (str::EqualNoCase(keyword, "ShowOrigin"))
			{
				loader.ExtractBool(m_show_origin);
			}
			else if (str::EqualNoCase(keyword, "ShowAxis"))
			{
				loader.ExtractBool(m_show_axis);
			}
			else if (str::EqualNoCase(keyword, "ShowFocusPoint"))
			{
				loader.ExtractBool(m_show_focus_point);
			}
			else if (str::EqualNoCase(keyword, "ShowSelectionBox"))
			{
				loader.ExtractBool(m_show_selection_box);
			}
			else if (str::EqualNoCase(keyword, "AsterixScale"))
			{
				loader.ExtractReal(m_asterix_scale);
			}
			else if (str::EqualNoCase(keyword, "ResetCameraOnLoad"))
			{
				loader.ExtractBool(m_reset_camera_on_load);
			}
			else if (str::EqualNoCase(keyword, "PersistObjectState"))
			{
				loader.ExtractBool(m_persist_object_state);
			}

			// Renderer
			else if (str::EqualNoCase(keyword, "ShaderVersion"))
			{
				loader.ExtractString(m_shader_version);
			}
			else if (str::EqualNoCase(keyword, "GeometryQuality"))
			{
				int quality;
				loader.ExtractInt(quality, 10);
				m_geometry_quality = static_cast<rdr::EQuality::Type>(Clamp<int>(quality, rdr::EQuality::Low, rdr::EQuality::High));
			}
			else if (str::EqualNoCase(keyword, "TextureQuality"))
			{
				int quality;
				loader.ExtractInt(quality, 10);
				m_texture_quality = static_cast<rdr::EQuality::Type>(Clamp<int>(quality, rdr::EQuality::Low, rdr::EQuality::High));
			}
			else if (str::EqualNoCase(keyword, "EnableResourceMonitor"))
			{
				loader.ExtractBool(m_enable_resource_monitor);
			}

			// Light
			else if (str::EqualNoCase(keyword, "LightIsCameraRelative"))
			{
				loader.ExtractBool(m_light_is_camera_relative);
			}
			else if (str::EqualNoCase(keyword, "LightData"))
			{
				loader.SectionStart();
				loader.ExtractData(&m_light, sizeof(m_light));
				loader.SectionEnd();
			}
		
			// Error Output
			else if (str::EqualNoCase(keyword, "IgnoreMissingIncludes"))
			{
				loader.ExtractBool(m_ignore_missing_includes);
			}
			else if (str::EqualNoCase(keyword, "ErrorOutputMessageBox"))
			{
				loader.ExtractBool(m_error_output_msgbox);
			}
			else if (str::EqualNoCase(keyword, "ErrorOutputToFile"))
			{
				loader.ExtractBool(m_error_output_to_file);
			}
			else if (str::EqualNoCase(keyword, "ErrorOutputFilename"))
			{
				loader.ExtractString(m_error_output_log_filename);
			}
		}
	}
	catch (std::exception const& e)
	{
		std::string settings_filename = m_settings_filename;
		*this = UserSettings();
		m_settings_filename = settings_filename;
		throw LdrException(e.what());
	}
	catch (...)
	{
		*this = UserSettings();
		throw;
	}
}

// Write binary data to a stream
void WriteBinary(std::ostream& out, void const* data, std::size_t length, int bytes_per_row)
{
	unsigned char const* base = static_cast<unsigned char const*>(data);
	for (unsigned char const* ptr = base, *end = ptr + length; ptr != end; ++ptr)
	{
		out << FmtS("%2.2X ", *ptr);
		if (((ptr - base) % bytes_per_row) == bytes_per_row - 1)
			out << "\n";
	}
}

// Save user preferences
void UserSettings::Save()
{
	std::stringstream out;	
	out <<	"//==================================\n";
	out <<	"// User options file for LineDrawer\n";
	out <<	"//==================================\n";

	// Version
	out <<	"*Version \"" << pr::ldr::g_version_string << "\"\n\n";

	// Recent files
	out <<	"*RecentFiles\n{\n";
	for (TStringList::const_iterator iter = m_recent_files.begin(), iter_end = m_recent_files.end(); iter != iter_end; ++iter)
		out << "\t\"" << iter->c_str() << "\"\n";
	out <<	"}\n";

	std::string new_obj_str;
	str::StringToCString(m_new_object_string, new_obj_str);

	// GUI
	out << "*WindowPos " << m_window_pos.left << " " << m_window_pos.top << " " << m_window_pos.right << " " << m_window_pos.bottom << "\n";
	out << "*NewObjectString \"" << new_obj_str << "\"\n";
	out << "*ShowOrigin " << m_show_origin << "\n";
	out << "*ShowAxis " << m_show_axis << "\n";
	out << "*ShowFocusPoint " << m_show_focus_point << "\n";
	out << "*ShowSelectionBox " << m_show_selection_box << "\n";
	out << "*AsterixScale " << m_asterix_scale << "\n";
	out << "*ResetCameraOnLoad " << m_reset_camera_on_load << "\n";
	out << "*PersistObjectState " << m_persist_object_state << "\n\n";

	// Renderer
	out << "*ShaderVersion \"" << m_shader_version << "\"\n";
	out << "*GeometryQuality " << m_geometry_quality << "\n";
	out << "*TextureQuality " << m_texture_quality << "\n";
	out << "*EnableResourceMonitor " << m_enable_resource_monitor << "\n\n";

	// Light
	out << "*LightIsCameraRelative " << m_light_is_camera_relative << "\n";
	out << "*LightData\n{\n";
	WriteBinary(out, &m_light, sizeof(m_light), 16);
	out << "\n}\n";

	// Error Output
	out << "*IgnoreMissingIncludes " << m_ignore_missing_includes << "\n";
	out << "*ErrorOutputMessageBox " << m_error_output_msgbox << "\n";
	out << "*ErrorOutputToFile " << m_error_output_to_file << "\n";
	out << "*ErrorOutputFilename \"" << m_error_output_log_filename << "\"\n\n";

	BufferToFile(out.str(), m_settings_filename.c_str());
	//return saver.Save(m_settings_filename.c_str());
	////LineDrawer::Get().m_error_output.Error(Fmt("Failed to write user settings file '%s'", m_settings_filename.c_str()).c_str());
}

