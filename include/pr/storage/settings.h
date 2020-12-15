//*****************************************
// Settings
// Copyright (c) Rylogic Ltd 2013
//*****************************************
// Usage:
// #include "pr/storage/settings.h"
//
// #define PR_SETTING(x)\
//   x(type, name, default_value, hash-value, description)\
//   x(type, name, default_value, hash-value, description)\
//   x(type, name, default_value, hash-value, description)
// PR_DEFINE_SETTINGS(MySettings, PR_SETTING);
// #undef PR_SETTING

#pragma once

#include <string>
#include <filesystem>
#include <type_traits>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/gfx/colour.h"
#include "pr/common/hash.h"
#include "pr/common/event_handler.h"
#include "pr/maths/maths.h"
#include "pr/script/reader.h"
#include "pr/str/string_util.h"
#include "pr/str/string.h"

namespace pr::settings
{
	// Export/Import function overloads - overload as necessary (with appropriate return types)
	//template <typename T> inline T const& Write(T const& t)                      { static_assert(false, "No suitable pr::settings::Write() overload for this type"); return t; }
	//template <typename T> inline bool     Read(pr::script::Reader& reader, T& t) { static_assert(false, "No suitable pr::settings::Read() overload for this type"); }
}
namespace pr::settings
{
	using string = pr::string<wchar_t>;
	using Reader = pr::script::Reader;

	// Export function overloads
	inline string Write(bool t)
	{
		return t ? L"true" : L"false";
	}
	inline string Write(float t)
	{
		return pr::FmtS(L"%g", t);
	}
	inline string Write(int t)
	{
		return pr::FmtS(L"%d", t);
	}
	inline string Write(unsigned int t)
	{
		return pr::FmtS(L"%u", t);
	}
	inline string Write(unsigned __int64 t)
	{
		return pr::FmtS(L"%u", t);
	}
	inline string Write(pr::v2 const& t)
	{
		return pr::FmtS(L"%f %f", t.x, t.y);
	}
	inline string Write(pr::v4 const& t)
	{
		return pr::FmtS(L"%f %f %f %f", t.x, t.y, t.z, t.w);
	}
	inline string Write(pr::Colour32 t)
	{
		return pr::FmtS(L"%08X", t.argb);
	}
	inline string Write(wchar_t const* t)
	{
		auto s = pr::str::StringToCString<string>(t);
		s = str::Quotes(s, true);
		return s;
	}
	template <typename Char> inline string Write(std::basic_string<Char> const& t)
	{
		return Write(t.c_str());
	}
	inline string Write(char const* t)
	{
		return Write(Widen(t));
	}
	template <typename TEnum, typename = std::enable_if_t<std::is_enum<TEnum>::value>> inline string Write(TEnum x)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		if constexpr (is_reflected_enum<TEnum>::value)
		{
			return Enum<TEnum>::ToStringW(x);
		}
		else
		{
			return Write(ut(x));
		}
	}

	// Import function helper overloads
	inline bool Read(Reader& reader, bool& t)
	{
		return reader.BoolS(t);
	}
	inline bool Read(Reader& reader, float& t)
	{
		return reader.RealS(t);
	}
	inline bool Read(Reader& reader, int& t)
	{
		return reader.IntS(t, 10);
	}
	inline bool Read(Reader& reader, unsigned int& t)
	{
		return reader.IntS(t, 10);
	}
	inline bool Read(Reader& reader, unsigned __int64& t)
	{
		return reader.IntS(t, 10);
	}
	inline bool Read(Reader& reader, pr::v2& t)
	{
		return reader.Vector2S(t);
	}
	inline bool Read(Reader& reader, pr::v4& t)
	{
		return reader.Vector4S(t);
	}
	inline bool Read(Reader& reader, pr::Colour32& t)
	{
		return reader.IntS(t.argb, 16);
	}
	inline bool Read(Reader& reader, std::string& t)
	{
		return reader.CStringS(t);
	}
	inline bool Read(Reader& reader, std::wstring& t)
	{
		return reader.CStringS(t);
	}
	template <typename TEnum, typename = std::enable_if_t<std::is_enum<TEnum>::value>> inline bool Read(Reader& reader, TEnum& x)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		if constexpr (is_reflected_enum<TEnum>::value)
		{
			std::string ident;
			return reader.IdentifierS(ident) && Enum<TEnum>::TryParse(x, ident.c_str(), false);
		}
		else
		{
			return reader.IntS(reinterpret_cast<ut&>(x), 10);
		}
	}
}
namespace pr
{
	// A CRTP base class for settings types
	template<typename Derived>
	struct SettingsBase
	{
		std::filesystem::path m_filepath; // The file path to save the settings
		std::size_t m_crc;                // The CRC of the settings last time they were saved
		std::string m_comments;           // Comments to add to the head of the exported settings

		// Settings constructor
		SettingsBase(std::filesystem::path const& filepath, bool throw_on_error = true)
			:m_filepath(filepath)
			,m_crc()
			,m_comments()
		{
			if (throw_on_error)
			{
				OnError += [](auto&, ErrorEventArgs const& err)
				{
					throw std::runtime_error(Narrow(err.m_msg));
				};
			}
		}

		// Raised on error conditions
		EventHandler<SettingsBase&, ErrorEventArgs const&> OnError;

		// Load settings from file
		bool Load()
		{
			return Load(m_filepath);
		}
		bool Load(std::filesystem::path const& file)
		{
			m_filepath = file;
			if (!std::filesystem::exists(m_filepath))
				OnError(*this, { Fmt(L"User settings file '%s' not found", m_filepath.c_str()) });

			// Read the settings into a buffer
			std::string settings;
			if (!pr::filesys::FileToBuffer(m_filepath, settings))
				OnError(*this, { Fmt(L"User settings file '%s' could not be read", m_filepath.c_str()) });

			return Import(settings);
		}

		// Save settings to file
		bool Save()
		{
			return Save(m_filepath);
		}
		bool Save(std::filesystem::path const& file)
		{
			m_filepath = file;

			auto dir = m_filepath.parent_path();
			if (!std::filesystem::exists(dir) && !std::filesystem::create_directories(dir))
			{
				OnError(*this, { Fmt(L"Failed to save user settings file '%S'", m_filepath.c_str()) });
				return false;
			}

			auto settings = Export();
			if (!filesys::BufferToFile(settings, m_filepath))
			{
				OnError(*this, { Fmt(L"Failed to save user settings file '%S'", m_filepath.c_str()) });
				return false;
			}

			m_crc = Crc(settings);
			return true;
		}

		// Returns true if the settings have changed since last saved
		bool SaveRequired() const
		{
			return m_crc != Crc(Export());
		}

		// Export the settings to a string
		std::string Export() const
		{
			std::stringstream out;
			if (!m_comments.empty()) out << "// " << m_comments << "\r\n";
			for (int i = 0; i != Derived::NumberOf; ++i)
				static_cast<Derived const*>(this)->Write(out, Derived::ByIndex(i));
			return out.str();
		}

		// Import settings from a string using a default script reader
		bool Import(std::string const& settings)
		{
			using namespace pr::script;

			// Create a default reader from the import string
			StringSrc src(settings);
			Reader reader(src);
			return Import(reader);
		}

		// Load settings from a script reader
		bool Import(script::Reader& reader)
		{
			using Enum_ = typename Derived::Enum_;

			try
			{
				// Verify the hash values are correct
				#if PR_DBG
				std::string invalid_hashcodes;
				for (int i = 0; i != Derived::NumberOf; ++i)
				{
					int hash;
					auto setting = Derived::ByIndex(i);
					auto name    = Derived::NameW(setting);
					if ((hash = reader.HashKeyword(name)) != static_cast<pr::hash::HashValue>(setting))
						invalid_hashcodes += pr::FmtS("%-48s hash value should be 0x%08X\n", Derived::NameA(setting), hash);
				}
				if (!invalid_hashcodes.empty())
				{
					OutputDebugStringA(invalid_hashcodes.c_str());
					PR_ASSERT(PR_DBG, false, "Settings hash codes are incorrect");
				}
				#endif

				// Read the settings
				for (Enum_ setting; reader.NextKeywordH<Enum_>(setting);)
					static_cast<Derived*>(this)->Read(reader, setting);

				m_crc = Crc(Export());
				return true;
			}
			catch (std::exception const& e)
			{
				OnError(*this, { Fmt(L"Error found while parsing user settings.\n%S", e.what()) });
			}

			// Initialise to defaults on failure
			static_cast<Derived&>(*this) = Derived(m_filepath, false);
			return false;
		}

	protected:

		// Returns the CRC of 'settings'
		size_t Crc(std::string const& settings) const
		{
			return pr::hash::Hash(settings.c_str());
		}
	};
}

#define PR_SETTINGS_INSTANTIATE(type, name, default_value, description)   type m_##name;
#define PR_SETTINGS_CONSTRUCT(type, name, default_value, description)     ,m_##name(default_value)
#define PR_SETTINGS_ENUM(type, name, default_value, description)          name = pr::hash::HashCT(#name),
#define PR_SETTINGS_ENUM_TOSTRINGA(type, name, default_value, description) case name: return #name;
#define PR_SETTINGS_ENUM_TOSTRINGW(type, name, default_value, description) case name: return L#name;
#define PR_SETTINGS_ENUM_FIELDS(type, name, default_value, description)   name,
#define PR_SETTINGS_COUNT(type, name, default_value, description)         +1
#define PR_SETTINGS_READ(type, name, default_value, description)          case name: return pr::settings::Read(reader, m_##name);
#define PR_SETTINGS_WRITE(type, name, default_value, description)         case name: out << '*' << #name << " {" << pr::settings::Write(m_##name) << "}" << (""description[0]?" // "description:"") << "\r\n"; break;

#define PR_DEFINE_SETTINGS(settings_name, x)\
	struct settings_name :pr::SettingsBase<settings_name>\
	{\
		/* Members */\
		x(PR_SETTINGS_INSTANTIATE)\
\
		/* Setting names and hash values*/\
		static constexpr int NumberOf = 0 x(PR_SETTINGS_COUNT);\
		enum Enum_ { x(PR_SETTINGS_ENUM) };\
\
		settings_name(std::filesystem::path const& filepath = L"", bool load = false)\
			:SettingsBase(filepath)\
			x(PR_SETTINGS_CONSTRUCT)\
		{\
			if (load && !m_filepath.empty())\
				Load(m_filepath);\
			else\
				m_crc = Crc(Export());\
		}\
\
		/* Enum to string*/\
		static char const* NameA(Enum_ setting)\
		{\
			switch (setting)\
			{\
				x(PR_SETTINGS_ENUM_TOSTRINGA)\
				default: return pr::FmtS("Unknown setting. Hash value = %d", setting);\
			};\
		}\
		static wchar_t const* NameW(Enum_ setting)\
		{\
			switch (setting)\
			{\
				x(PR_SETTINGS_ENUM_TOSTRINGW)\
				default: return pr::FmtS(L"Unknown setting. Hash value = %d", setting);\
			};\
		}\
\
		/* Enum by index */\
		static Enum_ ByIndex(int i)\
		{\
			static settings_name::Enum_ const map[] = { x(PR_SETTINGS_ENUM_FIELDS) };\
			if (i < 0 || i >= NumberOf) throw std::runtime_error("index out of range for setting in "#settings_name);\
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
				x(PR_SETTINGS_READ)\
				default: PR_INFO(PR_DBG, pr::FmtS("Unknown user setting '"#settings_name"::%s' ignored", NameA(setting))); return false;\
			}\
		}\
\
		/* Write the value of 'setting' to 'out' */\
		void Write(std::stringstream& out, Enum_ setting) const\
		{\
			switch (setting)\
			{\
				x(PR_SETTINGS_WRITE)\
				default: PR_INFO(PR_DBG, pr::FmtS("Unknown user setting '"#settings_name"::%s' ignored", NameA(setting))); break;\
			}\
		}\
	};

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/macros/enum.h"
namespace pr::storage
{
	namespace unittests::settings
	{
		// pr enum
		#define PR_ENUM(x)\
			x(One)\
			x(Two)\
			x(Three)
		PR_DEFINE_ENUM1(Enum1, PR_ENUM);
		#undef PR_ENUM

		// standard enum
		enum class Enum2
		{
			Won,
			Too,
			Free,
		};

		//x(type, name, default_value, description)
		#define PR_SETTING(x)\
			x(int          , count    , 2                 , "")\
			x(float        , scale    , 3.14f             , "")\
			x(unsigned int , mask     , 0xABCU            , "")\
			x(pr::Colour32 , colour   , pr::Colour32Green , "the colour")\
			x(pr::v2       , area     , pr::v2(1,2)       , "")\
			x(pr::v4       , position , pr::v4(1,2,3,1)   , "")\
			x(std::string  , name     , "hello settings"  , "")\
			x(Enum1        , emun     , Enum1::Two        , "")\
			x(Enum2        , emun2    , Enum2::Free       , "")
		PR_DEFINE_SETTINGS(Settings, PR_SETTING);
		#undef PR_SETTING
	}

	PRUnitTest(SettingsTests)
	{
		using namespace unittests::settings;
			
		Enum<Enum1>::NameA();

		Settings s;
		PR_CHECK(s.m_count    , 2                 );
		PR_CHECK(s.m_scale    , 3.14f             );
		PR_CHECK(s.m_mask     , 0xABCU            );
		PR_CHECK(s.m_colour   , pr::Colour32Green );
		PR_CHECK(s.m_area     , pr::v2(1,2)       );
		PR_CHECK(s.m_position , pr::v4(1,2,3,1)   );
		PR_CHECK(s.m_name == "hello settings" , true);
		PR_CHECK(s.m_emun == Enum1::Two    , true);
		PR_CHECK(s.m_emun2 == Enum2::Free , true);
		PR_CHECK(s.SaveRequired(), false);

		s.m_count    = 4;
		s.m_scale    = 1.6f;
		s.m_mask     = 0xCDEU;
		s.m_colour   = pr::Colour32Blue;
		s.m_area     = pr::v2One;
		s.m_position = pr::v4(3, 2, 1, 1);
		s.m_name     = "renamed";
		s.m_emun     = Enum1::Three;
		s.m_emun2    = Enum2::Won;
		PR_CHECK(s.SaveRequired(), true         );
		PR_CHECK(s.m_count    , 4               );
		PR_CHECK(s.m_scale    , 1.6f            );
		PR_CHECK(s.m_mask     , 0xCDEU          );
		PR_CHECK(s.m_colour   , pr::Colour32Blue);
		PR_CHECK(s.m_area     , pr::v2One       );
		PR_CHECK(s.m_position , pr::v4(3,2,1,1) );
		PR_CHECK(s.m_name == "renamed" , true);
		PR_CHECK(s.m_emun == Enum1::Three , true);
		PR_CHECK(s.m_emun2 == Enum2::Won  , true);

		std::string settings = s.Export();
		PR_CHECK(settings,
			"*count {4}\r\n"
			"*scale {1.6}\r\n"
			"*mask {3294}\r\n"
			"*colour {FF0000FF} // the colour\r\n"
			"*area {1.000000 1.000000}\r\n"
			"*position {3.000000 2.000000 1.000000 1.000000}\r\n"
			"*name {\"renamed\"}\r\n"
			"*emun {Three}\r\n"
			"*emun2 {0}\r\n"
			);

		Settings s2;
		s2.Import(settings                       );
		PR_CHECK(s2.m_count    , 4               );
		PR_CHECK(s2.m_scale    , 1.6f            );
		PR_CHECK(s2.m_mask     , 0xCDEU          );
		PR_CHECK(s2.m_colour   , pr::Colour32Blue);
		PR_CHECK(s2.m_area     , pr::v2One       );
		PR_CHECK(s2.m_position , pr::v4(3,2,1,1) );
		PR_CHECK(s2.m_name == "renamed"       , true);
		PR_CHECK(s2.m_emun == Enum1::Three , true);
		PR_CHECK(s2.m_emun2 == Enum2::Won , true);
		PR_CHECK(s2.SaveRequired(), false);
	}
}
#endif
