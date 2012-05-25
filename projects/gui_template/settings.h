//**************************************************************
// GUI template
//  Copyright © Rylogic Ltd 2011
//**************************************************************

#ifndef USER_SETTING
#define USER_SETTING(name, type, member, default_value, hashvalue, desc)
#endif
USER_SETTING(Notepad ,std::string ,m_notepad ,"C:\\windows\\notepad.exe" ,0x12719f27 ,"The text editor to view this settings file in")
#undef USER_SETTING

#ifndef PR_GUI_TEMPLATE_SETTINGS_H
#define PR_GUI_TEMPLATE_SETTINGS_H

#include <string>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/events.h"
#include "pr/common/colour.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/script/reader.h"

namespace gui_template
{
	// User settings
	struct Settings
	{
		typedef pr::script::string string;
		
		// An event generated if there is an error parsing the settings
		struct Evt
		{
			enum ELevel { Info, Warning, Error };
			string m_msg;
			ELevel m_level;
			Evt(string const& msg, ELevel level) :m_msg(msg) ,m_level(level) {}
		};
		
		// Generate an enum of the hash values
		enum Type
		{
		#define USER_SETTING(name, type, member, default_value, hashvalue, desc) name = hashvalue,
		#include "settings.h"
		};
		
		// members
		string m_filepath; // The file path to save the settings
		#define USER_SETTING(name, type, member, default_value, hashvalue, desc) type member;
		#include "settings.h"
		
		// Construction with defaults
		Settings(string const& filepath = "", bool load = false)
		:m_filepath(filepath)
		#define USER_SETTING(name, type, member, default_value, hashvalue, desc) ,member(default_value)
		#include "settings.h"
		{
			if (load && !m_filepath.empty())
				Load(m_filepath);
		}
		
		struct ExportHelper
		{
			template <typename T> static T const& Write(T const& t) { return t; }
			static std::string Write(std::string const& t)          { return pr::filesys::AddQuotesC(t); }
			static std::string Write(pr::Colour32 c)                { return pr::FmtS("%08X", c.m_aarrggbb); }
		};

		// Return a string containing the settings
		string Export() const
		{
			std::stringstream out; out << "// User Settings\r\n";
			#define USER_SETTING(name, type, member, default_value, hashvalue, desc) out << '*' << #name << " {" << ExportHelper::Write(member) << "} // " << desc << "\r\n";
			#include "settings.h"
			return out.str();
		}
		
		// Load settings from a string
		bool Import(string const& settings)
		{
			try
			{
				// Parse the settings
				pr::script::Reader reader;
				pr::script::PtrSrc src(settings.c_str());
				reader.AddSource(src);
				
				// Verify the hash values are correct
				#if PR_DBG_COMMON
				#define USER_SETTING(name, type, member, default_value, hashvalue, desc) PR_ASSERT(PR_DBG_COMMON, reader.HashKeyword(#name) == hashvalue, pr::FmtS("Hash value for %s incorrect. Should be 0x%08x\n", #name, reader.HashKeyword(#name)));
				#include "settings.h"
				#endif
				
				// Read the settings
				for (Type setting; reader.NextKeywordH(setting);)
				{
					switch (setting)
					{
					default: PR_ASSERT(PR_DBG_COMMON, false, "Unknown user setting"); break;
					case Notepad:        reader.ExtractStringS(m_notepad); break;
					}
				}
				return true;
			}
			catch (pr::script::Exception const& e) { pr::events::Send(Evt(string("Error found while parsing user settings.\n") + e.msg(), Evt::Error)); }
			catch (std::exception const& e)        { pr::events::Send(Evt(string("Error found while parsing user settings.\nError details: ") + e.what(), Evt::Error)); }
			*this = Settings(m_filepath, false); // Initialise to defaults
			return false;
		}
		
		// Load settings from file
		bool Load() { return Load(m_filepath); }
		bool Load(string const& file)
		{
			// Read the user settings file into a string
			for (;;)
			{
				m_filepath = file;
				string settings;
				if (!pr::filesys::FileExists(file))            { pr::events::Send(Evt(string("User settings file '")+file+"' not found", Evt::Warning)); break; }
				if (!pr::FileToBuffer(file.c_str(), settings)) { pr::events::Send(Evt(string("User settings file '")+file+"' could not be read", Evt::Error)); break; }
				return Import(settings);
			}
			*this = Settings(file, false);
			return false;
		}
		
		// Save settings to file
		bool Save() { return Save(m_filepath); }
		bool Save(string const& file)
		{
			m_filepath = file;
			string settings = Export();
			string dir = pr::filesys::GetDirectory(file); // Create the directory if it doesn't exist
			if (!pr::filesys::DirectoryExists(dir) && !pr::filesys::CreateDir(dir)) { pr::events::Send(Evt(string("Failed to save user settings file '")+file+"'", Evt::Error)); return false; }
			if (!pr::BufferToFile(settings, file.c_str()))                          { pr::events::Send(Evt(string("Failed to save user settings file '")+file+"'", Evt::Error)); return false; }
			return true;
		}
	};
}

#endif
