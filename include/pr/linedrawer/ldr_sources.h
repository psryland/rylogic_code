//*****************************************************************************************
// LDraw
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

// A container of Ldr script sources that can watch for external change.

#include <string>
#include <sstream>
#include <unordered_map>
#include <unordered_set>
#include <mutex>
#include "pr/common/guid.h"
#include "pr/container/vector.h"
#include "pr/script/forward.h"
#include "pr/filesys/filewatch.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/threads/synchronise.h"

namespace pr
{
	namespace ldr
	{
		// A collection of LDraw script sources
		class ScriptSources :pr::IFileChangedHandler
		{
			// Notes:
			//  - A collection of the ldr sources.
			//  - Typically ldr sources are files, but string sources are also supported.
			//  - This class manages an internal collection of objects, 'ObjectCont'.
			//  - It adds/removes objects from the object container, but only the ones it knows about.
			//  - Files each have their own unique Guid. This is so all objects created by a file group can be removed.

		public:

			using filepath_t = pr::string<wchar_t>;
			using Location = script::Location;
			using GuidSet = std::unordered_set<Guid, std::hash<Guid>>;
			using GuidCont = pr::vector<Guid>;

			// Reasons for changes to the sources collection
			enum class EReason
			{
				NewData,
				Reload,
				Removal,
			};

			// An Ldr script source
			struct Source
			{
				ObjectCont           m_objects;    // Objects created by this source
				pr::Guid             m_context_id; // Id for the group of files that this object is part of
				filepath_t           m_filepath;   // The filepath of the source (if there is one)
				pr::script::Includes m_includes;   // Include paths to use with this file

				Source()
					:m_context_id(pr::GuidZero)
					,m_filepath()
					,m_includes()
				{}
				Source(Guid const& context_id)
					:m_context_id(context_id)
					,m_filepath()
					,m_includes()
				{}
				Source(Guid const& context_id, wchar_t const* filepath, script::Includes const& includes)
					:m_context_id(context_id)
					,m_filepath(pr::filesys::Standardise<filepath_t>(filepath))
					,m_includes(includes)
				{
					if (!m_filepath.empty())
						m_includes.AddSearchPath(pr::filesys::GetDirectory(m_filepath));
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

			// Store changed event args
			struct StoreChangedEventArgs
			{
				// The context ids that changed
				std::initializer_list<Guid> m_context_ids;

				// Contains the results of parsing including the object container that the objects where added to
				ParseResult const* m_result;

				// The number of objects added as a result of the parsing.
				int m_object_count;

				// The origin of the object container change
				EReason m_reason;

				StoreChangedEventArgs(Guid const* context_ids, int id_count, ParseResult const& result, int object_count, EReason why)
					:m_context_ids(context_ids, context_ids + id_count)
					,m_result(&result)
					,m_object_count(object_count)
					,m_reason(why)
				{}
			};

			// Source (context id) removed event args
			struct SourceRemovedEventArgs
			{
				// The Guid of the source to be removed
				pr::Guid m_context_id;

				// The origin of the object container change
				EReason m_reason;

				SourceRemovedEventArgs(pr::Guid context_id, EReason reason)
					:m_context_id(context_id)
					,m_reason(reason)
				{}
			};

		private:

			SourceCont                 m_srcs;           // The sources of ldr script
			GizmoCont                  m_gizmos;         // The created ldr gizmos
			pr::Renderer*              m_rdr;            // Renderer used to create models
			pr::script::IEmbeddedCode* m_embed;          // Embedded code handler
			GuidSet                    m_loading;        // File group ids in the process of being reloaded
			pr::FileWatch              m_watcher;        // The watcher of files
			std::thread::id            m_main_thread_id; // The main thread id

		public:

			ScriptSources(pr::Renderer& rdr, pr::script::IEmbeddedCode* embed)
				:m_srcs()
				,m_gizmos()
				,m_rdr(&rdr)
				,m_embed(embed)
				,m_loading()
				,m_watcher()
				,m_main_thread_id(std::this_thread::get_id())
			{
				// Handle notification of changed files from the watcher.
				// 'OnFilesChanged' is raised before any of the 'FileWatch_OnFileChanged'
				// callbacks are made. So this notifies of the reload before anything starts changing.
				m_watcher.OnFilesChanged += [&](FileWatch::FileCont&)
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
			pr::EventHandler<ScriptSources&, ErrorEventArgs const&> OnError;

			// Reload event. Note: Don't AddFile() or RefreshChangedFiles() during this event.
			pr::EventHandler<ScriptSources&, EmptyArgs const&> OnReload;

			// An event raised during parsing of files. This is called in the context of the threads that call 'AddFile'. Do not sign up while AddFile calls are running.
			pr::EventHandler<ScriptSources&, AddFileProgressEventArgs&> OnAddFileProgress;

			// Store changed event.
			pr::EventHandler<ScriptSources&, StoreChangedEventArgs const&> OnStoreChanged;

			// Source removed event (i.e. objects deleted by Id)
			pr::EventHandler<ScriptSources&, SourceRemovedEventArgs const&> OnSourceRemoved;

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
				OnStoreChanged(*this, StoreChangedEventArgs(guids.data(), int(guids.size()), ParseResult(), 0, EReason::Removal));
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
				OnStoreChanged(*this, StoreChangedEventArgs(guids.data(), int(guids.size()), ParseResult(), 0, EReason::Removal));
			}

			// Remove a single object from the object container
			void Remove(LdrObject* object, EReason reason = EReason::Removal)
			{
				assert(std::this_thread::get_id() == m_main_thread_id);
				auto id = object->m_context_id;

				// Remove the object from the source it belongs to
				auto& src = m_srcs[id];
				auto count = src.m_objects.size();
				pr::ldr::Remove(src.m_objects, object);

				// Notify of the object container change
				if (src.m_objects.size() != count)
					OnStoreChanged(*this, StoreChangedEventArgs(&id, 1, ParseResult(), 0, reason));

				// If that was the last object for the source, remove the source too
				if (src.m_objects.empty())
					Remove(id);
			}

			// Remove all objects associated with 'context_id'
			void Remove(Guid const& context_id, EReason reason = EReason::Removal)
			{
				// Copy the id, because removing the source will delete the memory that 'context_id' is in
				assert(std::this_thread::get_id() == m_main_thread_id);
				auto id = context_id;

				// Notify of objects about to be deleted
				OnSourceRemoved(*this, SourceRemovedEventArgs(id, reason));

				// Delete the source and its associated objects
				m_srcs.erase(id);

				// Delete any associated files and watches
				m_watcher.RemoveAll(id);

				// Notify of the object container change
				OnStoreChanged(*this, StoreChangedEventArgs(&id, 1, ParseResult(), 0, reason));
			}

			// Remove a file source
			void RemoveFile(wchar_t const* filepath, EReason reason = EReason::Removal)
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
				// Note: don't need to clear because each file will remove itself.
				// Don't re-add non file sources, since they can't change.
				for (auto& src : srcs)
				{
					if (!src.second.IsFile()) continue;
					auto& file = src.second;

					// Skip files that are in the process of loading
					if (m_loading.find(file.m_context_id) != std::end(m_loading)) continue;
					m_loading.insert(file.m_context_id);

					// Fire off a thread to add the file
					std::thread([=]{ AddFile(file.m_filepath.c_str(), EReason::Reload, file.m_context_id, file.m_includes, true); }).detach();
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
				OnStoreChanged(*this, StoreChangedEventArgs(&context_id, 1, ParseResult(), 1, reason));
			}

			// Add a file source.
			// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
			// Returns the Guid of the context that the objects were added to.
			Guid AddFile(wchar_t const* filepath, script::Includes const& includes, bool additional)
			{
				return AddFile(filepath, EReason::NewData, pr::GenerateGUID(), includes, additional);
			}

			// Add ldr objects from a script string or file (but not as a file source).
			// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
			// Returns the Guid of the context that the objects were added to.
			Guid AddScript(wchar_t const* ldr_script, bool file, Guid const* context_id, script::Includes const& includes)
			{
				// Create a context id if none given
				auto guid = context_id ? *context_id : pr::GenerateGUID();
				return AddScript(ldr_script, file, EReason::NewData, guid, includes);
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
				pr::erase_first(m_gizmos, [&](LdrGizmoPtr const& p){ return p.m_ptr == gizmo; });
			}

			// Return the file group id for objects created from 'filepath' (if filepath is an existing source)
			Guid const* ContextIdFromFilepath(wchar_t const* filepath) const
			{
				assert(std::this_thread::get_id() == m_main_thread_id);

				// Find the corresponding source in the sources collection
				auto fpath = pr::filesys::Standardise<filepath_t>(filepath);
				auto iter = pr::find_if(m_srcs, [=](auto& src){ return src.second.m_filepath == fpath; });
				return iter != std::end(m_srcs) ? &iter->second.m_context_id : nullptr;
			}

		private:

			// 'filepath' is the name of the changed file
			void FileWatch_OnFileChanged(wchar_t const*, Guid const& context_id, void*, bool&)
			{
				assert(std::this_thread::get_id() == m_main_thread_id);

				// Look for the root file for group 'context_id'
				auto iter = pr::find_if(m_srcs, [=](auto& src){ return src.second.m_context_id == context_id; });
				if (iter == std::end(m_srcs)) return;
				auto root_file = iter->second;

				// Skip files that are in the process of loading
				if (m_loading.find(root_file.m_context_id) != std::end(m_loading)) return;
				m_loading.insert(root_file.m_context_id);

				// Reload that file group (asynchronously)
				auto filepath = root_file.m_filepath;
				auto ctx_id = root_file.m_context_id;
				auto inc = root_file.m_includes;
				std::thread([=]
				{
					auto id = AddFile(filepath.c_str(), EReason::Reload, ctx_id, inc, true);
					if (id != pr::GuidZero)
						return;

					// Don't do this, because it leaves LDraw in an infinite loop trying to load a broken file
					//// On failure, mark the file as changed again.
					//m_rdr->RunOnMainThread([=]{ m_watcher.MarkAsChanged(root_file.m_filepath.c_str()); });
				}).detach();
			}

			// Internal add file.
			// Note: 'file' not passed by reference because it can be a file already in the collection, so we need a local copy.
			// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
			// Returns the Guid of the context that the objects were added to.
			Guid AddFile(wchar_t const* ldr_file, EReason reason, Guid const& context_id, script::Includes const& includes, bool additional) // worker thread context
			{
				using namespace pr::script;
				assert(ldr_file && *ldr_file != 0);

				// Create a file source
				Source file(context_id, ldr_file, includes);

				// Record the files that get included so we can watch them for changes
				pr::vector<filepath_t> filepaths;
				filepaths.push_back(file.m_filepath);
				auto file_opened = [&](filepath_t const& fp)
				{
					// Add the directory of the included file to the paths
					file.m_includes.AddSearchPath(filesys::GetDirectory(fp));
					filepaths.push_back(filesys::Standardise(fp));
				};

				// Parse the contents of the file
				ParseResult out;
				ErrorEventArgs errors;
				#pragma region Parse
				try
				{
					// Add the file based on it's file type
					auto extn = filesys::GetExtension(file.m_filepath);
					if (str::EqualI(extn, "lua"))
					{
						// Lua script that generates ldr script
						//m_lua_src.Add(fpath.c_str());
					}
					else if (str::EqualI(extn, "p3d"))
					{
						// P3D binary model file
						Buffer<> src(ESrcType::Buffered, pr::FmtS(L"*Model {\"%s\"}", file.m_filepath.c_str()));
						Reader reader(src, false, &file.m_includes, nullptr, m_embed);
						Parse(*m_rdr, reader, out, file.m_context_id, pr::StaticCallBack(AddFileProgressCB, this));
					}
					else if (str::EqualI(extn, "csv"))
					{
						// CSV data, create a chart to graph the data
						Buffer<> src(ESrcType::Buffered, pr::FmtS(L"*Chart {3 #include \"%s\"}", file.m_filepath.c_str()));
						Reader reader(src, false, &file.m_includes, nullptr, m_embed);
						Parse(*m_rdr, reader, out, file.m_context_id, pr::StaticCallBack(AddFileProgressCB, this));
					}
					else
					{
						// Assume an ldr script file
						pr::LockFile lock(file.m_filepath, 10, 5000);
						FileSrc src(file.m_filepath.c_str());

						// When the include handler opens files, add them to the watcher as well
						file.m_includes.FileOpened = file_opened;

						// Parse the script
						Reader reader(src, false, &file.m_includes, nullptr, m_embed);
						Parse(*m_rdr, reader, out, file.m_context_id, pr::StaticCallBack(AddFileProgressCB, this));
					}
				}
				catch (pr::script::Exception const& ex)
				{
					errors = ErrorEventArgs(pr::Fmt(L"Script error found while parsing source file '%s'.\r\n", file.m_filepath.c_str()) + Widen(ex.what()));
				}
				catch (std::exception const& ex)
				{
					errors = ErrorEventArgs(pr::Fmt(L"Error found while parsing source file '%s'.\r\n", file.m_filepath.c_str()) + Widen(ex.what()));
				}
				#pragma endregion

				// Raise events on the main thread
				auto merge = [=]() mutable
				{
					// If not additional, clear all sources.
					// Otherwise, just remove any objects associated with 'file'.
					if (!additional)
						ClearAll();
					else
						RemoveFile(file.m_filepath.c_str(), reason);

					// Remove from the 'loading' set
					m_loading.erase(file.m_context_id);

					// Add to the container of script sources
					auto& src = m_srcs[file.m_context_id];
					src.m_filepath = file.m_filepath;
					src.m_includes = file.m_includes;
					src.m_context_id = file.m_context_id;
					src.m_objects.insert(std::end(src.m_objects), std::begin(out.m_objects), std::end(out.m_objects));

					// Add to the watcher
					for (auto& fp : filepaths)
						m_watcher.Add(fp.c_str(), this, src.m_context_id);

					// Notify of any errors that occurred
					if (!errors.m_msg.empty())
						OnError(*this, errors);

					// Notify of the object container change
					OnStoreChanged(*this, StoreChangedEventArgs(&src.m_context_id, 1, out, int(out.m_objects.size()), reason));
				};

				// Marshal to the main thread if this is a worker thread context
				if (std::this_thread::get_id() != m_main_thread_id)
					m_rdr->RunOnMainThread(merge);
				else
					merge();

				return context_id;
			}

			// Internal add script.
			// Add ldr objects from a script string or file (but not as a file source).
			// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
			// Returns the Guid of the context that the objects were added to
			Guid AddScript(wchar_t const* ldr_script, bool file, EReason reason, Guid const& context_id, script::Includes const& includes) // worker thread context
			{
				using namespace pr::script;

				// Create a writeable includes handler
				auto inc = includes;

				// Parse the description
				ParseResult out;
				ErrorEventArgs errors;
				#pragma region Parse
				try
				{
					if (file)
					{
						inc.AddSearchPath(filesys::GetDirectory<script::string>(ldr_script));

						FileSrc src(ldr_script);
						Reader reader(src, false, &inc, nullptr, m_embed);
						ldr::Parse(*m_rdr, reader, out, context_id);
					}
					else // string
					{
						PtrW src(ldr_script);
						Reader reader(src, false, &inc, nullptr, m_embed);
						ldr::Parse(*m_rdr, reader, out, context_id);
					}
				}
				catch (pr::script::Exception const& ex)
				{
					errors = ErrorEventArgs(pr::FmtS(L"Script error found while parsing script.\r\n%S", ex.what()));
				}
				catch (std::exception const& ex)
				{
					errors = ErrorEventArgs(pr::FmtS(L"Error found while parsing script.\r\n%S", ex.what()));
				}
				#pragma endregion

				// Merge the results
				auto merge = [=]
				{
					// Don't remove previous 'context_id' objects.
					// Objects for a set may be added with multiple AddScript calls

					// Ensure the source exists and add the objects
					auto& src = m_srcs[context_id];
					src.m_context_id = context_id;
					src.m_objects.insert(std::end(src.m_objects), std::begin(out.m_objects), std::end(out.m_objects));

					// Notify of any errors that occurred
					if (!errors.m_msg.empty())
						OnError(*this, errors);

					// Notify of the object container change
					OnStoreChanged(*this, StoreChangedEventArgs(&context_id, 1, out, int(out.m_objects.size()), reason));

					// Throw on errors
					if (!errors.m_msg.empty())
						throw std::exception(Narrow(errors.m_msg).c_str());
				};

				// Marshal to the main thread if this is a worker thread context
				if (std::this_thread::get_id() != m_main_thread_id)
					m_rdr->RunOnMainThread(merge);
				else
					merge();

				return context_id;
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
}



#if 0
// This class processes lua code
struct LuaSource :pr::script::IEmbeddedCode
{
	pr::lua::Lua m_lua;

	LuaSource()
		:m_lua()
	{
		m_lua.SetOutputFuncs(pr::lua::DebugPrint, pr::lua::DebugPrint, 0, 0);
	}

	// Add a lua source file
	void Add(wchar_t const* filepath)
	{
		(void)filepath;
		//m_lua.DoFile(filepath);
	}

	bool Execute(pr::script::string const& lang, pr::script::string const& code, pr::script::string& result) override
	{
		// We only handle lua code
		if (!pr::str::Equal(lang, "lua"))
			return false;

		// Record the number of items on the stack
		int base = lua_gettop(m_lua);

		// Convert the lua code to a compiled chunk
		pr::string<> error_msg;
		if (pr::lua::PushLuaChunk<pr::string<>>(m_lua, pr::Narrow(code), error_msg) != pr::lua::EResult::Success)
			throw std::exception(error_msg.c_str());

		// Execute the chunk
		if (!pr::lua::CallLuaChunk(m_lua, 0, false))
			return false;

		// If there's something still on the stack, copy it to result
		if (lua_gettop(m_lua) != base && !lua_isnil(m_lua, -1))
		{
			auto r = pr::Widen(lua_tostring(m_lua, -1));
			result = r;
			lua_pop(m_lua, 1);
		}

		// Ensure the stack does not grow
		if (lua_gettop(m_lua) != base)
		{
			PR_ASSERT(PR_DBG, false, "lua stack height not constant");
			lua_settop(m_lua, base);
		}

		return true;
	}

	// Return a string containing demo ldr lua script
	std::string CreateDemoLuaSource() const
	{
		std::stringstream out;
		out <<  "--********************************************\n"
				"-- Demo Ldr lua script\n"
				"--********************************************\n"
				"\n"
				"-- Set the rate to call the OnLdrStep() function\n"
				"LdrStepRate = 50 -- 50fps\n"
				"\n"
				"-- Called when the file is loaded.\n"
				"function LdrLoad()\n"
				"    -- Create some ldr objects\n"
				"    ldrCreate('*Box point FF00FF00 {1}')\n"
				"end\n"
				"\n"
				"-- Called repeatedly.\n"
				"function LdrStep()\n"
				"    -- Create some ldr objects\n"
				"    ldrCreate('*Box point FF00FF00 {1}')\n"
				"end\n"
				"\n"
				;
		return out.str();
	}
};
#endif
