//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************

#ifndef USER_SETTING
#define USER_SETTING(name, type, member, default_value, hashvalue)
#endif

USER_SETTING(DBPath      ,string   ,m_db_path       ,"medialist.db"                   ,0x05a6c141)
USER_SETTING(RecentFiles ,string   ,m_recent_files  ,                                 ,0x07beccd6)
USER_SETTING(ImageExtns  ,string   ,m_image_extns   ,"+bmp;+jpg;+jpeg;+png;+tiff"     ,0x004d1fbb)
USER_SETTING(VideoExtns  ,string   ,m_video_extns   ,"+avi;+mpg;+mpeg;+mp4;+mod;+mov" ,0x08fb31b5)
USER_SETTING(AudioExtns  ,string   ,m_audio_extns   ,"+wav;+mp3;+raw"                 ,0x14ec815c)
USER_SETTING(ZoomFill    ,bool     ,m_zoom_fill     ,true                             ,0x0d5a5aea)
USER_SETTING(ZoomFit     ,bool     ,m_zoom_fit      ,true                             ,0x03cb9070)

#undef USER_SETTING

#ifndef IMAGERN_USER_SETTINGS_H
#define IMAGERN_USER_SETTINGS_H

#include "imagern/main/forward.h"

// Generate an enum of the hash values
namespace EUserSetting
{
	enum Type
	{
#		define USER_SETTING(name, type, member, default_value, hashvalue) name = hashvalue,
#		include "user_settings.h"
	};
}

// User settings
struct UserSettings
{
	string   m_filepath; // The file path to save the settings
	pr::uint m_hash;     // The hash of this object last time it was saved
	
	// The user settings
#	define USER_SETTING(name, type, member, default_value, hashvalue) type member;
#	include "user_settings.h"
	
	// Construction with defaults
	UserSettings(string const& filepath, bool load);
	
	// Return true if the settings have changed since last save
	bool SaveRequired() const;
	
	// Return a string containing the settings
	string Export() const;
	
	// Load settings from a string
	bool Import(string const& settings);
	
	// Load/Save settings to/from file
	bool Load(string const& file);
	bool Save(string const& file);
	bool Save() { return Save(m_filepath); }
};

#endif
