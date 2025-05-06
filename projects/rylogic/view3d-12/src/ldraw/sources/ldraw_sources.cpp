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
	ScriptSources::ScriptSources(Renderer& rdr, ISourceEvents& events)
		: m_srcs()
		, m_gizmos()
		, m_rdr(&rdr)
		, m_events(&events)
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
		m_watcher.OnFilesChanged += [this](FileWatch&, FileWatch::FileCont&) mutable
		{
			// needed now?
			//m_events->OnReload();
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
	SourceCont const& ScriptSources::Sources() const
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
		m_events->OnStoreChange({EDataChangedReason::Removal, guids, nullptr, false});
	}

	// Remove a single object from the store
	void ScriptSources::Remove(LdrObject* object, EDataChangedReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto id = object->m_context_id;

		// Remove the object from the source it belongs to
		auto& src = m_srcs[id];
		auto count = src->m_output.m_objects.size();
		ldraw::Remove(src->m_output.m_objects, object);

		// Notify of the object container change
		if (src->m_output.m_objects.size() != count)
			m_events->OnStoreChange({ reason, {&id, 1}, nullptr, false });

		// If that was the last object for the source, remove the source too
		if (src->m_output.m_objects.empty())
			Remove(id);
	}

	// Remove all objects associated with 'context_ids'
	void ScriptSources::Remove(std::span<Guid const> include, std::span<Guid const> exclude, EDataChangedReason reason)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Build the set of ids to remove
		GuidCont removed;
		for (auto& src : m_srcs)
		{
			auto id = src.second->m_context_id;
			if (!IncludeFilter(id, include, exclude)) continue;
			removed.push_back(id);
		}

		// Notify of the object container about to change
		if (!removed.empty())
		{
			m_events->OnStoreChange({ reason, removed, nullptr, true });
		}

		// Remove the sources
		for (auto& id : removed)
		{
			// Delete any associated files and watches
			m_watcher.RemoveAll(id);

			// Delete the source and its associated objects
			m_srcs.erase(id);
		}

		// Notify of the object container change
		if (!removed.empty())
		{
			m_events->OnStoreChange({ reason, removed, nullptr, false });
		}
	}
	void ScriptSources::Remove(Guid const& context_id, EDataChangedReason reason)
	{
		Remove({ &context_id, 1 }, {}, reason);
	}

	// Reload a range of sources
	void ScriptSources::Reload(std::span<Guid const> ids)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		pr::vector<SourceBase*> srcs;
		for (auto& id : ids)
			srcs.push_back(m_srcs[id].get());

		m_events->OnStoreChange({ EDataChangedReason::Reload, ids, nullptr, true });

		// Reload each source in a background thread
		std::for_each(std::execution::par_unseq, std::begin(srcs), std::end(srcs), [this](auto* src)
		{
			src->Load(rdr(), EDataChangedReason::Reload, nullptr);
		});

		m_events->OnStoreChange({ EDataChangedReason::Reload, ids, nullptr, false });
	}

	// Reload all sources
	void ScriptSources::Reload()
	{
		GuidCont ids;
		for (auto& src : m_srcs)
			ids.push_back(src.first);

		Reload(ids);
	}

	// Check all file sources for modifications and reload any that have changed
	void ScriptSources::RefreshChangedFiles()
	{
		m_watcher.CheckForChangedFiles();
	}

	// Add an object created externally
	Guid ScriptSources::Add(LdrObjectPtr object)
	{
		auto src = std::shared_ptr<SourceBase>(new SourceBase{ &object->m_context_id });
		src->m_output.m_objects.push_back(object);
		src->Notify += std::bind(&ScriptSources::SourceNotifyHandler, this, _1, _2);
		src->Load(rdr(), EDataChangedReason::NewData, nullptr);
		return src->m_context_id;
	}

	// Parse a string containing ldr script.
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	template <typename Char>
	Guid ScriptSources::AddString(std::basic_string_view<Char> script, EEncoding enc, Guid const* context_id, PathResolver const& includes, AddCompleteCB add_complete) // worker thread context
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		auto src = std::shared_ptr<SourceString<Char>>(new SourceString<Char>(context_id, script, enc, includes));
		src->Notify += std::bind(&ScriptSources::SourceNotifyHandler, this, _1, _2);
		src->Load(rdr(), EDataChangedReason::NewData, add_complete);
		return src->m_context_id;
	}
	template Guid ScriptSources::AddString<wchar_t>(std::wstring_view script, EEncoding enc, Guid const* context_id, PathResolver const& includes, AddCompleteCB add_complete);
	template Guid ScriptSources::AddString<char>(std::string_view script, EEncoding enc, Guid const* context_id, PathResolver const& includes, AddCompleteCB add_complete);

	// Parse file containing ldr script.
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	Guid ScriptSources::AddFile(std::filesystem::path filepath, EEncoding enc, Guid const* context_id, PathResolver const& includes, AddCompleteCB add_complete) // worker thread context
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		auto src = std::shared_ptr<SourceFile>(new SourceFile{ context_id, filepath, enc, includes });
		src->Notify += std::bind(&ScriptSources::SourceNotifyHandler, this, _1, _2);
		src->Load(rdr(), EDataChangedReason::NewData, add_complete);
		return src->m_context_id;
	}

	// Parse binary data containing ldraw script
	// This function can be called from any thread and may be called concurrently by multiple threads.
	// Returns the GUID of the context that the objects were added to.
	Guid ScriptSources::AddBinary(std::span<std::byte const> data, Guid const* context_id, AddCompleteCB add_complete)
	{
		// Note: when called from a worker thread, this function returns after objects have
		// been created, but before they've been added to the main 'm_srcs' collection.
		// The 'on_add' callback function should be used as a continuation function.
		auto src = std::shared_ptr<SourceBinary>(new SourceBinary{ context_id, data });
		src->Notify += std::bind(&ScriptSources::SourceNotifyHandler, this, _1, _2);
		src->Load(rdr(), EDataChangedReason::NewData, add_complete);
		return src->m_context_id;
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
								auto src = std::shared_ptr<SourceStream>(new SourceStream{ nullptr, &rdr(), std::move(client), client_addr });
								src->Notify += std::bind(&ScriptSources::SourceNotifyHandler, this, _1, _2);
								src->Load(rdr(), EDataChangedReason::NewData, nullptr);
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
			clone->Notify += std::bind(&ScriptSources::SourceNotifyHandler, this, _1, _2);
			clone->Load(rdr(), EDataChangedReason::Reload, nullptr);
		}).detach();
	}

	// Handler for when new data is received from a source
	void ScriptSources::SourceNotifyHandler(std::shared_ptr<SourceBase> source, NotifyEventArgs const& args)
	{
		// Marshal to the main thread
		if (std::this_thread::get_id() != m_main_thread_id)
		{
			return rdr().RunOnMainThread([this, source, args = args]() mutable noexcept
			{
				SourceNotifyHandler(source, args);
			});
		}

		// Should be on the main thread now
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto context_id = source->m_context_id;

		switch (args.m_reason)
		{
			case ENotifyReason::LoadComplete:
			{
				// Remove 'context_id' from the "currently loading" set.
				// Don't remove previous objects associated with 'context',
				// leave that to the caller via the 'on_add' callback.  ?? TODO or not?
				m_loading.erase(context_id);

				// Notify of the store about to change
				m_events->OnStoreChange({ args.m_trigger, { &context_id, 1 }, &source->m_output, true });
				if (args.m_add_complete) args.m_add_complete(context_id, true);

				// Add any dependent files to the file watcher
				for (auto& fp : source->m_filepaths)
					m_watcher.Add(fp.c_str(), this, context_id);

				// Notify of any errors that occurred
				for (auto& err : source->m_errors)
					m_events->OnError(err);

				// Update the store
				auto& src = m_srcs[context_id];
				if (src == nullptr)
					src = source;
				else if (src != source)
					src->m_output += std::move(source->m_output);

				// Notify of the store change
				m_events->OnStoreChange({ args.m_trigger, { &context_id, 1 }, &src->m_output, false });
				if (args.m_add_complete) args.m_add_complete(context_id, false);

				// Process any commands
				if (!src->m_output.m_commands.empty())
					m_events->OnHandleCommands(*src);

				break;
			}
			case ENotifyReason::Disconnected:
			{
				// 'source' has disconnected.
				Remove(source->m_context_id);
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown notify reason");
			}
		}
	}
}
