//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#ifndef SETTING
#define SETTING(type, member, name, hashvalue, default_value)
#endif

SETTING(char const*                   ,m_version                       ,Version                     ,0x1f8417ed        ,las::AppVersion())
SETTING(bool                          ,m_fullscreen                    ,FullScreen                  ,0x1f8412ed        ,false)
SETTING(pr::uint                      ,m_res_x                         ,XResolution                 ,0x138412ed        ,1024)
SETTING(pr::uint                      ,m_res_y                         ,YResolution                 ,0x138412ee        ,768)
SETTING(pr::rdr::EQuality::Type       ,m_geometry_quality              ,GeometryQuality             ,0x1c0bba13        ,pr::rdr::EQuality::High)
SETTING(pr::rdr::EQuality::Type       ,m_texture_quality               ,TextureQuality              ,0x17a72093        ,pr::rdr::EQuality::High)
#undef SETTING

#ifndef LAS_SETTINGS_H
#define LAS_SETTINGS_H

#include "forward.h"

namespace las
{
	// Generate an enum of the hash values
	namespace ESetting
	{
		enum Type
		{
		#define SETTING(type, member, name, hashvalue, default_value) name = hashvalue,
		#include "settings.h"
		};
	}
	
	char const* AppTitle();
	char const* AppVersion();
	char const* AppVendor();
	char const* AppCopyright();
	
	// A structure containing the user settings
	struct Settings
	{
		typedef std::list<std::string> TStringList;
		enum { MaxRecentFiles = 20 };
		wstring  m_filename; // The file to save settings to
		pr::hash::HashValue m_hash; // The crc of this object last time it was saved

		// Declare settings
		#define SETTING(type, member, name, hashvalue, default_value) type member;
		#include "settings.h"

		// Construct with defaults
		Settings(wstring const& filename, bool load);

		// Load/Save the user settings from/to file
		bool SaveRequired() const;
		string Export() const;
		bool Import(string settings);
		bool Load(wstring const& file);
		bool Save(wstring const& file);
		bool Save() { return Save(m_filename); }
	};
}

#endif
