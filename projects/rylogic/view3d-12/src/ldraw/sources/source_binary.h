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
	struct SourceBinary : SourceBase
	{
		byte_data<> m_script;

		SourceBinary(Guid const* context_id, std::span<std::byte const> data);

		// Construct a new instance of the source (if possible)
		std::unique_ptr<SourceBase> Clone() override;

	protected:

		// Regenerate the output from the source
		ParseResult ReadSource(Renderer& rdr) override;
	};
}
