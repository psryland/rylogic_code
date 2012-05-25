//***************************************************************************
//
//	A default effect file
//
//***************************************************************************
#include "Stdafx.h"
#include "Renderer/Effects/EffectDefault.h"
#include "Renderer/Renderer.h"

using namespace Effect;

//*****
// Constructor
Default::Default()
:Base()
,m_fragment_linker(0)
,m_fragment_buffer(0)
,m_fragment_compile_errors(0)

,m_frag_projectP(0)
,m_frag_projectN(0)
,m_frag_projectC(0)
,m_frag_projectT(0)
,m_frag_light_start(0)
,m_frag_ambient(0)
,m_frag_diffuse(0)
,m_frag_point(0)
,m_frag_spot(0)
,m_frag_directional(0)

,m_lighting(Light::Ambient)
,m_vertex_fvf(FVF_XYZ)

,m_vertex_shader(0)
,m_pixel_shader(0)
,m_world_view_proj(0)
,m_instance_to_world(0)
,m_light_ambient(0)
,m_light_diffuse(0)
,m_light_position(0)
,m_light_direction(0)
,m_material_ambient(0)
,m_material_diffuse(0)
{}

//*****
// Constructor
Default::Default(const char* effect_name)
:Base(effect_name)
,m_fragment_linker(0)
,m_fragment_buffer(0)
,m_fragment_compile_errors(0)

,m_frag_projectP(0)
,m_frag_projectN(0)
,m_frag_projectC(0)
,m_frag_projectT(0)
,m_frag_light_start(0)
,m_frag_ambient(0)
,m_frag_diffuse(0)
,m_frag_point(0)
,m_frag_spot(0)
,m_frag_directional(0)

,m_lighting(Light::Ambient)
,m_vertex_fvf(FVF_XYZ)

,m_vertex_shader(0)
,m_pixel_shader(0)
,m_world_view_proj(0)
,m_instance_to_world(0)
,m_light_ambient(0)
,m_light_diffuse(0)
,m_light_position(0)
,m_light_direction(0)
,m_material_ambient(0)
,m_material_diffuse(0)

{
	SetName("DefaultEffect");
}

//*****
// Create the default effect.
bool Default::Create(Renderer* renderer, ID3DXEffectPool* effect_pool, const char*)
{
	PR_ASSERT_STR(m_renderer == 0, "Call Release first");

	// Save the renderer pointer
	m_renderer = renderer;

	// Load the effect
	if( Failed(D3DXCreateEffectFromFile(
			m_renderer->GetD3DDevice(),						// D3D Device
			"P:/Renderer/Effects/DefaultEffect.fx",			// Source data
			0,											// Macro definitions
			0,											// Include interface
			m_shader_flags,									// Shader flags
			effect_pool,									// EffectDefault pool
			&m_effect,										// The effect
			&m_compile_errors)) )							// Any compile errors
	{
		if( m_compile_errors )
		{
			const char* str = (const char*)m_compile_errors->GetBufferPointer();
			PR_WARN(Fmt("Reason: %s", str));
		}
		PR_ERROR_STR("Failed to compile the default effect file. See output window");
		Release();
		return false;
	}

	// Get the techniques that will work on this device
	if( !GetValidTechniques() ) { PR_WARN(Fmt("Failed	to get a valid technique from this effect: %s\n", m_name.c_str())); Release(); return false; }

	// Get handles to the effect parameters
	if( !GetParameterHandles() ) { PR_WARN(Fmt("Error while getting handles to effect parameters: %s\n", m_name.c_str()));  Release(); return false; }

	// Create the fragment linker
	if( Failed(D3DXCreateFragmentLinker(m_renderer->GetD3DDevice(), 0, &m_fragment_linker)) )
	{
		PR_ERROR_STR("Failed to create a fragment linker interface");
		Release();
		return false;
	}

	// Load in the shader fragments.
	if( Failed(D3DXGatherFragmentsFromFile(
		"P:/Renderer/Effects/DefaultEffect.fx",				// Path to the fragments
		0,												// Macro defines
		0,												// Include interface
		m_shader_flags,										// Shader flags
		&m_fragment_buffer,									// A buffer to hold the fragments
		&m_fragment_compile_errors)) )						// Any compile errors
	{
		if( m_fragment_compile_errors )
		{
			const char* str = (const char*)m_fragment_compile_errors->GetBufferPointer();
			PR_WARN(Fmt("Reason: %s", str));
		}
		PR_ERROR_STR("Failed to compile the shader fragments. See output window");
		Release();
		return false;
	}

	// Add the fragments to the linker
	if( Failed(m_fragment_linker->AddFragments((DWORD*)m_fragment_buffer->GetBufferPointer())) )
	{
		PR_ERROR_STR("Failed to add fragments to the linker");
		Release();
		return false;
	}

	// Get handles to the fragments
	m_frag_projectP		= m_fragment_linker->GetFragmentHandleByName("Frag_ProjectionP"	);	if( !m_frag_projectP	) return false;
	m_frag_projectN		= m_fragment_linker->GetFragmentHandleByName("Frag_ProjectionN"	);	if( !m_frag_projectN	) return false;
	m_frag_projectC		= m_fragment_linker->GetFragmentHandleByName("Frag_ProjectionC"	);	if( !m_frag_projectC	) return false;
	m_frag_projectT		= m_fragment_linker->GetFragmentHandleByName("Frag_ProjectionT"	);	if( !m_frag_projectT	) return false;
	m_frag_light_start	= m_fragment_linker->GetFragmentHandleByName("Frag_LightStart"	);	if( !m_frag_light_start	) return false;
	m_frag_ambient		= m_fragment_linker->GetFragmentHandleByName("Frag_Ambient"		);	if( !m_frag_ambient		) return false;
	m_frag_diffuse		= m_fragment_linker->GetFragmentHandleByName("Frag_Diffuse"		);	if( !m_frag_diffuse		) return false;
	m_frag_point		= m_fragment_linker->GetFragmentHandleByName("Frag_Point"		);	if( !m_frag_point		) return false;
	m_frag_spot			= m_fragment_linker->GetFragmentHandleByName("Frag_Spot"		);	if( !m_frag_spot		) return false;
	m_frag_directional	= m_fragment_linker->GetFragmentHandleByName("Frag_Directional"	);	if( !m_frag_directional	) return false;

	// Link a shader and set it in the effect
	BuildShader();
	return true;
}

//*****
// Release()
void Default::Release()
{
	D3DRelease(m_fragment_compile_errors, true);
	D3DRelease(m_fragment_buffer, true);
	D3DRelease(m_fragment_linker, false);
	Base::Release();
}

//*****
// Get the parameters from this technique
bool Default::GetParameterHandles()
{
	m_vertex_shader					= m_effect->GetParameterByName(0, "g_VertexShader"			);	if( !m_vertex_shader			) return false;
	m_pixel_shader					= m_effect->GetParameterByName(0, "g_PixelShader"			);	if( !m_pixel_shader				) return false;
	m_world_view_proj				= m_effect->GetParameterByName(0, "g_WorldViewProj"			);	if( !m_world_view_proj			) return false;
	m_instance_to_world				= m_effect->GetParameterByName(0, "g_World"					);	if( !m_instance_to_world		) return false;
	m_light_ambient					= m_effect->GetParameterByName(0, "g_LightAmbient"			);	if( !m_light_ambient			) return false;
	m_light_diffuse					= m_effect->GetParameterByName(0, "g_LightDiffuse"			);	if( !m_light_diffuse			) return false;
	m_light_position				= m_effect->GetParameterByName(0, "g_LightPosition"			);	if( !m_light_position			) return false;
	m_light_direction				= m_effect->GetParameterByName(0, "g_LightDirection"			);	if( !m_light_direction			) return false;
	m_material_ambient				= m_effect->GetParameterByName(0, "g_MaterialAmbient"		);	if( !m_material_ambient			) return false;
	m_material_diffuse				= m_effect->GetParameterByName(0, "g_MaterialDiffuse"		);	if( !m_material_diffuse			) return false;
	return true;
}

//*****
// Set the parameters for this Effect.
void Default::PrePass(DrawListElement* draw_list_element)
{
	if( IsNewShaderNeeded(draw_list_element) )
		BuildShader();

	// Start setting parameters
	Verify(m_effect->BeginParameterBlock());

	// Set transforms
	const RenderStateManager::RendererState& state = m_renderer->GetCurrentState();
	const m4x4* proj = draw_list_element->m_instance->GetProjectionTransform();
	if( !proj ) proj = &state.m_proj_transform;
	
	m_current_instance_to_world	= draw_list_element->m_instance->GetInstanceToWorld();
	m_current_world_view_proj	= m_current_instance_to_world * state.m_view_transform * *proj;
	Verify(m_effect->SetMatrix		(m_world_view_proj,			&m_current_world_view_proj.m));
	Verify(m_effect->SetMatrix		(m_instance_to_world,		&m_current_instance_to_world.m));

	// Set lighting properties
	const Light& light = m_renderer->GetLight(0);
	m_current_light_ambient		= m_renderer->GetAmbient();
	m_current_light_diffuse		= light.m_diffuse;
	m_current_light_position	= light.m_position;
	m_current_light_direction	= light.m_direction;
	Verify(m_effect->SetFloatArray	(m_light_ambient,			(float*)&m_current_light_ambient,				4));
	Verify(m_effect->SetFloatArray	(m_light_diffuse,			(float*)&m_current_light_diffuse,				4));
	Verify(m_effect->SetFloatArray	(m_light_position,			(float*)&m_current_light_position.v,			3));
	Verify(m_effect->SetFloatArray	(m_light_direction,			(float*)&m_current_light_direction.v,			3));

	// Set material properties
	m_current_mat_index = draw_list_element->GetMaterialIndex();
	const D3DMATERIAL9* mat = m_renderer->GetMaterial(m_current_mat_index);
	Verify(m_effect->SetFloatArray	(m_material_ambient,		(float*)&mat->Ambient,							4));
	Verify(m_effect->SetFloatArray	(m_material_diffuse,		(float*)&mat->Diffuse,							4));

	// Done setting parameters
	D3DXHANDLE param = m_effect->EndParameterBlock();
	Verify(m_effect->ApplyParameterBlock(param));

	// Test to se
//PSR...	PR_ASSERT(m_effect);
//PSR...
//PSR...	// Get data needed to set the effect parameters
//PSR...	const RenderStateManager::RendererState&	rs			= m_renderer->GetCurrentState();
//PSR...	const D3DMATERIAL9*							mat			= m_renderer->GetMaterial(rs.m_material_index);
//PSR...	const Light&								light		= m_renderer->GetLight(0);
//PSR...	const D3DLIGHT9*							d3dlight	= light.GetD3DLight();
//PSR...
//PSR...	Verify(m_effect->SetInt				(m_ambient,				D3DColour(d3dlight->Ambient)));
//PSR...	Verify(m_effect->SetFloatArray		(m_material_ambient,	(float*)&mat->Ambient,		4));
//PSR...	Verify(m_effect->SetFloatArray		(m_material_diffuse,	(float*)&mat->Diffuse,		4));
//PSR...	Verify(m_effect->SetFloatArray		(m_material_specular,	(float*)&mat->Specular,		4));
//PSR...	Verify(m_effect->SetFloat			(m_material_power,		mat->Power));
//PSR...	Verify(m_effect->SetBool			(m_light_on,			light.GetState() == Light::On));
//PSR...	if( d3dlight->Type != Light::Ambient )
//PSR...	{
//PSR...		Verify(m_effect->SetInt			(m_light_type,			d3dlight->Type));
//PSR...		Verify(m_effect->SetBool		(m_use_specular,		true));
//PSR...		Verify(m_effect->SetFloatArray	(m_light_ambient,		(float*)&d3dlight->Ambient,		4));
//PSR...		Verify(m_effect->SetFloatArray	(m_light_diffuse,		(float*)&d3dlight->Diffuse,		4));
//PSR...		Verify(m_effect->SetFloatArray	(m_light_specular,		(float*)&d3dlight->Specular,	4));
//PSR...		Verify(m_effect->SetFloatArray	(m_light_position,		(float*)&d3dlight->Position,	3));
//PSR...		Verify(m_effect->SetFloatArray	(m_light_direction,		(float*)&d3dlight->Direction,	3));
//PSR...		Verify(m_effect->SetFloat		(m_light_range,			d3dlight->Range));
//PSR...		Verify(m_effect->SetFloat		(m_light_attenuation0,	d3dlight->Attenuation0));
//PSR...		Verify(m_effect->SetFloat		(m_light_attenuation1,	d3dlight->Attenuation1));
//PSR...		Verify(m_effect->SetFloat		(m_light_attenuation2,	d3dlight->Attenuation2));
//PSR...	}
//PSR...
//PSR...	m_last_material						= rs.m_material_index;
//PSR...	IDirect3DTexture9* texture			= m_renderer->GetTexture(rs.m_material_index);
//PSR...	Verify(m_effect->SetTexture			(m_texture,				texture));
//PSR...
//PSR...
//PSR...	PR_ASSERT(m_technique.GetCount() == 2);
//PSR...	if( d3dlight->Type == Light::Ambient )		Verify(m_effect->SetTechnique(m_technique[0]));
//PSR...	else										Verify(m_effect->SetTechnique(m_technique[1]));
}

//*****
// Set parameters for an instance midway through a pass
void Default::MidPass(DrawListElement* draw_list_element)
{
	if( IsNewShaderNeeded(draw_list_element) )
		BuildShader();
//PSR...
//PSR...	bool somethings_changed = false;
//PSR...
//PSR...	// Set transforms
//PSR...	m4x4 new_view_proj;
//PSR...	const RenderStateManager::RendererState& state = m_renderer->GetCurrentState();
//PSR...	if( draw_list_element->m_instance->GetProjectionTransform() )	new_view_proj = state.m_view_transform * *draw_list_element->m_instance->GetProjectionTransform();
//PSR...	else															new_view_proj = state.m_view_transform * state.m_proj_transform;
//PSR...	if( !m_current_view_proj.FastIsEqual(new_view_proj) )
//PSR...	{
//PSR...		somethings_changed = true;
//PSR...		m_current_view_proj = new_view_proj;
//PSR...		Verify(m_effect->SetMatrix		(m_world_view_proj,				&m_current_view_proj.m));
//PSR...	}
//PSR...	const m4x4& new_instance_to_world = draw_list_element->m_instance->GetInstanceToWorld();
//PSR...	if( !m_current_instance_to_world.FastIsEqual(new_instance_to_world) )
//PSR...	{
//PSR...		somethings_changed = true;
//PSR...        m_current_instance_to_world	= new_instance_to_world;
//PSR...		Verify(m_effect->SetMatrix		(m_instance_to_world,		&m_current_instance_to_world.m));
//PSR...	}
//PSR...
//PSR...	// Set lighting properties
//PSR...	D3DCOLORVALUE new_light_ambient = m_renderer->GetAmbient();
//PSR...	if( D3DColour(m_current_light_ambient) != D3DColour(new_light_ambient) )
//PSR...	{
//PSR...		somethings_changed = true;
//PSR...		m_current_light_ambient = new_light_ambient;
//PSR...		Verify(m_effect->SetFloatArray	(m_light_ambient,			(float*)&m_current_light_ambient,				4));
//PSR...	}
//PSR...
//PSR...	// Set material properties
//PSR...	MaterialIndex new_mat_index = draw_list_element->GetMaterialIndex();
//PSR...	if( m_current_mat_index.MatIndex() != new_mat_index.MatIndex() )
//PSR...	{
//PSR...		somethings_changed = true;
//PSR...		m_current_mat_index.SetMatIndex(new_mat_index.MatIndex());
//PSR...		const D3DMATERIAL9* mat = m_renderer->GetMaterial(m_current_mat_index);
//PSR...		Verify(m_effect->SetFloatArray	(m_material_ambient,		(float*)&mat->Ambient,							4));
//PSR...	}
//PSR...	
//PSR...	if( somethings_changed ) Verify(m_effect->CommitChanges());
}

//*****
// Set parameters at the end of a pass
void Default::PostPass()
{}

//*****
// Return the render states used in this effect
void Default::GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const
{
	static const D3DRENDERSTATETYPE RenderStates[] = { D3DRS_AMBIENT, D3DRS_LIGHTING };
	static const uint				NumRenderStates = sizeof(RenderStates) / sizeof(D3DRENDERSTATETYPE);
	rs		= RenderStates;
	num_rs	= NumRenderStates;
}

//*****
// Returns true if we need to compile a new shader
bool Default::IsNewShaderNeeded(DrawListElement* draw_list_element)
{
	FVF vf = draw_list_element->m_instance->GetVertexFVF();
	const Light& light = m_renderer->GetLight(0);

	bool new_shader_needed = false;
	if( vf != m_vertex_fvf )
	{
		new_shader_needed = true;
		m_vertex_fvf = vf;
	}
	if( light.m_state == Light::Off && m_lighting != Light::Ambient )
	{
		new_shader_needed = true;
		m_lighting = Light::Ambient;
	}
	else if( light.m_type != m_lighting )
	{
		new_shader_needed = true;
		m_lighting = light.m_type;
	}
	return new_shader_needed;
}

//*****
// Construct and set the shader in our effect based on the current vertex format and lighting state
void Default::BuildShader()
{
	PR::Array<D3DXHANDLE, true> fragment;

	uint fvfmask = FVFFormat(m_vertex_fvf);
	if( fvfmask & D3DFVF_XYZ )							fragment.Add(m_frag_projectP);
	if( fvfmask & D3DFVF_NORMAL )						fragment.Add(m_frag_projectN);
	if( fvfmask & D3DFVF_DIFFUSE )						fragment.Add(m_frag_projectC);
	if( fvfmask & D3DFVF_TEX0 )							fragment.Add(m_frag_projectT);

	fragment.Add(m_frag_light_start);
	if( fvfmask & D3DFVF_DIFFUSE )						fragment.Add(m_frag_diffuse);
	const Light& light = m_renderer->GetLight(0);
	if( fvfmask & D3DFVF_NORMAL && light.m_state == Light::On )
	{
		switch( light.m_type )
		{
		case Light::Point:								fragment.Add(m_frag_point);
		case Light::Spot:								fragment.Add(m_frag_spot);
		case Light::Directional:						fragment.Add(m_frag_directional);
		};
	}
	fragment.Add(m_frag_ambient);

	// Link the fragments together to form a shader
    IDirect3DVertexShader9* vertex_shader = 0;
	if( Failed(m_fragment_linker->LinkVertexShader("vs_1_1", D3DXSHADER_DEBUG & m_shader_flags, &fragment[0], fragment.GetCount(), &vertex_shader, &m_fragment_compile_errors)) )
	{
		vertex_shader = 0;
		if( m_fragment_compile_errors )
		{
			const char* str = (const char*)m_fragment_compile_errors->GetBufferPointer();
			PR_WARN(Fmt("Reason: %s", str));
		}
		PR_ERROR_STR("Failed to link shader fragments. See output window");
	}

    // Associate this vertex shader with the effect object
    Verify(m_effect->SetVertexShader(m_vertex_shader, vertex_shader));
	D3DRelease(vertex_shader, false);
}
