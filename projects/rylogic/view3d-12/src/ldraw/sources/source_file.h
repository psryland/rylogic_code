//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "view3d-12/src/ldraw/sources/source_base.h"

namespace pr::rdr12::ldraw
{
	// An Ldr script source
	struct SourceFile : SourceBase
	{
		filepath_t   m_filepath;  // The filepath of the source (if there is one)
		PathResolver m_includes;  // Include paths to use with this file
		EEncoding    m_encoding;  // The file encoding

		SourceFile(Guid const* context_id, filepath_t const& filepath, EEncoding enc, PathResolver const& includes);

		// Construct a new instance of the source (if possible)
		std::unique_ptr<SourceBase> Clone() override;

	protected:

		// Regenerate the output from the source
		ParseResult ReadSource(Renderer& rdr) override;
	};
}
