//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/materials/effects/fragments.h"
#include "pr/renderer/instances/instance.h"
#include "pr/renderer/viewport/viewport.h"
#include "pr/renderer/viewport/drawlistelement.h"
#include "pr/renderer/renderer.h"


using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;
using namespace pr::rdr::effect::frag;
	
void pr::rdr::effect::frag::Header::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	switch (m_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown shader fragment type"); return;
	case EFrag::Txfm:      return frag_cast<Txfm>     (this)->SetHandles(effect);
	case EFrag::Tinting:   return frag_cast<Tinting>  (this)->SetHandles(effect);
	case EFrag::PVC:       return;// frag_cast<PVC>      (this)->SetHandles(effect);
	case EFrag::Texture2D: return frag_cast<Texture2D>(this)->SetHandles(effect);
	case EFrag::EnvMap:    return frag_cast<EnvMap>   (this)->SetHandles(effect);
	case EFrag::Lighting:  return frag_cast<Lighting> (this)->SetHandles(effect);
	case EFrag::SMap:      return frag_cast<SMap>     (this)->SetHandles(effect);
	}
}
void pr::rdr::effect::frag::Header::AddTo(Desc& desc) const
{
	switch (m_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown shader fragment type"); return;
	case EFrag::Txfm:      return frag_cast<Txfm>     (this)->AddTo(desc);
	case EFrag::Tinting:   return frag_cast<Tinting>  (this)->AddTo(desc);
	case EFrag::PVC:       return frag_cast<PVC>      (this)->AddTo(desc);
	case EFrag::Texture2D: return frag_cast<Texture2D>(this)->AddTo(desc);
	case EFrag::EnvMap:    return frag_cast<EnvMap>   (this)->AddTo(desc);
	case EFrag::Lighting:  return frag_cast<Lighting> (this)->AddTo(desc);
	case EFrag::SMap:      return frag_cast<SMap>     (this)->AddTo(desc);
	}
}
void pr::rdr::effect::frag::Header::Variables(ShaderBuffer& data) const
{
	switch (m_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown shader fragment type"); return;
	case EFrag::Txfm:      return Txfm       ::Variables(this, data);
	case EFrag::Tinting:   return Tinting    ::Variables(this, data);
	case EFrag::PVC:       return;//PVC      ::Variables(this, data);
	case EFrag::Texture2D: return Texture2D  ::Variables(this, data);
	case EFrag::EnvMap:    return EnvMap     ::Variables(this, data);
	case EFrag::Lighting:  return Lighting   ::Variables(this, data);
	case EFrag::SMap:      return SMap       ::Variables(this, data);
	}
}
void pr::rdr::effect::frag::Header::Functions(ShaderBuffer& data) const
{
	switch (m_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown shader fragment type"); return;
	case EFrag::Txfm:      return Txfm       ::Functions(this, data);
	case EFrag::Tinting:   return;//Tinting  ::Functions(this, data);
	case EFrag::PVC:       return;//PVC      ::Functions(this, data);
	case EFrag::Texture2D: return;//Texture2D::Functions(this, data);
	case EFrag::EnvMap:    return EnvMap     ::Functions(this, data);
	case EFrag::Lighting:  return Lighting   ::Functions(this, data);
	case EFrag::SMap:      return SMap       ::Functions(this, data);
	}
}
void pr::rdr::effect::frag::Header::VSfragment(ShaderBuffer& data, int vs_idx) const
{
	switch (m_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown shader fragment type"); return;
	case EFrag::Txfm:      return Txfm       ::VSfragment(this, data, vs_idx);
	case EFrag::Tinting:   return Tinting    ::VSfragment(this, data, vs_idx);
	case EFrag::PVC:       return PVC        ::VSfragment(this, data, vs_idx);
	case EFrag::Texture2D: return Texture2D  ::VSfragment(this, data, vs_idx);
	case EFrag::EnvMap:    return;//EnvMap   ::VSfragment(this, data, vs_idx);
	case EFrag::Lighting:  return;//Lighting ::VSfragment(this, data, vs_idx);
	case EFrag::SMap:      return SMap       ::VSfragment(this, data, vs_idx);
	}
}
void pr::rdr::effect::frag::Header::PSfragment(ShaderBuffer& data, int ps_idx) const
{
	switch (m_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown shader fragment type"); return;
	case EFrag::Txfm:      return Txfm      ::PSfragment(this, data, ps_idx);
	case EFrag::Tinting:   return Tinting   ::PSfragment(this, data, ps_idx);
	case EFrag::PVC:       return PVC       ::PSfragment(this, data, ps_idx);
	case EFrag::Texture2D: return Texture2D ::PSfragment(this, data, ps_idx);
	case EFrag::EnvMap:    return EnvMap    ::PSfragment(this, data, ps_idx);
	case EFrag::Lighting:  return Lighting  ::PSfragment(this, data, ps_idx);
	case EFrag::SMap:      return SMap      ::PSfragment(this, data, ps_idx);
	}
}
	
// Txfm Fragment *****************************************************
pr::rdr::effect::frag::Txfm::Txfm()
:m_header(Header::make<Txfm>())
,m_object_to_world()
,m_norm_to_world()
,m_object_to_screen()
,m_world_to_camera()
,m_camera_to_world()
,m_camera_to_screen()
{}
void pr::rdr::effect::frag::Txfm::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	m_object_to_world  = effect->GetParameterByName(0, "g_object_to_world"  );
	m_norm_to_world    = effect->GetParameterByName(0, "g_norm_to_world"    );
	m_object_to_screen = effect->GetParameterByName(0, "g_object_to_screen" );
	m_world_to_camera  = effect->GetParameterByName(0, "g_world_to_camera"  );
	m_camera_to_world  = effect->GetParameterByName(0, "g_camera_to_world"  );
	m_camera_to_screen = effect->GetParameterByName(0, "g_camera_to_screen" );
}
void pr::rdr::effect::frag::Txfm::AddTo(Desc& desc) const
{
	desc.m_vsout[0].Add(ESemantic::Position  ,"float4" ,"pos"     ,"0");
	desc.m_vsout[0].Add(ESemantic::TexCoord0 ,"float4" ,"ws_pos"  ,"0");
	desc.m_vsout[0].Add(ESemantic::TexCoord1 ,"float4" ,"ws_norm" ,"0");
	desc.m_psout[0].Add(ESemantic::Color0    ,"float4" ,"diff"    ,"1");
}
void pr::rdr::effect::frag::Txfm::Variables(void const*, ShaderBuffer& data)
{
	data +=
		"// Txfm variables\n"
		"shared uniform float4x4 g_object_to_world  :World;\n"
		"shared uniform float4x4 g_norm_to_world    :World;\n"
		"shared uniform float4x4 g_object_to_screen :WorldViewProjection;\n"
		"shared uniform float4x4 g_camera_to_world  :ViewInverse;\n"
		"shared uniform float4x4 g_world_to_camera  :View;\n"
		"shared uniform float4x4 g_camera_to_screen :Projection;\n"
		"\n";
}
void pr::rdr::effect::frag::Txfm::Functions(void const*, ShaderBuffer& data)
{
	data +=
		"// Txfm functions\n"
		"float4 WSCameraPosition()                { return g_camera_to_world[3]; }\n"
		"float4 ObjectToWorld(in float4 os_vec)   { return mul(os_vec, g_object_to_world); }\n"
		"float4 NormToWorld(in float4 os_norm)    { return mul(os_norm, g_norm_to_world); }\n"
		"float4 ObjectToScreen(in float4 os_vec)  { return mul(os_vec, g_object_to_screen); }\n"
		"float4 ObjectToCamera(in float4 os_vec)  { return mul(os_vec, mul(g_object_to_world, g_world_to_camera)); }\n"
		"float4 CameraToScreen(in float4 cs_vec)  { return mul(cs_vec, g_camera_to_screen); }\n"
		"\n";
}
void pr::rdr::effect::frag::Txfm::VSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// Txfm\n"
		"	Out.pos     = ObjectToScreen(ms_pos);\n"
		"	Out.ws_pos  = ObjectToWorld(ms_pos);\n"
		"	Out.ws_norm = NormToWorld(ms_norm);\n"
		"\n";
}
void pr::rdr::effect::frag::Txfm::PSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// Txfm\n"
		"	In.ws_norm = normalize(In.ws_norm);\n"
		"\n";
}
void pr::rdr::effect::frag::Txfm::SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle)
{
	// Cache the last set transforms to prevent setting them unnecessarily
	static m4x4 last_i2w;
	static m4x4 last_i2s;
	static m4x4 last_w2c;
	static m4x4 last_c2w;
	static m4x4 last_c2s;

	pr::m4x4 i2w = GetI2W(*dle.m_instance);
	pr::m4x4 w2c = viewport.WorldToCamera();
	pr::m4x4 c2s; if (!FindC2S(*dle.m_instance, c2s)) { c2s = viewport.CameraToScreen(); }

	// Determine which transforms need updating
	enum { EI2W = 1<<0, EI2S = 1<<1, EW2C = 1<<2, EC2W = 1<<3, EC2S = 1<<4 };
	pr::uint cache_state = 0;
	if (last_i2w != i2w)
	{
		last_i2w = i2w;
		last_i2s = c2s * w2c * i2w;
		cache_state |= EI2W|EI2S;
	}
	if (last_w2c != w2c)
	{
		if (!(cache_state&EI2S)) last_i2s = c2s * w2c * i2w;
		last_w2c = w2c;
		last_c2w = GetInverseFast(w2c);
		cache_state |= EI2S|EW2C|EC2W;
	}
	if (last_c2s != c2s)
	{
		if (!(cache_state&EI2S)) last_i2s = c2s * w2c * i2w;
		last_c2s = c2s;
		cache_state |= EI2S|EC2S;
	}

	D3DPtr<IDirect3DDevice9> d3ddevice = viewport.Rdr().D3DDevice();

	// Set the handles based on cache status
	Txfm const& me = *frag_cast<Txfm>(fragment);
	if (cache_state & EI2W)
	{
		PR_ASSERT(PR_DBG_RDR, FEql(last_i2w.w.w, 1.0f), "Invalid instance to world transform found");
		pr::m4x4 n2w = last_i2w;
		n2w.x.w = 0.0f; if (!FEqlZero3(n2w.x)) Normalise3(n2w.x);
		n2w.y.w = 0.0f; if (!FEqlZero3(n2w.y)) Normalise3(n2w.y);
		n2w.z.w = 0.0f; if (!FEqlZero3(n2w.z)) Normalise3(n2w.z);
		n2w.w = pr::v4Origin;
		Verify(d3ddevice->SetTransform(D3DTS_WORLD    ,&reinterpret_cast<const D3DXMATRIX&>(last_i2w)));
		Verify(effect->SetMatrix(me.m_object_to_world ,&reinterpret_cast<const D3DXMATRIX&>(last_i2w)));
		Verify(effect->SetMatrix(me.m_norm_to_world   ,&reinterpret_cast<const D3DXMATRIX&>(n2w)));
	}
	if (cache_state & EI2S)
	{
		Verify(effect->SetMatrix(me.m_object_to_screen ,&reinterpret_cast<const D3DXMATRIX&>(last_i2s)));
	}
	if (cache_state & EW2C)
	{
		PR_ASSERT(PR_DBG_RDR, FEql(last_w2c.w.w, 1.0f), "Invalid world to camera transform found");
		Verify(d3ddevice->SetTransform(D3DTS_VIEW     ,&reinterpret_cast<const D3DXMATRIX&>(last_w2c)));
		Verify(effect->SetMatrix(me.m_world_to_camera ,&reinterpret_cast<const D3DXMATRIX&>(last_w2c)));
	}
	if (cache_state & EC2W)
	{
		Verify(effect->SetMatrix(me.m_camera_to_world ,&reinterpret_cast<const D3DXMATRIX&>(last_c2w)));
	}
	if (cache_state & EC2S)
	{
		Verify(d3ddevice->SetTransform(D3DTS_PROJECTION ,&reinterpret_cast<const D3DXMATRIX&>(last_c2s)));
		Verify(effect->SetMatrix(me.m_camera_to_screen  ,&reinterpret_cast<const D3DXMATRIX&>(last_c2s)));
	}
}
	
// Tinting Fragment *****************************************************
pr::rdr::effect::frag::Tinting::Tinting(int tint_index, EStyle style)
:m_header(Header::make<Tinting>())
,m_tint_index(tint_index)
,m_style(style)
,m_tint_colour()
{}
void pr::rdr::effect::frag::Tinting::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	m_tint_colour = effect->GetParameterByName(0, FmtS("g_tint_colour%d", m_tint_index));
}
void pr::rdr::effect::frag::Tinting::AddTo(Desc& desc) const
{
	desc.m_vsout[0].Add(ESemantic::Color0 ,"float4" ,"diff" ,"1");
	desc.m_psout[0].Add(ESemantic::Color0 ,"float4" ,"diff" ,"1");
}
void pr::rdr::effect::frag::Tinting::Variables(void const* fragment, ShaderBuffer& data)
{
	Tinting const& me = *frag_cast<Tinting>(fragment);
	data += pr::FmtS(
		"// Tinting variables\n"
		"shared uniform float4 g_tint_colour%1$d = float4(1,1,1,1);\n"
		"\n" ,me.m_tint_index);
}
void pr::rdr::effect::frag::Tinting::VSfragment(void const* fragment, ShaderBuffer& data, int)
{
	Tinting const& me = *frag_cast<Tinting>(fragment);
	switch (me.m_style)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown tinting style"); return;
	case EStyle_Tint: data += pr::FmtS(
		"	// Tinting\n"
		"	Out.diff = g_tint_colour%1$d;\n"
		"\n" ,me.m_tint_index);
		break;
	case EStyle_Tint_x_Diff: data += pr::FmtS(
		"	// Tinting\n"
		"	Out.diff = g_tint_colour%1$d * Out.diff;\n"
		"\n" ,me.m_tint_index);
		break;
	}
}
void pr::rdr::effect::frag::Tinting::PSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// Tinting\n"
		"	Out.diff = In.diff;\n"
		"\n";
}
void pr::rdr::effect::frag::Tinting::SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const&, DrawListElement const& dle)
{
	using namespace pr::rdr::instance;

	Tinting const& me = *frag_cast<Tinting>(fragment);
	pr::Colour32 const* tint_colour32 = FindCpt<pr::Colour32>(*dle.m_instance, ECpt_TintColour32, me.m_tint_index);
	pr::Colour          tint_colour   = tint_colour32 ? pr::Colour::make(*tint_colour32) : ColourWhite;
	Verify(effect->SetFloatArray(me.m_tint_colour, tint_colour.ToArray(), 4));
}
	
// PVC Fragment *****************************************************
pr::rdr::effect::frag::PVC::PVC(EStyle style)
:m_header(Header::make<PVC>())
,m_style(style)
{}
void pr::rdr::effect::frag::PVC::AddTo(Desc& desc) const
{
	desc.m_vsout[0].Add(ESemantic::Color0 ,"float4" ,"diff" ,"1");
	desc.m_psout[0].Add(ESemantic::Color0 ,"float4" ,"diff" ,"1");
}
void pr::rdr::effect::frag::PVC::VSfragment(void const* fragment, ShaderBuffer& data, int)
{
	PVC const& me = *frag_cast<PVC>(fragment);
	switch (me.m_style)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "unknown pvc style"); return;
	case EStyle_PVC: data +=
		"	// PVC\n"
		"	Out.diff = In.diff;\n"
		"\n";
		break;
	case EStyle_PVC_x_Diff: data +=
		"	// PVC\n"
		"	Out.diff = In.diff * Out.diff;\n"
		"\n";
		break;
	}
}
void pr::rdr::effect::frag::PVC::PSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// PVC\n"
		"	Out.diff = In.diff;\n"
		"\n";
}
	
// Texturing Fragment ************************************************
pr::rdr::effect::frag::Texture2D::Texture2D(int tex_index, EStyle style)
:m_header(Header::make<Texture2D>())
,m_tex_index(tex_index)
,m_style(style)
,m_texture()
,m_tex_to_surf()
,m_mip_filter()
,m_min_filter()
,m_mag_filter()
,m_addrU()
,m_addrV()
{}
void pr::rdr::effect::frag::Texture2D::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	m_texture     = effect->GetParameterByName(0, FmtS("g_texture%d" ,m_tex_index));
	m_tex_to_surf = effect->GetParameterByName(0, FmtS("g_texture%d_to_surf", m_tex_index));
	m_mip_filter  = effect->GetParameterByName(0, FmtS("g_texture%d_mip_filter" ,m_tex_index));
	m_min_filter  = effect->GetParameterByName(0, FmtS("g_texture%d_min_filter" ,m_tex_index));
	m_mag_filter  = effect->GetParameterByName(0, FmtS("g_texture%d_mag_filter" ,m_tex_index));
	m_addrU       = effect->GetParameterByName(0, FmtS("g_texture%d_addrU" ,m_tex_index));
	m_addrV       = effect->GetParameterByName(0, FmtS("g_texture%d_addrV" ,m_tex_index));
}
void pr::rdr::effect::frag::Texture2D::AddTo(Desc& desc) const
{
	ESemantic::Type sem = static_cast<ESemantic::Type>(ESemantic::TexCoord2 + m_tex_index);
	char name[] = "tex0"; name[sizeof(name)-1] += char(m_tex_index);
	desc.m_vsout[0].Add(sem               ,"float2" ,name   ,"0");
	desc.m_psout[0].Add(ESemantic::Color0 ,"float4" ,"diff" ,"1");
}
void pr::rdr::effect::frag::Texture2D::Variables(void const* fragment, ShaderBuffer& data)
{
	Texture2D const& me = *frag_cast<Texture2D>(fragment);
	data += pr::FmtS(
		"// Texture2D variables\n"
		"shared texture2D g_texture%1$d = NULL;\n"
		"shared uniform float4x4 g_texture%1$d_to_surf = float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);\n"
		"shared uniform DWORD g_texture%1$d_mip_filter = 2;\n"
		"shared uniform DWORD g_texture%1$d_min_filter = 2;\n"
		"shared uniform DWORD g_texture%1$d_mag_filter = 2;\n"
		"shared uniform DWORD g_texture%1$d_addrU      = 3;\n"
		"shared uniform DWORD g_texture%1$d_addrV      = 3;\n"
		"sampler2D g_sampler_texture%1$d = sampler_state { Texture=<g_texture%1$d>; MipFilter=<g_texture%1$d_mip_filter>; MinFilter=<g_texture%1$d_min_filter>; MagFilter=<g_texture%1$d_mag_filter>; AddressU=<g_texture%1$d_addrU>; AddressV=<g_texture%1$d_addrV>;};\n"
		"\n"
		,me.m_tex_index
		);
}
void pr::rdr::effect::frag::Texture2D::VSfragment(void const* fragment, ShaderBuffer& data, int)
{
	Texture2D const& me = *frag_cast<Texture2D>(fragment);
	data += pr::FmtS(
		"	// Texture2D\n"
		"	Out.tex%1$d = mul(float4(In.tex%1$d,0,1), g_texture%1$d_to_surf).xy;\n"
		"\n" ,me.m_tex_index);
}
void pr::rdr::effect::frag::Texture2D::PSfragment(void const* fragment, ShaderBuffer& data, int)
{
	Texture2D const& me = *frag_cast<Texture2D>(fragment);
	switch (me.m_style)
	{
	case EStyle_Tex: data += pr::FmtS(
		"	// Texture2D\n"
		"	Out.diff = tex2D(g_sampler_texture%1$d, In.tex%1$d);\n"
		"\n" ,me.m_tex_index);
		break;
	case EStyle_Tex_x_Diff: data += pr::FmtS(
		"	// Texture2D\n"
		"	Out.diff = tex2D(g_sampler_texture%1$d, In.tex%1$d) * Out.diff;\n"
		"\n" ,me.m_tex_index);
		break;
	}
}
void pr::rdr::effect::frag::Texture2D::SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle)
{
	Texture2D const& me = *frag_cast<Texture2D>(fragment);
	Material const& material = dle.m_nugget->m_material;
	pr::rdr::TexturePtr tex = 0;
	if (material.m_diffuse_texture) tex = material.m_diffuse_texture;
	else                            tex = viewport.Rdr().m_mat_mgr.FindTexture(EStockTexture::White);
	if (!tex)
	{
		Verify(effect->SetTexture (me.m_texture     ,0));
		Verify(effect->SetMatrix  (me.m_tex_to_surf ,&reinterpret_cast<const D3DXMATRIX&>(pr::m4x4Identity)));
	}
	else
	{
		Verify(effect->SetTexture (me.m_texture     ,tex->m_tex.m_ptr));
		Verify(effect->SetMatrix  (me.m_tex_to_surf ,&reinterpret_cast<const D3DXMATRIX&>(tex->m_t2s)));
		Verify(effect->SetInt     (me.m_mip_filter  ,tex->m_filter.m_mip));
		Verify(effect->SetInt     (me.m_min_filter  ,tex->m_filter.m_min));
		Verify(effect->SetInt     (me.m_mag_filter  ,tex->m_filter.m_mag));
		Verify(effect->SetInt     (me.m_addrU       ,tex->m_addr_mode.m_addrU));
		Verify(effect->SetInt     (me.m_addrV       ,tex->m_addr_mode.m_addrV));
	}
}
	
// EnvMap Fragment ***********************************************
pr::rdr::effect::frag::EnvMap::EnvMap()
:m_header(Header::make<EnvMap>())
,m_texture()
{}
void pr::rdr::effect::frag::EnvMap::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	m_texture = effect->GetParameterByName(0, "g_envmap");
}
void pr::rdr::effect::frag::EnvMap::AddTo(Desc& desc) const
{
	desc.m_vsout[0].Add(ESemantic::TexCoord0 ,"float4" ,"ws_pos"  ,"0");
	desc.m_vsout[0].Add(ESemantic::TexCoord1 ,"float4" ,"ws_norm" ,"0");
	desc.m_psout[0].Add(ESemantic::Color0    ,"float4" ,"diff"    ,"1");
}
void pr::rdr::effect::frag::EnvMap::Variables(void const*, ShaderBuffer& data)
{
	data +=
		"// EnvMap variables\n"
		"shared uniform float g_envmap_blend_fraction = 0;\n"
		"shared textureCUBE g_envmap :Environment = NULL;\n"
		"samplerCUBE g_sampler_envmap = sampler_state { Texture=<g_envmap>; MipFilter=Linear; MinFilter=Linear; MagFilter=Linear; };\n"
		"\n";
}
void pr::rdr::effect::frag::EnvMap::Functions(void const*, ShaderBuffer& data)
{
	data +=
		"// EnvMap functions\n"
		"float4 EnvMap(in float4 ws_pos, in float4 ws_norm, in float4 unenvmapped_diff)\n"
		"{\n"
		"	if (g_envmap_blend_fraction < 0.01) return unenvmapped_diff;\n"
		"	float4 ws_toeye_norm = normalize(WSCameraPosition() - ws_pos);\n"
		"	float4 ws_env        = reflect(-ws_toeye_norm, ws_norm);\n"
		"	float4 env           = texCUBE(g_sampler_envmap, ws_env.xyz);\n"
		"	return lerp(unenvmapped_diff, env, g_envmap_blend_fraction);\n"
		"}\n"
		"\n";
}
void pr::rdr::effect::frag::EnvMap::PSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// EnvMap\n"
		"	Out.diff = EnvMap(In.ws_pos, In.ws_norm, Out.diff);\n"
		"\n";
}
void pr::rdr::effect::frag::EnvMap::SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const&, DrawListElement const& dle)
{
	EnvMap const& me = *frag_cast<EnvMap>(fragment);
	Material const& material = dle.m_nugget->m_material;
	IDirect3DTexture9* tex = material.m_envmap_texture ? material.m_envmap_texture->m_tex.m_ptr : 0;
	Verify(effect->SetTexture(me.m_texture, tex));
}
	
// Lighting Fragment *****************************************************
pr::rdr::effect::frag::Lighting::Lighting(int light_count, int caster_count, bool specular)
:m_header(Header::make<Lighting>())
,m_light_count(light_count)
,m_caster_count(caster_count)
,m_specular(specular)
,m_light_type()
,m_ws_light_position()
,m_ws_light_direction()
,m_light_ambient()
,m_light_diffuse()
,m_light_specular()
,m_specular_power()
,m_spot_inner_cosangle()
,m_spot_outer_cosangle()
,m_spot_range()
,m_world_to_smap()
,m_cast_shadows()
,m_smap_frust()
,m_smap_frust_dim()
,m_smap()
{}
void pr::rdr::effect::frag::Lighting::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	m_light_type          = effect->GetParameterByName(0, "g_light_type"         );
	m_ws_light_position   = effect->GetParameterByName(0, "g_ws_light_position"  );
	m_ws_light_direction  = effect->GetParameterByName(0, "g_ws_light_direction" );
	m_light_ambient       = effect->GetParameterByName(0, "g_light_ambient"      );
	m_light_diffuse       = effect->GetParameterByName(0, "g_light_diffuse"      );
	if (m_specular)
	{
		m_light_specular  = effect->GetParameterByName(0, "g_light_specular"     );
		m_specular_power  = effect->GetParameterByName(0, "g_specular_power"     );
	}
	m_spot_inner_cosangle = effect->GetParameterByName(0, "g_spot_inner_cosangle");
	m_spot_outer_cosangle = effect->GetParameterByName(0, "g_spot_outer_cosangle");
	m_spot_range          = effect->GetParameterByName(0, "g_spot_range"         );
	if (m_caster_count != 0)
	{
		m_cast_shadows    = effect->GetParameterByName(0, "g_cast_shadows"       );
		m_smap_frust      = effect->GetParameterByName(0, "g_smap_frust"         );
		m_smap_frust_dim  = effect->GetParameterByName(0, "g_smap_frust_dim"     );
		for (int i = 0; i != m_caster_count; ++i)
			m_smap[i]     = effect->GetParameterByName(0, FmtS("g_smap%d",i)     );
	}
}
void pr::rdr::effect::frag::Lighting::AddTo(Desc& desc) const
{
	if (m_caster_count != 0)
	desc.m_vsout[0].Add(ESemantic::TexCoord0 ,"float4" ,"ws_pos"  ,"0");
	desc.m_vsout[0].Add(ESemantic::TexCoord1 ,"float4", "ws_norm" ,"0");
	desc.m_psout[0].Add(ESemantic::Color0    ,"float4", "diff"    ,"1");
}
void pr::rdr::effect::frag::Lighting::Variables(void const* fragment, ShaderBuffer& data)
{
	Lighting const& me = *frag_cast<Lighting>(fragment);
	data += pr::FmtS(
		"// Lighting variables *********************\n"
		"#define LightCount %1$d\n"
		"shared uniform int    g_light_type         [LightCount];\n"
		"shared uniform float4 g_ws_light_position  [LightCount] :Position ;\n"
		"shared uniform float4 g_ws_light_direction [LightCount] :Direction;\n"
		"shared uniform float4 g_light_ambient      [LightCount] :Ambient;\n"
		"shared uniform float4 g_light_diffuse      [LightCount] :Diffuse;\n"
		"shared uniform float  g_spot_inner_cosangle[LightCount];\n"
		"shared uniform float  g_spot_outer_cosangle[LightCount];\n"
		"shared uniform float  g_spot_range         [LightCount];\n"
		,me.m_light_count);

	if (me.m_specular) data +=
		"shared uniform float4 g_light_specular     [LightCount] :Specular;\n"
		"shared uniform float  g_specular_power     [LightCount] :SpecularPower;\n";

	if (me.m_caster_count != 0)
	{
		data += pr::FmtS(
			"// ShadowMap variables *********************\n"
			"#define SMapCasters %1$d\n"
			"#define SMapTexSize %2$d\n"
			"#define SMapEps 0.01f\n"
			"shared uniform int g_cast_shadows[LightCount];\n"
			"shared uniform float4x4 g_smap_frust;\n"
			"shared uniform float4   g_smap_frust_dim;\n"
			,me.m_caster_count ,SMap::TexSize);
		for (int i = 0; i != me.m_caster_count; ++i) data += pr::FmtS(
			"shared texture g_smap%d = NULL;\n",i);
		data +=
			"sampler2D g_sampler_smap[SMapCasters] =\n"
			"{\n";
		for (int i = 0; i != me.m_caster_count; ++i) data += pr::FmtS(
			"	sampler_state {Texture=<g_smap%d>; MinFilter=Point; MagFilter=Point; MipFilter=Point; AddressU=Clamp; AddressV = Clamp;},\n",i);
		data +=
			"};\n"
			"\n";
	}
}
void pr::rdr::effect::frag::Lighting::Functions(void const* fragment, ShaderBuffer& data)
{
	Lighting const& me = *frag_cast<Lighting>(fragment);

	// Get SMap to write it's functions
	if (me.m_caster_count != 0)
		SMap::Functions(0, data);

	data +=
		"// Lighting functions\n"
		"float LightDirectional(in float4 ws_light_direction, in float4 ws_norm, in float alpha)\n"
		"{\n"
		"	float brightness = -dot(ws_light_direction, ws_norm);\n"
		"	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);\n"
		"	return saturate(brightness);\n"
		"}\n"
		"float LightPoint(in float4 ws_light_position, in float4 ws_norm, in float4 ws_pos, in float alpha)\n"
		"{\n"
		"	float4 light_to_pos = ws_pos - ws_light_position;\n"
		"	float dist = length(light_to_pos);\n"
		"	float brightness = -dot(light_to_pos, ws_norm) / dist;\n"
		"	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);\n"
		"	return saturate(brightness);\n"
		"}\n"
		"float LightSpot(in float4 ws_light_position, in float4 ws_light_direction, in float inner_cosangle, in float outer_cosangle, in float range, in float4 ws_norm, in float4 ws_pos, in float alpha)\n"
		"{\n"
		"	float brightness = LightPoint(ws_light_position, ws_norm, ws_pos, alpha);\n"
		"	float4 light_to_pos = ws_pos - ws_light_position;\n"
		"	float dist = length(light_to_pos);\n"
		"	float cos_angle = saturate(dot(light_to_pos, ws_light_direction) / dist);\n"
		"	brightness *= saturate((outer_cosangle - cos_angle) / (outer_cosangle - inner_cosangle));\n"
		"	brightness *= saturate((range - dist) * 9 / range);\n"
		"	return brightness;\n"
		"}\n"
		"float LightSpecular(in float4 ws_light_direction, in float specular_power, in float4 ws_norm, in float4 ws_toeye_norm, in float alpha)\n"
		"{\n"
		"	float4 ws_H = normalize(ws_toeye_norm - ws_light_direction);\n"
		"	float brightness = dot(ws_norm, ws_H);\n"
		"	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);\n"
		"	return pow(saturate(brightness), specular_power);\n"
		"}\n"
		"float4 Illuminate(float4 ws_pos, float4 ws_norm, float4 ws_cam, float4 unlit_diff)\n"
		"{\n"
		"	float4 ltdiff = 0;\n"
		"	float4 ltspec = 0;\n"
		"	float  ltvis = 1;\n";
	if (me.m_specular) data +=
		"	float4 ws_toeye_norm = normalize(ws_cam - ws_pos);\n";
	data +=
		"	for (int i = 0; i != LightCount; ++i)\n"
		"	{\n"
		"		ltdiff += g_light_ambient[i];\n";
	if (me.m_caster_count != 0) data +=
		"		ltvis = LightVisibility(i, ws_pos);\n"
		"		if (ltvis == 0) continue;\n";
	data +=
		"		float intensity = 0;\n"
		"		if      (g_light_type[i] == 1) intensity = LightDirectional(g_ws_light_direction[i] ,ws_norm         ,unlit_diff.a);\n"
		"		else if (g_light_type[i] == 2) intensity = LightPoint      (g_ws_light_position[i]  ,ws_norm ,ws_pos ,unlit_diff.a);\n"
		"		else if (g_light_type[i] == 3) intensity = LightSpot       (g_ws_light_position[i]  ,g_ws_light_direction[i] ,g_spot_inner_cosangle[i] ,g_spot_outer_cosangle[i] ,g_spot_range[i] ,ws_norm ,ws_pos ,unlit_diff.a);\n"
		"		ltdiff += ltvis * intensity * g_light_diffuse[i];\n";
	if (me.m_specular) data +=
		"		float4 ws_light_dir = (g_light_type[i] == 1) ? g_ws_light_direction[i] : normalize(ws_pos - g_ws_light_position[i]);\n"
		"		ltspec += ltvis * intensity * g_light_specular[i] * LightSpecular(ws_light_dir ,g_specular_power[i] ,ws_norm ,ws_toeye_norm ,unlit_diff.a);\n";
	data +=
		"	}\n"
		"	return saturate(2.0*(ltdiff-0.5)*unlit_diff + ltspec + unlit_diff);\n"
		"}\n"
		"\n";
}
void pr::rdr::effect::frag::Lighting::PSfragment(void const* fragment, ShaderBuffer& data, int)
{
	Lighting const& me = *frag_cast<Lighting>(fragment); (void)me;

	data +=
		"	// Lighting\n"
		"	Out.diff = Illuminate(In.ws_pos, In.ws_norm, WSCameraPosition(), Out.diff);\n"
		"\n";
}
void pr::rdr::effect::frag::Lighting::SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const&)
{
	Lighting const&        me     = *frag_cast<Lighting>(fragment);
	LightingManager const& ltmgr  = viewport.Rdr().m_light_mgr;
	Light const*           lights = ltmgr.m_light;

	// Cache the light state
	struct LtCache
	{
		Light    lights[MaxLights];
		int        type[MaxLights];
		pr::v4     pos [MaxLights];
		pr::v4     dir [MaxLights];
		pr::Colour ambi[MaxLights];
		pr::Colour diff[MaxLights];
		pr::Colour spec[MaxLights];
		float      spwr[MaxLights];
		float      innr[MaxLights];
		float      outr[MaxLights];
		float      rnge[MaxLights];
		int        shdw[MaxLights];
	};
	static LtCache ltcache;

	// Look for changes to the lights
	bool update = false;
	int caster_index = 0;
	for (int i = 0; i != me.m_light_count; ++i)
	{
		Light const& light = lights[i];
		if (light == ltcache.lights[i]) continue;
		ltcache.lights[i] = light;

		PR_ASSERT(PR_DBG_RDR, light.m_ambient.a() == 0, "");
		PR_ASSERT(PR_DBG_RDR, light.m_diffuse.a() == 255, "");
		PR_ASSERT(PR_DBG_RDR, light.m_specular.a() == 0, "");

		ltcache.type[i] = light.m_on ? light.m_type : 0;
		ltcache.pos [i] = light.m_position;
		ltcache.dir [i] = light.m_direction;
		ltcache.ambi[i] = Colour::make(light.m_ambient);
		ltcache.diff[i] = Colour::make(light.m_diffuse);
		ltcache.spec[i] = Colour::make(light.m_specular);
		ltcache.spwr[i] = light.m_specular_power;
		ltcache.innr[i] = light.m_inner_cos_angle;
		ltcache.outr[i] = light.m_outer_cos_angle;
		ltcache.rnge[i] = light.m_range;
		ltcache.shdw[i] = light.m_cast_shadows && caster_index < MaxShadowCasters ? caster_index++ : -1;
		update = true;
	}
	if (update)
	{
		// Update the effect variables
		Verify(effect->SetIntArray  (me.m_light_type          ,ltcache.type              ,me.m_light_count));
		Verify(effect->SetFloatArray(me.m_ws_light_position   ,ltcache.pos[0].ToArray()  ,me.m_light_count * 4));
		Verify(effect->SetFloatArray(me.m_ws_light_direction  ,ltcache.dir[0].ToArray()  ,me.m_light_count * 4));
		Verify(effect->SetFloatArray(me.m_light_ambient       ,ltcache.ambi[0].ToArray() ,me.m_light_count * 4));
		Verify(effect->SetFloatArray(me.m_light_diffuse       ,ltcache.diff[0].ToArray() ,me.m_light_count * 4));
		if (me.m_specular) {
		Verify(effect->SetFloatArray(me.m_light_specular      ,ltcache.spec[0].ToArray() ,me.m_light_count * 4));
		Verify(effect->SetFloatArray(me.m_specular_power      ,ltcache.spwr              ,me.m_light_count));
		}
		Verify(effect->SetFloatArray(me.m_spot_inner_cosangle ,ltcache.innr              ,me.m_light_count));
		Verify(effect->SetFloatArray(me.m_spot_outer_cosangle ,ltcache.outr              ,me.m_light_count));
		Verify(effect->SetFloatArray(me.m_spot_range          ,ltcache.rnge              ,me.m_light_count));
		if (me.m_caster_count != 0) {
		Verify(effect->SetIntArray  (me.m_cast_shadows        ,ltcache.shdw              ,me.m_light_count));
		}
	}

	// Update the shadow maps
	if (me.m_caster_count != 0)
	{
		pr::Frustum f = viewport.ShadowFrustum();
		pr::v4 fdim = f.Dim();
		
		Verify(effect->SetVector(me.m_smap_frust_dim ,&reinterpret_cast<const D3DXVECTOR4&>(fdim)       ));
		Verify(effect->SetMatrix(me.m_smap_frust     ,&reinterpret_cast<const D3DXMATRIX&>(f.m_Tnorms) ));
		for (int i = 0; i != me.m_caster_count; ++i)
			Verify(effect->SetTexture(me.m_smap[i], ltmgr.m_smap[i].m_ptr));
	}
}
	
// SMap Fragment *****************************************************
pr::rdr::effect::frag::SMap::SMap()
:m_header(Header::make<SMap>())
,m_object_to_world   ()
,m_world_to_smap     ()
,m_ws_smap_plane     ()
,m_smap_frust_dim    ()
,m_light_type        ()
,m_ws_light_position ()
,m_ws_light_direction()
{}
void pr::rdr::effect::frag::SMap::SetHandles(D3DPtr<ID3DXEffect> effect)
{
	m_object_to_world    = effect->GetParameterByName(0, "g_object_to_world"    );
	m_world_to_smap      = effect->GetParameterByName(0, "g_world_to_smap"      );
	m_ws_smap_plane      = effect->GetParameterByName(0, "g_ws_smap_plane"      );
	m_smap_frust_dim     = effect->GetParameterByName(0, "g_smap_frust_dim"     );
	m_light_type         = effect->GetParameterByName(0, "g_light_type"         );
	m_ws_light_position  = effect->GetParameterByName(0, "g_ws_light_position"  );
	m_ws_light_direction = effect->GetParameterByName(0, "g_ws_light_direction" );
}
void pr::rdr::effect::frag::SMap::AddTo(Desc& desc) const
{
	desc.m_ps[0].m_sig += ",uniform bool fwd,uniform float sign0,uniform float sign1";
	desc.m_tech[0].m_pass.resize(5);
	desc.m_tech[0].m_pass[0].m_ps_params += ",true,+1,-1";
	desc.m_tech[0].m_pass[1].m_ps_params += ",true,-1,+1";
	desc.m_tech[0].m_pass[2].m_ps_params += ",true,+1,+1";
	desc.m_tech[0].m_pass[3].m_ps_params += ",true,-1,-1";
	desc.m_tech[0].m_pass[4].m_ps_params += ",false,0,0";
	desc.m_tech[0].m_pass[0].m_rdr_states += "ColorWriteEnable=Red|Green; \n	CullMode=CCW;\n";
	desc.m_tech[0].m_pass[1].m_rdr_states += "ColorWriteEnable=Red|Green; \n	CullMode=CCW;\n";
	desc.m_tech[0].m_pass[2].m_rdr_states += "ColorWriteEnable=Red|Green; \n	CullMode=CCW;\n";
	desc.m_tech[0].m_pass[3].m_rdr_states += "ColorWriteEnable=Red|Green; \n	CullMode=CCW;\n";
	desc.m_tech[0].m_pass[4].m_rdr_states += "ColorWriteEnable=Blue|Alpha;\n	CullMode=CW;\n";

	desc.m_vsout[0].Add(ESemantic::Position  ,"float4" ,"pos"    ,"0");
	desc.m_vsout[0].Add(ESemantic::TexCoord0 ,"float4" ,"ws_pos" ,"0");
	desc.m_vsout[0].Add(ESemantic::TexCoord1 ,"float2" ,"ss_pos" ,"0");
	desc.m_psout[0].Add(ESemantic::Color0    ,"float4" ,"diff"   ,"1");
}
void pr::rdr::effect::frag::SMap::Variables(void const*, ShaderBuffer& data)
{
	data += pr::FmtS(
		"// SMap variables\n"
		"#define SMapTexSize %1$d\n"
		"#define SMapEps 0.01f\n"
		"shared uniform float4x4 g_object_to_world :World;\n"
		"shared uniform float4x4 g_world_to_smap;\n"
		"shared uniform float4   g_ws_smap_plane;\n"
		"shared uniform float4   g_smap_frust_dim;\n"
		"shared uniform int      g_light_type[1];\n"
		"shared uniform float4   g_ws_light_position[1]  :Position ;\n"
		"shared uniform float4   g_ws_light_direction[1] :Direction;\n"
		"shared uniform float4x4 g_smap_frust;\n"
		"shared uniform float4x4 g_world_to_camera :View;\n"
		"shared uniform int      g_cast_shadows[1];\n"
		"sampler2D g_sampler_smap[1];\n"
		"\n"
		,SMap::TexSize);
}
void pr::rdr::effect::frag::SMap::Functions(void const*, ShaderBuffer& data)
{
	data +=
		"// SMap functions\n"
		"float2 EncodeFloat2(float value)\n"
		"{\n"
		"	const float2 shift = float2(2.559999e2f, 9.999999e-1f);\n"
		"	float2 packed = frac(value * shift);\n"
		"	packed.y -= packed.x / 256.0f;\n"
		"	return packed;\n"
		"}\n"
		"float DecodeFloat2(float2 value)\n"
		"{\n"
		"	const float2 shifts = float2(3.90625e-3f, 1.0f);\n"
		"	return dot(value, shifts);\n"
		"}\n"
		"float ClipToPlane(uniform float4 plane, in float4 s, in float4 e)\n"
		"{\n"
		"	float d0 = dot(plane, s);\n"
		"	float d1 = dot(plane, e);\n"
		"	float d  = d1 - d0;\n"
		"	return -d0/d;\n"
		"}\n"
		"float4 ShadowRayWS(in float4 ws_pos, in int light_index)\n"
		"{\n"
		"	return (g_light_type[light_index] == 1) ? (g_ws_light_direction[light_index]) : (ws_pos - g_ws_light_position[light_index]);\n"
		"}\n"
		"float IntersectFrustum(uniform float4x4 frust, in float4 s, in float4 e)\n"
		"{\n"
		"	// Intersect the line passing through 's' and 'e' with 'frust' return the parametric value 't'\n"
		"	// Assumes 's' is within the frustum to start with\n"
		"	const float4 T  = 100000;\n"
		"	float4 d0 = mul(s, frust);\n"
		"	float4 d1 = mul(e, frust);\n"
		"	float4 t0 = step(d1,d0)   * min(T, -d0/(d1 - d0));        // Clip to the frustum sides\n"
		"	float  t1 = step(e.z,s.z) * min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane\n"
		"\n"
		"	float t = T.x;\n"
		"	if (t0.x != 0) t = min(t,t0.x);\n"
		"	if (t0.y != 0) t = min(t,t0.y);\n"
		"	if (t0.z != 0) t = min(t,t0.z);\n"
		"	if (t0.w != 0) t = min(t,t0.w);\n"
		"	if (t1   != 0) t = min(t,t1);\n"
		"	return t;\n"
		"}\n"
		"float LightVisibility(int light_index, float4 ws_pos)\n"
		"{\n"
		"	// return a value between [0,1] where 0 means fully in shadow, 1 means not in shadow\n"
		"	if (g_cast_shadows[light_index] == -1) return 1;\n"
		"\n"
		"	// find the shadow ray in frustum space and its intersection with the frustum\n"
		"	float4 ws_ray = ShadowRayWS(ws_pos, light_index);\n"
		"	float4 fs_pos0 = mul(ws_pos          ,g_world_to_camera); fs_pos0.z += g_smap_frust_dim.z;\n"
		"	float4 fs_pos1 = mul(ws_pos + ws_ray ,g_world_to_camera); fs_pos1.z += g_smap_frust_dim.z;\n"
		"	float t = IntersectFrustum(g_smap_frust, fs_pos0, fs_pos1);\n"
		"\n"
		"	// convert the intersection to texture space\n"
		"	float4 intersect = lerp(fs_pos0, fs_pos1, t);\n"
		"	float2 uv = float2(0.5 + 0.5*intersect.x/g_smap_frust_dim.x, 0.5 - 0.5*intersect.y/g_smap_frust_dim.y);\n"
		"\n"
		"	// find the distance from the frustum to 'ws_pos'\n"
		"	float dist = saturate(t * length(ws_ray) / g_smap_frust_dim.w) + SMapEps;\n"
		"\n"
		"	const float d = 0.5 / SMapTexSize;\n"
		"	float4 px0 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d,-d));\n"
		"	float4 px1 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d,-d));\n"
		"	float4 px2 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d, d));\n"
		"	float4 px3 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d, d));\n"
		"	if (intersect.z > TINY)\n"
		"		return (step(DecodeFloat2(px0.rg), dist) +\n"
		"				step(DecodeFloat2(px1.rg), dist) +\n"
		"				step(DecodeFloat2(px2.rg), dist) +\n"
		"				step(DecodeFloat2(px3.rg), dist)) / 4.0f;\n"
		"	else\n"
		"		return (step(DecodeFloat2(px0.ba), dist) +\n"
		"				step(DecodeFloat2(px1.ba), dist) +\n"
		"				step(DecodeFloat2(px2.ba), dist) +\n"
		"				step(DecodeFloat2(px3.ba), dist)) / 4.0f;\n"
		"}\n"
		"\n";
}
void pr::rdr::effect::frag::SMap::VSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// SMap\n"
		"	Out.ws_pos = mul(ms_pos, g_object_to_world);\n"
		"	Out.pos    = mul(Out.ws_pos, g_world_to_smap);\n"
		"	Out.ss_pos = Out.pos.xy;\n"
		"\n";
}
void pr::rdr::effect::frag::SMap::PSfragment(void const*, ShaderBuffer& data, int)
{
	data +=
		"	// SMap\n"
		"	// find a world space ray starting from 'ws_pos' and away from the light source\n"
		"	float4 ws_ray = ShadowRayWS(In.ws_pos, 0);\n"
		"\n"
		"	// clip it to the frustum plane\n"
		"	float t = ClipToPlane(g_ws_smap_plane, In.ws_pos, In.ws_pos + ws_ray);\n"
		"	float dist = t * length(ws_ray) / g_smap_frust_dim.w;\n"
		"\n"
		"	// clip pixels with a negative distance\n"
		"	clip(dist);\n"
		"\n"
		"	// clip to the wedge of the fwd texture we're rendering to\n"
		"	if (fwd)\n"
		"	{\n"
		"		clip(sign0 * (In.ss_pos.y - In.ss_pos.x) + TINY);\n"
		"		clip(sign1 * (In.ss_pos.y + In.ss_pos.x) + TINY);\n"
		"	}\n"
		"\n"
		"	// encode the distance into the output\n"
		"	if (fwd) Out.diff.rg = EncodeFloat2(dist);\n"
		"	else     Out.diff.ba = EncodeFloat2(dist);\n"
		"\n";
}
bool pr::rdr::effect::frag::SMap::CreateProjection(int face, pr::Frustum const& frust, pr::m4x4 const& c2w, Light const& light, pr::m4x4& w2s)
{
	// Create a projection transform that will take points in world space and project them 
	// onto a surface parallel to the frustum plane for the given face (based on light type).
	#define DBG_PROJ 0
	#if DBG_PROJ
	struct Dump {
		Dump(v4 const& tl_, v4 const& tr_, v4 const& bl_, v4 const& br_) {
			v4 tl = tl_ / tl_.w;
			v4 tr = tr_ / tr_.w;
			v4 bl = bl_ / bl_.w;
			v4 br = br_ / br_.w;
			std::string str;
			pr::ldr::Box("smap", 0xFFFFFFFF, pr::v4::make(0,0,0.5f,1), pr::v4::make(2,2,1,0), str);
			pr::ldr::Box("tl", 0xFFFF0000, tl, 0.2f, str);
			pr::ldr::Box("tr", 0xFF00FF00, tr, 0.2f, str);
			pr::ldr::Box("bl", 0xFF0000FF, bl, 0.2f, str);
			pr::ldr::Box("br", 0xFFFFFF00, br, 0.2f, str);
			pr::ldr::Write(str, "d:/deleteme/smap_proj_screen.ldr");
		}
	};
	#endif

	// Get the frustum normal for 'face'
	pr::v4 ws_norm = c2w * frust.Normal(face);
	
	// Get the corners of the plane we want to project onto in world space.
	pr::v4 fdim     = frust.Dim();
	float sign_z[4] = {pr::Sign<float>(face==1||face==3), pr::Sign<float>(face==0||face==3), pr::Sign<float>(face==1||face==2), pr::Sign<float>(face==0||face==2)};
	pr::v4 tl, TL = c2w * pr::v4::make(-fdim.x,  fdim.y, sign_z[0]*fdim.z, 1.0f);
	pr::v4 tr, TR = c2w * pr::v4::make( fdim.x,  fdim.y, sign_z[1]*fdim.z, 1.0f);
	pr::v4 bl, BL = c2w * pr::v4::make(-fdim.x, -fdim.y, sign_z[2]*fdim.z, 1.0f);
	pr::v4 br, BR = c2w * pr::v4::make( fdim.x, -fdim.y, sign_z[3]*fdim.z, 1.0f);

	switch (light.m_type)
	{
	case ELight::Directional:
		{
			// The surface must face the light source
			if (pr::Dot3(light.m_direction, ws_norm) >= 0) return false;
			
			// Create a light to world transform
			// Position the light camera at the centre of the plane we're projecting looking in the light direction
			pr::v4 pos = (TL + TR + BL + BR) * 0.25f;
			pr::m4x4 lt2w = pr::LookAt(pos, pos + light.m_direction, pr::Parallel(light.m_direction,c2w.y) ? c2w.z : c2w.y);
			w2s = pr::GetInverseFast(lt2w);

			// Create an orthographic projection
			pr::m4x4 lt2s = pr::ProjectionOrthographic(1.0f, 1.0f, -100.0f, 100.0f, true);
			w2s = lt2s * w2s;

			// Project the four corners of the plane
			tl = w2s * TL;
			tr = w2s * TR;
			bl = w2s * BL;
			br = w2s * BR;

			// Rotate so that TL is above BL and TR is above BR (i.e. the left and right edges are vertical)
			pr::v2 ledge = GetNormal2((tl - bl).xy());
			pr::m4x4 R = pr::m4x4Identity;
			R.x.set( ledge.y,  ledge.x, 0, 0);
			R.y.set(-ledge.x,  ledge.y, 0, 0);
			w2s = R * w2s;
			
			// Project the four corners of the plane
			tl = w2s * TL;
			tr = w2s * TR;
			bl = w2s * BL;
			br = w2s * BR;

			// Scale the face of the frustum into the viewport
			pr::m4x4 S = pr::Scale4x4(2.0f/(tr.x - tl.x), 2.0f/(tr.y - br.y), 1.0f, pr::v4Origin);
			w2s = S * w2s;
			
			// Project the four corners of the plane
			tl = w2s * TL;
			tr = w2s * TR;
			bl = w2s * BL;
			br = w2s * BR;

			// Shear to make the projected plane square
			pr::m4x4 H = pr::Shear4x4(-(tr.y - tl.y)/(tr.x - tl.x), 0, 0, 0, 0, 0, pr::v4Origin);
			w2s = H * w2s;

			//// Project the four corners of the plane
			//tl = w2s * TL;
			//tr = w2s * TR;
			//bl = w2s * BL;
			//br = w2s * BR;
			//PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br));
			return true;
		}
	case ELight::Spot:
	case ELight::Point:
		{
			// The surface must face the light source
			float dist_to_light = pr::Dot3(light.m_position - c2w.pos, ws_norm) + (face == 4)*frust.ZDist();
			if (dist_to_light <= 0) return false;

			// Create a light to world transform
			// Position the light camera at the light position looking in the -frustum plane normal direction
			pr::m4x4 lt2w = pr::LookAt(light.m_position, light.m_position - ws_norm, pr::Parallel(ws_norm,c2w.y) ? c2w.z : c2w.y);
			w2s = pr::GetInverse(lt2w);
			tl = w2s * TL;
			tr = w2s * TR;
			bl = w2s * BL;
			br = w2s * BR;

			// Create a perspective projection
			float zr = 0.001f, zf = dist_to_light, zn = zf*zr;
			pr::m4x4 lt2s = pr::ProjectionPerspective(tl.x*zr, tr.x*zr, tl.y*zr, bl.y*zr, zn, zf, true);
			w2s = lt2s * w2s;

			//// Project the four corners of the plane
			//tl = w2s * TL;
			//tr = w2s * TR;
			//bl = w2s * BL;
			//br = w2s * BR;
			//PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br));
			return true;
		}
	}
	return false;
	#undef DBG_PROJ
}
bool pr::rdr::effect::frag::SMap::SetSceneParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, int pass, pr::Frustum const& frust, m4x4 const& c2w, Light const& light)
{
	SMap const& me = *frag_cast<SMap>(fragment);

	// Create the projection transform for this face of the frustum
	pr::m4x4 w2smap;
	if (!pr::rdr::effect::frag::SMap::CreateProjection(pass, frust, c2w, light, w2smap))
		return false;

	int light_type = light.m_type;
	pr::v4 ws_smap_plane = pass < 4 ?
		pr::plane::make(c2w.pos, c2w * frust.Normal(pass)) :
		pr::plane::make(c2w.pos - frust.ZDist()*c2w.z, c2w.z);

	v4 frust_dim = frust.Dim();
	Verify(effect->SetMatrix      (me.m_world_to_smap      ,&reinterpret_cast<const D3DXMATRIX&>(w2smap)              ));
	Verify(effect->SetVector      (me.m_ws_smap_plane      ,&reinterpret_cast<const D3DXVECTOR4&>(ws_smap_plane)       ));
	Verify(effect->SetVector      (me.m_smap_frust_dim     ,&reinterpret_cast<const D3DXVECTOR4&>(frust_dim)         ));
	Verify(effect->SetIntArray    (me.m_light_type         ,&light_type               ,1));
	Verify(effect->SetVectorArray (me.m_ws_light_position  ,&reinterpret_cast<const D3DXVECTOR4&>(light.m_position)  ,1));
	Verify(effect->SetVectorArray (me.m_ws_light_direction ,&reinterpret_cast<const D3DXVECTOR4&>(light.m_direction) ,1));
	return true;
}
void pr::rdr::effect::frag::SMap::SetObjectToWorld(void const* fragment, D3DPtr<ID3DXEffect> effect, m4x4 const& o2w)
{
	SMap const& me = *frag_cast<SMap>(fragment);
	Verify(effect->SetMatrix(me.m_object_to_world ,&reinterpret_cast<const D3DXMATRIX&>(o2w)));
}
	
// effect::Desc *****************************************************
pr::rdr::effect::Desc::Desc(D3DPtr<IDirect3DDevice9> d3d_device)
:m_vs_version(0x0200)
,m_ps_version(0x0300)
,m_effect_id(0)
,m_buf()
,m_vs()
,m_ps()
,m_tech()
{
	if (d3d_device)
	{
		D3DCAPS9 caps; d3d_device->GetDeviceCaps(&caps);
		m_vs_version = caps.VertexShaderVersion & 0xFFFF;
		m_ps_version = caps.PixelShaderVersion  & 0xFFFF;
	}

	Reset();
}
	
// Helper for converting a semantic to a string
char const* pr::rdr::effect::ESemantic::ToString(ESemantic::Type sem)
{
	using namespace pr::rdr::effect::ESemantic;
	switch (sem)
	{
	default:PR_ASSERT(PR_DBG_RDR, false, ""); return "";
	case Position:  return "Position";
	case Color0:    return "Color0";
	case Color1:    return "Color1";
	case Color2:    return "Color2";
	case Color3:    return "Color3";
	case Depth:     return "Depth";
	case TexCoord0: return "TexCoord0";
	case TexCoord1: return "TexCoord1";
	case TexCoord2: return "TexCoord2";
	case TexCoord3: return "TexCoord3";
	case TexCoord4: return "TexCoord4";
	}
}
	
// Reset the Desc
void pr::rdr::effect::Desc::Reset(int tech_count, int vs_count, int ps_count, int vsout_count, int psout_count)
{
	m_effect_id = 0;
	m_buf.clear();
	m_vsout.resize(vsout_count);
	m_psout.resize(psout_count);
	m_vs.resize(vs_count);
	m_ps.resize(ps_count);
	m_tech.resize(tech_count);

	for (int i = 0; i != tech_count; ++i)
	{
		m_tech[i].m_pass.resize(1);
		m_tech[i].m_pass[0].m_vs_idx = 0;
		m_tech[i].m_pass[0].m_ps_idx = 0;
		m_tech[i].m_pass[0].m_vs_params.clear();
		m_tech[i].m_pass[0].m_ps_params.clear();
	}
	for (int i = 0; i != vsout_count; ++i)
	{
		m_vsout[i].m_member.clear();
	}
	for (int i = 0; i != psout_count; ++i)
	{
		m_psout[i].m_member.clear();
	}
	for (int i = 0; i != vs_count; ++i)
	{
		m_vs[i].m_in_idx = 0;
		m_vs[i].m_out_idx = i;
		m_vs[i].m_version = m_vs_version;
		m_vs[i].m_sig.clear();
	}
	for (int i = 0; i != ps_count; ++i)
	{
		m_ps[i].m_in_idx = i;
		m_ps[i].m_out_idx = i;
		m_ps[i].m_version = m_ps_version;
		m_ps[i].m_sig.clear();
	}
	
	m_effect_id = pr::hash::HashData(&tech_count  ,sizeof(tech_count)  );
	m_effect_id = pr::hash::HashData(&vs_count    ,sizeof(vs_count)    ,m_effect_id);
	m_effect_id = pr::hash::HashData(&ps_count    ,sizeof(ps_count)    ,m_effect_id);
	m_effect_id = pr::hash::HashData(&vsout_count ,sizeof(vsout_count) ,m_effect_id);
	m_effect_id = pr::hash::HashData(&psout_count ,sizeof(psout_count) ,m_effect_id);
}

// Add a fragment to the effect description
void pr::rdr::effect::Desc::Add(frag::Header const& frag)
{
	std::size_t size = m_buf.size();
	m_buf.resize(size + frag.m_size);
	memcpy(&m_buf[size], &frag, frag.m_size);
	m_effect_id = pr::hash::HashData(&frag, frag.m_size ,m_effect_id);
	if (frag.m_type != EFrag::Terminator) frag.AddTo(*this);
}
	
// Add a member to a struct
void pr::rdr::effect::Desc::Struct::Add(ESemantic::Type channel, char const* type, char const* name, char const* init)
{
	MemberCont::const_iterator iter = std::find(m_member.begin(), m_member.end(), channel);
	if (iter == m_member.end())
	{
		Member mb;
		mb.m_channel = channel;
		mb.m_type = type;
		mb.m_name = name;
		mb.m_init = init;
		mb.m_chnl = ESemantic::ToString((ESemantic::Type)channel);
		m_member.push_back(mb);
		std::sort(m_member.begin(), m_member.end());
	}
	else
	{
		PR_ASSERT(PR_DBG_RDR, iter->m_type == type, "");
		PR_ASSERT(PR_DBG_RDR, iter->m_name == name, "");
		PR_ASSERT(PR_DBG_RDR, iter->m_init == init, "");
	}
}
	
// Add declarations for each member to 'data'
void pr::rdr::effect::Desc::Struct::decl(ShaderBuffer& data) const
{
	for (MemberCont::const_iterator i = m_member.begin(), iend = m_member.end(); i != iend; ++i)
		data += i->decl();
}
	
// Add initialisers for each member to 'data'
void pr::rdr::effect::Desc::Struct::init(ShaderBuffer& data) const
{
	for (MemberCont::const_iterator i = m_member.begin(), iend = m_member.end(); i != iend; ++i)
		data += i->init();
}
	
// Generate the text of an effect from the fragments
void pr::rdr::effect::Desc::GenerateText(ShaderBuffer& data) const
{
	// Get a pointer to the list of fragments
	if (m_buf.empty()) return;
	frag::Header const* frags = frag::Begin(&m_buf[0]);

	data +=
		"//***********************************************\n"
		"// Renderer - Generated Shader\n"
		"//  Copyright © Rylogic Ltd 2010\n"
		"//***********************************************\n"
		"#pragma warning (disable:3557)\n"
		"#define TINY 0.0005f\n"
		"\n";
	
	// Add variables for the shader fragments
	for (frag::Header const* f = frags; f; f = frag::Inc(f))
		f->Variables(data);

	// Add functions for the shader fragments
	pr::uint seen = 0;
	for (frag::Header const* f = frags; f; f = frag::IncUnique(f, seen))
		f->Functions(data);

	// Add shader in/out structs
	data +=
		"// Structs ********************************\n"
		"struct VSIn\n"
		"{\n"
		"	float3   pos      :Position;\n"
		"	float3   norm     :Normal;\n"
		"	float4   diff     :Color0;\n"
		"	float2   tex0     :TexCoord0;\n"
		"};\n";
	for (StructCont::const_iterator i = m_vsout.begin(), iend = m_vsout.end(); i != iend; ++i)
	{
		data += pr::FmtS("struct VSOut%d\n{\n", i-m_vsout.begin());
		i->decl(data);
		data += "};\n";
	}
	for (StructCont::const_iterator i = m_psout.begin(), iend = m_psout.end(); i != iend; ++i)
	{
		data += pr::FmtS("struct PSOut%d\n{\n", i-m_psout.begin());
		i->decl(data);
		data += "};\n";
	}
	data += "\n";

	data += "// Shaders ********************************\n";
	for (ShaderCont::const_iterator i = m_vs.begin(), iend = m_vs.end(); i != iend; ++i)
	{
		Shader const& vs = *i;
		int vs_idx = static_cast<int>(i - m_vs.begin());

		data += pr::FmtS(
			"VSOut%1$d VS%2$d(VSIn In%3$s)\n" 
			"{\n"
			"	VSOut%1$d Out;\n"
			,vs.m_out_idx ,vs_idx ,vs.m_sig.c_str());
		m_vsout[vs.m_out_idx].init(data);
		data += 
			"	float4 ms_pos  = float4(In.pos  ,1);\n"
			"	float4 ms_norm = float4(In.norm ,0);\n"
			"\n";

		// Add vertex shader code
		for (frag::Header const* f = frags; f; f = frag::Inc(f))
			f->VSfragment(data, vs_idx);

		data +=
			"	return Out;\n"
			"}\n"
			"\n";
	}
	for (ShaderCont::const_iterator i = m_ps.begin(), iend = m_ps.end(); i != iend; ++i)
	{
		Shader const& ps = *i;
		int ps_idx = static_cast<int>(i - m_ps.begin());
		
		data += pr::FmtS(
			"PSOut%1$d PS%2$d(VSOut%3$d In%4$s)\n"
			"{\n"
			"	PSOut%1$d Out;\n"
			,ps.m_out_idx ,ps_idx ,ps.m_in_idx ,ps.m_sig.c_str());
		m_psout[ps.m_out_idx].init(data);
		data += "\n";

		// Add pixel shader code
		for (frag::Header const* f = frags; f; f = frag::Inc(f))
			f->PSfragment(data, ps_idx);

		data +=
			"	return Out;\n"
			"}\n"
			"\n";
	}

	data += "// Techniques ********************************\n";
	for (TechCont::const_iterator i = m_tech.begin(), iend = m_tech.end(); i != iend; ++i)
	{
		Technique const& tech = *i;
		data += pr::FmtS(
			"technique t%d\n"
			"{\n"
			,i-m_tech.begin());

		for (PassCont::const_iterator j = tech.m_pass.begin(), jend = tech.m_pass.end(); j != jend; ++j)
		{
			Pass const& pass = *j;
			Shader const& vs = m_vs[pass.m_vs_idx];
			Shader const& ps = m_ps[pass.m_ps_idx];
			char const* vs_params = pass.m_vs_params.c_str(); vs_params += *vs_params == ',';
			char const* ps_params = pass.m_ps_params.c_str(); ps_params += *ps_params == ',';

			data += pr::FmtS(
				"	pass p%d {\n"
				"	VertexShader = compile vs_%d_%d VS%d(%s);\n"
				"	PixelShader  = compile ps_%d_%d PS%d(%s);\n"
				,j-tech.m_pass.begin()
				,(vs.m_version >> 8) & 0xFF
				,(vs.m_version >> 0) & 0xFF
				,pass.m_vs_idx
				,vs_params
				,(ps.m_version >> 8) & 0xFF
				,(ps.m_version >> 0) & 0xFF
				,pass.m_ps_idx
				,ps_params
				);
			if (!pass.m_rdr_states.empty()) data += pr::FmtS(
				"	%s"
				,pass.m_rdr_states.c_str()
				);
			data +=
				"	}\n";
		}
		data += "}\n";
	}
	data += "\n";
}
