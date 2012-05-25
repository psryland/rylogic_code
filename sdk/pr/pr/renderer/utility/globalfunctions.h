//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_GLOBAL_FUNCTIONS_H
#define PR_RDR_GLOBAL_FUNCTIONS_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/models/types.h"

namespace pr
{
	namespace rdr
	{
		// Check that required dlls to run the renderer are available
		// Throws pr::Exception if not
		void CheckDependencies();
		
		// Return the monitor associated with the device
		HMONITOR GetMonitor(D3DPtr<IDirect3DDevice9> const& d3d_device);

		// Convert a filename into an id. 'filename' is converted to lower case before creating an id
		inline RdrId GetId(const char* unique_name) { return pr::hash::HashLwr(unique_name); }
		
		// Convert a pointer to an id.
		template <typename T> RdrId GetId(T const* ptr) { std::ptrdiff_t n = ptr - (T*)0; return static_cast<RdrId>(n); }
		
		// Get the Vrange from an Irange in an index buffer
		model::Range GetVRange(const model::Range& i_range, const rdr::Index* ibuffer);
		
		// Return the number of bits per pixel for a given format
		uint BytesPerPixel(D3DFORMAT format);
		
		// Return a multisampling level based on a quality and the capabilities of the hardware
		D3DMULTISAMPLE_TYPE GetAntiAliasingLevel(D3DPtr<IDirect3D9> d3d, DeviceConfig const& config, D3DFORMAT format, EQuality::Type quality);
		
		// Configure a texture filter to a particular quality level based on the capabilities of the hardware
		void SetTextureFilter(TextureFilter& filter, const D3DCAPS9& caps, EQuality::Type quality);
		
		// Set the render states in 'rsb' suitable for alpha blending
		void SetAlphaRenderStates(rs::Block& rsb, bool on, uint blend_op = D3DBLENDOP_ADD, uint src = D3DBLEND_SRCALPHA, uint dest = D3DBLEND_ONE);
		
		// Create a material from a 'pr::Material'
		pr::rdr::Material LoadMaterial(Renderer& rdr, pr::Material const& material, pr::GeomType geom_type);
		
		// Create a model from a 'pr::Mesh'
		pr::rdr::ModelPtr LoadMesh(Renderer& rdr, pr::Mesh const& mesh);
		
		// Convert a surface from one format to another
		// example 'conv':
		//struct R32FtoA8R8G8B8
		//{
		//	typedef float        Src;
		//	typedef pr::Colour32 Dst;
		//	void operator()(Src src, Dst& dst) { dst.set(pr::Clamp(src/1000.0f, 0.0f, 1.0f), 0.f, 0.f, 0.f); }
		//};
		template <typename Conv> void ConvertSurface(D3DPtr<IDirect3DSurface9> src, D3DPtr<IDirect3DSurface9> dst, Conv conv)
		{
			typedef unsigned char byte;
			D3DLOCKED_RECT sbuf; src->LockRect(&sbuf,0,D3DLOCK_READONLY);
			D3DLOCKED_RECT dbuf; dst->LockRect(&dbuf,0,0);
			byte const* sbits = static_cast<byte const*>(sbuf.pBits);
			byte*       dbits = static_cast<byte*>      (dbuf.pBits);

			D3DSURFACE_DESC sdesc; src->GetDesc(&sdesc);
			D3DSURFACE_DESC ddesc; dst->GetDesc(&ddesc);
			for (int y = 0, yend = pr::Minimum(sdesc.Height, ddesc.Height); y != yend; ++y)
			{
				Conv::Src const* sptr = static_cast<Conv::Src const*>(sbits + y*sbuf.Pitch);
				Conv::Dst*       dptr = static_cast<Conv::Dst*>      (dbits + y*dbuf.Pitch);
				for (int x = 0, xend = pr::Minimum(sdesc.Width, ddesc.Width); x != xend; ++x, ++sptr, ++dptr)
					conv(*sptr, *dptr);
			}
			
			src->UnlockRect();
			dst->UnlockRect();
		}
	}
}

#endif
