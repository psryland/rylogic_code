//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_MATERIAL_EFFECTS_H
#define PR_RDR_MATERIAL_EFFECTS_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/materials/effects/fragments.h"
#include "pr/renderer/renderstates/renderstate.h"

namespace pr
{
	namespace rdr
	{
		// A class representing an effect
		struct Effect
			:pr::RefCount<Effect>
			,pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			D3DPtr<ID3DXEffect>   m_effect;          // The d3d effect handle
			MaterialManager*      m_mat_mgr;         // The material manager that created this effect
			RdrId                 m_id;              // An id for the compiled effect
			rs::Block             m_rsb;             // Render state for this effect
			pr::GeomType          m_geom_type;       // The geometry type this effect is intended for
			pr::uint16            m_sort_id;         // Sort key for the effect
			effect::frag::Buffer  m_buf;             // A memory buffer containing the effect fragments
			string32              m_name;            // Name of the effect (for debugging mainly)
			
			// Effects are created and owned by the material manager.
			// This is so that pointers (i.e. "handles") to pr::rdr::Effect's can
			// be passed out from dlls, to C#, etc.
			Effect();
			
			// Set the parameters for this effect
			effect::frag::Header const* Frags() const { return effect::frag::Begin(&m_buf[0]); }
			effect::frag::Header*       Frags()       { return effect::frag::Begin(&m_buf[0]); }
			void SetParameters(Viewport const& viewport, DrawListElement const& dle) const;
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&)     { m_effect->OnLostDevice(); }
			void OnEvent(pr::rdr::Evt_DeviceRestored const&) { m_effect->OnResetDevice(); }
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Effect>* doomed);
		};

		// Generate the geometry type that is the minimum requirement for effect composed of 'frags'
		pr::GeomType GenerateMinGeomType(effect::frag::Header const* frags);

		// Generate a simple name for the effect composed of 'frags'
		string32 GenerateEffectName(effect::frag::Header const* frags);
	}
}

#endif
