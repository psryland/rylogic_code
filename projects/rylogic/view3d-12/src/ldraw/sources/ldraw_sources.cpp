//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/ldraw/sources/ldraw_sources.h"
#include "view3d-12/src/ldraw/sources/source_base.h"
#include "view3d-12/src/ldraw/sources/source_string.h"
#include "view3d-12/src/ldraw/sources/source_binary.h"
#include "view3d-12/src/ldraw/sources/source_file.h"
#include "view3d-12/src/ldraw/sources/source_stream.h"
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
	ScriptSources::ScriptSources(Renderer& rdr)
		: m_srcs()
		, m_gizmos()
		, m_rdr(&rdr)
		, m_winsock()
		, m_loading()
		, m_watcher()
		, m_listen_thread()
		, m_main_thread_id(std::this_thread::get_id())
		, m_listen_port()
	{
		// Handle notification of changed files from the watcher.
		// 'OnFilesChanged' is raised before any of the 'FileWatch_OnFileChanged'
		// callbacks are made. So this notifies of the reload before anything starts changing.
		m_watcher.OnFilesChanged += [&](FileWatch&, FileWatch::FileCont&)
		{
			OnReload(*this, EmptyArgs());
		};
	}
	ScriptSources::~ScriptSources()
	{
		StopConnections();
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
		StoreChangeEventArgs args(ESourceChangeReason::Removal, guids, nullptr, false);
		OnStoreChange(*this, args);
	}

#if 0
	// Remove all file sources
	void ScriptSources::ClearFiles()
	{
		throw std::runtime_error("not implemented");
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Notify of the delete of each file source
		GuidCont guids;
		for (auto& src : m_srcs)
		{
			if (!src.second.IsFile()) continue;
			OnSourceRemoved(*this, SourceRemovedEventArgs(src.first, ESourceChangeReason::Removal));
			guids.push_back(src.first);
		}

		// Remove all file sources
		for (auto& id : guids)
			m_srcs.erase(id);

		// Remove watcher references
		m_watcher.RemoveAll();

		// Notify of the object container change
		StoreChangeEventArgs args(ESourceChangeReason::Removal, guids, nullptr, false);
		OnStoreChange(*this, args);
	}
#endif

	// Remove a single object from the object container
	void ScriptSources::Remove(LdrObject* object, ESourceChangeReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto id = object->m_context_id;

		// Remove the object from the source it belongs to
		auto& src = m_srcs[id];
		auto count = src->m_output.m_objects.size();
		ldraw::Remove(src->m_output.m_objects, object);

		// Notify of the object container change
		if (src->m_output.m_objects.size() != count)
		{
			StoreChangeEventArgs args{ reason, std::initializer_list<Guid const>(&id, &id + 1), nullptr, false };
			OnStoreChange(*this, args);
		}

		// If that was the last object for the source, remove the source too
		if (src->m_output.m_objects.empty())
			Remove(id);
	}

	// Remove all objects associated with 'context_ids'
	void ScriptSources::Remove(Guid const* context_ids, int include_count, int exclude_count, ESourceChangeReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Build the set of ids to remove
		GuidCont removed;
		for (auto& src : m_srcs)
		{
			auto id = src.second->m_context_id;
			if (!IncludeFilter(id, context_ids, include_count, exclude_count)) continue;
			removed.push_back(id);
		}

		// Remove the sources
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
			StoreChangeEventArgs args{ reason, removed, nullptr, false };
			OnStoreChange(*this, args);
		}
	}
	void ScriptSources::Remove(Guid const& context_id, ESourceChangeReason reason)
	{
		Remove(&context_id, 1, 0, reason);
	}

#if 0
	// Remove a file source
	void ScriptSources::RemoveFile(filepath_t const& filepath, ESourceChangeReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Remove the objects created 'filepath'
		auto context_id = ContextIdFromFilepath(filepath);
		if (context_id != nullptr)
			Remove(*context_id, reason);
	}
#endif

	// Reload all sources
	void ScriptSources::Reload()
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Notify reloading
		OnReload(*this, EmptyArgs());

		// Reload each source in a background thread
		std::for_each(std::execution::par_unseq, std::begin(m_srcs), std::end(m_srcs), [this](auto& src)
		{
			src.second->Reload(rdr());
		});


#if 0
		// Make a copy of the sources container
		auto srcs = m_srcs;

		// Add each file again (asynchronously)
		for (auto const& src : srcs)
		{
			// Skip files that are in the process of loading
			if (m_loading.find(src.second.m_context_id) != std::end(m_loading))
				continue;

			m_loading.insert(src.second.m_context_id);

			// Fire off a worker thread to reload the file.
			Source file = src.second;
			std::thread([=]
			{
				AddFile(file.m_filepath, file.m_encoding, ESourceChangeReason::Reload, &file.m_context_id, file.m_includes, [&](Guid const& id, bool before) noexcept
				{
					if (!before) return;
					Remove(id, ESourceChangeReason::Reload);
				});
			}).detach();
		}
#endif
	}

	// Check all file sources for modifications and reload any that have changed
	void ScriptSources::RefreshChangedFiles()
	{
		m_watcher.CheckForChangedFiles();
	}

	// Add an object created externally
	Guid ScriptSources::Add(LdrObjectPtr object, ESourceChangeReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		auto context_id = object->m_context_id;

		// Add the object to the collection
		auto& src = m_srcs[context_id];
		if (src == nullptr) src = std::unique_ptr<SourceBase>(new SourceBase{ &context_id });
		src->m_output.m_objects.push_back(object);

		// Notify of the object container change
		StoreChangeEventArgs args(reason, { &context_id, 1 }, nullptr, false);
		OnStoreChange(*this, args);

		return context_id;
	}

	// Parse a string containing ldr script.
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	template <typename Char>
	Guid ScriptSources::AddString(std::basic_string_view<Char> script, EEncoding enc, ESourceChangeReason reason, Guid const* context_id, PathResolver const& includes, OnAddCB on_add) // worker thread context
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		auto source = std::unique_ptr<SourceString<Char>>(new SourceString<Char>(context_id, script, enc, includes));
		source->Reload(rdr());
		return Merge(std::move(source), reason, on_add);
	}
	template Guid ScriptSources::AddString<wchar_t>(std::wstring_view script, EEncoding enc, ESourceChangeReason reason, Guid const* context_id, PathResolver const& includes, OnAddCB on_add);
	template Guid ScriptSources::AddString<char>(std::string_view script, EEncoding enc, ESourceChangeReason reason, Guid const* context_id, PathResolver const& includes, OnAddCB on_add);

	// Parse file containing ldr script.
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	Guid ScriptSources::AddFile(std::filesystem::path filepath, EEncoding enc, ESourceChangeReason reason, Guid const* context_id, PathResolver const& includes, OnAddCB on_add) // worker thread context
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		auto source = std::unique_ptr<SourceFile>(new SourceFile{ context_id, filepath, enc, includes });
		source->Reload(rdr());
		return Merge(std::move(source), reason, on_add);
	}

	// Parse binary data containing ldraw script
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	Guid ScriptSources::AddBinary(std::span<std::byte const> data, ESourceChangeReason reason, Guid const* context_id, OnAddCB on_add)
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		auto source = std::unique_ptr<SourceBinary>(new SourceBinary{ context_id, data });
		source->Reload(rdr());
		return Merge(std::move(source), reason, on_add);
	}

	// Merge a script source with the existing sources collection
	Guid ScriptSources::Merge(std::unique_ptr<SourceBase>&& source, ESourceChangeReason reason, OnAddCB on_add)
	{
		auto context_id = source->m_context_id;
		if (std::this_thread::get_id() != m_main_thread_id)
		{
			rdr().RunOnMainThread([this, source = std::move(source), reason, on_add]() mutable noexcept
			{
				Merge(std::move(source), reason, on_add);
			});
			return context_id;
		}
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Don't remove previous objects associated with 'context', 
		// leave that to the caller via the 'on_add' callback.
		// Remove from the file watcher's 'loading' set
		m_loading.erase(context_id);

		// Notify of the store about to change
		StoreChangeEventArgs args0{ reason, { &context_id, 1 }, &source->m_output, true };
		OnStoreChange(*this, args0);
		if (on_add)
			on_add(context_id, true);

		// Add any dependent files to the file watcher
		for (auto& fp : source->m_filepaths)
			m_watcher.Add(fp.c_str(), this, context_id);

		// Notify of any errors that occurred
		for (auto& err : source->m_errors)
			OnError(*this, err);

		// Update the store (invalidating 'source')
		auto& src = m_srcs[context_id];
		if (src == nullptr)
			src = std::move(source);
		else
			src->m_output += std::move(source->m_output);

		// Notify of the store change
		StoreChangeEventArgs args1{ reason, { &context_id, 1 }, &src->m_output, false };
		OnStoreChange(*this, args1);
		if (on_add)
			on_add(context_id, false);

		return context_id;
	}

	// Allow connections on 'port'
	void ScriptSources::AllowConnections(uint16_t listen_port)
	{
		using Socket = network::Socket;

		StopConnections();

		// Start the thread for incoming connections
		m_listen_port = listen_port;
		m_listen_thread = std::jthread([this, listen_port]()
		{
			threads::SetCurrentThreadName("Stream Sources Listen Thread");
			enum class EState { Disconnected, Idle, Listening, Broken } state = EState::Disconnected;

			Socket listen_socket;

			// Check for client connections to the server and dump old connections
			for (;!m_listen_thread.get_stop_token().stop_requested();)
			{
				// Don't exit the thread unless shutting down.
				// Try to handle re-connections, and other errors gracefully.
				try
				{
					switch (state)
					{
						case EState::Disconnected:
						{
							// Create the listen socket. If this fails with WSAEACCESS, it's probably because the firewall is blocking it
							listen_socket = Socket(::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP));
							if (listen_socket == nullptr)
								network::Throw(WSAGetLastError());

							// Bind the local address to the socket
							sockaddr_in my_address = {};
							my_address.sin_family = AF_INET;
							my_address.sin_addr.S_un.S_addr = INADDR_ANY;
							my_address.sin_port = htons(static_cast<u_short>(listen_port));
							auto result = ::bind(listen_socket, (sockaddr const*)&my_address, sizeof(my_address));
							if (result == SOCKET_ERROR)
								network::Throw(WSAGetLastError());

							state = EState::Idle;
							break;
						}
						case EState::Idle:
						{
							// Start listening for incoming connections
							auto result = ::listen(listen_socket, SOMAXCONN);
							if (result != SOCKET_ERROR || WSAGetLastError() == WSAEISCONN) // No error, or already connected
							{
								state = EState::Listening;
								break;
							}

							// Listen failed, check the error code
							auto code = WSAGetLastError();
							switch (code)
							{
								case WSAEINPROGRESS: // A blocking Windows Sockets 1.1 call is in progress, or the service provider is still processing a callback function.
								case WSAENETDOWN:    // The network subsystem has failed.
								case WSAEWOULDBLOCK:
								{
									// Retry after a delay
									std::this_thread::sleep_for(std::chrono::milliseconds(200));
									break;
								}
								default:
								{
									network::Throw(code);
									break;
								}
							}
							break;
						}
						case EState::Listening:
						{
							// Wait for new connections
							if (network::SelectToRecv(listen_socket, 5000))
							{
								// Someone is trying to connect
								sockaddr_in client_addr;
								auto client_addr_size = static_cast<int>(sizeof(client_addr));
								auto client = Socket(::accept(listen_socket, (sockaddr*)&client_addr, &client_addr_size));
								network::Check(client != INVALID_SOCKET, "Accepting connection failed");

								// Add this connection as a new source
								auto source = std::unique_ptr<SourceStream>(new SourceStream{ nullptr, &rdr(), std::move(client), client_addr });
								Merge(std::move(source), ESourceChangeReason::NewData, {});
							}
							break;
						}
						case EState::Broken:
						{
							// Clean up the broken socket
							listen_socket = nullptr;
							state = EState::Disconnected;
							break;
						}
						default:
						{
							throw std::runtime_error("Unknown state");
						}
					}
				}
				catch (std::exception const& ex)
				{
					assert(false && ex.what()); (void)ex;
					state = EState::Broken;
				}
				catch (...)
				{
					assert(false && "Unhandled exception in TCP listen thread");
					state = EState::Broken;
				}
			}

			// Clean up the broken socket
			listen_socket = nullptr;
			state = EState::Disconnected;
		});
	}

	// Close all connections and stop listening
	void ScriptSources::StopConnections()
	{
		// Stop the incoming connections thread
		m_listen_thread.get_stop_source().request_stop();
		if (m_listen_thread.joinable())
			m_listen_thread.join();

		// Remove any stream sources
		std::erase_if(m_srcs, [](auto& src)
		{
			return dynamic_cast<SourceStream*>(src.second.get()) != nullptr;
		});
	}

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
		auto iter = find_if(m_srcs, [=](auto const& pair)
		{
			SourceBase const& src = *pair.second;

			// Find the source where the first filepath matches 'filepath'
			if (src.m_filepaths.empty())
				return false;
			
			return filesys::Equal(fpath, src.m_filepaths[0], true);
		});

		return iter != std::end(m_srcs) ? &iter->second->m_context_id : nullptr;
	}

	// 'filepath' is the name of the changed file
	void ScriptSources::FileWatch_OnFileChanged(wchar_t const*, Guid const& context_id, void*, bool&)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Look for a source that matches 'context_id'
		auto iter = m_srcs.find(context_id);
		if (iter == std::end(m_srcs))
			return;

		// Skip files that are already in the process of loading
		if (m_loading.find(context_id) != std::end(m_loading))
			return;

		m_loading.insert(context_id);

		// Make a copy of the source (if clone-able)
		auto clone = iter->second->Clone();
		if (clone == nullptr)
			return;

		// Reload that file group (asynchronously)
		std::jthread([this, clone = std::move(clone), context_id]() mutable
		{
			// Note: if loading a file fails, don't use 'MarkAsChanged' to trigger another load
			// attempt. Doing so results in an infinite loop trying to load a broken file.
			clone->Reload(rdr());
			Merge(std::move(clone), ESourceChangeReason::Reload, [=](Guid const&, bool before)
			{
				if (!before) return;
				Remove(context_id);
			});
		}).detach();
	}
}
