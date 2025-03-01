//********************************
// Ldraw Script Text Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/script/script.h"

namespace pr::rdr12::ldraw
{
	template <typename Char>
	struct TextReaderImpl : IReader
	{
		script::StreamSrc<Char> m_src;
		script::Preprocessor m_pp;
		mutable Location m_location;
		string32 m_keyword;
		wstring32 m_delim;

		TextReaderImpl(std::basic_istream<Char>& src, std::filesystem::path src_filepath, EEncoding enc, ReportErrorCB report_error_cb, ParseProgressCB progress_cb, IPathResolver const& resolver)
			: IReader(report_error_cb, progress_cb, resolver)
			, m_src(src, enc, script::Loc(src_filepath))
			, m_pp(m_src)
			, m_location()
			, m_keyword()
			, m_delim(L" \t\r\n\v,;")
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

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() override
		{
			EatDelimiters(m_pp, m_delim.c_str());
			return *m_pp == '}';
		}

		// True when the source is exhausted
		virtual bool IsSourceEnd() override
		{
			EatDelimiters(m_pp, m_delim.c_str());
			return *m_pp == 0;
		}
		
		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeywordImpl(int& kw) override
		{
			for (;*m_pp && *m_pp != '}' && *m_pp != '*';)
			{
				if (*m_pp == '\"') { script::EatLiteral(m_pp, m_pp.Location()); continue; }
				if (*m_pp == '{')  { script::EatSection(m_pp, m_pp.Location()); continue; }
				++m_pp;
			}
			if (*m_pp == '*') ++m_pp; else return false;
			
			wstring32 keyword;
			if (!str::ExtractIdentifier(keyword, m_pp, m_delim.c_str())) return false;
			str::LowerCase(keyword);

			m_keyword = Narrow(keyword);
			kw = HashI(m_keyword.c_str());
			return true;
		}

		// Read a utf8 string from the current section.
		// If 'has_length' is false, assume the whole section is the string.
		// If 'has_length' is true, assume the string is prefixed by its length.
		virtual string32 StringImpl(bool) override
		{
			wstring32 str;
			if (!str::ExtractString(str, m_pp, m_delim.c_str()))
				throw std::runtime_error("string expected");
		
			str::ProcessIndentedNewlines(str);
			return Narrow(str);
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
		virtual double RealImpl(int) override
		{
			double real_;
			if (!str::ExtractReal(real_, m_pp, m_delim.c_str()))
				throw std::runtime_error("real value expected");

			return real_;
		}

		// Read a boolean value from the current section
		virtual bool BoolImpl() override
		{
			bool bool_;
			if (!str::ExtractBool(bool_, m_pp, m_delim.c_str()))
				throw std::runtime_error("boolean value expected");

			return bool_;
		}
	};

	// --------------------------------------------------------------------------------------------

	TextReader::TextReader(std::istream& src, std::filesystem::path src_filepath, EEncoding enc, ReportErrorCB report_error_cb, ParseProgressCB progress_cb, IPathResolver const& resolver)
		: m_impl(new TextReaderImpl(src, src_filepath, enc, report_error_cb, progress_cb, resolver))
	{}
	TextReader::TextReader(std::wistream& src, std::filesystem::path src_filepath, EEncoding enc, ReportErrorCB report_error_cb, ParseProgressCB progress_cb, IPathResolver const& resolver)
		: m_impl(new TextReaderImpl(src, src_filepath, enc, report_error_cb, progress_cb, resolver))
	{}
}