//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/materials/effects/effect.h"
#include "pr/renderer/materials/effects/fragments.h"
#include "pr/renderer/materials/material_manager.h"
	
using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;
	
// Generate the geometry type that is the minimum requirement for effect composed of 'frags'
pr::GeomType pr::rdr::GenerateMinGeomType(frag::Header const* frags)
{
	pr::GeomType geom_type = pr::geom::EVertex;
	for (frag::Header const* f = frags; f; f = frag::Inc(f))
	{
		switch (f->m_type)
		{
		default: PR_ASSERT(PR_DBG_RDR, false, "Unknown effect fragment type"); break;
		case frag::EFrag::Txfm:      break;
		case frag::EFrag::Tinting:   break;
		case frag::EFrag::PVC:       geom_type |= pr::geom::EColour; break;
		case frag::EFrag::Texture2D: geom_type |= pr::geom::ETexture; break;
		case frag::EFrag::EnvMap:    geom_type |= pr::geom::ENormal; break;
		case frag::EFrag::Lighting:  geom_type |= pr::geom::ENormal; break;
		case frag::EFrag::SMap:      break;
		}
	}
	return geom_type;
}
	
// Generate a simple name for the effect composed of 'frags'
string32 pr::rdr::GenerateEffectName(effect::frag::Header const* frags)
{
	string32 name;
	pr::uint seen = 0;
	for (frag::Header const* f = frags; f; f = frag::IncUnique(f, seen))
	{
		switch (f->m_type)
		{
		default: PR_ASSERT(PR_DBG_RDR, false, "Unknown effect fragment type"); break;
		case frag::EFrag::Txfm:      name += "Tx"; break;
		case frag::EFrag::Tinting:   name += "Tint"; break;
		case frag::EFrag::PVC:       name += "Pvc"; break;
		case frag::EFrag::Texture2D: name += "Tex"; break;
		case frag::EFrag::EnvMap:    name += "Env"; break;
		case frag::EFrag::Lighting:  name += "Lit"; break;
		case frag::EFrag::SMap:      name += "Smap"; break;
		}
	}
	return name;
}
	
// Effect
pr::rdr::Effect::Effect()
:m_effect()
,m_mat_mgr()
,m_id()
,m_rsb()
,m_geom_type()
,m_sort_id()
,m_buf()
,m_name()
{}
	
//// Find the first technique in the shader supported on this system
//D3DXHANDLE FindBestTechnique(D3DPtr<ID3DXEffect> effect)
//{
//	D3DXHANDLE best_technique  = 0;
//	D3DXHANDLE prev            = 0;
//	D3DXHANDLE technique       = 0;
//
//	// Find the first valid technique
//	while (pr::Succeeded(effect->FindNextValidTechnique(prev, &technique)) && technique != 0)
//	{
//		D3DXTECHNIQUE_DESC description;
//		Verify(effect->GetTechniqueDesc(technique, &description));
//
//		// See if this is a better match
//		if (pr::str::CompareNoCase(description.Name, best_version) >= 0)
//		{
//			best_technique = technique;
//			memcpy(best_version, description.Name, 6);
//		}
//		prev = technique;
//	}
//	return best_technique;
//}
	
// Set the parameters for this effect
void pr::rdr::Effect::SetParameters(Viewport const& viewport, DrawListElement const& dle) const
{
	for (frag::Header const* f = Frags(); f; f = frag::Inc(f))
		f->SetParameters(f, m_effect, viewport, dle);
}
	
// Refcounting cleanup function
void pr::rdr::Effect::RefCountZero(pr::RefCount<Effect>* doomed)
{
	pr::rdr::Effect* eff = static_cast<pr::rdr::Effect*>(doomed);
	eff->m_mat_mgr->DeleteEffect(eff);
}
