//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/ldraw/sources/source_string.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	template <typename Char>
	SourceString<Char>::SourceString(Guid const* context_id, std::basic_string_view<Char> script, EEncoding enc, PathResolver const& includes)
		: SourceBase(context_id)
		, m_script(script)
		, m_includes(includes)
		, m_encoding(enc)
	{
		// Add the directory of the included file to the paths
		m_includes.FileOpened = [this](auto&, filepath_t const& fp)
		{
			m_includes.LocalDir(fp.parent_path());
			m_filepaths.push_back(fp.lexically_normal());
		};
	}

	// Construct a new instance of the source (if possible)
	template <typename Char>
	std::shared_ptr<SourceBase> SourceString<Char>::Clone()
	{
		return std::shared_ptr<SourceString<Char>>(new SourceString{ &m_context_id, m_script, m_encoding, m_includes });
	}

	// Regenerate the output from the source
	template <typename Char>
	ParseResult SourceString<Char>::ReadSource(Renderer& rdr)
	{
		m_errors.resize(0);
		m_filepaths.resize(0);
		m_includes.LocalDir("");

		mem_istream<Char> src{ m_script, 0 };
		TextReader reader(src, {}, m_encoding, { this, OnReportError }, { this, OnProgress }, m_includes);
		return Parse(rdr, reader, m_context_id);
	}

	template struct SourceString<char>;
	template struct SourceString<wchar_t>;
}
