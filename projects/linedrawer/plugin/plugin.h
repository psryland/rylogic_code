//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_LDR_PLUGIN_H
#define PR_LDR_PLUGIN_H

#include "linedrawer/main/forward.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/resources/linedrawer.res.h"
#include "pr/linedrawer/ldr_plugin_interface.h"

namespace ldr
{
	// A single dll plugin
	struct Plugin :pr::events::IRecv<pr::rdr::Evt_UpdateScene>
	{
		HMODULE                      m_dll;
		ldr::Main*                   m_ldr;
		std::string                  m_filepath;
		std::string                  m_name;
		std::string                  m_args;
		ldrapi::Plugin_Initialise    m_pi_initialise;
		ldrapi::Plugin_Uninitialise  m_pi_uninitialise;
		ldrapi::Plugin_Step          m_pi_step;
		pr::ldr::ObjectCont          m_store;

		Plugin(ldr::Main* ldr, char const* filepath, char const* args);
		~Plugin();

		// Return a pointer to the name of this plugin
		char const* Name() const { return m_name.c_str(); }

		// Return the full filepath of this plugin dll
		char const* Filepath() const { return m_filepath.c_str(); }

		// Called when the draw list is being built
		void OnEvent(pr::rdr::Evt_UpdateScene const&) override;

		// Call 'm_pi_initialise' to start the plugin.
		// This is not done in the constructor as we want the plugin to be added to be
		// added to the plugin manager before any client code is run.
		void Start();

		// Step the plugin forward by 'elapsed_s'
		void Poll(double elapsed_s) const;

		// Create one or more objects described by 'reader'
		// The last object created is returned. (hmm, could return a range of objects...)
		pr::ldr::LdrObject* Plugin::RegisterObject(pr::script::Reader& reader, pr::ldr::ContextId ctx_id, bool async);

		// Create one or more objects described by 'object_description'
		// The last object created is returned. (hmm, could return a range of objects...)
		template <typename Char> pr::ldr::LdrObject* RegisterObject(Char const* object_description, wchar_t const* include_paths, pr::ldr::ContextId ctx_id, bool async)
		{
			using namespace pr::script;

			Ptr<Char const*> src(object_description);
			FileIncludes<> inc(include_paths);
			Reader reader(src, false, &inc);

			return RegisterObject(reader, ctx_id, async);
		}

		// Remove 'object' from the store
		void UnregisterObject(pr::ldr::LdrObject* object);

		// Remove all objects from the store
		void UnregisterAllObjects();
	};
}
#endif
