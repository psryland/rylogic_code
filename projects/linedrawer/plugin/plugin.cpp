//***************************************************************************************************
// Lighting Dialog
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/plugin/plugin.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/main/linedrawer.h"
#include "pr/filesys/filesys.h"
#include "pr/common/hash.h"

// Constructor
ldr::Plugin::Plugin(LineDrawer* ldr, char const* filepath, char const* args)
:m_ldr(ldr)
,m_dll()
,m_filepath(pr::filesys::StandardiseC(pr::filesys::CanonicaliseC<std::string>(filepath)))
,m_name(pr::filesys::GetFiletitle(m_filepath))
,m_args(args)
,m_pi_initialise()
,m_pi_uninitialise()
,m_pi_step()
,m_store()
{
	// Load the dll
	UINT last_error_mode = ::SetErrorMode(SEM_FAILCRITICALERRORS);
	m_dll = ::LoadLibrary(m_filepath.c_str());
	::SetErrorMode(last_error_mode);
	if (!m_dll) throw LdrException(ELdrException::FailedToLoad, pr::Fmt("LoadLibrary called failed for %s",m_filepath.c_str()));

	// Setup the function pointers
	m_pi_initialise     = (ldrapi::Plugin_Initialise  )::GetProcAddress(m_dll, "ldrInitialise");
	m_pi_uninitialise   = (ldrapi::Plugin_Uninitialise)::GetProcAddress(m_dll, "ldrUninitialise");
	m_pi_step           = (ldrapi::Plugin_Step        )::GetProcAddress(m_dll, "ldrStep");
}
ldr::Plugin::~Plugin()
{
	if (m_pi_uninitialise) m_pi_uninitialise();
	::FreeLibrary(m_dll);
}

// Called when the viewport is being built
void ldr::Plugin::OnEvent(pr::rdr::Evt_SceneRender const& e)
{
	// Add instances from the store
	for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
		m_store[i]->AddToScene(e.m_scene);
}

// Call 'm_pi_initialise' to start the plugin.
// This is not done in the constructor as we want the plugin to be
// added to the plugin manager before any client code is run.
void ldr::Plugin::Start()
{
	// Initialise the plugin
	if (m_pi_initialise)
		m_pi_initialise(this, m_args.c_str());
}

// Step the plugin forward by 'elapsed_s'
void ldr::Plugin::Poll(double elapsed_s) const
{
	if (m_pi_step)
		m_pi_step(elapsed_s);
}

// Create one or more objects described by 'object_description'
// The last object created is returned. (hmm, could return a range of objects...)
pr::ldr::LdrObject* ldr::Plugin::RegisterObject(char const* object_description, pr::ldr::ContextId ctx_id, bool async)
{
	size_t initial = m_store.size();
	pr::ldr::AddString(m_ldr->m_rdr, object_description, m_store, ctx_id, async);
	if (initial == m_store.size()) return 0;
	return m_store.back().m_ptr; // Return the pointer to the last object added, other objects will be orphaned
}

// Remove 'object' from the store
void ldr::Plugin::UnregisterObject(pr::ldr::LdrObject* object)
{
	size_t i = 0, iend = m_store.size();
	for (; i != iend && m_store[i].m_ptr != object; ++i) {}
	if (i != iend) m_store.erase(m_store.begin() + i);
}

// Remove all objects from the store
void ldr::Plugin::UnregisterAllObjects()
{
	m_store.resize(0);
}
