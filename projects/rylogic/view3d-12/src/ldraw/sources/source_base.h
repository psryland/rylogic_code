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
	// Callback after data has been added to the store
	using AddCompleteCB = std::function<void(Guid const&, bool)>;

	// The initiating reason for a new data event
	enum class EDataChangeReason
	{
		// New objects have been added
		NewData,

		// Data has been refreshed from the source
		Reload,

		// Objects have been removed
		Removal,
	};
	
	// Event args for the SourceBase NewData event
	struct NewDataEventArgs
	{
		// The initiating reason for this event
		EDataChangeReason m_reason;

		// Called after data has been added to the store
		AddCompleteCB m_add_complete;
	};

	// An Ldr script SourceBase
	struct SourceBase : std::enable_shared_from_this<SourceBase>
	{
		// Notes:
		//  - Sources are containers of LdrObjects associated with a GUID context id.
		//  - Sources do their parsing an a background thread, returning a new 'ParseResult' object.
		//  - Sources fire the 'NewData' event when new data is ready (e.g. after a Reload)

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

		// An event raised during parsing.
		EventHandler<SourceBase&, ParsingProgressEventArgs&, true> ParsingProgress;

		// An event raised when this source has new data to add
		EventHandler<std::shared_ptr<SourceBase>, NewDataEventArgs const&, true> NewData;

		// Construct a new instance of the source (if possible)
		virtual std::shared_ptr<SourceBase> Clone();

		// Parse the contents of the script
		void Load(Renderer& rdr, NewDataEventArgs args);

	protected:

		// Callbacks for parsing
		static void __stdcall OnReportError(void* ctx, EParseError err, Location const& loc, std::string_view msg);
		static bool __stdcall OnProgress(void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete);

		// Regenerate the output from the source
		virtual ParseResult ReadSource(Renderer& rdr);
	};
}

