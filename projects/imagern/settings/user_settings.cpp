//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************

#include "imagern/main/stdafx.h"
#include "imagern/settings/user_settings.h"
#include "imagern/main/events.h"

// Construction with defaults
UserSettings::UserSettings(string const& filepath, bool load)
:m_filepath(filepath)
#define USER_SETTING(name, type, member, default_value, hashvalue) ,member(default_value)
#include "user_Settings.h"
{
	if (load && !m_filepath.empty())
		Load(m_filepath);
}

// Return true if the settings have changed since last save
bool UserSettings::SaveRequired() const
{
	std::string settings = Export();
	return m_hash != pr::hash::FastHash(settings.c_str(), (pr::uint)settings.size(), 0);
}
	
// Return a string containing the settings
string UserSettings::Export() const
{
	std::stringstream out;
	out << "// ImagerN User Options\n"
		<< "*DBPath {\"" << m_db_path.c_str() << "\"}\n"
		<< "*RecentFiles {\"" << m_recent_files.c_str() << "\"}\n"
		<< "*ImageExtns {\"" << m_image_extns.c_str() << "\"}\n"
		<< "*VideoExtns {\"" << m_video_extns.c_str() << "\"}\n"
		<< "*AudioExtns {\"" << m_audio_extns.c_str() << "\"}\n"
		<< "*ZoomFill {" << m_zoom_fill << "}\n"
		<< "*ZoomFit  {" << m_zoom_fit  << "}\n"
		;
	return string(out.str());
}

// Load settings from a string
bool UserSettings::Import(string const& settings)
{
	try
	{
		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings.c_str());
		reader.AddSource(src);
		
		// Verify the hash values are correct
#		if DBG
#		define USER_SETTING(name, type, member, default_value, hashvalue) PR_ASSERT(DBG, reader.HashKeyword(#name) == hashvalue, pr::FmtS("Hash value for %s incorrect. Should be 0x%08x\n", #name, reader.HashKeyword(#name)));
#		include "user_settings.h"
#		endif
		
		for (EUserSetting::Type setting; reader.NextKeywordH(setting);)
		{
			switch (setting)
			{
			default: PR_ASSERT(DBG, false, "Unknown user setting"); break;
			case EUserSetting::DBPath:      reader.ExtractStringS(m_db_path); break;
			case EUserSetting::RecentFiles: reader.ExtractStringS(m_recent_files); break;
			case EUserSetting::ImageExtns:  reader.ExtractStringS(m_image_extns); break;
			case EUserSetting::VideoExtns:  reader.ExtractStringS(m_video_extns); break;
			case EUserSetting::AudioExtns:  reader.ExtractStringS(m_audio_extns); break;
			case EUserSetting::ZoomFill:    reader.ExtractBoolS(m_zoom_fill); break;
			case EUserSetting::ZoomFit:     reader.ExtractBoolS(m_zoom_fit); break;
			}
		}
		return true;
	}
	catch (pr::script::Exception const& e)
	{
		string msg = "Error found while parsing user settings.\n" + e.msg();
		pr::events::Send(Event_Message(msg.c_str(), Event_Message::Error));
	}
	catch (std::exception const& e)
	{
		string msg = "Error found while parsing user settings.\n";
		msg       += "Error details: ";
		msg       += e.what();
		pr::events::Send(Event_Message(msg.c_str(), Event_Message::Error));
	}
	*this = UserSettings(m_filepath, false);
	return false;
}

// Load/Save settings to/from file
bool UserSettings::Load(string const& file)
{
	// Read the user settings file into a string
	for (;;)
	{
		string user_settings;
		if (!pr::filesys::FileExists(file))
		{
			pr::events::Send(Event_Message(pr::FmtS("User settings file '%s' not found", file.c_str()), Event_Message::Warning));
			break;
		}
		if (!pr::FileToBuffer(file.c_str(), user_settings))
		{
			pr::events::Send(Event_Message(pr::FmtS("User settings file '%s' could not be read", file.c_str()), Event_Message::Error));
			break;
		}
		return Import(user_settings);
	}
	*this = UserSettings(file, false);
	return false;
}
bool UserSettings::Save(string const& file)
{
	string settings = Export();
	m_hash = pr::hash::FastHash(settings.c_str(), (pr::uint)settings.size(), 0);
	
	// Create the directory if it doesn't exist
	string dir = pr::filesys::GetDirectory(file);
	if (!pr::filesys::DirectoryExists(dir) && !pr::filesys::CreateDir(dir))
	{
		pr::events::Send(Event_Message(pr::FmtS("Failed to save user settings file '%s'", file.c_str()), Event_Message::Error));
		return false;
	}
	if (!pr::BufferToFile(settings, file.c_str()))
	{
		pr::events::Send(Event_Message(pr::FmtS("Failed to save user settings file '%s'", file.c_str()), Event_Message::Error));
		return false;
	}
	return true;
}

