//**********************************************************************************************
//
//	Tooltip
//
//**********************************************************************************************
#ifndef TOOLTIP_H
#define TOOLTIP_H

#include "pr/common/StdString.h"

class ToolTip
{
public:
	ToolTip();
	ToolTip(const char* str, short cx, short cy, COLORREF colour, COLORREF bk_colour);
	void Draw  (CWnd* parent);
	void UnDraw(CWnd* parent)	{ parent->InvalidateRect(&m_tip_rect, FALSE); }

	std::string	m_text;			// The text
	CPoint		m_position;		// The screen position
	COLORREF	m_colour;		// The text colour
	COLORREF	m_bk_colour;	// The background colour

private:
	CRect		m_tip_rect;
};

#endif//TOOLTIP_H
