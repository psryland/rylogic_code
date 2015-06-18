//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#pragma once

#include "lost_at_sea/src/forward.h"

namespace las
{
	#define LAS_SETTING(x) \
		x(std::string ,Version     ,AppVersionA() ,0x1f8417ed ,"Application version number")\
		x(bool        ,FullScreen  ,false         ,0x1f8412ed ,"Full screen mode enabled"  )\
		x(pr::uint    ,XResolution ,1024          ,0x138412ed ,"Screen X resolution"       )\
		x(pr::uint    ,YResolution ,768           ,0x138412ee ,"Screen Y resolution"       )
	
	PR_DEFINE_SETTINGS(Settings, LAS_SETTING);
	#undef LAS_SETTING


	//// Generate an enum of the hash values
	//namespace ESetting
	//{
	//	enum Type
	//	{
	//	#define SETTING(type, member, name, hashvalue, default_value) name = hashvalue,
	//	#include "settings.h"
	//	};
	//}
	//
	//char const* AppTitle();
	//char const* AppVersion();
	//char const* AppVendor();
	//char const* AppCopyright();
	//
	//// A structure containing the user settings
	//struct Settings
	//{
	//	typedef std::list<std::string> TStringList;
	//	enum { MaxRecentFiles = 20 };
	//	wstring  m_filename; // The file to save settings to
	//	pr::hash::HashValue m_hash; // The crc of this object last time it was saved

	//	// Declare settings
	//	#define SETTING(type, member, name, hashvalue, default_value) type member;
	//	#include "settings.h"

	//	// Construct with defaults
	//	Settings(wstring const& filename, bool load);

	//	// Load/Save the user settings from/to file
	//	bool SaveRequired() const;
	//	string Export() const;
	//	bool Import(string settings);
	//	bool Load(wstring const& file);
	//	bool Save(wstring const& file);
	//	bool Save() { return Save(m_filename); }
	//};
}
