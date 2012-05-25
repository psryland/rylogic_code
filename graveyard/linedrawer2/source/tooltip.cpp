//**********************************************************************************************
//
// Tooltip a class for drawing tooltips
//
//**********************************************************************************************
#include "stdafx.h"
#include "LineDrawer/Source/ToolTip.h"

//*****
// Constructors
ToolTip::ToolTip()
:m_text("")
,m_position(0,0)
,m_tip_rect(0,0,0,0)
,m_colour(RGB(0,0,0))
,m_bk_colour(RGB(255,255,192))
{}
ToolTip::ToolTip(const char* str, short cx, short cy, COLORREF colour, COLORREF bk_colour)
:m_text(str)
,m_position(cx, cy)
,m_colour(colour)
,m_bk_colour(bk_colour)
{}

//*****
// Draw the tooltip
void ToolTip::Draw(CWnd* parent) 
{
	// Undraw the tip in the old position
	UnDraw(parent);

	CRect rect;
	parent->GetClientRect(rect);

	CPaintDC dc(parent); // device context for painting

	// Create the Pen, Brush and Font for the tip
    CPen pen(PS_SOLID, 1, m_colour);
    CBrush brush(m_bk_colour);
	CFont font;
	font.CreateFont(15, 0, 0, 0, FW_REGULAR, 0, 0, 0, 0, 0, 0, 0, 0, "MS Sans Serif");
    
	// Select them
    CPen *oldPen	= dc.SelectObject(&pen);
    CFont *oldFont	= dc.SelectObject(&font);
    CBrush *oldBsh	= dc.SelectObject(&brush);

	// Find the size of the tooltip text
    CSize string_size = dc.GetTextExtent(m_text.c_str());
	if( m_position.x < rect.Width() / 4 )
	{
		m_tip_rect.left		= m_position.x; 
		m_tip_rect.right	= m_tip_rect.left + string_size.cx;
	}
	else
	{
		m_tip_rect.right	= m_position.x; 
		m_tip_rect.left		= m_tip_rect.right - string_size.cx;
	}
	if( m_position.y < rect.Height() / 4 )
	{
		m_tip_rect.top		= m_position.y + 20;
		m_tip_rect.bottom	= m_tip_rect.top + string_size.cy;
	}
	else
	{
		m_tip_rect.bottom	= m_position.y - 10;
		m_tip_rect.top		= m_tip_rect.bottom - string_size.cy;
	}

	// Draw a background rectangle
	dc.SetBkColor(m_bk_colour);
	dc.SetTextColor(m_colour);
	
    // Now display the tip
	dc.TextOut(m_tip_rect.left, m_tip_rect.top, m_text.c_str()); 

	// Select the old Pen, Font and Brush
	dc.SelectObject(oldBsh);
	dc.SelectObject(oldPen);
	dc.SelectObject(oldFont);
}

