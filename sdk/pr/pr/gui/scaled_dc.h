//***********************************************
// Scaled DC
//  Copyright © Rylogic Ltd 2009
//***********************************************
#ifndef PR_GUI_SCALED_DC_H
#define PR_GUI_SCALED_DC_H
#pragma once

#include <windows.h>
#include "pr/gui/misc.h"

inline int Width(RECT const& rect) { return rect.right - rect.left; }
inline int Height(RECT const& rect) { return rect.bottom - rect.top; }

// Helper object for drawing in scaled screen space
struct CScaledDC
{
	HDC m_hdc;
	int m_old_mode;
	int m_old_bk_mode;
	float m_scale;
	
	// Create a DC the scales 'virtual_area' into 'client_area'
	CScaledDC(HDC hdc, RECT const& client_area, RECT const& virtual_area)
	:m_hdc(hdc)
	,m_old_mode(SetGraphicsMode(hdc, GM_ADVANCED))
	,m_old_bk_mode(SetBkMode(hdc, TRANSPARENT))
	,m_scale(100.0f)
	{
		// Set the clip bounds
		HRGN clip_region = CreateRectRgn(client_area.left, client_area.top, client_area.right, client_area.bottom);
		SelectClipRgn(m_hdc, clip_region);
		DeleteObject((HGDIOBJ)clip_region);
		
		// Create a scale and translation transform so that we can draw in image space
		float scale_x = Width(client_area)  / float(Width(virtual_area));
		float scale_y = Height(client_area) / float(Height(virtual_area));
		XFORM i2s = // image to screen
		{
			scale_x / m_scale,  0.0f,
			0.0f,               scale_y / m_scale,
			float(client_area.left - virtual_area.left* scale_x), float(client_area.top - virtual_area.top* scale_y)
		};
		SetWorldTransform(hdc, &i2s);
	}
	
	~CScaledDC()
	{
		XFORM id = { 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f };
		SetWorldTransform(m_hdc, &id);
		SelectClipRgn(m_hdc, 0);
		SetBkMode(m_hdc, m_old_bk_mode);
		SetGraphicsMode(m_hdc, m_old_mode);
	}
	operator HDC() const            { return m_hdc; }
	void MoveTo(float x, float y)   { ::MoveToEx(m_hdc, int(x * m_scale), int(y * m_scale), 0); }
	void LineTo(float x, float y)   { ::LineTo(m_hdc, int(x * m_scale), int(y * m_scale)); }
	void DrawLine(float x0, float y0, float x1, float y1)
	{
		::MoveToEx(m_hdc, int(x0 * m_scale), int(y0 * m_scale), 0);
		::LineTo(m_hdc, int(x1 * m_scale), int(y1 * m_scale));
	}
	void DrawEllipse(float x, float y, float w, float h)
	{
		DCSelect<HBRUSH> br(*this, (HBRUSH)GetStockObject(HOLLOW_BRUSH));
		FillEllipse(x, y, w, h);
	}
	void FillEllipse(float x, float y, float w, float h)
	{
		::Ellipse(m_hdc, int(x * m_scale), int(y * m_scale), int((x + w) * m_scale), int((y + h) * m_scale));
	}
	void DrawString(char const* str, float x, float y)
	{
		RECT rect = {int(x* m_scale), int(y* m_scale), int(x* m_scale), int(y* m_scale)};
		::DrawTextA(m_hdc, str, -1, &rect, DT_LEFT|DT_TOP|DT_CALCRECT);
		::DrawTextA(m_hdc, str, -1, &rect, DT_LEFT|DT_TOP|DT_NOCLIP);
	}
};

#endif
