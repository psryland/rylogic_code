//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	// An Ldr script SourceBase
	struct SourceBase
	{
		using filepath_t = std::filesystem::path;
		using PathsCont = pr::vector<filepath_t>;
		using ErrorCont = pr::vector<ParseErrorEventArgs>;

		ParseResult m_output;     // Objects created by this source
		Guid        m_context_id; // Id for the group of files that this object is part of
		PathsCont   m_filepaths;  // Dependent files of this source
		ErrorCont   m_errors;

		SourceBase(Guid const* context_id);
		SourceBase(SourceBase&&) = default;
		SourceBase(SourceBase const&) = default;
		SourceBase& operator=(SourceBase&&) = default;
		SourceBase& operator=(SourceBase const&) = default;

		// An event raised during parsing of files. This is called in the context of the threads that call 'AddFile'. Do not sign up while AddFile calls are running.
		EventHandler<SourceBase&, ParsingProgressEventArgs&, true> ParsingProgress;

		// Construct a new instance of the source (if possible)
		virtual std::unique_ptr<SourceBase> Clone();

		// Parse the contents of the script
		void Reload(Renderer& rdr);

	protected:

		// Callbacks for parsing
		static void __stdcall OnReportError(void* ctx, EParseError err, Location const& loc, std::string_view msg);
		static bool __stdcall OnProgress(void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete);

		// Regenerate the output from the source
		virtual ParseResult ReadSource(Renderer& rdr);
	};
}

