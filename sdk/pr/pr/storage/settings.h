//*****************************************
// Settings
// Copyright © Rylogic Ltd 2013
//*****************************************
// Usage:
// #include "pr/storage/settings.h"
//
// #define PR_SETTING(x)\
//   x(type, name, default_value, hashvalue, description)\
//   x(type, name, default_value, hashvalue, description)\
//   x(type, name, default_value, hashvalue, description)
// PR_DEFINE_SETTINGS(MySettings, PR_SETTING);
// #undef PR_SETTING

#ifndef PR_STORAGE_SETTINGS_H
#define PR_STORAGE_SETTINGS_H
#pragma once

#include <string>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/events.h"
#include "pr/common/colour.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/maths/maths.h"
#include "pr/script/reader.h"

namespace pr
{
	namespace settings
	{
		// Export/Import function overloads - overload as necessary (with appropriate return types)
		//template <typename T> inline T const& Write(T const& t)                      { static_assert(false, "No suitable pr::settings::Write() overload for this type"); return t; }
		//template <typename T> inline bool     Read(pr::script::Reader& reader, T& t) { static_assert(false, "No suitable pr::settings::Read() overload for this type"); }
	}
}
namespace pr
{
	namespace settings
	{
		// An event generated if there is an error parsing the settings
		// Specialised by each settings type
		template <typename TSettings> struct Evt
		{
			enum ELevel { Info, Warning, Error };
			std::string m_msg;
			ELevel m_level;
			Evt(std::string msg, ELevel level) :m_msg(msg) ,m_level(level) {}
		};

		// Export function helper overloads
		inline char const* Write(bool t)                { return t ? "true" : "false"; }
		inline char const* Write(float t)               { return pr::FmtS("%g", t); }
		inline char const* Write(int t)                 { return pr::FmtS("%d", t); }
		inline char const* Write(unsigned int t)        { return pr::FmtS("%u", t); }
		inline char const* Write(unsigned __int64 t)    { return pr::FmtS("%u", t); }
		inline char const* Write(pr::v2 const& t)       { return pr::FmtS("%f %f", t.x, t.y); }
		inline char const* Write(pr::v4 const& t)       { return pr::FmtS("%f %f %f %f", t.x, t.y, t.z, t.w); }
		inline char const* Write(pr::Colour32 t)        { return pr::FmtS("%08X", t.m_aarrggbb); }
		inline std::string Write(std::string const& t)  { return pr::filesys::AddQuotesC(pr::str::StringToCString<std::string>(t)); }

		// Import function helper overloads
		inline bool Read(pr::script::Reader& reader, bool& t)             { return reader.ExtractBoolS(t); }
		inline bool Read(pr::script::Reader& reader, float& t)            { return reader.ExtractRealS(t); }
		inline bool Read(pr::script::Reader& reader, int& t)              { return reader.ExtractIntS(t, 10); }
		inline bool Read(pr::script::Reader& reader, unsigned int& t)     { return reader.ExtractIntS(t, 10); }
		inline bool Read(pr::script::Reader& reader, unsigned __int64& t) { return reader.ExtractIntS(t, 10); }
		inline bool Read(pr::script::Reader& reader, pr::v2& t)           { return reader.ExtractVector2S(t); }
		inline bool Read(pr::script::Reader& reader, pr::v4& t)           { return reader.ExtractVector4S(t); }
		inline bool Read(pr::script::Reader& reader, pr::Colour32& t)     { return reader.ExtractIntS(t.m_aarrggbb, 16); }
		inline bool Read(pr::script::Reader& reader, std::string& t)      { return reader.ExtractCStringS(t); }

		template <typename TEnum> inline typename std::enable_if<pr::is_enum<TEnum>::value, char const*>::type Write(TEnum t)                             { return TEnum::ToString(t); }
		template <typename TEnum> inline typename std::enable_if<pr::is_enum<TEnum>::value, bool>::type        Read(pr::script::Reader& reader, TEnum& t) { return reader.ExtractEnumS(t); }
	}

	// A base class for settings types
	template<typename TSettings> struct SettingsBase
	{
		// Create an event for this settings type
		typedef typename pr::settings::Evt<TSettings> Evt;

		std::string m_filepath;    // The file path to save the settings
		pr::hash::HashValue m_crc; // The crc of the settings last time they were saved
		std::string m_comments;    // Comments to add to the head of the exported settings

		// Settings constructor
		SettingsBase(std::string filepath)
			:m_filepath(filepath)
			,m_crc()
			,m_comments()
		{}

		// Load settings from file
		bool Load() { return Load(m_filepath); }
		bool Load(std::string file)
		{
			m_filepath = file;
			std::string settings;
			if (!pr::filesys::FileExists(m_filepath)) { pr::events::Send(Evt(pr::FmtS("User settings file '%s' not found", m_filepath.c_str()), Evt::Warning)); return false; }
			if (!pr::FileToBuffer(m_filepath.c_str(), settings)) { pr::events::Send(Evt(pr::FmtS("User settings file '%s' could not be read", m_filepath.c_str()), Evt::Error)); return false; }
			return Import(settings);
		}

		// Save settings to file
		bool Save() { return Save(m_filepath); }
		bool Save(std::string file)
		{
			std::string settings = Export();
			m_filepath = file;
			std::string dir = pr::filesys::GetDirectory(m_filepath);
			if (!pr::filesys::DirectoryExists(dir) && !pr::filesys::CreateDir(dir)) { pr::events::Send(Evt(pr::FmtS("Failed to save user settings file '%s'",m_filepath.c_str()), Evt::Error)); return false; }
			if (!pr::BufferToFile(settings, m_filepath.c_str())) { pr::events::Send(Evt(pr::FmtS("Failed to save user settings file '%s'",m_filepath.c_str()), Evt::Error)); return false; }
			m_crc = Crc(settings);
			return true;
		}

		// Returns true if the settings have changed since last saved
		bool SaveRequired() const { return m_crc != Crc(Export()); }

		// Export the settings to a string
		std::string Export() const
		{
			std::stringstream out;
			if (!m_comments.empty()) out << "// " << m_comments << "\r\n";
			for (int i = 0; i != TSettings::NumberOf; ++i)
				static_cast<TSettings const*>(this)->Write(out, TSettings::ByIndex(i));
			return out.str();
		}

		// Import settings from a string using a default script reader
		bool Import(std::string const& settings)
		{
			// Create a default reader from the import string
			pr::script::Reader reader;
			pr::script::PtrSrc src(settings.c_str());
			reader.AddSource(src);
			return Import(reader);
		}

		// Load settings from a script reader
		bool Import(pr::script::Reader& reader)
		{
			try
			{
				// Verify the hash values are correct
				#if PR_DBG
				std::string invalid_hashcodes;
				for (int i = 0; i != TSettings::NumberOf; ++i)
				{
					pr::hash::HashValue hash;
					auto setting = TSettings::ByIndex(i);
					auto name    = TSettings::Name(setting);
					if ((hash = reader.HashKeyword(name)) != static_cast<pr::hash::HashValue>(setting))
						invalid_hashcodes += pr::FmtS("\n%-48s hash value should be 0x%08X", name, hash);
				}
				PR_ASSERT(PR_DBG, invalid_hashcodes.empty(), invalid_hashcodes.c_str());
				#endif

				// Read the settings
				for (TSettings::Enum_ setting; reader.NextKeywordH<TSettings::Enum_>(setting);)
					static_cast<TSettings*>(this)->Read(reader, setting);

				m_crc = Crc(Export());
				return true;
			}
			catch (std::exception const& e)
			{
				pr::events::Send(Evt(pr::FmtS("Error found while parsing user settings.\n%s", e.what()), Evt::Error));
			}

			// Initialise to defaults on failure
			static_cast<TSettings&>(*this) = TSettings(m_filepath, false);
			return false;
		}

	protected:

		// Returns the crc of 'settings'
		pr::hash::HashValue Crc(std::string const& settings) const
		{
			return pr::hash::FastHash(settings.c_str(), settings.size());
		}
	};
}

#define PR_SETTINGS_INSTANTIATE(type, name, default_value, hashvalue, description)   type m_##name;
#define PR_SETTINGS_CONSTRUCT(type, name, default_value, hashvalue, description)     ,m_##name(default_value)
#define PR_SETTINGS_ENUM(type, name, default_value, hashvalue, description)          name = hashvalue,
#define PR_SETTINGS_ENUM_TOSTRING(type, name, default_value, hashvalue, description) case name: return #name;
#define PR_SETTINGS_ENUM_FIELDS(type, name, default_value, hashvalue, description)   name,
#define PR_SETTINGS_COUNT(type, name, default_value, hashvalue, description)         +1
#define PR_SETTINGS_READ(type, name, default_value, hashvalue, description)          case name: return pr::settings::Read(reader, m_##name);
#define PR_SETTINGS_WRITE(type, name, default_value, hashvalue, description)         case name: out << '*' << #name << " {" << pr::settings::Write(m_##name) << "}" << (""description[0]?" // "description:"") << "\r\n"; break;

#define PR_DEFINE_SETTINGS(settings_name, x)\
	struct settings_name :pr::SettingsBase<settings_name>\
	{\
		/* Members */\
		x(PR_SETTINGS_INSTANTIATE)\
\
		settings_name(std::string filepath = "", bool load = false)\
			:SettingsBase(filepath)\
			x(PR_SETTINGS_CONSTRUCT)\
		{\
			if (load && !m_filepath.empty())\
				Load(m_filepath);\
			else\
				m_crc = Crc(Export());\
		}\
\
		/* Setting names and hash values*/\
		enum Enum_ { x(PR_SETTINGS_ENUM) };\
		enum { NumberOf = 0 x(PR_SETTINGS_COUNT) };\
\
		/* Enum to string*/\
		static char const* Name(Enum_ setting)\
		{\
			switch (setting)\
			{\
			default: return pr::FmtS("Unknown setting. Hash value = %d", setting);\
			x(PR_SETTINGS_ENUM_TOSTRING)\
			};\
		}\
\
		/* Enum by index */\
		static Enum_ ByIndex(int i)\
		{\
			static settings_name::Enum_ const map[] = { x(PR_SETTINGS_ENUM_FIELDS) };\
			if (i < 0 || i > NumberOf) throw std::exception("index out of range for setting in "#settings_name);\
			return map[i];\
		}\
\
	private:\
		friend struct pr::SettingsBase<settings_name>;\
\
		/* Read the value of 'setting' from 'reader' */\
		bool Read(pr::script::Reader& reader, Enum_ setting)\
		{\
			switch (setting)\
			{\
			default: PR_INFO(PR_DBG, pr::FmtS("Unknown user setting '"#settings_name"::%s' ignored", Name(setting))); return false;\
			x(PR_SETTINGS_READ)\
			}\
		}\
\
		/* Write the value of 'setting' to 'out' */\
		void Write(std::stringstream& out, Enum_ setting) const\
		{\
			switch (setting)\
			{\
			default: PR_INFO(PR_DBG, pr::FmtS("Unknown user setting '"#settings_name"::%s' ignored", Name(setting))); break;\
			x(PR_SETTINGS_WRITE)\
			}\
		}\
	};

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/macros/enum.h"
namespace pr
{
	namespace unittests
	{
		namespace settings
		{
			#define PR_ENUM(x)\
			x(One)\
			x(Two)\
			x(Three)
			PR_DEFINE_ENUM1(TestEnum, PR_ENUM);
			#undef PR_ENUM

			//x(type, name, default_value, hashvalue, description)
			#define PR_SETTING(x)\
				x(int          , count    , 2                     , 0x153E841B, "")\
				x(float        , scale    , 3.14f                 , 0x1837069D, "")\
				x(unsigned int , mask     , 0xABCU                , 0x061897B0, "")\
				x(pr::Colour32 , colour   , pr::Colour32Green     , 0x08B2C176, "the colour")\
				x(pr::v2       , area     , pr::v2::make(1,2)     , 0x1A1C8937, "")\
				x(pr::v4       , position , pr::v4::make(1,2,3,1) , 0x06302A63, "")\
				x(std::string  , name     , "hello settings"      , 0x0C08C4A4, "")\
				x(TestEnum     , emun     , TestEnum::Two         , 0x079F037D, "")
			PR_DEFINE_SETTINGS(Settings, PR_SETTING);
			#undef PR_SETTING
		}

		PRUnitTest(pr_storage_settings)
		{
			using namespace settings;

			Settings s;
			PR_CHECK(s.m_count    , 2                     );
			PR_CHECK(s.m_scale    , 3.14f                 );
			PR_CHECK(s.m_mask     , 0xABCU                );
			PR_CHECK(s.m_colour   , pr::Colour32Green     );
			PR_CHECK(s.m_area     , pr::v2::make(1,2)     );
			PR_CHECK(s.m_position , pr::v4::make(1,2,3,1) );
			PR_CHECK(s.m_name     , "hello settings"      );
			PR_CHECK(s.m_emun     , TestEnum::Two         );
			PR_CHECK(s.SaveRequired(), false);

			s.m_count    = 4                     ;
			s.m_scale    = 1.6f                  ;
			s.m_mask     = 0xCDEU                ;
			s.m_colour   = pr::Colour32Blue      ;
			s.m_area     = pr::v2One             ;
			s.m_position = pr::v4::make(3,2,1,1) ;
			s.m_name     = "renamed"             ;
			s.m_emun     = TestEnum::Three       ;
			PR_CHECK(s.SaveRequired(), true);
			PR_CHECK(s.m_count    , 4                     );
			PR_CHECK(s.m_scale    , 1.6f                  );
			PR_CHECK(s.m_mask     , 0xCDEU                );
			PR_CHECK(s.m_colour   , pr::Colour32Blue      );
			PR_CHECK(s.m_area     , pr::v2One             );
			PR_CHECK(s.m_position , pr::v4::make(3,2,1,1) );
			PR_CHECK(s.m_name     , "renamed"             );
			PR_CHECK(s.m_emun     , TestEnum::Three       );

			std::string settings = s.Export();
			PR_CHECK(settings,
				"*count {4}\r\n"
				"*scale {1.6}\r\n"
				"*mask {3294}\r\n"
				"*colour {FF0000FF} // the colour\r\n"
				"*area {1.000000 1.000000}\r\n"
				"*position {3.000000 2.000000 1.000000 1.000000}\r\n"
				"*name {\"renamed\"}\r\n"
				"*emun {Three}\r\n");

			Settings s2;
			s2.Import(settings);
			PR_CHECK(s2.m_count    , 4                     );
			PR_CHECK(s2.m_scale    , 1.6f                  );
			PR_CHECK(s2.m_mask     , 0xCDEU                );
			PR_CHECK(s2.m_colour   , pr::Colour32Blue      );
			PR_CHECK(s2.m_area     , pr::v2One             );
			PR_CHECK(s2.m_position , pr::v4::make(3,2,1,1) );
			PR_CHECK(s2.m_name     , "renamed"             );
			PR_CHECK(s2.m_emun     , TestEnum::Three       );
			PR_CHECK(s2.SaveRequired(), false);
		}
	}
}
#endif

#endif
