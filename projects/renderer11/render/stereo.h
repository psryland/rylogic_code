//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// The magic data that the NVidia driver looks for
		struct NvStereoImageHeader
		{
			enum
			{
				NvSig        = 0x4433564e, // NV3D
				NvSwapEyes   = 0x00000001,
				NvScaleToFit = 0x00000002,
			};

			unsigned int dwSignature;
			unsigned int dwWidth;
			unsigned int dwHeight;
			unsigned int dwBPP;
			unsigned int dwFlags;
			unsigned int pad[3];

			static NvStereoImageHeader make(std::size_t width, std::size_t height, std::size_t bbp, bool swap_eyes)
			{
				NvStereoImageHeader x = {};
				x.dwSignature = checked_cast<unsigned int>(NvSig);
				x.dwWidth     = checked_cast<unsigned int>(width);
				x.dwHeight    = checked_cast<unsigned int>(height);
				x.dwBPP       = checked_cast<unsigned int>(bbp);
				x.dwFlags     = checked_cast<unsigned int>(swap_eyes ? NvSwapEyes : 0);
				return x;
			}

			unsigned int offscreen_width() const  { return dwWidth * 2; }
			unsigned int offscreen_height() const { return dwHeight + 1; }
			unsigned int target_width() const     { return dwWidth; }
			unsigned int target_height() const    { return dwHeight; }
			unsigned int pixel_width() const      { return sizeof(*this) * 8 / dwBPP; }
			unsigned int pixel_height() const     { return 1; }
		};

		// A helper for managing the extra resources needed for stereoscopic rendering
		struct Stereo
		{
			NvStereoImageHeader m_nv_magic;       // The magic NVidia data to be added to the render target
			D3DPtr<ID3D11Texture2D> m_mark;       // A staging texture that holds the nvidia magic data ready to be blitted to the rtv
			D3DPtr<ID3D11Texture2D> m_rt_tex;     // The off screen render target used to render the left and right views into
			D3DPtr<ID3D11RenderTargetView> m_rtv; // A render target view of 'm_rt_tex'
			D3DPtr<ID3D11Texture2D> m_ds_tex;     // The off screen depth stencil buffer used to render the left and right views into
			D3DPtr<ID3D11DepthStencilView> m_dsv; // A depth stencil view of 'm_ds_tex'
			float m_eye_separation;               // The eye separation value to use (world space distance)

			Stereo(ID3D11Device* device, Viewport const& viewport, DXGI_FORMAT target_format, bool swap_eyes, float eye_separation);

			// Add the NVidia magic data to the bottom row of the current render target
			void BlitNvMagic(ID3D11DeviceContext* dc) const;

			// Copy the off screen render target to the current render target
			void BlitRTV(ID3D11DeviceContext* dc) const;

			// An RAII object for managing set up/tear down when rendering a stereoscopic scene
			struct RenderScope
			{
				std::shared_ptr<Stereo>        m_stereo;
				ID3D11DeviceContext*           m_dc;
				D3DPtr<ID3D11RenderTargetView> m_rtv;
				D3DPtr<ID3D11DepthStencilView> m_dsv;

				RenderScope(std::shared_ptr<Stereo> stereo, ID3D11DeviceContext* dc)
					:m_stereo(stereo)
					,m_dc(dc)
					,m_rtv()
					,m_dsv()
				{
					// Save the current render target views so we can
					// restore them later and set the off-screen render target
					dc->OMGetRenderTargets(1, &m_rtv.m_ptr, &m_dsv.m_ptr);
					dc->OMSetRenderTargets(1, &m_stereo->m_rtv.m_ptr, nullptr);//m_stereo->m_dsv.m_ptr);
					dc->ClearRenderTargetView(m_rtv.m_ptr, ColourBlack.arr);
					dc->ClearDepthStencilView(m_dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
				}
				~RenderScope()
				{
					m_stereo->BlitNvMagic(m_dc);
					m_dc->OMSetRenderTargets(1, &m_rtv.m_ptr, m_dsv.m_ptr); // Restore the render target
					m_stereo->BlitRTV(m_dc);
				}
			};
		};
	}
}
