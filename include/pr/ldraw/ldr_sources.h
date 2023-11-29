//*****************************************************************************************
// LDraw
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
// A container of Ldr script sources that can watch for external change.
#pragma once
#include <string>
#include <string_view>
#include <sstream>
#include <unordered_map>
#include <unordered_set>
#include <filesystem>
#include <mutex>
#include "pr/common/guid.h"
#include "pr/container/vector.h"
#include "pr/script/forward.h"
#include "pr/filesys/filewatch.h"
#include "pr/ldraw/ldr_object.h"
#include "pr/ldraw/ldr_gizmo.h"
#include "pr/threads/synchronise.h"

namespace pr::ldr
{
	// A collection of LDraw script sources
	class ScriptSources :pr::IFileChangedHandler
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


	public:

		using GuidCont = pr::vector<Guid>;
		using GuidSet = std::unordered_set<Guid, std::hash<Guid>>;
		using OnAddCB = std::function<void(Guid const&, bool)>;
		using Location = pr::script::Loc;
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
			ObjectCont            m_objects;    // Objects created by this source
			Guid                  m_context_id; // Id for the group of files that this object is part of
			std::filesystem::path m_filepath;   // The filepath of the source (if there is one)
			EEncoding             m_encoding;   // The file encoding
			script::Includes      m_includes;   // Include paths to use with this file
			Camera                m_cam;        // Camera properties associated with this source
			ECamField             m_cam_fields; // Bitmask of fields in 'm_cam' that are valid

			Source()
				:m_context_id(GuidZero)
				,m_filepath()
				,m_encoding(EEncoding::auto_detect)
				,m_includes()
				,m_cam()
				,m_cam_fields(ECamField::None)
			{}
			Source(Guid const& context_id)
				:m_context_id(context_id)
				,m_filepath()
				,m_encoding(EEncoding::auto_detect)
				,m_includes()
				,m_cam()
				,m_cam_fields(ECamField::None)
			{}
			Source(Guid const& context_id, std::filesystem::path const&  filepath, EEncoding enc, script::Includes const& includes)
				:m_context_id(context_id)
				,m_filepath(filepath.lexically_normal())
				,m_encoding(enc)
				,m_includes(includes)
				,m_cam()
				,m_cam_fields(ECamField::None)
			{
				if (!m_filepath.empty())
					m_includes.AddSearchPath(m_filepath.parent_path());
			}
			bool IsFile() const
			{
				return !m_filepath.empty();
			}
		};

		// A container that doesn't invalidate on add/remove is needed because
		// the file watcher contains a pointer into the 'Source' objects.
		using SourceCont = std::unordered_map<Guid, Source>;

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
				:m_msg()
				,m_result(script::EResult::Success)
				,m_loc()
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

			StoreChangeEventArgs(EReason why, std::initializer_list<Guid const> context_ids, ParseResult const* result, bool before)
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

	private:

		SourceCont          m_srcs;           // The sources of ldr script
		GizmoCont           m_gizmos;         // The created ldr gizmos
		Renderer*           m_rdr;            // Renderer used to create models
		EmbeddedCodeFactory m_emb_factory;    // Embedded code handler factory
		GuidSet             m_loading;        // File group ids in the process of being reloaded
		FileWatch           m_watcher;        // The watcher of files
		std::thread::id     m_main_thread_id; // The main thread id

	public:

		ScriptSources(Renderer& rdr, EmbeddedCodeFactory emb_factory)
			:m_srcs()
			,m_gizmos()
			,m_rdr(&rdr)
			,m_emb_factory(emb_factory)
			,m_loading()
			,m_watcher()
			,m_main_thread_id(std::this_thread::get_id())
		{
			// Handle notification of changed files from the watcher.
			// 'OnFilesChanged' is raised before any of the 'FileWatch_OnFileChanged'
			// callbacks are made. So this notifies of the reload before anything starts changing.
			m_watcher.OnFilesChanged += [&](FileWatch&, FileWatch::FileCont&)
			{
				OnReload(*this, EmptyArgs());
			};
		}

		// The ldr script sources
		SourceCont const& Sources() const
		{
			return m_srcs;
		}

		// The store of gizmos
		GizmoCont const& Gizmos() const
		{
			return m_gizmos;
		}

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
		void ClearAll()
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
				
			GuidCont guids;
			for (auto& src : m_srcs)
				guids.push_back(src.first);

			m_gizmos.clear();
			m_srcs.clear();
			m_watcher.RemoveAll();

			// Notify of the object container change
			StoreChangeEventArgs args(EReason::Removal, guids, nullptr, false);
			OnStoreChange(*this, args);
		}

		// Remove all file sources
		void ClearFiles()
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			GuidCont guids;

			// Notify of the delete of each file source
			for (auto& src : m_srcs)
			{
				if (!src.second.IsFile()) continue;
				OnSourceRemoved(*this, SourceRemovedEventArgs(src.first, EReason::Removal));
				guids.push_back(src.first);
			}

			// Remove all file sources and watcher references
			for (auto& id : guids) m_srcs.erase(id);
			m_watcher.RemoveAll();

			// Notify of the object container change
			StoreChangeEventArgs args(EReason::Removal, guids, nullptr, false);
			OnStoreChange(*this, args);
		}

		// Remove a single object from the object container
		void Remove(LdrObject* object, EReason reason = EReason::Removal)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			auto id = object->m_context_id;

			// Remove the object from the source it belongs to
			auto& src = m_srcs[id];
			auto count = src.m_objects.size();
			ldr::Remove(src.m_objects, object);

			// Notify of the object container change
			if (src.m_objects.size() != count)
			{
				StoreChangeEventArgs args(reason, std::initializer_list<Guid const>(&id, &id + 1), nullptr, false);
				OnStoreChange(*this, args);
			}

			// If that was the last object for the source, remove the source too
			if (src.m_objects.empty())
				Remove(id);
		}

		// Remove all objects associated with 'context_ids'
		void Remove(Guid const* context_ids, int include_count, int exclude_count, EReason reason = EReason::Removal)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);

			// Build the set of ids to remove
			GuidCont removed;
			for (auto& src : m_srcs)
			{
				auto id = src.second.m_context_id;
				if (!IncludeFilter(id, context_ids, include_count, exclude_count)) continue;
				removed.push_back(id);
			}

			for (auto& id : removed)
			{
				// Notify of objects about to be deleted
				OnSourceRemoved(*this, SourceRemovedEventArgs(id, reason));

				// Delete any associated files and watches
				m_watcher.RemoveAll(id);

				// Delete the source and its associated objects
				m_srcs.erase(id);
			}

			// Notify of the object container change
			if (!removed.empty())
			{
				StoreChangeEventArgs args(reason, removed, nullptr, false);
				OnStoreChange(*this, args);
			}
		}
		void Remove(Guid const& context_id, EReason reason = EReason::Removal)
		{
			Remove(&context_id, 1, 0, reason);
		}

		// Remove a file source
		void RemoveFile(std::filesystem::path const& filepath, EReason reason = EReason::Removal)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);

			// Remove the objects created 'filepath'
			auto context_id = ContextIdFromFilepath(filepath);
			if (context_id != nullptr)
				Remove(*context_id, reason);
		}

		// Reload all files
		void ReloadFiles()
		{
			assert(std::this_thread::get_id() == m_main_thread_id);

			// Notify reloading
			OnReload(*this, EmptyArgs());

			// Make a copy of the sources container
			auto srcs = m_srcs;

			// Add each file again (asynchronously)
			for (auto const& src : srcs)
			{
				// Don't re-add non file sources, since they can't change.
				if (!src.second.IsFile())
					continue;

				// Skip files that are in the process of loading
				if (m_loading.find(src.second.m_context_id) != std::end(m_loading)) continue;
				m_loading.insert(src.second.m_context_id);

				// Fire off a worker thread to reload the file.
				auto const& file = src.second;
				std::thread([=]
				{
					Add<wchar_t>(file.m_filepath.wstring(), true, file.m_encoding, EReason::Reload, &file.m_context_id, file.m_includes, [&](Guid const& id, bool before)
					{
						if (!before) return;
						Remove(id, EReason::Reload);
					});
				}).detach();
			}
		}

		// Check all file sources for modifications and reload any that have changed
		void RefreshChangedFiles()
		{
			m_watcher.CheckForChangedFiles();
		}

		// Add an object created externally
		void Add(LdrObjectPtr object, EReason reason = EReason::NewData)
		{
			auto context_id = object->m_context_id;

			// Add the object to the collection
			m_srcs[context_id].m_context_id = context_id;
			m_srcs[context_id].m_objects.push_back(object);

			// Notify of the object container change
			StoreChangeEventArgs args(reason, std::initializer_list<Guid const>(&context_id, &context_id + 1), nullptr, false);
			OnStoreChange(*this, args);
		}

		// Parse a string or file containing ldr script.
		// This function can be called from any thread and may be called concurrently by multiple threads.
		// Returns the GUID of the contxt that the objets were added to.
		Guid Add(std::string_view script, bool is_file, EEncoding enc, EReason reason, Guid const* context_id, script::Includes const& includes, OnAddCB on_add) // worker thread context
		{
			return Add<char>(script, is_file, enc, reason, context_id, includes, on_add);
		}
		Guid Add(std::wstring_view script, bool is_file, EEncoding enc, EReason reason, Guid const* context_id, script::Includes const& includes, OnAddCB on_add) // worker thread context
		{
			return Add<wchar_t>(script, is_file, enc, reason, context_id, includes, on_add);
		}

		// Create a gizmo object and add it to the gizmo collection
		LdrGizmo* CreateGizmo(LdrGizmo::EMode mode, m4x4 const& o2w)
		{
			auto giz = LdrGizmoPtr(new LdrGizmo(*m_rdr, mode, o2w), true);
			m_gizmos.push_back(giz);
			return giz.m_ptr;
		}

		// Destroy a gizmo
		void RemoveGizmo(LdrGizmo* gizmo)
		{
			// Delete the gizmo from the gizmo container (removing the last reference)
			erase_first(m_gizmos, [&](LdrGizmoPtr const& p){ return p.m_ptr == gizmo; });
		}

		// Return the file group id for objects created from 'filepath' (if filepath is an existing source)
		Guid const* ContextIdFromFilepath(std::filesystem::path const& filepath) const
		{
			assert(std::this_thread::get_id() == m_main_thread_id);

			// Find the corresponding source in the sources collection
			auto fpath = filepath.lexically_normal();
			auto iter = find_if(m_srcs, [=](auto& src){ return filesys::Equal(fpath, src.second.m_filepath, true); });
			return iter != std::end(m_srcs) ? &iter->second.m_context_id : nullptr;
		}

	private:

		// 'filepath' is the name of the changed file
		void FileWatch_OnFileChanged(wchar_t const*, Guid const& context_id, void*, bool&)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);

			// Look for the root file for group 'context_id'
			auto iter = find_if(m_srcs, [=](auto& src){ return src.second.m_context_id == context_id; });
			if (iter == std::end(m_srcs))
				return;

			// Skip files that are in the process of loading
			if (m_loading.find(iter->second.m_context_id) != std::end(m_loading)) return;
			m_loading.insert(iter->second.m_context_id);

			// Make a local copy
			auto file = iter->second;

			// Reload that file group (asynchronously)
			std::thread([=]
			{
				// Note: if loading a file fails, don't use 'MarkAsChanged' to trigger another load
				// attempt. Doing so results in an infinite loop trying to load a broken file.
				Add<wchar_t>(file.m_filepath.wstring(), true, file.m_encoding, EReason::Reload, &file.m_context_id, file.m_includes, [=](Guid const& id, bool before)
				{
					if (!before) return;
					Remove(id);
				});
			}).detach();
		}

		// Parse a string or file containing ldr script.
		// This function can be called from any thread and may be called concurrently by multiple threads.
		// Returns the GUID of the contxt that the objets were added to.
		template <typename Char>
		Guid Add(std::basic_string_view<Char> script, bool is_file, EEncoding enc, EReason reason, Guid const* context_id, script::Includes const& includes, OnAddCB on_add) // worker thread context
		{
			// Note: when called from a worker thread, this function returns after objects have
			// been created, but before they've been added to the main 'm_srcs' collection.
			// The 'on_add' callback function should be used as a continuation function.

			using namespace pr;
			using namespace pr::script;
			using namespace std::filesystem;

			// Create a source object
			auto context = context_id ? *context_id : GenerateGUID();
			auto filepath = is_file ? path(script).lexically_normal() : path();
			auto extn = is_file ? filepath.extension() : path();
			Source source(context, filepath, enc, includes);

			// Monitor the files that get included so we can watch them for changes
			vector<path> filepaths;
			source.m_includes.FileOpened = [&](auto&, path const& fp)
			{
				// Add the directory of the included file to the paths
				source.m_includes.AddSearchPath(fp.parent_path());
				filepaths.push_back(fp.lexically_normal());
			};
			if (source.IsFile())
			{
				source.m_includes.FileOpened(source.m_includes, filepath);
			}

			// Parse the contents of the script
			ParseResult out;
			vector<ParseErrorEventArgs> errors;
			#pragma region Parse
			try
			{
				// String script
				if (!is_file)
				{
					StringSrc src(script, StringSrc::EFlags::None, enc);
					Reader reader(src, false, &source.m_includes, m_emb_factory);
					Parse(*m_rdr, reader, out, context, StaticCallback(AddFileProgressCB, this));
				}
				else if (
					str::EqualI(extn.c_str(), ".p3d") ||
					str::EqualI(extn.c_str(), ".stl") ||
					str::EqualI(extn.c_str(), ".3ds"))
				{
					// P3D = My custom binary model file format
					// STL = "Stereolithography" model files (binary and text)
					StringSrc src(FmtS(L"*Model {\"%s\"}", filepath.c_str()), StringSrc::EFlags::BufferLocally);
					Reader reader(src, false, &source.m_includes, m_emb_factory);
					Parse(*m_rdr, reader, out, context, StaticCallback(AddFileProgressCB, this));
				}
				else if (str::EqualI(extn.c_str(), ".csv"))
				{
					// CSV data, create a chart to graph the data
					StringSrc src(FmtS(L"*Chart {3 #include \"%s\"}", filepath.c_str()), StringSrc::EFlags::BufferLocally);
					Reader reader(src, false, &source.m_includes, m_emb_factory);
					Parse(*m_rdr, reader, out, context, StaticCallback(AddFileProgressCB, this));
				}
				else if (str::EqualI(extn.c_str(), ".lua"))
				{
					// Lua script that generates ldr script
					//m_lua_src.Add(fpath.c_str());
				}
				else // Assume an ldr script file
				{
					// Use a lock file to synchronise access to 'filepath'
					filesys::LockFile lock(filepath, 10, 5000);

					// Parse the ldr script file
					FileSrc src(filepath, 0, enc);
					Reader reader(src, false, &source.m_includes, m_emb_factory);
					Parse(*m_rdr, reader, out, context, StaticCallback(AddFileProgressCB, this));
				}
			}
			catch (ScriptException const& ex)
			{
				errors.emplace_back(ex);
			}
			catch (std::exception const& ex)
			{
				errors.emplace_back(pr::Widen(ex.what()), EResult::Failed, Loc());
			}
			#pragma endregion

			// Merge the results
			auto merge = [=]() mutable noexcept // main thread context
			{
				// Don't remove previous objects associated with 'context', 
				// leave that to the caller via the 'on_add' callback.

				// Remove from the file watcher's 'loading' set
				m_loading.erase(context);

				// Notify of the store about to change
				StoreChangeEventArgs args(reason, std::initializer_list<Guid const>(&context, &context + 1), &out, true);
				OnStoreChange(*this, args);
				if (on_add)
					on_add(context, true);

				// Update the store
				auto& src = m_srcs[context];
				src.m_context_id = context;
				src.m_objects.insert(std::end(src.m_objects), std::begin(out.m_objects), std::end(out.m_objects));
				src.m_filepath = source.m_filepath;
				src.m_includes = source.m_includes;
				src.m_cam = out.m_cam;
				src.m_cam_fields = out.m_cam_fields;

				// Add the file and anything it included to the file watcher
				if (is_file)
				{
					for (auto& fp : filepaths)
						m_watcher.Add(fp.c_str(), this, context);
				}

				// Notify of any errors that occurred
				for (auto& err : errors)
					OnError(*this, err);

				// Notify of the store change
				args.m_before = false;
				OnStoreChange(*this, args);
				if (on_add)
					on_add(context, false);
			};

			// Marshal to the main thread if this is a worker thread context
			if (std::this_thread::get_id() != m_main_thread_id)
				m_rdr->RunOnMainThread(merge);
			else
				merge();

			return context;
		}

		// Callback function for 'Parse'
		static bool __stdcall AddFileProgressCB(void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete)
		{
			auto This = static_cast<ScriptSources*>(ctx);
			AddFileProgressEventArgs args(context_id, out, loc, complete);
			This->OnAddFileProgress(*This, args);
			return !args.m_cancel;
		}
	};
}
