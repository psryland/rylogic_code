//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/ldraw/sources/source_binary.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"

namespace pr::rdr12::ldraw
{
	SourceBinary::SourceBinary(Guid const* context_id, std::span<std::byte const> data)
		: SourceBase(context_id)
		, m_script(data)
	{
	}

	// Construct a new instance of the source (if possible)
	std::unique_ptr<SourceBase> SourceBinary::Clone()
	{
		return std::unique_ptr<SourceBinary>(new SourceBinary{ &m_context_id, m_script });
	}

	// Regenerate the output from the source
	ParseResult SourceBinary::ReadSource(Renderer& rdr)
	{
		mem_istream<char> src{ m_script.data(), m_script.size() };
		BinaryReader reader(src, {}, { OnReportError, this }, { OnProgress, this });
		return Parse(rdr, reader, m_context_id);
	}
}
