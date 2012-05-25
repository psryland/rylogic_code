//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/viewport/viewport.h"
#include "pr/renderer/viewport/drawlistelement.h"
#include "pr/renderer/renderer/renderer.h"
#include "pr/renderer/models/modelbuffer.h"
#include "pr/renderer/instances/instance.h"

using namespace pr;
using namespace pr::rdr;

#define PR_DBG_SMAP 0
#if PR_DBG_SMAP
#pragma message ("******************************************* PR_DBG_SMAP defined")
#endif
#define PR_DBG_SMAP_SHOW 0
#if PR_DBG_SMAP_SHOW
#pragma message ("******************************************* PR_DBG_SMAP_SHOW defined")
#endif

// Construction
pr::rdr::Viewport::Viewport(VPSettings const& settings)
:m_settings(settings)
,m_d3d_viewport()
,m_render_state()
,m_drawlist(*settings.m_renderer, settings.m_identifier)
{
	// Register with the renderer
	m_settings.m_renderer->RegisterViewport(*this);

	// Initialise the d3d viewport
	ViewRect(ViewRect());

	// We have been 'restored' with a device
	OnEvent(pr::rdr::Evt_DeviceRestored());
}
pr::rdr::Viewport::~Viewport()
{
	OnEvent(pr::rdr::Evt_DeviceLost());
	m_settings.m_renderer->UnregisterViewport(*this);
}
	
// Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
void pr::rdr::Viewport::SetView(float fovY, float aspect, float centre_dist, bool orthographic)
{
	PR_ASSERT(PR_DBG_RDR, IsFinite(fovY) && IsFinite(aspect) && IsFinite(centre_dist), "");
	m_settings.m_fovY            = fovY;
	m_settings.m_aspect          = aspect;
	m_settings.m_centre_dist     = centre_dist;
	m_settings.m_orthographic    = orthographic;
	m_settings.UpdateCameraToScreen();
}
	
// Update the viewport area
// This changes the region of the screen that we draw to.
// Note, the view rect is independent of the aspect ratio and this function
// does not update the projection transform. This should be done in a separated call to SetView
void pr::rdr::Viewport::ViewRect(FRect const& rect)
{
	PR_ASSERT(PR_DBG_RDR, rect.Area() > 0.0f, "");
	m_settings.m_view_rect = rect;

	IRect vp_area = m_settings.m_renderer->ClientArea();
	int width  = vp_area.SizeX();
	int height = vp_area.SizeY();
	
	IRect client_area = {width, height, width, height};
	IRect area;
	area.m_min.x = int(client_area.m_min.x * m_settings.m_view_rect.m_min.x);
	area.m_min.y = int(client_area.m_min.y * m_settings.m_view_rect.m_min.y);
	area.m_max.x = int(client_area.m_max.x * m_settings.m_view_rect.m_max.x);
	area.m_max.y = int(client_area.m_max.y * m_settings.m_view_rect.m_max.y);

	m_d3d_viewport.X      = area.m_min.x;
	m_d3d_viewport.Y      = area.m_min.y;
	m_d3d_viewport.Width  = area.SizeX();
	m_d3d_viewport.Height = area.SizeY();
	m_d3d_viewport.MinZ   = 0.0f;
	m_d3d_viewport.MaxZ   = 1.0f;
}
	
// Render the draw list for this viewport
void pr::rdr::Viewport::Render(bool clear_back_buffer, rs::Block const& rsb_override)
{
	PR_ASSERT(PR_DBG_RDR, m_settings.m_renderer->m_rendering_phase == EState::BuildingScene, "Incorrect render call sequence");

	// Sort the drawlist (if needed)
	m_drawlist.SortIfNecessary();

	// Create the shadow maps if necessary
	GenerateShadowMap();

	// Set the state of the renderer ready for this viewport
	rs::stack_frame::Viewport viewport_vp (Rdr().m_rdrstate_mgr, m_d3d_viewport);
	rs::stack_frame::RSB      viewport_rsb(Rdr().m_rdrstate_mgr, m_render_state);
	Rdr().m_rdrstate_mgr.Flush(ERSMFlush::Diff); // Push the viewport area and states

	// Clear the area of this viewport
	if (clear_back_buffer) Rdr().ClearBackBuffer();

	// Loop over the elements in the draw list
	TDrawList::const_iterator element     = m_drawlist.Begin();
	TDrawList::const_iterator element_end = m_drawlist.End();
	while (element != element_end)
	{
		// Get the material with which to renderer this element
		Material const& material = element->GetMaterial();
		D3DPtr<ID3DXEffect> d3d_effect = material.m_effect->m_effect;
		
		// Begin the effect
		uint num_passes;
		Verify(d3d_effect->Begin(&num_passes, 0));
		TDrawList::const_iterator pass_start = element;
		
		// Begin the pass
		for (uint p = 0; p != num_passes; ++p)
		{
			element = pass_start;
			Verify(d3d_effect->BeginPass(p));

			// Loop over the draw list elements that are using this effect
			do
			{
				// Set the state of the renderer ready for this element
				rs::stack_frame::DLE dle_sf(Rdr().m_rdrstate_mgr, *element);
				
				// Set effect properties specific to this draw list element
				material.m_effect->SetParameters(*this, *element);
				Verify(d3d_effect->CommitChanges());
				
				// If there are no overrides to the render state, draw the element
				if (rsb_override.m_state.empty())
				{
					RenderDrawListElement(*element);
				}
				// Otherwise, apply the overrides first
				else
				{
					rs::stack_frame::RSB rsb_override_sf(Rdr().m_rdrstate_mgr, rsb_override);
					RenderDrawListElement(*element);
				}
				++element;
			}
			while (element != element_end && element->GetMaterial().m_effect == material.m_effect);
			
			// End this pass
			d3d_effect->EndPass();
		}

		// End the effect
		d3d_effect->End();
	}

	// Copy the smap to the main render target
	#if PR_DBG_SMAP_SHOW
	if (Rdr().m_light_mgr.m_smap[0])
	{
		// Get the back buffer
		D3DPtr<IDirect3DSurface9> bb; Rdr().D3DDevice()->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &bb.m_ptr);

		// Blt the tex to the backbuffer
		D3DPtr<IDirect3DSurface9> surf; Rdr().m_light_mgr.m_smap[0]->GetSurfaceLevel(0, &surf.m_ptr);
		RECT rect = {0,  0,400,400}; Rdr().D3DDevice()->StretchRect(surf.m_ptr, 0, bb.m_ptr, &rect, D3DTEXF_NONE);
	}
	#endif
}
	
// Interpret a draw list element and render it
void pr::rdr::Viewport::RenderDrawListElement(const DrawListElement& element)
{
	// Enure the render state is up to date
	Rdr().m_rdrstate_mgr.Flush(ERSMFlush::Diff);

	// Draw the element
	RenderNugget const& nugget = *element.m_nugget;
	if (nugget.m_primitive_type != model::EPrimitive::PointList)
	{
		Verify(m_settings.m_renderer->D3DDevice()->DrawIndexedPrimitive(
			(D3DPRIMITIVETYPE)nugget.m_primitive_type,
			0,
			(UINT)nugget.m_Vrange.m_begin,
			(UINT)nugget.m_Vrange.size(),
			(UINT)nugget.m_Irange.m_begin,
			(UINT)nugget.m_primitive_count));
	}
	else
	{
		Verify(m_settings.m_renderer->D3DDevice()->DrawPrimitive(
			(D3DPRIMITIVETYPE)nugget.m_primitive_type,
			(UINT)nugget.m_Vrange.m_begin,
			(UINT)nugget.m_primitive_count));
	}
}
	
#if PR_DBG_SMAP
namespace dbg_smap
{
	typedef pr::v2   float2;
	typedef pr::v4   float4;
	typedef pr::m4x4 float4x4;
	const float TINY = 0.0005f;
	const float SMapEps = 0.005f;
	#define uniform const
	#define in
	#define out
	struct sampler2D
	{
		enum { Size = pr::rdr::effect::frag::SMap::TexSize };
		typedef std::map<pr::iv2, float4> TexMap;
		TexMap m_texture;
		float4 operator ()(float2 uv) const
		{
			iv2 UV;
			UV.x = int(uv.x * Size);
			UV.y = int(uv.y * Size);
			TexMap::const_iterator iter = m_texture.find(UV);
			return iter == m_texture.end() ? v4Zero : iter->second;
		}
		void Write(int pass, float2 uv, float4 col)
		{
			iv2 UV;
			UV.x = int((0.5f + 0.5f * uv.x) * Size);
			UV.y = int((0.5f - 0.5f * uv.y) * Size);
			float4& px = m_texture[UV];
			if (pass < 4) px.xy() = col.xy();
			else          px.zw() = col.zw();
		}
	};

	float4x4 g_object_to_world;
	float4x4 g_world_to_camera;
	float4x4 g_world_to_smap;
	float4   g_ws_smap_plane;
	float4   g_smap_frust_dim;
	float4x4 g_smap_frust;
	int      g_light_type[1];
	float4   g_ws_light_position[1];
	float4   g_ws_light_direction[1];
	int      g_cast_shadows[1];
	sampler2D g_sampler_smap[1];
	
	struct VSOut
	{
		float4 pos;
		float4 ws_pos;
		float2 ss_pos;
	};
	struct PSOut
	{
		float4 diff;
		bool clipped;
	};
	
	inline float4 mul(float4 const& v, float4x4 const& tx) { return tx * v; }
	inline float  sign(float x)                            { return x > 0.0f ? 1.0f : x < 0.0f ? -1.0f : 0.0f; }
	inline float  saturate(float x)                        { return pr::Clamp(x, 0.0f, 1.0f); }
	inline float  length(float4 v)                         { return Length4(v); }
	inline float4 lerp(float4 x, float4 y, float t)        { return x*(1-t) + y*(t); }
	inline float2 frac(float2 x)                           { return pr::Frac(x); }
	inline float4 frac(float4 x)                           { return pr::Frac(x); }
	inline float  dot(float4 a, float4 b)                  { return pr::Dot4(a,b); }
	inline float  dot(float2 a, float2 b)                  { return pr::Dot2(a,b); }
	inline bool   any(float4 x)                            { return pr::Any4(x); }
	inline float  step(float y, float x)                   { return x >= y ? 1.0f : 0.0f; }
	inline float  smoothstep(float mn, float mx, float x)  { return pr::Clamp((x - mn) / (mx - mn), 0.0f, 1.0f); }
	inline float4 tex2D(sampler2D& samp, float2 uv)        { return samp(uv); }
	inline bool   clip(float x)                            { return x < 0; }

	void Clear()
	{
		g_sampler_smap[0].m_texture.clear();
	}
	bool SetSceneParameters(void const*, D3DPtr<ID3DXEffect>, int pass, pr::Frustum const& frust, m4x4 const& c2w, Light const& light)
	{
		// Create the projection transform for this face of the frustum
		pr::m4x4 w2smap;
		if (!pr::rdr::effect::frag::SMap::CreateProjection(pass, frust, c2w, light, w2smap))
			return false;

		int light_type = light.m_type;
		pr::v4 ws_smap_plane = pass < 4 ?
			pr::plane::make(c2w.pos, c2w * frust.Normal(pass)) :
			pr::plane::make(c2w.pos - frust.ZDist()*c2w.z, c2w.z);

		g_world_to_smap         = w2smap;
		g_ws_smap_plane         = ws_smap_plane;
		g_smap_frust_dim        = frust.Dim();
		g_smap_frust            = frust.m_Tnorms;
		g_light_type[0]         = light_type;
		g_ws_light_position[0]  = light.m_position;
		g_ws_light_direction[0] = light.m_direction;

		g_world_to_camera = pr::GetInverseFast(c2w);
		g_cast_shadows[0] = light.m_cast_shadows ? 0 : -1;
		return true;
	}
	void SetObjectToWorld(void const*, D3DPtr<ID3DXEffect>, float4x4 const& o2w)
	{
		g_object_to_world = o2w;
	}
	
	// Smap functions
	float2 EncodeFloat2(float value)
	{
		const float2 shift = float2::make(2.559999e2f, 9.999999e-1f);
		float2 packed = frac(value * shift);
		packed.y -= packed.x / 256.0f;

		packed.x = int(256.0f*packed.x)/256.0f;
		packed.y = int(256.0f*packed.y)/256.0f;
		return packed;
	}
	float DecodeFloat2(float2 value)
	{
		const float2 shifts = float2::make(3.90625e-3f, 1.0f);
		return dot(value, shifts);
	}
	
	float ClipToPlane(uniform float4 plane, in float4 s, in float4 e)
	{
		float d0 = dot(plane, s);
		float d1 = dot(plane, e);
		float d  = d1 - d0;
		return (abs(d) > TINY) ? (-d0/d) : (sign(d0)*1000.0f);
	}
	
	float4 ShadowRayWS(in float4 ws_pos, in int light_index)
	{
		return (g_light_type[light_index] == 1) ? (g_ws_light_direction[light_index]) : (ws_pos - g_ws_light_position[light_index]);
	}
	
	float2 IntersectFrustum(uniform float4x4 frust, in float4 s, in float4 e, in float2 t)
	{
		// Intersect the line passing through 's' and 'e' with 'frust' return the parametric values 't'
		// clip to the far plane
		if (abs(e.z - s.z) > TINY)
		{
			if (e.z > s.z) t.x = max(t.x, -s.z / (e.z - s.z));
			else           t.y = min(t.y, -s.z / (e.z - s.z));
		}
		else if (s.z < 0) { t.y = t.x; return t; }
	
		float4 d0 = mul(s, frust);
		float4 d1 = mul(e, frust);
		float4 d  = d1 - d0;
		for (int i = 0; i != 4; ++i)
		{
			if (abs(d[i]) > TINY) // if the line is not parallel to this plane
			{
				if (d1[i] > d0[i]) t.x = max(t.x, -d0[i] / d[i]);
				else               t.y = min(t.y, -d0[i] / d[i]);
			}
			else if (d0[i] < 0) { t.y = t.x; return t; } // If behind the plane, then wholely clipped
		}
		return t;
	}
	
	float LightVisibility(int light_index, float4 ws_pos)
	{
		// return a value between [0,1] where 0 means fully in shadow, 1 means not in shadow
		if (g_cast_shadows[light_index] == -1) return 1;// if this light source doesn't cast shadows, then it's visible
	
		// find the shadow ray in frustum space and its intersection with the frustum
		float4 ws_ray = ShadowRayWS(ws_pos, light_index);
		float4 fs_pos0 = mul(ws_pos          ,g_world_to_camera); fs_pos0.z += g_smap_frust_dim.z;
		float4 fs_pos1 = mul(ws_pos + ws_ray ,g_world_to_camera); fs_pos1.z += g_smap_frust_dim.z;
		float2 t = IntersectFrustum(g_smap_frust, fs_pos0, fs_pos1, float2::make(0, any(ws_ray)*100000.0f));
		if (t.x >= t.y) return 1;
	
		// convert the intersection to texture space
		float4 intersect = lerp(fs_pos0, fs_pos1, t.y);
		float2 uv = float2::make(0.5f + 0.5f*intersect.x/g_smap_frust_dim.x, 0.5f - 0.5f*intersect.y/g_smap_frust_dim.y);
	
		// find the distance from the frustum to 'ws_pos'
		float dist = saturate((t.y - t.x) * length(ws_ray) / g_smap_frust_dim.w);
	
		// do the depth test
		const float d = 0.5 / SMapTexSize;
		if (intersect.z > TINY)
			return (step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d,-d)).rg), dist) +
					step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d,-d)).rg), dist) +
					step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d, d)).rg), dist) +
					step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d, d)).rg), dist)) / 4.0f;
		else
			return (step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d,-d)).ba), dist) +
					step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d,-d)).ba), dist) +
					step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d, d)).ba), dist) +
					step(DecodeFloat2(tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d, d)).ba), dist)) / 4.0f;
	}
	
	VSOut VSShader(pr::rdr::vf::RefVertex const& In)
	{
		VSOut Out;
		Out.pos    = v4Zero;
		Out.ws_pos = v4Zero;
		Out.ss_pos = v2Zero;
		float4 ms_pos  = float4::make(In.vertex(), 1.0f); (void)ms_pos;
		float4 ms_norm = float4::make(In.normal(), 0.0f); (void)ms_norm;

		Out.ws_pos = mul(ms_pos, g_object_to_world);
		Out.pos    = mul(Out.ws_pos, g_world_to_smap);
		Out.ss_pos = Out.pos.xy();

		return Out;
	}
	PSOut PSShader(VSOut In, uniform bool fwd, uniform float sign0, uniform float sign1, std::string& smap_scene, std::string& smap_output)
	{
		PSOut Out;
		Out.diff = v4Zero;
		Out.clipped = false;

		// find a world space ray starting from 'ws_pos' and away from the light source
		float4 ws_ray = ShadowRayWS(In.ws_pos, 0);
		
		// clip it to the frustum plane
		float t = ClipToPlane(g_ws_smap_plane, In.ws_pos, In.ws_pos + ws_ray);
		float dist = t * length(ws_ray) / g_smap_frust_dim.w - SMapEps;
		
		// clip pixels with a negative distance
		Out.clipped |= clip(dist);

		// clip to the wedge of the fwd texture we're rendering to
		if (fwd)
		{
			Out.clipped |= clip(sign0 * (In.ss_pos.y - In.ss_pos.x) + TINY);
			Out.clipped |= clip(sign1 * (In.ss_pos.y + In.ss_pos.x) + TINY);
		}

		// encode the distance into the output
		if (fwd) Out.diff.xy() = EncodeFloat2(dist);
		else     Out.diff.zw() = EncodeFloat2(dist);

		pr::ldr::BoxLine("pt", 0xFF00FF00, In.ws_pos, In.ws_pos + t*ws_ray, 0.5f, smap_scene);
		pr::ldr::Box("pt", Out.clipped ? 0xFFFF0000 : 0xFF00FF00, In.pos, 0.1f, smap_output);
		return Out;
	}
	void SmapEffect(DrawListElement const& dle, int pass, std::string& smap_scene, std::string& smap_output)
	{
		struct { bool fwd; float sign0, sign1; } params[5] = 
		{
			{true,+1,-1},
			{true,-1,+1},
			{true,+1,+1},
			{true,-1,-1},
			{false,0,0},
		};

		pr::ldr::GroupStart(FmtS("Pass%d", pass), smap_scene);
		pr::ldr::GroupStart(FmtS("Pass%d", pass), smap_output);

		rdr::model::VLock vlock;
		vf::iterator vp = dle.m_nugget->m_model_buffer->LockVBuffer(vlock, dle.m_nugget->m_Vrange);
		for (int i = 0; i != (int)dle.m_nugget->m_Vrange.m_count; ++i, ++vp)
		{
			VSOut vs = VSShader(*vp);
			PSOut px = PSShader(vs, params[pass].fwd, params[pass].sign0, params[pass].sign1, smap_scene, smap_output);
			if (!px.clipped)
				g_sampler_smap[0].Write(pass, vs.pos.xy(), px.diff);
		}

		pr::ldr::GroupEnd(smap_scene);
		pr::ldr::GroupEnd(smap_output);
	}
	void LightingEffect(DrawListElement const& dle)
	{
		rdr::model::VLock vlock;
		vf::iterator vp = dle.m_nugget->m_model_buffer->LockVBuffer(vlock, dle.m_nugget->m_Vrange);
		for (int i = 0; i != (int)dle.m_nugget->m_Vrange.m_count; ++i, ++vp)
		{
			float4 ms_pos = float4::make(vp->vertex(), 1.0f);
			float4 ws_pos = mul(ms_pos, g_object_to_world);
			float vis = LightVisibility(0, ws_pos);
			(void)vis;
		}
	}

	#undef uniform
	#undef in
	#undef out
}
#endif
	
// Generate a shadow map from each shadow casting light source
void pr::rdr::Viewport::GenerateShadowMap()
{
	using namespace effect::frag;

	LightingManager& ltmgr = Rdr().m_light_mgr;

	// Find the first 'MaxShadowCasters' lights that cast shadows
	// We generate the shadow maps for each light that casts shadows because
	// we don't know how many casters the effects will use.
	pr::pod_array<int, MaxShadowCasters> casting_lights;
	for (int i = 0; i != MaxLights && !casting_lights.full(); ++i)
		if (ltmgr.m_light[i].m_cast_shadows) casting_lights.push_back(i);
	
	// No shadow casters means nothing to do
	if (casting_lights.size() == 0)
	{
		ltmgr.ReleaseSmaps(0);
		return;
	}

	// Save the current render target and depth buffer
	D3DPtr<IDirect3DSurface9> main_rt; Rdr().D3DDevice()->GetRenderTarget(0, &main_rt.m_ptr);
	D3DPtr<IDirect3DSurface9> main_db; Rdr().D3DDevice()->GetDepthStencilSurface(&main_db.m_ptr);

	// Get the shadow map generation effect and push the effect renderstates
	EffectPtr effect = Rdr().m_mat_mgr.GetShadowCastEffect();
	SMap const* smap = Find<effect::frag::SMap>(effect->Frags());
	rs::stack_frame::RSB effrs_sf(Rdr().m_rdrstate_mgr, effect->m_rsb);
	
	// Get the lighting frustum
	pr::Frustum f = ShadowFrustum();

	#if PR_DBG_SMAP
	std::string smap_scene, smap_output_fwd, smap_output_bwd;
	bool dump_ldr = (GetAsyncKeyState('R') & 0x8000) != 0;
	if (dump_ldr)
	{
		pr::ldr::Frustum("view_volume", 0xFFFF0000, f, CameraToWorld()*pr::Translation(0,0,-f.ZDist()), smap_scene);
		pr::ldr::Box("Smap", 0xFFFFFF00, pr::v4::make(0,0,0.5f,1), pr::v4::make(2,2,1,0), smap_output_fwd);
		pr::ldr::Box("Smap", 0xFFFFFF00, pr::v4::make(0,0,0.5f,1), pr::v4::make(2,2,1,0), smap_output_bwd);
	}
	#endif

	// For each shadow casting light, render a smap
	for (int const* lt = casting_lights.begin(), *ltend = casting_lights.end(); lt != ltend; ++lt)
	{
		Light const& light = ltmgr.m_light[*lt];
		int idx = static_cast<int>(lt - casting_lights.begin());

		#if PR_DBG_SMAP
		if (dump_ldr)
		{
			if (light.m_type == ELight::Directional) pr::ldr::BoxLine("light", 0xFFFFFFFF, CameraToWorld().pos, CameraToWorld().pos + 5.0f*light.m_direction, 0.2f, smap_scene);
			else                                     pr::ldr::Sphere ("light", 0xFFFFFFFF, light.m_position, 0.2f, smap_scene);
		}
		#endif
	
		// Ensure the shadow map exists
		ltmgr.CreateSmap(idx);

		// Set the render target and depth buffer
		D3DPtr<IDirect3DSurface9> surf;
		ltmgr.m_smap[idx]->GetSurfaceLevel(0, &surf.m_ptr);
		Rdr().D3DDevice()->SetRenderTarget(0, surf.m_ptr);
		Rdr().D3DDevice()->SetDepthStencilSurface(ltmgr.m_smap_depth.m_ptr); // depth surface must be set after the RT

		// Clear the colour target
		Verify(Rdr().D3DDevice()->Clear(0L, 0, D3DCLEAR_TARGET, 0, 1.0f, 0L));
		PR_EXPAND(PR_DBG_SMAP, dbg_smap::Clear());

		// Render the smap
		uint num_passes;
		Verify(effect->m_effect->Begin(&num_passes, 0));
		for (uint pass = 0; pass != num_passes; ++pass)
		{
			// Set the global parameters
			if (!SMap::SetSceneParameters(smap, effect->m_effect, pass, f, CameraToWorld(), light)) continue;
			PR_EXPAND(PR_DBG_SMAP, if (dump_ldr && !dbg_smap::SetSceneParameters(smap, effect->m_effect, pass, f, CameraToWorld(), light)) continue);

			// Clear the depth target
			Verify(Rdr().D3DDevice()->Clear(0L, 0, D3DCLEAR_ZBUFFER|D3DCLEAR_STENCIL, 0, 1.0f, 0L));

			// Render the drawlist into the smap
			Verify(effect->m_effect->BeginPass(pass));
			for (TDrawList::const_iterator e = m_drawlist.Begin(), eend = m_drawlist.End(); e != eend; ++e)
			{
				// Set the state of the renderer ready for this element
				rs::stack_frame::DLEShadows dle_sf(Rdr().m_rdrstate_mgr, *e);
				pr::m4x4 i2w = instance::GetI2W(*e->m_instance);

				// Set effect properties specific to this draw list element
				SMap::SetObjectToWorld(smap, effect->m_effect, i2w);
				Verify(effect->m_effect->CommitChanges());
				PR_EXPAND(PR_DBG_SMAP, if (dump_ldr) dbg_smap::SetObjectToWorld(smap, effect->m_effect, i2w));

				RenderDrawListElement(*e);
				PR_EXPAND(PR_DBG_SMAP, if (dump_ldr) dbg_smap::SmapEffect(*e, pass, smap_scene, (pass<4?smap_output_fwd:smap_output_bwd)));
			}
			effect->m_effect->EndPass();
		}
		effect->m_effect->End();
	}

	// Restore the main render target and depth buffer
	Rdr().D3DDevice()->SetRenderTarget(0, main_rt.m_ptr);
	if (main_db) Rdr().D3DDevice()->SetDepthStencilSurface(main_db.m_ptr);

	#if PR_DBG_SMAP
	if (dump_ldr)
	{
		pr::ldr::Write(smap_scene, "d:/deleteme/smap_scene.ldr");
		pr::ldr::Write(smap_output_fwd, "d:/deleteme/smap_output_fwd.ldr");
		pr::ldr::Write(smap_output_bwd, "d:/deleteme/smap_output_bwd.ldr");
		
		for (TDrawList::const_iterator e = m_drawlist.Begin(), eend = m_drawlist.End(); e != eend; ++e)
			dbg_smap::LightingEffect(*e);
	}
	#endif
}
