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

	// Reasons that data in the store has changed
	enum class EDataChangedReason
	{
		None,

		// New objects have been added to the store
		NewData,

		// Data has been refreshed from the sources
		Reload,

		// Objects have been removed from the store
		Removal,
	};

	// The initiating reason for a notify event
	enum class ENotifyReason
	{
		// 'Load' was called, so new data is ready
		LoadComplete,

		// The source has disconnected
		Disconnected,
	};

	// Event args for the SourceBase Notify event
	struct NotifyEventArgs
	{
		// The initiating reason for this event
		ENotifyReason m_reason;

		// The trigger that initiated a Load call
		EDataChangedReason m_trigger;

		// Called after data has been added to the store
		AddCompleteCB m_add_complete;
	};

	// Base class for a source of LDraw objects
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

		// An event raised when something happens with this source (e.g, has new data, disconnected, etc)
		EventHandler<std::shared_ptr<SourceBase>, NotifyEventArgs const&, true> Notify;

		// Construct a new instance of the source (if possible)
		virtual std::shared_ptr<SourceBase> Clone();

		// Parse the contents of the script
		void Load(Renderer& rdr, EDataChangedReason trigger, AddCompleteCB add_complete_cb);

	protected:

		// Callbacks for parsing
		static void __stdcall OnReportError(void* ctx, EParseError err, Location const& loc, std::string_view msg);
		static bool __stdcall OnProgress(void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete);

		// Regenerate the output from the source
		virtual ParseResult ReadSource(Renderer& rdr);
	};
}

