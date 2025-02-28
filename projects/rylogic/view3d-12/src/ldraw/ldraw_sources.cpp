//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/ldraw/ldraw_sources.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"
#include "pr/view3d-12/ldraw/ldraw_gizmo.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12::ldraw
{
	// Sources *************************************
	
	ScriptSources::Source::Source()
		:Source(GuidZero, "", EEncoding::auto_detect, PathResolver::Instance())
	{}
	ScriptSources::Source::Source(Guid const& context_id)
		:Source(context_id, "", EEncoding::auto_detect, PathResolver::Instance())
	{}
	ScriptSources::Source::Source(Guid const& context_id, filepath_t const& filepath, EEncoding enc, PathResolver const& includes)
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
	bool ScriptSources::Source::IsFile() const
	{
		return !m_filepath.empty();
	}

	// ScriptSources *******************************

	ScriptSources::ScriptSources(Renderer& rdr)
		: m_srcs()
		, m_gizmos()
		, m_rdr(&rdr)
		, m_loading()
		, m_watcher()
		, m_main_thread_id(std::this_thread::get_id())
	{
		// Handle notification of changed files from the watcher.
		// 'OnFilesChanged' is raised before any of the 'FileWatch_OnFileChanged'
		// callbacks are made. So this notifies of the reload before anything starts changing.
		m_watcher.OnFilesChanged += [&](FileWatch&, FileWatch::FileCont&)
		{
			OnReload(*this, EmptyArgs());
		};
	}

	// Renderer access
	Renderer& ScriptSources::rdr() const
	{
		return *m_rdr;
	}

	// The ldr script sources
	ScriptSources::SourceCont const& ScriptSources::Sources() const
	{
		return m_srcs;
	}

	// The store of gizmos
	GizmoCont const& ScriptSources::Gizmos() const
	{
		return m_gizmos;
	}

	// Remove all objects and sources
	void ScriptSources::ClearAll()
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Make a copy of the GUIDs removed
		GuidCont guids;
		for (auto& src : m_srcs)
			guids.push_back(src.first);

		m_srcs.clear();
		m_gizmos.clear();
		m_watcher.RemoveAll();

		// Notify of the object container change
		StoreChangeEventArgs args(EReason::Removal, guids, nullptr, false);
		OnStoreChange(*this, args);
	}

	// Remove all file sources
	void ScriptSources::ClearFiles()
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Notify of the delete of each file source
		GuidCont guids;
		for (auto& src : m_srcs)
		{
			if (!src.second.IsFile()) continue;
			OnSourceRemoved(*this, SourceRemovedEventArgs(src.first, EReason::Removal));
			guids.push_back(src.first);
		}

		// Remove all file sources
		for (auto& id : guids)
			m_srcs.erase(id);

		// Remove watcher references
		m_watcher.RemoveAll();

		// Notify of the object container change
		StoreChangeEventArgs args(EReason::Removal, guids, nullptr, false);
		OnStoreChange(*this, args);
	}

	// Remove a single object from the object container
	void ScriptSources::Remove(LdrObject* object, EReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto id = object->m_context_id;

		// Remove the object from the source it belongs to
		auto& src = m_srcs[id];
		auto count = src.m_objects.size();
		ldraw::Remove(src.m_objects, object);

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
	void ScriptSources::Remove(Guid const* context_ids, int include_count, int exclude_count, EReason reason)
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
	void ScriptSources::Remove(Guid const& context_id, EReason reason)
	{
		Remove(&context_id, 1, 0, reason);
	}

	// Remove a file source
	void ScriptSources::RemoveFile(filepath_t const& filepath, EReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Remove the objects created 'filepath'
		auto context_id = ContextIdFromFilepath(filepath);
		if (context_id != nullptr)
			Remove(*context_id, reason);
	}

	// Reload all files
	void ScriptSources::ReloadFiles()
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
			Source file = src.second;
			std::thread([=]
			{
				AddFile(file.m_filepath, file.m_encoding, EReason::Reload, file.m_context_id, file.m_includes, [&](Guid const& id, bool before) noexcept
				{
					if (!before) return;
					Remove(id, EReason::Reload);
				});
			}).detach();
		}
	}

	// Check all file sources for modifications and reload any that have changed
	void ScriptSources::RefreshChangedFiles()
	{
		m_watcher.CheckForChangedFiles();
	}

	// Add an object created externally
	void ScriptSources::Add(LdrObjectPtr object, EReason reason)
	{
		auto context_id = object->m_context_id;

		// Add the object to the collection
		m_srcs[context_id].m_context_id = context_id;
		m_srcs[context_id].m_objects.push_back(object);

		// Notify of the object container change
		StoreChangeEventArgs args(reason, std::initializer_list<Guid const>(&context_id, &context_id + 1), nullptr, false);
		OnStoreChange(*this, args);
	}

	// Parse file containing ldr script.
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	Guid ScriptSources::AddFile(std::filesystem::path script, EEncoding enc, EReason reason, std::optional<Guid> context_id, PathResolver const& includes, OnAddCB on_add) // worker thread context
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.

		// Create a source object use for refreshing objects from the source
		auto context = context_id ? *context_id : GenerateGUID();
		auto filepath = filepath_t(script).lexically_normal();
		auto extn = filepath.extension().string();

		// A script source
		Source source(context, filepath, enc, includes);

		// Monitor the files that get included so we can watch them for changes
		PathsCont filepaths;
		source.m_includes.FileOpened = [&](auto&, filepath_t const& fp)
		{
			// Add the directory of the included file to the paths
			source.m_includes.LocalDir(fp.parent_path());
			filepaths.push_back(fp.lexically_normal());
		};
		source.m_includes.FileOpened(source.m_includes, filepath);

		// Parse the contents of the script
		ParseResult out;
		ErrorCont errors;
		#pragma region Parse
		try
		{
			// Callback functions for 'Parse'
			static StaticCB<void, EParseError, Location const&, std::string_view> ReportErrorCB = {
				[](void* ctx, EParseError err, Location const& loc, std::string_view msg) -> void
				{
					auto& errors = *static_cast<ErrorCont*>(ctx);
					ParseErrorEventArgs args(msg, err, loc);
					errors.push_back(std::move(args));
				}, &errors };
			static StaticCB<bool, Guid const&, ParseResult const&, Location const&, bool> AddFileProgressCB = {
				[](void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete) -> bool
				{
					auto& ss = *static_cast<ScriptSources*>(ctx);
					AddFileProgressEventArgs args(context_id, out, loc, complete);
					ss.OnAddFileProgress(ss, args);
					return !args.m_cancel;
				}, this };

			// Handle based on file extension
			switch (HashI(extn.c_str()))
			{
				case HashI(".ldr"):
				{
					// LDR = Text ldr script file
					filesys::LockFile lock(filepath, 10, 5000);

					// Parse the ldr script file
					std::ifstream src(filepath);
					ldraw::TextReader reader(src, enc, filepath, ReportErrorCB, AddFileProgressCB, includes);
					out = Parse(rdr(), reader, context);
					break;
				}
				case HashI(".bdr"):
				{
					// BDR = Binary ldr script file
					filesys::LockFile lock(filepath, 10, 5000);

					// Parse the ldr script file
					std::ifstream src(filepath, std::ios::binary);
					ldraw::BinaryReader reader(src, filepath, ReportErrorCB, AddFileProgressCB, includes);
					out = Parse(rdr(), reader, context);
					break;
				}
				case HashI(".p3d"):
				case HashI(".stl"):
				case HashI(".3ds"):
				{
					// P3D = My custom binary model file format
					// STL = "StereoLithography" model files (binary and text)
					std::istringstream src(std::format("*Model {{\"{}\"}}", filepath.string()));
					ldraw::TextReader reader(src, EEncoding::utf8, {}, ReportErrorCB, AddFileProgressCB, includes);
					out = Parse(rdr(), reader, context);
					break;
				}
				case HashI(".csv"):
				{
					// CSV data, create a chart to graph the data
					std::istringstream src(std::format("*Chart {{3 #include \"{}\"}}", filepath.string()));
					ldraw::TextReader reader(src, EEncoding::utf8, {}, ReportErrorCB, AddFileProgressCB, includes);
					out = Parse(rdr(), reader, context);
					break;
				}
				default:
				{
					// Unknown file type
					throw std::runtime_error(std::format("Unknown file type: {}", extn));
				}
			}
		}
		catch (std::exception const& ex)
		{
			ParseErrorEventArgs args(ex.what(), ldraw::EParseError::UnknownError, {});
			errors.push_back(std::move(args));
		}
		#pragma endregion

		// Marshal to the main thread if this is a worker thread context
		MergeResults(std::move(source), std::move(out), std::move(filepaths), std::move(errors), context, reason, on_add);
		return context;
	}

	// Parse a string containing ldr script.
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	template <typename Char>
	Guid ScriptSources::AddString(std::basic_string_view<Char> script, EEncoding enc, EReason reason, std::optional<Guid> context_id, PathResolver const& includes, OnAddCB on_add) // worker thread context
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		using namespace pr::script;

		// Create a source object
		auto context = context_id ? *context_id : GenerateGUID();
		Source source(context, {}, enc, includes);

		// Monitor the files that get included so we can watch them for changes
		PathsCont filepaths;
		source.m_includes.FileOpened = [&](auto&, filepath_t const& fp)
		{
			// Add the directory of the included file to the paths
			source.m_includes.AddSearchPath(fp.parent_path());
			filepaths.push_back(fp.lexically_normal());
		};

		// Callback function for 'Parse'
		auto AddFileProgressCB = StaticCB<bool, Guid const&, ParseResult const&, Location const&, bool>(
			[](void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete) -> bool
			{
				auto& ss = *static_cast<ScriptSources*>(ctx);
				AddFileProgressEventArgs args(context_id, out, loc, complete);
				ss.OnAddFileProgress(ss, args);
				return !args.m_cancel;
			}, this);

		// Parse the contents of the script
		ParseResult out;
		ErrorCont errors;
		#pragma region Parse
		try
		{
			StringSrc src(script, StringSrc::EFlags::None, enc);
			Reader reader(src, false, &source.m_includes, m_emb_factory);
			out = Parse(rdr(), reader, context, AddFileProgressCB);
		}
		catch (ScriptException const& ex)
		{
			ParseErrorEventArgs args(ex);
			errors.push_back(std::move(args));
		}
		catch (std::exception const& ex)
		{
			ParseErrorEventArgs args(Widen(ex.what()), script::EResult::Failed, Loc());
			errors.push_back(std::move(args));
		}
		#pragma endregion

		// Marshal to the main thread if this is a worker thread context
		MergeResults(std::move(source), std::move(out), std::move(filepaths), std::move(errors), context, reason, on_add);
		return context;
	}
	template Guid ScriptSources::AddString<wchar_t>(std::wstring_view script, EEncoding enc, EReason reason, std::optional<Guid> context_id, PathResolver const& includes, OnAddCB on_add);
	template Guid ScriptSources::AddString<char>(std::string_view script, EEncoding enc, EReason reason, std::optional<Guid> context_id, PathResolver const& includes, OnAddCB on_add);

	// Merge the results
	void ScriptSources::MergeResults(Source&& source, ParseResult&& out, PathsCont&& filepaths, ErrorCont&& errors, Guid context, EReason reason, OnAddCB on_add) noexcept // main thread context
	{
		// Marshal to the main thread if this is a worker thread context
		if (std::this_thread::get_id() != m_main_thread_id)
		{
			// There is no way to declare std::function as 'noexcept', so as a workaround, make it a captured variable.
			auto merge = [this, on_add](Source&& source, ParseResult&& out, PathsCont&& filepaths, ErrorCont&& errors, Guid context, EReason reason) mutable noexcept
			{
				try { MergeResults(std::move(source), std::move(out), std::move(filepaths), std::move(errors), context, reason, on_add); }
				catch (...) { assert(false); }
			};
			rdr().RunOnMainThread(merge, std::move(source), std::move(out), std::move(filepaths), std::move(errors), context, reason);
			return;
		}

		// Don't remove previous objects associated with 'context', 
		// leave that to the caller via the 'on_add' callback.
		// Remove from the file watcher's 'loading' set
		m_loading.erase(context);

		// Notify of the store about to change
		StoreChangeEventArgs args(reason, { &context, 1 }, &out, true);
		OnStoreChange(*this, args);
		if (on_add)
			on_add(context, true);

		// Update the store
		auto& src = m_srcs[context];
		src.m_context_id = context;
		src.m_objects.insert(std::end(src.m_objects), std::begin(out.m_objects), std::end(out.m_objects));
		src.m_filepath = source.m_filepath;
		src.m_includes = std::move(source.m_includes);
		src.m_cam = out.m_cam;
		src.m_cam_fields = out.m_cam_fields;

		// Add the file and anything it included to the file watcher
		if (source.IsFile())
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

	// Create a gizmo object and add it to the gizmo collection
	LdrGizmo* ScriptSources::CreateGizmo(EGizmoMode mode, m4x4 const& o2w)
	{
		LdrGizmoPtr giz(new LdrGizmo(rdr(), mode, o2w), true);
		m_gizmos.push_back(giz);
		return giz.m_ptr;
	}

	// Destroy a gizmo
	void ScriptSources::RemoveGizmo(LdrGizmo* gizmo)
	{
		// Delete the gizmo from the gizmo container (removing the last reference)
		erase_first(m_gizmos, [&](LdrGizmoPtr const& p){ return p.m_ptr == gizmo; });
	}

	// Return the file group id for objects created from 'filepath' (if filepath is an existing source)
	Guid const* ScriptSources::ContextIdFromFilepath(filepath_t const& filepath) const
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Find the corresponding source in the sources collection
		auto fpath = filepath.lexically_normal();
		auto iter = find_if(m_srcs, [=](auto& src){ return filesys::Equal(fpath, src.second.m_filepath, true); });
		return iter != std::end(m_srcs) ? &iter->second.m_context_id : nullptr;
	}

	// 'filepath' is the name of the changed file
	void ScriptSources::FileWatch_OnFileChanged(wchar_t const*, Guid const& context_id, void*, bool&)
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
		Source file = iter->second;

		// Reload that file group (asynchronously)
		std::thread([=]
		{
			// Note: if loading a file fails, don't use 'MarkAsChanged' to trigger another load
			// attempt. Doing so results in an infinite loop trying to load a broken file.
			AddFile(file.m_filepath, file.m_encoding, EReason::Reload, file.m_context_id, file.m_includes, [=](Guid const& id, bool before)
			{
				if (!before) return;
				Remove(id);
			});
		}).detach();
	}
}
