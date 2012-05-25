//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_SETTINGS_H
#define PR_RDR_SETTINGS_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/configure.h"
#include "pr/renderer/materials/textures/texturefilter.h"

namespace pr
{
	namespace rdr
	{
		struct RdrSettings
		{
			HWND                 m_window_handle;       // The window that the device will be associated with. One per renderer
			DeviceConfig         m_device_config;       // How to set up the device
			IAllocator*          m_allocator;           // An interface that handles our project specific the memory requirements
			IRect                m_client_area;         // The dimensions of the back buffer
			D3DFORMAT            m_zbuffer_format;      // Depth and stencil buffer format
			D3DSWAPEFFECT        m_swap_effect;         // Use 'D3DSWAPEFFECT_COPY' if you want to present the back buffer more than once
			uint                 m_present_flags;       // A combination of D3DPRESENTFLAG's (e.g. D3DPRESENTFLAG_LOCKABLE_BACKBUFFER)
			uint                 m_back_buffer_count;   // Length of the back buffer chain
			EQuality::Type       m_geometry_quality;    // Use the rdr::EQuality enum
			EQuality::Type       m_texture_quality;     // Use the rdr::EQuality enum
			Colour32             m_background_colour;   // The clear screen colour
			TextureFilter        m_texture_filter;      // Texture filters for Mag, Mip, Min
			EShaderVersion::Type m_max_shader_version;  // The maximum shader version to use
			
			RdrSettings()
			:m_window_handle      (0)                           // required
			,m_device_config      ()                            // required
			,m_allocator          (0)                           // required
			,m_client_area        (IRect::make(0, 0, 640, 480)) // required
			,m_zbuffer_format     (D3DFMT_D24S8)
			,m_swap_effect        (D3DSWAPEFFECT_DISCARD)
			,m_present_flags      (0)
			,m_back_buffer_count  (1)
			,m_geometry_quality   (EQuality::High)
			,m_texture_quality    (EQuality::High)
			,m_background_colour  (Colour32::make(0,0,0,0))
			,m_texture_filter     ()
			,m_max_shader_version (EShaderVersion::v3_0)
			{}
		};

		//************************************************************************************

		struct VPSettings
		{
			Renderer*  m_renderer;            // The renderer that will own this viewport
			ViewportId m_identifier;          // Used to distingush between viewports when instances are added
			bool       m_orthographic;        // True for orthographic projection
			float      m_fovY;                // FOV
			float      m_aspect;              // Aspect ratio = width / height
			float      m_centre_dist;         // Distance to the centre of the frustum
			FRect      m_view_rect;           // Normalised sub-area of the client area that this viewport occupies
			m4x4       m_camera_to_world;     // InvView transform
			m4x4       m_camera_to_screen;    // Projection transform

			VPSettings()
			:m_renderer             (0)       // required
			,m_identifier           (0)       // required
			,m_orthographic         (false)
			,m_fovY                 (maths::tau_by_8)
			,m_aspect               (1.0f)
			,m_centre_dist          (1.0f)
			,m_view_rect            (FRectUnit)
			,m_camera_to_world      (m4x4Identity)
			,m_camera_to_screen     ()
			{
				UpdateCameraToScreen();
			}
			void UpdateCameraToScreen()
			{
				// Note: the aspect ratio is independent of 'm_view_rect' allowing the view to be stretched
				float height = 2.0f * m_centre_dist * pr::Tan(m_fovY * 0.5f);
				if (m_orthographic) ProjectionOrthographic  (m_camera_to_screen ,height*m_aspect ,height   ,NearPlane() ,FarPlane() ,true);
				else                ProjectionPerspectiveFOV(m_camera_to_screen ,m_fovY          ,m_aspect ,NearPlane() ,FarPlane() ,true);
			}
			float NearPlane() const { return m_centre_dist * 0.01f; }
			float FarPlane() const  { return m_centre_dist * 100.0f; }
		};
	}
}

#endif
