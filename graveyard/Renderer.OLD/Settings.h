//*******************************************************************************
//
//	The settings for the renderer
//
//*******************************************************************************
#ifndef PR_RDR_SETTINGS_H
#define PR_RDR_SETTINGS_H

#include "PR/Geometry/PRColour.h"
#include "PR/Renderer/D3DHeaders.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/Configure.h"
#include "PR/Renderer/TextureFilter.h"

namespace pr
{
	namespace rdr
	{
		struct RdrSettings
		{
			enum Quality { Low, Medium, High };
			RdrSettings(HWND window_handle, DeviceConfig device_config, uint screen_width, uint screen_height)
			:m_window_handle			(window_handle)
			,m_device_config            (device_config)
			,m_screen_width				(screen_width)
			,m_screen_height			(screen_height)
			,m_zbuffer_format			(D3DFMT_D24S8)
			,m_swap_effect				(D3DSWAPEFFECT_DISCARD)
			,m_back_buffer_count		(1)
			,m_geometry_quality			(Low)
			,m_texture_quality			(Low)
			,m_background_colour		(Colour32::construct(0,0,0,0))
			,m_client_area				(IRect::construct(0, 0, m_screen_width, m_screen_height))
			,m_window_bounds			(IRect::construct(0, 0, m_screen_width, m_screen_height))
			,m_texture_filter           ()
			,m_shader_paths				()
			{}

			HWND			m_window_handle;
			DeviceConfig	m_device_config;
			uint			m_screen_width;
			uint			m_screen_height;
			D3DFORMAT		m_zbuffer_format;
			D3DSWAPEFFECT	m_swap_effect;
			uint			m_back_buffer_count;
			uint			m_geometry_quality;		// Use the Settings::Quality enum
			uint			m_texture_quality;		// Use the Settings::Quality enum
			Colour32		m_background_colour;
			IRect			m_client_area;
			IRect			m_window_bounds;
			TextureFilter	m_texture_filter;		// Texture filters for Mag, Mip, Min
			rdr::TPathList	m_shader_paths;
		};

		//************************************************************************************

		struct VPSettings
		{
			VPSettings(Renderer* renderer)
			:m_renderer				(renderer)
			,m_righthanded			(true)
			,m_field_of_view		(maths::pi / 4.0f)
			,m_near_clipping_plane	(0.1f)
			,m_far_clipping_plane	(1000.0f)
			,m_viewport_rect		(FRectUnit)
			,m_world_to_camera		(m4x4Identity)
			{
				UpdateProjectionMatrix();
			}
			void UpdateProjectionMatrix()
			{
				float aspect = m_viewport_rect.Width() / m_viewport_rect.Height();
				ProjectionPerspectiveFOV(m_camera_to_screen, m_field_of_view, aspect, m_near_clipping_plane, m_far_clipping_plane, m_righthanded);
			}

			Renderer*		m_renderer;
			bool			m_righthanded;					// True for righthanded, False for lefthanded
			float			m_field_of_view;
			float			m_near_clipping_plane;
			float			m_far_clipping_plane;
			FRect			m_viewport_rect;
			m4x4			m_world_to_camera;
			m4x4			m_camera_to_screen;
		};

	}//namespace rdr
}//namespace pr

#endif//PR_RDR_SETTINGS_H
