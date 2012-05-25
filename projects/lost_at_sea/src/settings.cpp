//************************************
// Lost at Sea
//  Copyright © Rylogic Ltd 2011
//************************************

#include "stdafx.h"
#include "settings.h"
#include "event.h"

using namespace las;

namespace las
{
	char const* AppTitle()     { return "Lost at Sea"; }
	char const* AppVersion()   { return "v0.00.01"; }
	char const* AppVendor()    { return "Rylogic Ltd"; }
	char const* AppCopyright() { return "Copyright © Rylogic Ltd 2011"; }
}

// Construct with defaults
las::Settings::Settings(wstring const& filename, bool load)
:m_filename(filename)
#define SETTING(type, member, name, hashvalue, default_value) ,member(default_value)
#include "settings.h"
{
	if (load && !m_filename.empty())
		Load(m_filename);
}
	
// Return true if the settings have changed since last save
bool las::Settings::SaveRequired() const
{
	string settings = Export();
	return m_hash != pr::hash::HashC(settings.begin(), settings.end());
}
	
// Return a string containing the settings data
string las::Settings::Export() const
{
	std::stringstream out;
	out << "//==================================\n"
		<< "// " << AppTitle() << " Options\n"
		<< "//==================================\n"
		<< "\n"
		<< "*Version {\"" << las::AppVersion() << "\"}\n"
		<< "*FullScreen {" << m_fullscreen << "}\n"
		<< "*XResolution {" << m_res_x << "}\n"
		<< "*YResolution {" << m_res_y << "}\n"
		<< "*GeometryQuality {" << m_geometry_quality << "}\n"
		<< "*TextureQuality  {" << m_texture_quality  << "}\n"
		;
	return out.str();
}
	
// Load settings from a string of settings data
bool las::Settings::Import(string settings)
{
	try
	{
		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings.c_str());
		reader.AddSource(src);
		
		// Verify the hash values are correct
		#if PR_DBG_LDR
		#define SETTING(type, member, name, hashvalue, default_value) PR_ASSERT(DBG, reader.HashKeyword(#name) == hashvalue, pr::FmtS("Hash value for %s incorrect. Should be 0x%08x\n", #name, reader.HashKeyword(#name)));
		#include "settings.h"
		#endif

		for (ESetting::Type setting; reader.NextKeywordH(setting);)
		{
			switch (setting)
			{
			default:
				PR_ASSERT(DBG, false, "Unknown setting");
				break; // Ignore unknown settings
			case ESetting::Version:
				{
					std::string version; reader.ExtractStringS(version);
					if (!pr::str::Equal(version, las::AppVersion())) { throw las::Exception(EResult::SettingsOutOfDate); }
				}break;
			case ESetting::FullScreen:
				{
					reader.ExtractBoolS(m_fullscreen);
				}break;
			case ESetting::XResolution:
				{
					reader.ExtractIntS(m_res_x, 10);
				}break;
			case ESetting::YResolution:
				{
					reader.ExtractIntS(m_res_y, 10);
				}break;
			case ESetting::GeometryQuality:
				{
					int quality; reader.ExtractIntS(quality, 10);
					m_geometry_quality = static_cast<pr::rdr::EQuality::Type>(pr::Clamp<int>(quality, pr::rdr::EQuality::Low, pr::rdr::EQuality::High));
				}break;
			case ESetting::TextureQuality:
				{
					int quality; reader.ExtractIntS(quality, 10);
					m_texture_quality = static_cast<pr::rdr::EQuality::Type>(pr::Clamp<int>(quality, pr::rdr::EQuality::Low, pr::rdr::EQuality::High));
				}break;
			}
		}
		return true;
	}
	catch (las::Exception const& e)
	{
		pr::script::string msg = "Error found while parsing settings.\n"; msg += e.what();
		if (e.code() == las::EResult::SettingsOutOfDate) pr::events::Send(las::Evt_Warn("Settings file is out of date. Default settings used."));
		else pr::events::Send(las::Evt_Error(msg));
	}
	catch (pr::script::Exception const& e)
	{
		pr::script::string msg = "Error found while parsing settings.\n" + e.msg();
		pr::events::Send(las::Evt_Error(msg));
	}
	catch (std::exception const& e)
	{
		std::string msg = "Error found while parsing settings.\nError details: "; msg += e.what();
		pr::events::Send(las::Evt_Error(msg));
	}
	*this = Settings(m_filename, false);
	return false;
}
	
// Fill out the user settings from a file
bool las::Settings::Load(wstring const& file)
{
	for (;;)
	{
		// Read the user settings file into a string
		std::string settings;
		if (!pr::filesys::FileExists(file))            { pr::events::Send(las::Evt_Info(pr::FmtS("Settings file '%s' not found", file.c_str()))); break; }
		if (!pr::FileToBuffer(file.c_str(), settings)) { pr::events::Send(las::Evt_Error(pr::FmtS("Settings file '%s' could not be read", file.c_str()))); break; }
		return Import(settings);
	}
	*this = Settings(file, false);
	return false;
}
	
// Save user preferences
bool las::Settings::Save(wstring const& file)
{
	std::string settings = Export();
	m_hash = pr::hash::FastHash(settings.c_str(), (pr::uint)settings.size(), 0);
	return pr::BufferToFile(settings, file.c_str());
}
