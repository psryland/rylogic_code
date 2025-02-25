//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// A container of Ldr script sources that can watch for external changes.
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	// A collection of LDraw script sources
	struct ScriptSources :IFileChangedHandler
	{
		// Notes:
		//  - A collection of sources of ldr objects.
		//  - Typically ldr sources are files, but string sources are also supported.
		//  - This class maintains a map from context ids to a collection of files/strings.
		//  - The 'Additional' flag is no longer supported. File scripts each have a
		//    unique context id. When reloaded, objects previously associated with that
		//    file context id are removed. String scripts have a user provided id. String
		//    scripts are not reloaded because they shouldn't change externally. Callers
		//    should manage the removal of objects associated with string script sources.
		//  - This class manages the file watching/reload mechanism because when an included
		//    file changes, and reload of the root file is needed, even if unchanged.
		//  - If a file in a context id set has changed, an event is raised allowing the
		//    change to be ignored. The event args contain the context id and list of
		//    associated files.

		using GuidCont = pr::vector<Guid>;
		using GuidSet = std::unordered_set<Guid, std::hash<Guid>>;
		using OnAddCB = std::function<void(Guid const&, bool)>;
		using filepath_t = std::filesystem::path;
		using Includes = pr::script::Includes;
		using EmbeddedCodeFactory = pr::script::EmbeddedCodeFactory;

		// Reasons for changes to the sources collection
		enum class EReason
		{
			// AddScript/AddFile has been called
			NewData,

			// Data has been refreshed from the sources
			Reload,

			// Objects have been removed
			Removal,
		};

		// An Ldr script source
		struct Source
		{
			ObjectCont m_objects;    // Objects created by this source
			Guid       m_context_id; // Id for the group of files that this object is part of
			filepath_t m_filepath;   // The filepath of the source (if there is one)
			EEncoding  m_encoding;   // The file encoding
			Includes   m_includes;   // Include paths to use with this file
			Camera     m_cam;        // Camera properties associated with this source
			ECamField  m_cam_fields; // Bitmask of fields in 'm_cam' that are valid

			Source();
			Source(Guid const& context_id);
			Source(Guid const& context_id, filepath_t const& filepath, EEncoding enc, Includes const& includes);
			Source(Source&&) = default;
			Source(Source const&) = default;
			Source& operator=(Source&&) = default;
			Source& operator=(Source const&) = default;
			bool IsFile() const;
		};

		// Progress update event args
		struct AddFileProgressEventArgs :CancelEventArgs
		{
			// The context id for the file group
			Guid m_context_id;

			// The parse result that objects are being added to
			ParseResult const* m_result;

			// The current location in the source
			Location m_loc;

			// True if parsing is complete (i.e. last update notification)
			bool m_complete;

			AddFileProgressEventArgs(Guid const& context_id, ParseResult const& result, Location const& loc, bool complete)
				:m_context_id(context_id)
				,m_result(&result)
				,m_loc(loc)
				,m_complete(complete)
			{}
		};

		// Parse error event args
		struct ParseErrorEventArgs
		{
			// Error message
			std::wstring m_msg;

			// Script error code
			script::EResult m_result;

			// The filepath of the source that contains the error (if there is one)
			script::Loc m_loc;

			ParseErrorEventArgs()
				:ParseErrorEventArgs(L"", script::EResult::Success, {})
			{}
			ParseErrorEventArgs(std::wstring_view msg, script::EResult result, script::Loc const& loc)
				:m_msg(msg)
				,m_result(result)
				,m_loc(loc)
			{}
			explicit ParseErrorEventArgs(script::ScriptException const& ex)
				:ParseErrorEventArgs(pr::Widen(ex.what()), ex.m_result, ex.m_loc)
			{}
		};

		// Store change event args
		struct StoreChangeEventArgs
		{
			// The origin of the object container change
			EReason m_reason;

			// The context ids that changed
			std::span<Guid const> m_context_ids;

			// Contains the results of parsing including the object container that the objects where added to
			ParseResult const* m_result;

			// True if this event is just prior to the changes being made to the store
			bool m_before;

			StoreChangeEventArgs(EReason why, std::span<Guid const> context_ids, ParseResult const* result, bool before)
				:m_reason(why)
				,m_context_ids(context_ids)
				,m_result(result)
				,m_before(before)
			{}
		};

		// Source (context id) removed event args
		struct SourceRemovedEventArgs
		{
			// The Guid of the source to be removed
			Guid m_context_id;

			// The origin of the object container change
			EReason m_reason;

			SourceRemovedEventArgs(Guid context_id, EReason reason)
				:m_context_id(context_id)
				,m_reason(reason)
			{}
		};

		// A container that doesn't invalidate on add/remove is needed because
		// the file watcher contains a pointer into the 'Source' objects.
		using SourceCont = std::unordered_map<Guid, Source>;

		// Container of errors
		using ErrorCont = pr::vector<ParseErrorEventArgs>;

		// Container of file paths
		using PathsCont = pr::vector<filepath_t>;

	private:

		SourceCont          m_srcs;           // The sources of ldr script
		GizmoCont           m_gizmos;         // The created ldr gizmos
		Renderer*           m_rdr;            // Renderer used to create models
		EmbeddedCodeFactory m_emb_factory;    // Embedded code handler factory
		GuidSet             m_loading;        // File group ids in the process of being reloaded
		FileWatch           m_watcher;        // The watcher of files
		std::thread::id     m_main_thread_id; // The main thread id

	public:

		ScriptSources(Renderer& rdr, EmbeddedCodeFactory emb_factory);

		// Renderer access
		Renderer& rdr() const;

		// The ldr script sources
		SourceCont const& Sources() const;

		// The store of gizmos
		GizmoCont const& Gizmos() const;

		// Parse error event.
		EventHandler<ScriptSources&, ParseErrorEventArgs const&, true> OnError;

		// Reload event. Note: Don't AddFile() or RefreshChangedFiles() during this event.
		EventHandler<ScriptSources&, EmptyArgs const&, true> OnReload;

		// An event raised during parsing of files. This is called in the context of the threads that call 'AddFile'. Do not sign up while AddFile calls are running.
		EventHandler<ScriptSources&, AddFileProgressEventArgs&, true> OnAddFileProgress;

		// Store change event. Called before and after a change to the collection of objects in the store.
		EventHandler<ScriptSources&, StoreChangeEventArgs&, true> OnStoreChange;

		// Source removed event (i.e. objects deleted by Id)
		EventHandler<ScriptSources&, SourceRemovedEventArgs const&, true> OnSourceRemoved;

		// Remove all objects and sources
		void ClearAll();

		// Remove all file sources
		void ClearFiles();

		// Remove a single object from the object container
		void Remove(LdrObject* object, EReason reason = EReason::Removal);

		// Remove all objects associated with 'context_ids'
		void Remove(Guid const* context_ids, int include_count, int exclude_count, EReason reason = EReason::Removal);
		void Remove(Guid const& context_id, EReason reason = EReason::Removal);

		// Remove a file source
		void RemoveFile(filepath_t const& filepath, EReason reason = EReason::Removal);

		// Reload all files
		void ReloadFiles();

		// Check all file sources for modifications and reload any that have changed
		void RefreshChangedFiles();

		// Add an object created externally
		void Add(LdrObjectPtr object, EReason reason = EReason::NewData);

		// Parse file containing ldr script.
		// This function can be called from any thread and may be called concurrently by multiple threads.
		// Returns the GUID of the context that the objects were added to.
		Guid AddFile(std::filesystem::path script, EEncoding enc, EReason reason, std::optional<Guid> context_id, Includes const& includes, OnAddCB on_add);

		// Parse a string containing ldr script.
		// This function can be called from any thread and may be called concurrently by multiple threads.
		// Returns the GUID of the context that the objects were added to.
		template <typename Char>
		Guid AddString(std::basic_string_view<Char> script, EEncoding enc, EReason reason, std::optional<Guid> context_id, Includes const& includes, OnAddCB on_add);

		// Create a gizmo object and add it to the gizmo collection
		LdrGizmo* CreateGizmo(EGizmoMode mode, m4x4 const& o2w);

		// Destroy a gizmo
		void RemoveGizmo(LdrGizmo* gizmo);

		// Return the file group id for objects created from 'filepath' (if filepath is an existing source)
		Guid const* ContextIdFromFilepath(filepath_t const& filepath) const;

	private:
		
		// Merge parsed objects into the pool of sources
		void MergeResults(Source&& source, ParseResult&& out, PathsCont&& filepaths, ErrorCont&& errors, Guid context, EReason reason, OnAddCB on_add) noexcept;

		// 'filepath' is the name of the changed file
		void FileWatch_OnFileChanged(wchar_t const*, Guid const& context_id, void*, bool&);
	};
}
