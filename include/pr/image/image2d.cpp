//********************************************************
//
//	Image2D
//
//********************************************************
#include "pr/Image/Image.h"
#include "pr/Image/Image2D.h"
#include "pr/Image/Image2DIter.h"
#include "pr/Image/ImageAssertEnable.h"
#include <d3dx9.h>
#include <d3dx9tex.h>

using namespace pr;
using namespace pr::image;

//*****
// Get an iterator into the image
Image2D::iterator Image2D::Lock(image::Lock& lock, uint mip_level, const IRect* area, uint flags)
{
	PR_ASSERT(PR_DBG_IMAGE, !lock.m_image, "An empty lock must be provided");

	// Get the area of the image that is to be locked
	RECT img_rect = {0, 0, m_info.Width >> mip_level, m_info.Height >> mip_level};
	if( area )
	{
		img_rect.left   = area->m_min.x;
		img_rect.top    = area->m_min.y;
		img_rect.right  = area->m_max.x;
		img_rect.bottom = area->m_max.y;
	}

	// Lock the image surface
	D3DLOCKED_RECT lock_rect; 
	if( Failed(m_image->LockRect(mip_level, &lock_rect, &img_rect, flags)) )
	{
		return Image2D::iterator();
	}

	// Save the lock data
	lock.m_image		= m_image;
	lock.m_mip_level	= mip_level;

	// Return a iterator to the surface data
	return Image2D::iterator(
		static_cast<uint8*>(lock_rect.pBits),
		img_rect.left,
		img_rect.top,
		img_rect.right - img_rect.left,
		img_rect.bottom - img_rect.top,
		lock_rect.Pitch,
		lock_rect.Pitch / (img_rect.right - img_rect.left)
		);
}
