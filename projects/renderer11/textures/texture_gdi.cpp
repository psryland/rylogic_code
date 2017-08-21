//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture_gdi.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/image.h"

namespace pr
{
	namespace rdr
	{
		// Here is a list of things you should know/do to make it all work:
		// Check the surface requirements for a GetDC method to work here: http://msdn.microsoft.com/en-us/library/windows/desktop/ff471345(v=vs.85).aspx
		// - You must create the surface by using the D3D11_RESOURCE_MISC_GDI_COMPATIBLE flag
		//   for a surface or by using the DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE flag for swap chains,
		//   otherwise this method fails.
		// - You must release the device and call the IDXGISurface1::ReleaseDC method before you
		//   issue any new Direct3D commands.
		// - This method fails if an outstanding DC has already been created by this method.
		// - The format for the surface or swap chain must be DXGI_FORMAT_B8G8R8A8_UNORM_SRGB
		//   or DXGI_FORMAT_B8G8R8A8_UNORM.
		// - On GetDC, the render target in the output merger of the Direct3D pipeline is
		//   unbound from the surface. You must call the ID3D11DeviceContext::OMSetRenderTargets
		//   method on the device prior to Direct3D rendering after GDI rendering.
		// - Prior to resizing buffers you must release all outstanding DCs.
		// - If you're going to use it in the back buffer, remember to re-bind render target
		//   after you've called ReleaseDC. It is not necessary to manually unbind RT before
		//   calling GetDC as this method does that for you.
		// - You can not use any Direct3D drawing between GetDC() and ReleaseDC() calls as the
		//   surface is exclusively locked out by DXGI for GDI. However you can mix GDI and D3D
		//   rendering provided that you call GetDC()/ReleaseDC() every time you need to use GDI,
		//   before moving on to D3D.
		//
		// This last bit may sounds easy, but you'd be surprised how many developers fall into
		// this issue
		// - when you draw with GDI on the back buffer, remember that this is the back buffer,
		//   not a frame buffer, so in order to actually see what you've drawn, you have to
		//   re-bind RT to OM and call swapChain->Present() method so the back buffer will
		//   become a frame buffer and its contents will be displayed on the screen.

		TextureGdi::TextureGdi(TextureManager* mgr, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, ShaderResViewDesc const* srvdesc)
			:Texture2D(mgr, src, tdesc, sdesc, sort_id, srvdesc)
		{}

		// Ref-counting clean up function
		void TextureGdi::Delete()
		{
			m_mgr->Delete(this);
		}
	}
}
