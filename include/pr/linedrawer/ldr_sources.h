//*****************************************************************************************
// LDraw
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

// A container of Ldr script sources that can watch for external change.

#include <string>
#include <sstream>
#include <unordered_map>
#include <mutex>
#include "pr/container/vector.h"
#include "pr/script/forward.h"
#include "pr/filesys/filewatch.h"
#include "pr/linedrawer/ldr_object.h"

namespace pr
{
	namespace ldr
	{
		// A collection of the file sources.
		// This class manages an externally provided 'ObjectCont'.
		// It adds/removes objects from the 'store', but only the ones it knows about.
		// Files each have their own unique Guid. This is so all objects created by a
		// file group can be removed.
		class ScriptSources :pr::IFileChangedHandler
		{
		public:
			using filepath_t = pr::string<wchar_t>;
			using Location = pr::script::Location;

			enum class EReason
			{
				NewData,
				Reload,
			};

			// A watched file
			struct File
			{
				filepath_t             m_filepath;      // The file to watch
				pr::Guid               m_file_group_id; // Id for the group of files that this object is part of
				pr::script::Includes<> m_includes;      // Include paths to use with this file

				File()
					:m_filepath()
					,m_file_group_id(pr::GuidZero)
					,m_includes()
				{}
				File(wchar_t const* filepath, pr::Guid const& file_group_id, pr::script::Includes<> const& includes)
					:m_filepath(pr::filesys::Standardise<filepath_t>(filepath))
					,m_file_group_id(file_group_id)
					,m_includes(includes)
				{
					m_includes.AddSearchPath(pr::filesys::GetDirectory(m_filepath));
				}
			};

			// A container that doesn't invalidate on add/remove is needed because
			// the file watcher contains a pointer to 'File' objects.
			using FileCont = std::unordered_map<filepath_t, File>;

			// Progress update event args
			struct AddFileProgressEventArgs :CancelEventArgs
			{
				// The context id for the AddFile group
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

			// Source reload event args
			struct ReloadEventArgs
			{
				// The store that is effected
				ObjectCont const* m_store;

				// The origin of the store change
				EReason m_reason;

				ReloadEventArgs(ObjectCont const& store, EReason why)
					:m_store(&store)
					,m_reason(why)
				{}
			};

			// Store changed event args
			struct StoreChangedEventArgs
			{
				// The store that was added to
				ObjectCont const* m_store;

				// Contains the results of parsing including the object container that the objects where added to
				ParseResult const* m_result;

				// The number of objects added as a result of the parsing.
				int m_count;

				// The origin of the store change
				EReason m_reason;

				StoreChangedEventArgs(ObjectCont const& store, int count, ParseResult const& result, EReason why)
					:m_store(&store)
					,m_result(&result)
					,m_count(count)
					,m_reason(why)
				{}
			};

			// Source file removed event args
			struct FileRemovedEventArgs
			{
				pr::Guid m_file_group_id;

				FileRemovedEventArgs(pr::Guid file_group_id)
					:m_file_group_id(file_group_id)
				{}
			};

		private:

			FileCont                   m_files;    // The file sources of ldr script
			pr::FileWatch              m_watcher;  // The watcher of files
			ObjectCont*                m_store;    // The store to add Ldr objects to
			pr::Renderer*              m_rdr;      // Renderer used to create models
			pr::script::IEmbeddedCode* m_embed;    // Embedded code handler
			std::recursive_mutex       m_mutex;    // Sync access to the store

		public:

			ScriptSources(ObjectCont& store, pr::Renderer& rdr, pr::script::IEmbeddedCode* embed)
				:m_files()
				,m_watcher()
				,m_store(&store)
				,m_rdr(&rdr)
				,m_embed(embed)
				,m_mutex()
				,OnReload()
				,OnStoreChanged()
				,OnFileRemoved()
				,OnError()
			{
				m_watcher.OnFilesChanged += [&](FileWatch::FileCont&)
				{
					OnReload(*this, ReloadEventArgs(*m_store, EReason::Reload));
				};
			}

			// An event raised during parsing of files. This is called in the context of the threads that call 'AddFile'. Do not sign up while AddFile calls are running.
			pr::EventHandler<ScriptSources&, AddFileProgressEventArgs&> OnAddFileProgress;

			// Reload event. Note: Don't AddFile() or RefreshChangedFiles() during this event.
			pr::EventHandler<ScriptSources&, ReloadEventArgs const&> OnReload;

			// Store changed event. Note: raised in the same thread context as 'AddFile'.
			pr::EventHandler<ScriptSources&, StoreChangedEventArgs const&> OnStoreChanged;

			// Source file being removed event (i.e. objects deleted by Id)
			pr::EventHandler<ScriptSources&, FileRemovedEventArgs const&> OnFileRemoved;

			// Parse error event. Note: raised in the same thread context as 'AddFile'.
			pr::EventHandler<ScriptSources&, ErrorEventArgs const&> OnError;

			// Return const access to the source files
			FileCont const& List() const
			{
				return m_files;
			}

			// Remove all file sources
			void Clear()
			{
				std::lock_guard<std::recursive_mutex> lock(m_mutex);

				// Delete all objects belonging to all file groups
				for (auto& file : m_files)
				{
					OnFileRemoved(*this, FileRemovedEventArgs(file.second.m_file_group_id));
					pr::ldr::Remove(*m_store, &file.second.m_file_group_id, 1, 0, 0);
				}

				// Remove all file watches
				m_watcher.RemoveAll();
				m_files.clear();
			}

			// Add a file source
			// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
			pr::Guid AddFile(wchar_t const* filepath, pr::script::Includes<> const& includes)
			{
				File file(filepath, pr::GenerateGUID(), includes);
				return AddFile(file, EReason::NewData);
			}

			// Reload all files
			void Reload()
			{
				std::lock_guard<std::recursive_mutex> lock(m_mutex);

				// Make a copy of the file list
				auto files = m_files;

				// Reset the sources
				Clear();

				// Notify reloading
				OnReload(*this, ReloadEventArgs(*m_store, EReason::Reload));

				// Add each file again
				for (auto& f : files)
					AddFile(f.second, EReason::Reload);
			}

			// Remove a file source
			void Remove(wchar_t const* filepath)
			{
				std::lock_guard<std::recursive_mutex> lock(m_mutex);

				// Find the file in the file list
				auto fpath = pr::filesys::Standardise<filepath_t>(filepath);
				auto iter = m_files.find(fpath);
				if (iter == std::end(m_files))
					return;

				auto& file = iter->second;

				// Notify of objects about to be deleted
				OnFileRemoved(*this, FileRemovedEventArgs(file.m_file_group_id));

				// Delete all objects belonging to this file group
				pr::ldr::Remove(*m_store, &file.m_file_group_id, 1, 0, 0);

				// Delete all associated file watches
				m_watcher.RemoveAll(file.m_file_group_id);

				// Remove from the file list
				m_files.erase(iter);
			}

			// Check all file sources for modifications and reload any that have changed
			void RefreshChangedFiles()
			{
				m_watcher.CheckForChangedFiles();
			}

		private:

			// 'filepath' is the name of the changed file
			// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
			void FileWatch_OnFileChanged(wchar_t const*, pr::Guid const& file_group_id, void*, bool&)
			{
				// Look for the root file for group 'file_group_id'
				auto iter = pr::find_if(m_files, [=](auto& file){ return file.second.m_file_group_id == file_group_id; });
				if (iter == std::end(m_files))
					return;

				// Reload that file group
				AddFile(iter->second, EReason::Reload);
			}

			// Internal add file.
			// Note: 'file_' not passed by reference because it can be a file already in the collection, so we need a local copy.
			// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
			pr::Guid AddFile(File file, EReason reason)
			{
				using namespace pr::script;

				// Ensure the same file is not added twice
				Remove(file.m_filepath.c_str());

				// Record the files that get included so we can watch them for changes
				pr::vector<filepath_t> filepaths;
				filepaths.push_back(pr::filesys::Standardise(file.m_filepath));
				auto file_opened = [&](filepath_t const& fp)
				{
					// Add the directory of the included file to the paths
					file.m_includes.AddSearchPath(pr::filesys::GetDirectory(fp));
					filepaths.push_back(pr::filesys::Standardise(fp));
				};

				ParseResult out;
				try
				{
					// Add the file based on it's file type
					auto extn = pr::filesys::GetExtension(file.m_filepath);
					if (pr::str::EqualI(extn, "lua"))
					{
						// Lua script that generates ldr script
						//m_lua_src.Add(fpath.c_str());
					}
					else if (pr::str::EqualI(extn, "p3d"))
					{
						// P3D binary model file
						Buffer<> src(ESrcType::Buffered, pr::FmtS(L"*Model {\"%s\"}", file.m_filepath.c_str()));
						Reader reader(src, false, &file.m_includes, nullptr, m_embed);
						Parse(*m_rdr, reader, out, file.m_file_group_id, pr::StaticCallBack(AddFileProgressCB, this));
					}
					else if (pr::str::EqualI(extn, "csv"))
					{
						// CSV data, create a chart to graph the data
						Buffer<> src(ESrcType::Buffered, pr::FmtS(L"*Chart {3 \"%s\"}", file.m_filepath.c_str()));
						Reader reader(src, false, &file.m_includes, nullptr, m_embed);
						Parse(*m_rdr, reader, out, file.m_file_group_id, pr::StaticCallBack(AddFileProgressCB, this));
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
						Parse(*m_rdr, reader, out, file.m_file_group_id, pr::StaticCallBack(AddFileProgressCB, this));
					}

					// Merge the objects into 'm_store' and add the files to the watch.
					{
						std::lock_guard<std::recursive_mutex> lock(m_mutex);

						// Add to the container of script sources
						m_files[file.m_filepath] = file;

						// Add to the watcher
						for (auto& fp : filepaths)
							m_watcher.Add(fp.c_str(), this, file.m_file_group_id);

						// Merge the store
						for (auto& obj : out.m_objects)
							m_store->push_back(obj);

						// Notify of the store change
						OnStoreChanged(*this, StoreChangedEventArgs(*m_store, int(out.m_objects.size()), out, reason));
					}
					return file.m_file_group_id;
				}
				catch (pr::script::Exception const& ex)
				{
					OnError(*this, ErrorEventArgs(pr::FmtS(L"Script error found while parsing source file '%s'.\r\n%S", file.m_filepath.c_str(), ex.what())));
				}
				catch (std::exception const& ex)
				{
					OnError(*this, ErrorEventArgs(pr::FmtS(L"Error found while parsing source file '%s'.\r\n%S", file.m_filepath.c_str(), ex.what())));
				}
				return pr::GuidZero;
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
// Add a string source
void AddString(wchar_t const* str)
{
	try
	{
		using namespace pr::script;

		ParseResult out(m_store);
		auto bcount = m_store.size();

		PtrW src(str);
		Reader reader(src, false, nullptr, nullptr, &m_lua_src);
		Parse(m_rdr, reader, out, false);

		auto added = int(m_store.size() - bcount);
		OnStoreChanged(*this, StoreChangedEventArgs(m_store, added, out, StoreChangedEventArgs::EReason::NewData));
	}
	catch (std::exception const& ex)
	{
		auto msg = pr::FmtS(L"Script error found while parsing source string.\r\n%S", ex.what());
		OnError(*this, ErrorEventArgs(msg, pr::script::EResult::Failed));
	}
}

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

	bool Execute(pr::script::string const& lang, pr::script::string const& code, pr::script::Location const& loc, pr::script::string& result) override
	{
		// We only handle lua code
		if (!pr::str::Equal(lang, "lua"))
			return false;

		// Record the number of items on the stack
		int base = lua_gettop(m_lua);

		// Convert the lua code to a compiled chunk
		pr::string<> error_msg;
		if (pr::lua::PushLuaChunk<pr::string<>>(m_lua, pr::Narrow(code), error_msg) != pr::lua::EResult::Success)
			throw pr::script::Exception(pr::script::EResult::EmbeddedCodeSyntaxError, loc, error_msg.c_str());

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
