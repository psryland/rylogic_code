//***************************************************************************
//
//	A default effect file
//
//***************************************************************************
// Use case:
//	The default effect should only be created once.
//	During create, the default effect creates a fragment linker, loads all
//		of the shader fragments, and creates a shader from the default vertex_format
//		and current renderer lighting state.
//
//	A model is loaded with any combination of XYZ, Normal, Diffuse, Tex
//	No effect file is given in the texture info file so the default effect is assigned
//
// OnRender:
//	PrePass and MidPass check to see if the vertex format of the draw_list_element or
//		lighting state have changed. If so, the shader cache is checked to see if we
//		already have an appropriate shader. If not, a new shader is linked and added.
//		The current shader is then added to the effect file.
//
#ifndef EFFECT_DEFAULT_H
#define EFFECT_DEFAULT_H

#include "Renderer/Effects/EffectBase.h"
#include "Renderer/Light.h"

namespace Effect
{
	class Default : public Base
	{
	public:
		Default();
		Default(const char* effect_name);
		virtual ~Default(){}	// Do not call Release() here because any temporary copy of an EffectDefault will get destroyed and release the effect.

		// Overrides
		virtual bool	Create(Renderer* renderer, ID3DXEffectPool* effect_pool, const char* filename = 0);
		virtual void	Release();
		virtual void	PrePass(DrawListElement* draw_list_element);
		virtual void	MidPass(DrawListElement* draw_list_element);
		virtual void	PostPass();
		virtual void	GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const;

	protected:
		virtual bool	GetParameterHandles();
		
		// Uses the fragment linker to create a shader from the current vertex format and light type
		bool			IsNewShaderNeeded(DrawListElement* draw_list_element);
		void			BuildShader();

	protected:
		ID3DXFragmentLinker*	m_fragment_linker;
		ID3DXBuffer*			m_fragment_buffer;
		ID3DXBuffer*			m_fragment_compile_errors;
		D3DXHANDLE				m_frag_projectP;
		D3DXHANDLE				m_frag_projectN;
		D3DXHANDLE				m_frag_projectC;
		D3DXHANDLE				m_frag_projectT;
		D3DXHANDLE				m_frag_light_start;
		D3DXHANDLE				m_frag_ambient;
		D3DXHANDLE				m_frag_diffuse;
		D3DXHANDLE				m_frag_point;
		D3DXHANDLE				m_frag_spot;
		D3DXHANDLE				m_frag_directional;
		
		Light::Type				m_lighting;
		FVF						m_vertex_fvf;

		D3DXHANDLE				m_vertex_shader;
		D3DXHANDLE				m_pixel_shader;
		D3DXHANDLE				m_world_view_proj;
		D3DXHANDLE				m_instance_to_world;
		D3DXHANDLE				m_light_ambient;
		D3DXHANDLE				m_light_diffuse;
		D3DXHANDLE				m_light_position;
		D3DXHANDLE				m_light_direction;
		D3DXHANDLE				m_material_ambient;
		D3DXHANDLE				m_material_diffuse;

		// Cached variables
		m4x4					m_current_world_view_proj;
		m4x4					m_current_instance_to_world;
		D3DCOLORVALUE			m_current_light_ambient;
		D3DCOLORVALUE			m_current_light_diffuse;
		v4						m_current_light_position;
		v4						m_current_light_direction;
		MaterialIndex			m_current_mat_index;
//PSR...		D3DXHANDLE		m_ambient;
//PSR...		D3DXHANDLE		m_material_ambient;
//PSR...		D3DXHANDLE		m_material_diffuse;
//PSR...		D3DXHANDLE		m_material_specular;
//PSR...		D3DXHANDLE		m_material_power;
//PSR...		D3DXHANDLE		m_use_specular;
//PSR...		D3DXHANDLE		m_light_on;
//PSR...		D3DXHANDLE		m_light_type;
//PSR...		D3DXHANDLE		m_light_ambient;
//PSR...		D3DXHANDLE		m_light_diffuse;
//PSR...		D3DXHANDLE		m_light_specular;
//PSR...		D3DXHANDLE		m_light_position;
//PSR...		D3DXHANDLE		m_light_direction;
//PSR...		D3DXHANDLE		m_light_range;
//PSR...		D3DXHANDLE		m_light_attenuation0;
//PSR...		D3DXHANDLE		m_light_attenuation1;
//PSR...		D3DXHANDLE		m_light_attenuation2;
//PSR...		D3DXHANDLE		m_texture;
	};

}//namespace Effect

#endif//EFFECT_DEFAULT_H
