//**********************************************************************************
//
// Colour conversion functions
//
//**********************************************************************************
#ifndef D3DCOLOUR_H
#define D3DCOLOUR_H

#include "PR/Common/PRAssert.h"
#pragma error(PR_LINK "Don't include")
//
//#include <DXSDK/Include/d3d9types.h>
//
//inline D3DCOLORVALUE D3DColourValue(float r, float g, float b, float a)
//{
//	D3DCOLORVALUE c;
//	c.r = r;
//	c.g = g;
//	c.b = b;
//	c.a = a;
//	return c;
//}
//inline D3DCOLORVALUE D3DColourValue(D3DCOLOR aarrggbb)
//{
//	D3DCOLORVALUE c;
//	c.a = ((aarrggbb >> 24) & 0xFF) / 255.0f;
//	c.r = ((aarrggbb >> 16) & 0xFF) / 255.0f;
//	c.g = ((aarrggbb >>  8) & 0xFF) / 255.0f;
//	c.b = ((aarrggbb >>  0) & 0xFF) / 255.0f;
//	return c;
//}
//inline D3DCOLOR D3DColour(D3DCOLORVALUE c)						{ return D3DCOLOR_COLORVALUE(c.r, c.g, c.b, c.a); }
//inline D3DCOLOR D3DColour(float r, float g, float b, float a)	{ return D3DCOLOR_COLORVALUE(r, g, b, a); }
//inline bool D3DHasAlpha(D3DCOLOR c)								{ return (c & 0xFF000000) != 0xFF000000; }
//inline bool D3DHasAlpha(D3DCOLORVALUE c)						{ return c.a != 1.0f; }
//inline void SetColourValue(D3DCOLORVALUE& colour, float r, float g, float b, float a)
//{
//	colour.r = r;
//	colour.g = g;
//	colour.b = b;
//	colour.a = a;
//}
//#ifdef _WINGDI_
//inline COLORREF D3DCOLORtoCOLORREF(D3DCOLOR colour)				{ D3DCOLORVALUE c = D3DColourValue(colour); return RGB(c.r, c.g, c.b); }
//inline D3DCOLOR COLORREFtoD3DCOLOR(COLORREF colour)				{ return D3DCOLOR_XRGB(GetRValue(colour), GetGValue(colour), GetBValue(colour)); }
//#endif//_WINGDI_

#endif//D3DCOLOUR_H
