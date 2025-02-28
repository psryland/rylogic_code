//********************************
// Ldraw Script Text Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/script/script.h"

namespace pr::rdr12::ldraw
{
	struct TextReaderImpl : IReader
	{
		script::StreamSrc m_src;
		script::Preprocessor m_pp;
		mutable Location m_location;
		string32 m_keyword;
		string32 m_delim;

		TextReaderImpl(std::istream& src, EEncoding enc, std::filesystem::path src_filepath, ReportErrorCB report_error_cb, ParseProgressCB progress_cb, IPathResolver const& resolver)
			: IReader(report_error_cb, progress_cb, resolver)
			, m_src(src, enc, script::Loc(src_filepath))
			, m_pp(m_src)
			, m_location()
			, m_keyword()
			, m_delim(" \t\r\n\v,;")
		{
		}
		
		// Return the current location in the source
		virtual Location const& Loc() const override
		{
			auto loc = m_pp.Location();
			m_location.m_filepath = loc.Filepath();
			m_location.m_column = loc.Col();
			m_location.m_line = loc.Line();
			m_location.m_offset = loc.Pos();
			return m_location;
		}

		// Move into a nested section
		virtual void PushSection() override
		{
			script::EatDelimiters(m_pp, m_delim.c_str());
			if (*m_pp != '{')
				throw std::runtime_error("section start expected");

			++m_pp;
		}
		
		// Leave the current nested section
		virtual void PopSection() override
		{
			script::EatDelimiters(m_pp, m_delim.c_str());
			if (*m_pp != '}')
				throw std::runtime_error("section end expected");

			++m_pp;
		}
		
		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeyword(ldraw::EKeyword& kw) override
		{
			for (;*m_pp && *m_pp != '}' && *m_pp != '*';)
			{
				if (*m_pp == '\"') { script::EatLiteral(m_pp, m_pp.Location()); continue; }
				if (*m_pp == '{')  { script::EatSection(m_pp, m_pp.Location()); continue; }
				++m_pp;
			}
			if (*m_pp == '*') ++m_pp; else return false;
			m_keyword.resize(0);
			if (!str::ExtractIdentifier(m_keyword, m_pp, m_delim.c_str())) return false;
			str::LowerCase(m_keyword);
			return true;
		}

		// Returns true if the next non-whitespace character is the start/end of a section
		bool IsSectionStart()
		{
			EatDelimiters(m_pp, m_delim.c_str());
			return *m_pp == '{';
		}

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() override
		{
			EatDelimiters(m_pp, m_delim.c_str());
			return *m_pp == '}';
		}

		// Read a utf8 string from the current section.
		// If 'has_length' is false, assume the whole section is the string.
		// If 'has_length' is true, assume the string is prefixed by its length.
		virtual string32 StringImpl(bool) override
		{
			string32 str;
			if (!str::ExtractString(str, m_pp, m_delim.c_str()))
				throw std::runtime_error("string expected");
		
			str::ProcessIndentedNewlines(str);
			return str;
		}

		// Read an integral value from the current section
		virtual int64_t IntImpl(int, int radix) override
		{
			int64_t int_;
			if (!str::ExtractInt(int_, radix, m_pp, m_delim.c_str()))
				throw std::runtime_error("integer value expected");

			return int_;
		}

		// Read a floating point value from the current section
		virtual double RealImpl(int byte_count) override
		{
			double real_;
			if (!str::ExtractReal(real_, m_pp, m_delim.c_str()))
				throw std::runtime_error("real value expected");

			return real_;
		}
	};

	// --------------------------------------------------------------------------------------------

	TextReader::TextReader(std::istream& src, EEncoding enc, std::filesystem::path src_filepath, ReportErrorCB report_error_cb, ParseProgressCB progress_cb, IPathResolver const& resolver)
		: m_impl(new TextReaderImpl(src, enc, src_filepath, report_error_cb, progress_cb, resolver))
	{
	}
}